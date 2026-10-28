param([switch]$Launch)
$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $PSScriptRoot
$publish = Join-Path $project 'artifacts/win-x64'
$install = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Programs/Petcompanion'
$exe = Join-Path $install 'Petcompanion.exe'
$legacyExe = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Programs\DragonBreak\DragonBreak.exe'
if (-not (Test-Path (Join-Path $publish 'Petcompanion.exe'))) { throw 'Publish the app first. See README.md.' }
foreach ($process in (Get-Process -Name Petcompanion,DragonBreak -ErrorAction SilentlyContinue)) {
    if ($process.Path -and ([string]::Equals($process.Path, $exe, [StringComparison]::OrdinalIgnoreCase) -or [string]::Equals($process.Path, $legacyExe, [StringComparison]::OrdinalIgnoreCase))) {
        throw 'Quit Petcompanion from its tray menu before updating.'
    }
}
New-Item -ItemType Directory -Path $install -Force | Out-Null
Get-ChildItem -LiteralPath $publish -Force | Copy-Item -Destination $install -Recurse -Force
$shell = New-Object -ComObject WScript.Shell
try {
    foreach ($location in @([Environment]::GetFolderPath('DesktopDirectory'), [Environment]::GetFolderPath('Programs'))) {
        $shortcut = $shell.CreateShortcut((Join-Path $location 'Petcompanion.lnk'))
        $shortcut.TargetPath = $exe
        $shortcut.WorkingDirectory = $install
        $shortcut.Description = 'Petcompanion - your pets and gentle reminders'
        $shortcut.Save()
        # Retire only an old shortcut whose target belongs to the previous app installation.
        $legacyLink = Join-Path $location 'DragonBreak.lnk'
        if (Test-Path -LiteralPath $legacyLink) {
            $oldShortcut = $shell.CreateShortcut($legacyLink)
            try {
                if ([string]::Equals($oldShortcut.TargetPath, $legacyExe, [StringComparison]::OrdinalIgnoreCase)) {
                    Remove-Item -LiteralPath $legacyLink
                }
            } finally { [Runtime.InteropServices.Marshal]::FinalReleaseComObject($oldShortcut) | Out-Null }
        }
        [Runtime.InteropServices.Marshal]::FinalReleaseComObject($shortcut) | Out-Null
    }
} finally { [Runtime.InteropServices.Marshal]::FinalReleaseComObject($shell) | Out-Null }
Write-Output "Installed: $exe"
Write-Output 'Desktop and Start menu shortcuts created. Automatic startup is off.'
if ($Launch) { Start-Process -FilePath $exe }
