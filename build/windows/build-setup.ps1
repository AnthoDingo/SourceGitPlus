# Builds SourceGitPlus and packages it into a Windows installer (Inno Setup 6 required).
# Usage: .\build\windows\build-setup.ps1 [-Runtime win-x64|win-arm64] [-NoAot]
param(
    [ValidateSet('win-x64', 'win-arm64')][string]$Runtime = 'win-x64',
    [switch]$NoAot  # skip Native AOT (no C++ build tools needed)
)
$ErrorActionPreference = 'Stop'
$root = Resolve-Path "$PSScriptRoot\..\.."
$version = (Get-Content "$root\VERSION" -Raw).Trim()
$publish = "$root\publish\$Runtime"
$iscc = @("${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe", "$env:ProgramFiles\Inno Setup 6\ISCC.exe") |
    Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) { throw 'Inno Setup 6 not found (winget install JRSoftware.InnoSetup)' }

Remove-Item $publish -Recurse -Force -ErrorAction SilentlyContinue
$extra = if ($NoAot) { @('-p:DisableAOT=true') } else { @() }
dotnet publish "$root\src\SourceGitPlus.csproj" -c Release -r $Runtime -o $publish @extra
if ($LASTEXITCODE) { throw 'dotnet publish failed' }

& $iscc "/DAppVersion=$version" "/DRuntime=$Runtime" "/DSourceDir=$publish" "/DOutDir=$root\dist" "$PSScriptRoot\SourceGitPlus.iss"
if ($LASTEXITCODE) { throw 'ISCC failed' }
Write-Host "Installer: $root\dist"
