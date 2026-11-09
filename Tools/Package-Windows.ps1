[CmdletBinding()]
param(
    [string]$BuildDirectory,
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
if (-not $BuildDirectory) { $BuildDirectory = Join-Path $projectRoot 'Builds\Windows' }
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $projectRoot 'dist' }
$buildRoot = (Resolve-Path -LiteralPath $BuildDirectory).Path
$outputRoot = [IO.Path]::GetFullPath($OutputDirectory)
foreach ($required in @('VelvetLoto.exe', 'UnityPlayer.dll', 'VelvetLoto_Data')) {
    if (-not (Test-Path -LiteralPath (Join-Path $buildRoot $required))) {
        throw "Incomplete Windows build: missing $required"
    }
}
if ($outputRoot.StartsWith($buildRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or $outputRoot -eq $buildRoot) {
    throw 'Choose an output directory outside the game directory.'
}
New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null
$archivePath = Join-Path $outputRoot 'LOTTO-Windows-x64.zip'
if (Test-Path -LiteralPath $archivePath) { throw "Archive already exists: $archivePath. Move it before creating a new package." }

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($buildRoot, $archivePath, [IO.Compression.CompressionLevel]::Optimal, $false)
$archive = [IO.Compression.ZipFile]::Open($archivePath, [IO.Compression.ZipArchiveMode]::Update)
try {
    $instructions = @'
LOTTO - Лото в уме

1. Полностью распакуйте ZIP в отдельную папку.
2. Запустите VelvetLoto.exe из распакованной папки.
3. Для тренировки выберите «Тренировка с соперниками».

Unity для запуска не требуется. Сохраняйте рядом VelvetLoto_Data,
UnityPlayer.dll и остальные файлы. Не запускайте EXE прямо из архива.

Для игры по локальной сети ведущий создаёт стол, остальные подключаются
по его локальному IPv4-адресу. Всем нужна одинаковая версия игры.
Используется TCP-порт 28741.

Мышь - осмотреть стол. ЛКМ - поставить на число.
Пробел - убрать бочонок. Esc - правила, звук и выход к меню.

Проект, правила, скриншоты и обновления:
https://github.com/BurimBasic/Lotto
'@
    $entry = $archive.CreateEntry('START-HERE.txt')
    $writer = New-Object IO.StreamWriter($entry.Open(), (New-Object Text.UTF8Encoding($false)))
    try { $writer.Write($instructions) } finally { $writer.Dispose() }
    $licenses = @(
        @{ Source = 'Assets\Resources\Fonts\PTSans-OFL.txt'; Destination = 'Licenses/PTSans-OFL.txt' },
        @{ Source = 'Assets\Resources\Fonts\PTSerif-OFL.txt'; Destination = 'Licenses/PTSerif-OFL.txt' },
        @{ Source = 'Assets\Resources\Audio\PencilStroke-Credits.txt'; Destination = 'Licenses/PencilStroke-Credits.txt' }
    )
    foreach ($license in $licenses) {
        if (-not $archive.GetEntry($license.Destination)) {
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, (Join-Path $projectRoot $license.Source), $license.Destination, [IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    }
} finally { $archive.Dispose() }
$hash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText((Join-Path $outputRoot 'SHA256SUMS.txt'), "$hash  LOTTO-Windows-x64.zip`n", (New-Object Text.UTF8Encoding($false)))
Write-Output "Ready: $archivePath"
Write-Output "SHA256: $hash"
