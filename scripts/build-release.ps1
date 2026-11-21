<# Build RAGECOOP (Legacy) + StorySync locally, package a release zip,
   and optionally push the current branch to GitHub.
   Run from the repo root (D:\Applications\GitHub\REPOS\RAGECOOP-V):
     powershell -ExecutionPolicy Bypass -File scripts\build-release.ps1
     powershell -ExecutionPolicy Bypass -File scripts\build-release.ps1 -Push
   Requires: .NET 10 SDK, .NET Framework 4.8 Developer Pack, nuget.exe
   on PATH (or in .\RSS\nuget.exe). See README.md Requirements. #>
param(
  [switch]$Push,
  [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $RepoRoot
$Stamp = (Get-Date).ToUniversalTime().ToString("yyyyMMdd-HHmmss")
$ZipName = "Legacy_RageCOOP-V-StorySync-main-$Stamp.zip"

Write-Host "== 1. Restore NuGet packages (client uses packages.config) =="
if (Test-Path "$RepoRoot\RSS\nuget.exe") { $Nuget = "$RepoRoot\RSS\nuget.exe" } else { $Nuget = "nuget" }
& $Nuget restore RageCoop-V.sln

Write-Host "== 2. Build RAGECOOP client installer =="
dotnet build RageCoop.Client.Installer/RageCoop.Client.Installer.csproj -c $Configuration

Write-Host "== 3. Build RAGECOOP server =="
dotnet build RageCoop.Server/RageCoop.Server.csproj -c $Configuration

Write-Host "== 4. Build StorySync =="
dotnet build StorySync/StorySync.sln -c $Configuration -p:RageCoopRoot=$RepoRoot

Write-Host "== 5. Copy StorySync DLLs into the server resource layout =="
New-Item -ItemType Directory -Force -Path "bin\$Configuration\Server\Resources\Server\StorySync" | Out-Null
Copy-Item "StorySync\Server\bin\$Configuration\StorySync.dll" "bin\$Configuration\Server\Resources\Server\StorySync\StorySync.dll"
New-Item -ItemType Directory -Force -Path "bin\$Configuration\Server\Resources\Client\StorySync" | Out-Null
Copy-Item "StorySync\Client\bin\$Configuration\StorySync.Client.dll" "bin\$Configuration\Server\Resources\Client\StorySync\StorySync.Client.dll"

Write-Host "== 6. Package release zip =="
$Stage = Join-Path $env:TEMP "ragecoop-package"
if (Test-Path $Stage) { Remove-Item $Stage -Recurse -Force }
New-Item $Stage -ItemType Directory -Force | Out-Null
foreach ($part in 'Client', 'Installer', 'Server') {
  Copy-Item "bin\$Configuration\$part" (Join-Path $Stage $part) -Recurse
}
New-Item -ItemType Directory -Force -Path (Join-Path $Stage 'StorySync\Server') | Out-Null
Copy-Item "StorySync\Server\bin\$Configuration\StorySync.dll" (Join-Path $Stage 'StorySync\Server\StorySync.dll')
New-Item -ItemType Directory -Force -Path (Join-Path $Stage 'StorySync\Client') | Out-Null
Copy-Item "StorySync\Client\bin\$Configuration\StorySync.Client.dll" (Join-Path $Stage 'StorySync\Client\StorySync.Client.dll')
Copy-Item "StorySync\README.md" (Join-Path $Stage 'StorySync\README.md')
Compress-Archive -Path (Join-Path $Stage '*') -DestinationPath (Join-Path $RepoRoot $ZipName) -Force
Write-Host "Wrote $ZipName"

if ($Push) {
  Write-Host "== 7. Push current branch =="
  git push
  Write-Host "Pushed. Upload $ZipName to https://github.com/XxZorro13xX/RAGECOOP-V/releases manually, or let the Actions workflow do it."
} else {
  Write-Host "Done. Zip is at $RepoRoot\$ZipName — run with -Push to also push the branch."
}
