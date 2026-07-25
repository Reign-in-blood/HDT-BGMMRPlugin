param(
    [Parameter(Mandatory = $true)]
    [string]$InputPath
)

$ErrorActionPreference = "Stop"

function Test-ManagedAssembly {
    param([string]$Path)

    try {
        [System.Reflection.AssemblyName]::GetAssemblyName($Path) | Out-Null
        return $true
    }
    catch {
        return $false
    }
}

$expandedPath = [Environment]::ExpandEnvironmentVariables(
    $InputPath.Trim().Trim('"')
)

if (-not (Test-Path -LiteralPath $expandedPath)) {
    throw "The specified path does not exist: $expandedPath"
}

$item = Get-Item -LiteralPath $expandedPath
$candidates = New-Object System.Collections.Generic.List[System.IO.FileInfo]

if (-not $item.PSIsContainer) {
    if ($item.Name -ieq "HearthstoneDeckTracker.exe") {
        $candidates.Add($item)
    }

    $searchRoot = $item.Directory.FullName
}
else {
    $searchRoot = $item.FullName
}

Get-ChildItem `
    -LiteralPath $searchRoot `
    -Filter "HearthstoneDeckTracker.exe" `
    -File `
    -Recurse `
    -ErrorAction SilentlyContinue |
    ForEach-Object {
        if (-not $candidates.Exists(
            [Predicate[System.IO.FileInfo]]{
                param($candidate)
                $candidate.FullName -ieq $_.FullName
            }
        )) {
            $candidates.Add($_)
        }
    }

if ($candidates.Count -eq 0) {
    throw "No HearthstoneDeckTracker.exe file was found under: $searchRoot"
}

$managedCandidates = $candidates |
    Where-Object { Test-ManagedAssembly $_.FullName } |
    Sort-Object LastWriteTime -Descending

$selected = $managedCandidates | Select-Object -First 1

if ($null -eq $selected) {
    $foundPaths = (
        $candidates |
        ForEach-Object { " - " + $_.FullName }
    ) -join [Environment]::NewLine

    throw (
        "HearthstoneDeckTracker.exe files were found, " +
        "but none is a usable managed .NET assembly for compilation." +
        [Environment]::NewLine +
        $foundPaths
    )
}

Write-Output $selected.FullName
