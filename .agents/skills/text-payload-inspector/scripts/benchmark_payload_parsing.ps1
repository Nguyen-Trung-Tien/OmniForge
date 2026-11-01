[CmdletBinding()]
param(
    [int]$Iterations = 1000
)

Write-Host "[Payload Inspector Benchmark] Measuring Utf8JsonWriter vs string formatting ($Iterations iterations)..." -ForegroundColor Cyan

$SampleJson = '{"id":"tool_01","name":"OmniForge","active":true,"version":1.0,"features":["batch-io","crypto-hash","text-inspector"]}'
$Sw = [System.Diagnostics.Stopwatch]::StartNew()

for ($i = 0; $i -lt $Iterations; $i++) {
    $Doc = [System.Text.Json.JsonDocument]::Parse($SampleJson)
    $Buffer = New-Object System.Buffers.ArrayBufferWriter[byte]
    $Options = New-Object System.Text.Json.JsonWriterOptions
    $Options.Indented = $true
    $Writer = New-Object System.Text.Json.Utf8JsonWriter($Buffer, $Options)
    $Doc.RootElement.WriteTo($Writer)
    $Writer.Dispose()
    $Doc.Dispose()
}

$Sw.Stop()
$PerOp = [Math]::Round($Sw.ElapsedMilliseconds / $Iterations, 4)
Write-Host "[PASSED] Completed $Iterations iterations in $($Sw.ElapsedMilliseconds) ms ($PerOp ms/op)." -ForegroundColor Green

