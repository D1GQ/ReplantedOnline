param(
    [string]$ProjectDirectory,
    [string]$AssemblyName,
    [string]$Configuration
)

$modPathFile = Join-Path $ProjectDirectory "build\mod_folder_path"
if (-not (Test-Path $modPathFile)) { 
    Write-Warning "mod_folder_path not found - mod will not be copied"
    exit 0
}

$gamePath = Get-Content $modPathFile
if ([string]::IsNullOrWhiteSpace($gamePath)) {
    Write-Warning "mod_folder_path is empty - set the game path to enable mod copying"
    exit 0
}

$gamePath = $gamePath.Trim()
$modsPath = Join-Path $gamePath "Mods"
if (-not (Test-Path $modsPath)) { 
    Write-Warning "Game path '$gamePath' not found - mod will not be copied"
    Write-Warning "Set the correct path in: $modPathFile"
    exit 0
}

$sourceFile = Join-Path $ProjectDirectory "bin\$Configuration\net6.0\$AssemblyName.dll"
if (Test-Path $sourceFile) {
    Copy-Item $sourceFile $modsPath -Force
    Write-Host "Mod DLL copied to: $modsPath\$AssemblyName.dll"
} else {
    Write-Warning "Source DLL not found: $sourceFile"
    exit 0
}