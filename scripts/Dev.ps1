param(
    [ValidateSet('help','init','build','run','preview','test','sound','publish','install')]
    [string]$Action = 'help'
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$output = Join-Path $repo 'artifacts/dev'
$executable = Join-Path $output 'Petcompanion.exe'
function Build-App {
    dotnet build Petcompanion.csproj --configuration Release --output $output --configfile NuGet.Config
    if ($LASTEXITCODE -ne 0) { throw 'Build failed. Close any app or preview launched from artifacts/dev and try again.' }
}
function Test-App([string]$Mode, [string]$Log, [int]$Timeout) {
    $logPath = Join-Path $output $Log
    # Remove only a known previous check result, so an old PASS cannot mask this run.
    if (Test-Path -LiteralPath $logPath) { Remove-Item -LiteralPath $logPath }
    $process = Start-Process -FilePath $executable -ArgumentList $Mode -WindowStyle Hidden -PassThru
    if (-not $process.WaitForExit($Timeout * 1000)) {
        Stop-Process -Id $process.Id
        throw "$Mode timed out after $Timeout seconds."
    }
    if (-not (Test-Path -LiteralPath $logPath)) { throw "$Mode did not produce its result log." }
    $result = Get-Content -LiteralPath $logPath -Raw
    Write-Output $result
    if ($process.ExitCode -ne 0 -or -not $result.StartsWith('PASS:')) { throw "$Mode failed." }
}
Push-Location $repo
try {
    if ($Action -eq 'help') {
        Write-Output @'
Petcompanion commands (Windows x64, .NET SDK 10.0.400):
  .\init.cmd                 Check SDK, restore, build, run self-checks
  .\petcompanion.cmd build    Build into artifacts/dev
  .\petcompanion.cmd run      Build and open the desktop pet
  .\petcompanion.cmd preview  Build and open the rotatable 3D model
  .\petcompanion.cmd test     Build; self-checks, muted UI and audio tests
  .\petcompanion.cmd sound    Play a short owl sound at 35% volume
  .\petcompanion.cmd publish  Produce a self-contained Windows app
  .\petcompanion.cmd install  Install the published app and shortcuts

The UI test briefly covers the current monitor, then closes itself.
Published files: artifacts/win-x64. Logs and test images: artifacts/dev.
Installation does not enable startup automatically. Quit the installed pet before updating.
'@
        exit 0
    }
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'Install the .NET 10 SDK, then open a new terminal.' }
    dotnet --version
    if ($LASTEXITCODE -ne 0) { throw 'Install a stable .NET SDK compatible with global.json (10.0.400 or later 10.0.4xx patch).' }
    switch ($Action) {
        'init' {
            dotnet restore Petcompanion.csproj --configfile NuGet.Config
            if ($LASTEXITCODE -ne 0) { throw 'Restore failed.' }
            Build-App
            Test-App '--self-test' 'self-test.log' 20
        }
        'build' { Build-App }
        'run' { Build-App; Start-Process -FilePath $executable }
        'preview' { Build-App; Start-Process -FilePath $executable -ArgumentList '--model-preview' }
        'test' {
            Build-App
            Test-App '--self-test' 'self-test.log' 20
            Test-App '--smoke-test' 'smoke-test.log' 100
            Test-App '--audio-test' 'audio-test.log' 55
        }
        'sound' { Build-App; Test-App '--sound-check' 'sound-check.log' 15 }
        'publish' {
            dotnet publish Petcompanion.csproj --configuration Release --runtime win-x64 --self-contained true --output artifacts/win-x64 --configfile NuGet.Config
            if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
        }
        'install' { & (Join-Path $PSScriptRoot 'Install.ps1') }
    }
} finally { Pop-Location }
