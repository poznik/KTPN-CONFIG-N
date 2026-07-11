# Build self-contained Windows exe.
# Run: powershell -ExecutionPolicy Bypass -File .\build.ps1
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$solution = Join-Path $root 'KtpnConfigurator.slnx'
$publishDir = Join-Path $root 'build/KtpnConfigurator_V6'
$buildRoot = Join-Path $root 'build'
$exeName = [string]::Concat([char[]](1050,1086,1085,1092,1080,1075,1091,1088,1072,1090,1086,1088,32,1050,1058,1055,1053,46,101,120,101))

Write-Host '== Tests ==' -ForegroundColor Cyan
dotnet test $solution -c Release
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host '== Publish ==' -ForegroundColor Cyan
$resolvedBuildRoot = [System.IO.Path]::GetFullPath($buildRoot)
$resolvedPublishDir = [System.IO.Path]::GetFullPath($publishDir)
if (-not $resolvedPublishDir.StartsWith($resolvedBuildRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Unsafe publish directory: $resolvedPublishDir"
}

if (Test-Path -LiteralPath $publishDir) {
    Remove-Item -LiteralPath $publishDir -Recurse -Force
}
New-Item -ItemType Directory -Path $publishDir | Out-Null

dotnet publish "$root/src/KtpnConfigurator.App/KtpnConfigurator.App.csproj" `
    -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o $publishDir
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$publishedExe = Join-Path $publishDir 'KtpnConfigurator.exe'
$friendlyExe = Join-Path $publishDir $exeName
if (Test-Path -LiteralPath $friendlyExe) {
    Remove-Item -LiteralPath $friendlyExe -Force
}
Rename-Item -LiteralPath $publishedExe -NewName $exeName

Get-ChildItem $publishDir -Filter *.pdb | Remove-Item -Force -ErrorAction SilentlyContinue

Write-Host '== Published selftest ==' -ForegroundColor Cyan
& $friendlyExe --selftest
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Done: $friendlyExe" -ForegroundColor Green
