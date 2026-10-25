<#
.SYNOPSIS
  Build completo do ARCA Teleprompter: testes, publicação do app, pacote e instalador.

.DESCRIPTION
  1. Executa os testes automatizados (xUnit).
  2. Publica o app (WPF, .NET 8, autossuficiente win-x64) em out\app.
  3. Publica o desinstalador (ArcaUninstall.exe) na mesma pasta, compartilhando o runtime.
  4. Monta o pacote (payload.zip): app\, docs\, models\ (modelo de voz Vosk pt-BR) e samples\.
  5. Publica o instalador em arquivo único: dist\ARCA Teleprompter Setup <versão>.exe

.PARAMETER SkipTests
  Não executa os testes.
.PARAMETER NoVoiceModel
  Não inclui o modelo de voz no pacote (instalador menor; o app baixa o modelo quando precisar).
.EXAMPLE
  powershell -ExecutionPolicy Bypass -File build.ps1
#>
param([switch]$SkipTests, [switch]$NoVoiceModel)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
Set-Location $root
[xml]$props = Get-Content "$root\Directory.Build.props"
$version = $props.Project.PropertyGroup.Version
$out = "$root\out"; $dist = "$root\dist"; $payloadDir = "$root\src\ArcaSetup\payload"
Write-Host "ARCA Teleprompter $version" -ForegroundColor Cyan

if (-not $SkipTests) {
  Write-Host "`n[1/5] Testes" -ForegroundColor Yellow
  dotnet test "$root\tests\Arca.Core.Tests" -c Release --nologo
  if ($LASTEXITCODE -ne 0) { throw "Testes falharam" }
}

Write-Host "`n[2/5] Publicando o aplicativo" -ForegroundColor Yellow
Remove-Item -Recurse -Force "$out\app", "$out\payload" -ErrorAction SilentlyContinue
Get-ChildItem $out -Directory -Filter "setup*" -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
dotnet publish "$root\src\ArcaTeleprompter" -c Release -r win-x64 --self-contained true -p:PublishReadyToRun=true -o "$out\app" --nologo
if ($LASTEXITCODE -ne 0) { throw "Falha ao publicar o app" }

Write-Host "`n[3/5] Publicando o desinstalador" -ForegroundColor Yellow
dotnet publish "$root\src\ArcaSetup" -c Release -r win-x64 --self-contained true -p:AssemblyName=ArcaUninstall -p:EmbedPayload=false -o "$out\app" --nologo
if ($LASTEXITCODE -ne 0) { throw "Falha ao publicar o desinstalador" }
Remove-Item "$out\app\*.pdb" -ErrorAction SilentlyContinue

Write-Host "`n[4/5] Montando o pacote" -ForegroundColor Yellow
$stage = "$out\payload"
New-Item -ItemType Directory -Force "$stage\app", "$stage\docs", "$stage\samples", "$stage\models" | Out-Null
Copy-Item -Recurse "$out\app\*" "$stage\app"
New-Item -ItemType Directory -Force "$stage\app\docs" | Out-Null
Copy-Item "$root\docs\Manual do Operador.html" "$stage\app\docs\" -ErrorAction SilentlyContinue
Copy-Item "$root\docs\NOVIDADES.txt" "$stage\docs\" -ErrorAction SilentlyContinue
Copy-Item "$root\CHANGELOG.md" "$stage\app\docs\" -ErrorAction SilentlyContinue
Copy-Item "$root\assets\samples\*.tps" "$stage\samples\"
if (-not $NoVoiceModel) {
  $model = Join-Path $env:LOCALAPPDATA "ARCA Teleprompter\models\vosk-model-small-pt-0.3"
  if (-not (Test-Path $model)) {
    Write-Host "Baixando o modelo de voz pt-BR (Vosk, 31 MB)…"
    $zip = "$out\vosk-pt.zip"
    Invoke-WebRequest "https://alphacephei.com/vosk/models/vosk-model-small-pt-0.3.zip" -OutFile $zip
    Expand-Archive $zip -DestinationPath (Split-Path $model) -Force
  }
  Copy-Item -Recurse $model "$stage\models\"
}
New-Item -ItemType Directory -Force $payloadDir | Out-Null
Remove-Item "$payloadDir\payload.zip" -ErrorAction SilentlyContinue
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($stage, "$payloadDir\payload.zip", [System.IO.Compression.CompressionLevel]::Optimal, $false)
"{0:N0} MB" -f ((Get-Item "$payloadDir\payload.zip").Length / 1MB) | Write-Host

Write-Host "`n[5/5] Gerando o instalador" -ForegroundColor Yellow
# Pasta própria por build: um instalador antigo ainda aberto não bloqueia a geração do novo.
$setupOut = "$out\setup-" + (Get-Date -Format "yyyyMMddHHmmss")
dotnet publish "$root\src\ArcaSetup" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:EmbedPayload=true -o "$setupOut" --nologo
if ($LASTEXITCODE -ne 0) { throw "Falha ao gerar o instalador" }
New-Item -ItemType Directory -Force $dist | Out-Null
$setup = "$dist\ARCA Teleprompter Setup $version.exe"
Copy-Item "$setupOut\ArcaSetup.exe" $setup -Force
$hash = (Get-FileHash $setup -Algorithm SHA256).Hash
"$hash  $(Split-Path $setup -Leaf)" | Set-Content "$dist\SHA256SUMS.txt" -Encoding ascii
Write-Host ("`nPronto: {0} ({1:N0} MB)" -f $setup, ((Get-Item $setup).Length / 1MB)) -ForegroundColor Green
Write-Host "SHA-256: $hash"

# Atualização automática: o app lê o latest.json da última versão no GitHub Releases (publish-release.ps1 envia os dois).
# No GitHub o instalador vai com hífens no nome (espaços em nomes de anexos viram pontos).
$asset = "ARCA-Teleprompter-Setup-$version.exe"
$notes = if (Test-Path "$root\docs\NOVIDADES.txt") { ((Get-Content "$root\docs\NOVIDADES.txt" -Encoding UTF8) | Where-Object { $_ -like '- *' } | Select-Object -First 8) -join "`n" } else { "" }
$latest = [ordered]@{
  version = $version; file = $asset; url = $asset; size = (Get-Item $setup).Length; sha256 = $hash
  notes = $notes; date = (Get-Date -Format "yyyy-MM-dd")
}
$json = $latest | ConvertTo-Json
[System.IO.File]::WriteAllText("$dist\latest.json", $json, (New-Object System.Text.UTF8Encoding $false))
# Assina já aqui quando a chave de atualizações existe neste computador (testes locais com ARCA_UPDATE_URL).
if ((Get-Command node -ErrorAction SilentlyContinue) -and (Select-String -Path "$root\server\licencas\.dev.vars" -Pattern '^UPDATE_PRIVATE_KEY=' -Quiet -ErrorAction SilentlyContinue)) {
  & node "$root\tools\assinar-atualizacao.mjs" "$dist\latest.json" "$dist\latest.json" | Out-Null
}
Write-Host "latest.json: versão $version ($asset)"
