$ErrorActionPreference = "Stop"

$csc = Join-Path $env:WINDIR "Microsoft.NET\\Framework64\\v4.0.30319\\csc.exe"
if (-not (Test-Path $csc)) {
    throw "csc.exe not found at $csc. Install .NET Framework 4.x or update the path."
}

if (-not (Test-Path "dist")) { New-Item -ItemType Directory -Path "dist" | Out-Null }

& $csc /nologo /target:winexe /optimize+ /out:dist\\Shutter.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll src\\Shutter.cs
if ($LASTEXITCODE -ne 0) { throw "Build failed (csc exit code $LASTEXITCODE)." }
Write-Host "Built dist\\Shutter.exe"
