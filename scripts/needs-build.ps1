# Used by StartTranslator.bat to decide whether the Release build is out of date.
# Exit code 0: the built app is newer than every source file, nothing to do.
# Exit code 1: the app is missing or a source file changed, so it must be rebuilt.
param(
    [Parameter(Mandatory = $true)][string]$Root,
    [Parameter(Mandatory = $true)][string]$Exe
)

if (-not (Test-Path $Exe)) { exit 1 }
$builtAt = (Get-Item $Exe).LastWriteTimeUtc

# node_modules, bin, obj and dist are build inputs/outputs, not sources, and are skipped
# (node_modules alone has tens of thousands of files).
$skip = '\\(bin|obj|node_modules|dist|\.vs|\.idea)(\\|$)'
$sources = @(
    'KikitanTranslator.Base',
    'KikitanTranslator.Resources',
    'KikitanTranslator.Overlay',
    'KikitanTranslator.Photino'
)

foreach ($dir in $sources) {
    $path = Join-Path $Root $dir
    if (-not (Test-Path $path)) { continue }

    $pending = [System.Collections.Generic.Queue[string]]::new()
    $pending.Enqueue($path)
    while ($pending.Count -gt 0) {
        $current = $pending.Dequeue()
        foreach ($item in Get-ChildItem -LiteralPath $current -Force -ErrorAction SilentlyContinue) {
            if ($item.FullName -match $skip) { continue }
            if ($item.PSIsContainer) { $pending.Enqueue($item.FullName); continue }
            if ($item.LastWriteTimeUtc -gt $builtAt) { exit 1 }
        }
    }
}

exit 0
