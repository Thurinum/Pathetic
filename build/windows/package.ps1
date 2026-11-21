<#
.SYNOPSIS
    Builds a self-contained, portable Pathetic for Windows.

.EXAMPLE
    build/windows/package.ps1 -Version 1.2.3 -Runtime win-x64

    Output: artifacts/Pathetic-windows-x64.zip, containing a Pathetic folder with
    Pathetic.App.exe and everything it needs (no .NET install required). Vulkan comes
    from the GPU driver.
#>
param(
    [string] $Version = "0.0.0-dev",

    [ValidateSet("win-x64", "win-arm64")]
    [string] $Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "../..")
$artifacts = Join-Path $root "artifacts"
$arch = $Runtime.Substring("win-".Length)
$publish = Join-Path $artifacts "publish-$Runtime"
$stage = Join-Path $artifacts "windows-$arch"
$zip = Join-Path $artifacts "Pathetic-windows-$arch.zip"

foreach ($path in @($publish, $stage, $zip)) {
    if (Test-Path $path) { Remove-Item -Recurse -Force $path }
}

# Self-contained: the .NET runtime ships with the app, nothing to install.
dotnet publish (Join-Path $root "Pathetic.App/Pathetic.App.csproj") `
    --configuration Release `
    --runtime $Runtime `
    --self-contained true `
    -p:SelfContained=true `
    -p:PublishSingleFile=true `
    -p:Version=$Version `
    --output $publish
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }

# Shaders, assets, Pathetic.ini and native DLLs are resolved next to the executable,
# so the whole folder is shipped. Debug symbols and XML docs are left out.
$app = Join-Path $stage "Pathetic"
New-Item -ItemType Directory -Force $app | Out-Null
Get-ChildItem $publish | Where-Object { $_.Extension -notin ".pdb", ".xml" } |
    Copy-Item -Destination $app -Recurse

Compress-Archive -Path $app -DestinationPath $zip

Write-Host "Built $zip"
