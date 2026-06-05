# Build self-contained Windows exe.
# Run: powershell -ExecutionPolicy Bypass -File .\build.ps1
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$solution = Join-Path $root 'KtpnConfigurator.slnx'
$publishDir = Join-Path $root 'build/KtpnConfigurator_V6'

Write-Host '== Tests ==' -ForegroundColor Cyan
dotnet test $solution -c Release

Write-Host '== Publish ==' -ForegroundColor Cyan
dotnet publish "$root/src/KtpnConfigurator.App/KtpnConfigurator.App.csproj" `
    -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o $publishDir

Get-ChildItem $publishDir -Filter *.pdb | Remove-Item -Force -ErrorAction SilentlyContinue
Write-Host "Done: $publishDir/KtpnConfigurator.exe" -ForegroundColor Green
