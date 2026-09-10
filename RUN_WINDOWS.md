# One-command Windows smoke test

Open **PowerShell as Administrator**, then from the project root run:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\scripts\setup-and-run.ps1
```

The script automatically:

1. finds Python,
2. finds the installed SOLIDWORKS `api\redist` folder,
3. finds Visual Studio/MSBuild,
4. creates the Python virtual environment,
5. installs and starts the local agent on `127.0.0.1:8765`,
6. calls `/health` and `/v1/chat`,
7. builds the x64 .NET Framework 4.8 add-in,
8. registers the COM add-in when PowerShell is elevated.

When it finishes, restart SOLIDWORKS and open **Tools > Add-Ins > Mechra**. Open any part and press **Check model** in the right Task Pane.

If it stops with an error, copy the full PowerShell output. The stopping stage identifies whether the problem is Python, SOLIDWORKS API discovery, MSBuild, agent startup, compilation, or COM registration.
