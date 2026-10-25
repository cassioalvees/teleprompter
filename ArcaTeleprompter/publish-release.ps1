# Publica a versão atual no GitHub Releases (cassioalvees/teleprompter) para a atualização automática do app.
#   1. .\build.ps1                      → dist\ARCA Teleprompter Setup <versão>.exe + dist\latest.json
#   2. $env:GITHUB_TOKEN = "<token>"    → token do GitHub com permissão "Contents: Read and write" neste repositório
#   3. .\publish-release.ps1            → cria a versão v<versão> e anexa o instalador e o latest.json
# O app consulta https://github.com/cassioalvees/teleprompter/releases/latest/download/latest.json ao abrir.
#   -SoLatestJson                      → só reenvia o latest.json (corrigir uma versão já publicada)
param([string]$Repo = "cassioalvees/teleprompter", [switch]$Draft, [switch]$SoLatestJson)
$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$version = ([xml](Get-Content "$root\Directory.Build.props")).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
$setup = "$root\dist\ARCA Teleprompter Setup $version.exe"
$latest = "$root\dist\latest.json"
if (-not (Test-Path $setup)) { throw "Instalador não encontrado: $setup (rode .\build.ps1)" }
if (-not (Test-Path $latest)) { throw "latest.json não encontrado (rode .\build.ps1)" }
$info = Get-Content $latest -Raw -Encoding UTF8 | ConvertFrom-Json
if ($info.version -ne $version) { throw "latest.json é da versão $($info.version), mas a versão atual é $version (rode .\build.ps1)" }
if ((Get-FileHash $setup -Algorithm SHA256).Hash -ne $info.sha256) { throw "O SHA-256 do instalador não confere com o latest.json (rode .\build.ps1)" }
if (-not $env:GITHUB_TOKEN) { throw "Defina `$env:GITHUB_TOKEN com um token do GitHub (Contents: Read and write em $Repo)." }

$h = @{ Authorization = "Bearer $env:GITHUB_TOKEN"; Accept = "application/vnd.github+json"; "X-GitHub-Api-Version" = "2022-11-28" }
$tag = "v$version"
# Texto puro: no Windows PowerShell, Get-Content -Raw traz propriedades extras (PSPath…) que o ConvertTo-Json enviaria junto.
$notes = [IO.File]::ReadAllText("$root\docs\NOVIDADES.txt", [Text.Encoding]::UTF8)
Write-Host "Publicando ARCA Teleprompter $version em $Repo ($tag)…" -ForegroundColor Yellow

try { $rel = Invoke-RestMethod -Headers $h "https://api.github.com/repos/$Repo/releases/tags/$tag" }
catch {
  $body = @{ tag_name = $tag; target_commitish = "main"; name = "ARCA Teleprompter $version"; body = $notes; draft = [bool]$Draft; make_latest = "true" } | ConvertTo-Json
  $rel = Invoke-RestMethod -Method Post -Headers $h -ContentType "application/json; charset=utf-8" -Body ([Text.Encoding]::UTF8.GetBytes($body)) "https://api.github.com/repos/$Repo/releases"
  Write-Host "Versão criada: $($rel.html_url)"
}

function Upload($file, $name, $type) {
  $old = $rel.assets | Where-Object { $_.name -eq $name }
  if ($old) { Invoke-RestMethod -Method Delete -Headers $h "https://api.github.com/repos/$Repo/releases/assets/$($old.id)" | Out-Null }
  Write-Host ("Enviando {0} ({1:N0} MB)…" -f $name, ((Get-Item $file).Length / 1MB))
  Invoke-RestMethod -Method Post -Headers $h -ContentType $type -InFile $file "https://uploads.github.com/repos/$Repo/releases/$($rel.id)/assets?name=$name" | Out-Null
}
if (-not $SoLatestJson) { Upload $setup $info.file "application/octet-stream" }
# No GitHub, o latest.json leva o endereço COMPLETO do instalador: o app baixa o latest.json por um endereço
# temporário assinado (release-assets…?jwt=…) e, com um nome relativo, montava o endereço sem a assinatura (erro 618).
$info.url = "https://github.com/$Repo/releases/download/$tag/$($info.file)"
$pub = Join-Path ([IO.Path]::GetTempPath()) "latest.json"
[IO.File]::WriteAllText($pub, ($info | ConvertTo-Json), (New-Object Text.UTF8Encoding $false))
# Assinatura (o app recusa latest.json sem assinatura válida): chave UPDATE_PRIVATE_KEY em server\licencas\.dev.vars.
& node "$root\tools\assinar-atualizacao.mjs" $pub $pub
if ($LASTEXITCODE -ne 0) { throw "Não foi possível assinar o latest.json (veja a chave UPDATE_PRIVATE_KEY em server\licencas\.dev.vars)." }
Upload $pub "latest.json" "application/json"
Remove-Item $pub
Write-Host "`nPronto: $($rel.html_url)" -ForegroundColor Green
Write-Host "O app encontra esta versão em: https://github.com/$Repo/releases/latest/download/latest.json"
