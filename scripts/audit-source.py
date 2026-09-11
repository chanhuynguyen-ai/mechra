"""Reproducibility and package checks, not a C# compiler or PowerShell runtime."""
from pathlib import Path
import ast
import re
import json
import subprocess
import sys
import xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parents[1]
version=(ROOT/'VERSION').read_text().strip()
sys.path.insert(0,str(ROOT/'src/AgentService'))
tree=ast.parse((ROOT/'src/AgentService/app/main.py').read_text())
app_version=next(ast.literal_eval(n.value) for n in tree.body if isinstance(n,ast.Assign)
    and any(isinstance(t,ast.Name) and t.id=='VERSION' for t in n.targets))
assert app_version==version,(app_version,version)
from app.agent import BLANK_PART_TYPES
executor=(ROOT/'src/SolidWorksAddin/Services/CadExecutor.cs').read_text()
block=executor.split('var systemTypes =',1)[1].split('};',1)[0]
assert {x.casefold() for x in re.findall(r'"([A-Za-z]+)"',block)}==BLANK_PART_TYPES, 'Planner/executor blank-template types drifted' 
assert version in (ROOT/'src/SolidWorksAddin/Properties/AssemblyInfo.cs').read_text()
assert version in (ROOT/'src/SolidWorksAddin/UI/CopilotPanel.cs').read_text()
csroot=ROOT/'src/SolidWorksAddin'
ns={'m':'http://schemas.microsoft.com/developer/msbuild/2003'}
project=ET.parse(csroot/'SolidWorksAddin.csproj')
includes={str(p.attrib['Include']).replace('\\','/') for p in project.findall('.//m:Compile',ns)}
actual={str(p.relative_to(csroot)) for p in csroot.rglob('*.cs')}
assert actual==includes, {'unlisted':list(actual-includes),'missing':list(includes-actual)}
for p in list((ROOT/'scripts').glob('*.ps1')) + list((ROOT/'tests').glob('*.ps1')):
    raw=p.read_bytes()
    assert raw.startswith(b'\xef\xbb\xbf') or raw.isascii(),f'PowerShell 5.1 encoding: {p}'
    contents=p.read_text(encoding='utf-8-sig')
    suspicious=re.findall(r'\$([A-Za-z_]\w*):',contents)
    assert all(x.lower() in {'env','script','global','local','private','using'} for x in suspicious), f'Use braced interpolation before colon: {p}' 
for p in ROOT.rglob('*.cs'): p.read_text(encoding='utf-8')
paths=list((ROOT/'src/Shared/contracts').glob('*.json'))
before={p:p.read_bytes() for p in paths}
subprocess.run([sys.executable,str(ROOT/'scripts/generate-contracts.py')],check=True)
assert all(p.read_bytes()==raw for p,raw in before.items()),'Generated contracts drifted'
print(f'PASS source audit: version {version}, {len(actual)} C# compile items, PowerShell encodings and {len(paths)} stable schemas.')
print('C# compilation and PowerShell execution remain Windows validation gates.')
