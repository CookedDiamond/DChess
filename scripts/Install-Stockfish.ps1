$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$directory = Join-Path $root 'Engines/Stockfish'
New-Item -ItemType Directory -Force -Path $directory | Out-Null
$archive = Join-Path $directory 'stockfish19.zip'
Invoke-WebRequest 'https://github.com/official-stockfish/Stockfish/releases/download/sf_19/stockfish-windows-x86-64-universal.zip' -OutFile $archive
if ((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ne '3c8bf1f9ea66a09350a40df4f632288285ac206d99f33ab5842c408fc30b48a7') {
    throw 'Stockfish download checksum mismatch.'
}
Expand-Archive -LiteralPath $archive -DestinationPath $directory -Force
$engine = Join-Path $directory 'stockfish/stockfish-windows-x86-64-universal.exe'
if (!(Test-Path -LiteralPath $engine)) { throw 'Stockfish executable missing from archive.' }
Copy-Item -LiteralPath $engine -Destination (Join-Path $directory 'stockfish.exe') -Force
Write-Output 'Installed Stockfish 19 (verified SHA256). Build DChess to copy it to the application directory.'
