param(
    [Parameter(Mandatory = $true)][int]$ProcessId,
    [ValidateRange(5,60)][int]$Seconds = 10,
    [string]$Scenario = 'unspecified'
)
$ErrorActionPreference = 'Stop'
$process = Get-Process -Id $ProcessId
if ($process.ProcessName -ne 'Petcompanion') { throw 'Choose a Petcompanion process ID.' }
$process.Refresh()
$cpuStart = $process.TotalProcessorTime.TotalSeconds
$watch = [Diagnostics.Stopwatch]::StartNew()
$memory = @()
for ($index = 0; $index -lt $Seconds; $index++) {
    Start-Sleep -Seconds 1
    $process.Refresh()
    if ($process.HasExited) { throw 'The measured process exited.' }
    $memory += $process.WorkingSet64
}
$watch.Stop()
$cpu = $process.TotalProcessorTime.TotalSeconds - $cpuStart
$result = [ordered]@{
    scenario = $Scenario
    processId = $ProcessId
    measuredAtUtc = [DateTime]::UtcNow.ToString('o')
    seconds = [Math]::Round($watch.Elapsed.TotalSeconds, 2)
    logicalProcessors = [Environment]::ProcessorCount
    cpuPercentOfMachine = [Math]::Round(100 * $cpu / $watch.Elapsed.TotalSeconds / [Environment]::ProcessorCount, 2)
    cpuPercentOfOneCore = [Math]::Round(100 * $cpu / $watch.Elapsed.TotalSeconds, 2)
    workingSetAverageMB = [Math]::Round(($memory | Measure-Object -Average).Average / 1MB, 1)
    workingSetPeakMB = [Math]::Round(($memory | Measure-Object -Maximum).Maximum / 1MB, 1)
    note = 'Process CPU and working set only; GPU, frame times, power draw, and battery use are not measured.'
}
$output = Join-Path (Split-Path -Parent $PSScriptRoot) 'artifacts/performance'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$json = ConvertTo-Json $result
$json | Set-Content -Path (Join-Path $output ("sample-{0}-{1}.json" -f $ProcessId, [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss'))) -Encoding UTF8
Write-Output $json
