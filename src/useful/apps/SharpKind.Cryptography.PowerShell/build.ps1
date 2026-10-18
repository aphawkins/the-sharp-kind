# 'SharpKind Libraries' - Andy Hawkins 2023-2026.

<#
.SYNOPSIS
Builds the SharpKind.Cryptography PowerShell module.

.DESCRIPTION
Builds the cryptography libraries in Release, and lays out the module in OutputPath: the manifest,
the script module, and the libraries under lib. The module version comes from the libraries'
MinVer version.
#>
[CmdletBinding()]
param(
    [string] $OutputPath = (Join-Path $PSScriptRoot '../../../../publish/SharpKind.Cryptography')
)

$ErrorActionPreference = 'Stop'

$project = Join-Path $PSScriptRoot '../../libs/SharpKind.Security.Cryptography.Cryptanalysis/SharpKind.Security.Cryptography.Cryptanalysis.csproj'
$buildPath = Join-Path ([System.IO.Path]::GetTempPath()) "SharpKind.Cryptography.$PID"

dotnet build $project --configuration Release --output $buildPath --verbosity quiet

if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed with exit code $LASTEXITCODE."
}

if (Test-Path $OutputPath) {
    Remove-Item $OutputPath -Recurse -Force
}

$lib = New-Item (Join-Path $OutputPath 'lib') -ItemType Directory -Force
Copy-Item (Join-Path $buildPath 'SharpKind.Security.Cryptography.dll'), (Join-Path $buildPath 'SharpKind.Security.Cryptography.Cryptanalysis.dll') $lib
Copy-Item (Join-Path $PSScriptRoot 'SharpKind.Cryptography.psm1') $OutputPath
Remove-Item $buildPath -Recurse -Force

# MinVer writes the full SemVer version, plus build metadata, as the informational version.
# A module version must be Major.Minor.Patch, and a Gallery prerelease label only letters and digits.
$semVer = ((Get-Item (Join-Path $lib 'SharpKind.Security.Cryptography.dll')).VersionInfo.ProductVersion -split '\+')[0]
$version, $prerelease = $semVer -split '-', 2
$prerelease = $prerelease -replace '[^0-9A-Za-z]'

$manifest = Get-Content (Join-Path $PSScriptRoot 'SharpKind.Cryptography.psd1') -Raw
$manifest = $manifest -replace "ModuleVersion(\s*)= '0\.0\.0'", "ModuleVersion`$1= '$version'"
$manifest = $manifest -replace "Prerelease(\s*)= ''", "Prerelease`$1= '$prerelease'"
Set-Content (Join-Path $OutputPath 'SharpKind.Cryptography.psd1') $manifest -NoNewline

Write-Host "Built SharpKind.Cryptography $semVer in $((Resolve-Path $OutputPath).Path)"
