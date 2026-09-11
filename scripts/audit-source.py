"""Reproducibility and package checks, not a C# compiler or PowerShell runtime."""
from pathlib import Path
import json
import subprocess
import sys
import xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parents[1]
version=(ROOT/'VERSION').read_text().strip()
sys.path.insert(0,str(ROOT/'src/AgentService'))
from app.main import VERSION
assert VERSION==version,(VERSION,version)
assert version in (ROOT/'src/SolidWorksAddin/Properties/AssemblyInfo.cs').read_text()
assert version in (ROOT/'src/SolidWorksAddin/UI/CopilotPanel.cs').read_text()
csroot=ROOT/'src/SolidWorksAddin'
ns={'m':'http://schemas.microsoft.com/developer/msbuild/2003'}
project=ET.parse(csroot/'SolidWorksAddin.csproj')
includes={str(p.attrib['Include']).replace('\\','/') for p in project.findall('.//m:Compile',ns)}
actual={str(p.relative_to(csroot)) for p in csroot.rglob('*.cs')}
assert actual==includes, {'unlisted':list(actual-includes),'missing':list(includes-actual)}
for p in (ROOT/'scripts').glob('*.ps1'):
    raw=p.read_bytes()
    assert raw.startswith(b'\xef\xbb\xbf') or raw.isascii(),f'PowerShell 5.1 encoding: {p}'
    p.read_text(encoding='utf-8-sig')
for p in ROOT.rglob('*.cs'): p.read_text(encoding='utf-8')
paths=list((ROOT/'src/Shared/contracts').glob('*.json'))
before={p:p.read_bytes() for p in paths}
subprocess.run([sys.executable,str(ROOT/'scripts/generate-contracts.py')],check=True)
assert all(p.read_bytes()==raw for p,raw in before.items()),'Generated contracts drifted'
print(f'PASS source audit: version {version}, {len(actual)} C# compile items, PowerShell encodings and {len(paths)} stable schemas.')
print('C# compilation and PowerShell execution remain Windows validation gates.')
