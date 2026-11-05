# 12 — Versionamento e atualizações

## Numeração (SemVer)
`MAIOR.MENOR.CORREÇÃO` — ex.: 2.1.0

| Parte | Quando sobe | Exemplos |
|---|---|---|
| MAIOR | Mudança incompatível: formato `.tps` que versões antigas não abrem, protocolo web/MOS quebrado, requisito novo de sistema | 2.x → 3.0.0 |
| MENOR | Funções novas ou mudanças de comportamento compatíveis (rodada de melhorias) | 2.0.0 → 2.1.0 |
| CORREÇÃO | Só correções, sem função nova | 2.1.0 → 2.1.1 |

**Fonte única da versão:** `<Version>` em `Directory.Build.props`. Ela vai para todas as DLLs, para o app (Ajuda › Sobre),
para o instalador (nome do arquivo `ARCA Teleprompter Setup <versão>.exe`, chave de desinstalação e `install.json`).

## Checklist de cada versão
1. Subir `<Version>` em `Directory.Build.props`.
2. Registrar as mudanças no topo do [CHANGELOG.md](../CHANGELOG.md) (Adicionado / Alterado / Corrigido / Desempenho).
3. Atualizar [NOVIDADES.txt](NOVIDADES.txt) — resumo da versão para o operador (aparece no instalador).
4. Atualizar a documentação afetada (manual, protocolos, testes) e os resultados medidos em [08](08-testes.md).
5. Se o cliente web mudou: subir `CACHE` em `src/Arca.Server/wwwroot/sw.js` (o iPad baixa o app novo).
6. Se um padrão de configuração mudou: escrever a **migração** (ex.: `PrompterSettings.MigrateSpeed`) e um teste.
7. `build.ps1` (roda os testes; falha interrompe) → `dist\ARCA Teleprompter Setup <versão>.exe` + `SHA256SUMS.txt`.

## Publicar a versão (atualização automática)
1. `.\build.ps1` — gera `dist\ARCA Teleprompter Setup <versão>.exe` e `dist\latest.json` (versão, arquivo, tamanho, SHA-256, novidades).
2. `GITHUB_TOKEN` — token do GitHub com **Contents: Read and write** no repositório `cassioalvees/teleprompter`. O login do GitHub já salvo no Windows serve: no Git Bash, `export GITHUB_TOKEN=$(printf 'protocol=https\nhost=github.com\n\n' | git credential fill | sed -n 's/^password=//p')` (o token não aparece na tela).
3. `.\publish-release.ps1` — cria a versão `v<versão>` no GitHub Releases, assina o `latest.json` (`tools/assinar-atualizacao.mjs`) e anexa o instalador e o `latest.json`. **Nunca publique sem assinatura**: as versões 2.9.0+ recusam.
4. Servidor/site mudou? Em `server/licencas`: `node tools/build-site.mjs` (se mudou o site) e `npx wrangler deploy` (**sem** `--secrets-file`: os segredos já estão na Cloudflare).

Os apps instalados consultam `https://github.com/cassioalvees/teleprompter/releases/latest/download/latest.json` ao abrir e oferecem a atualização. `ARCA_UPDATE_URL` troca o endereço (testes com servidor local).
O instalador sem assinatura digital mostra o aviso do SmartScreen nas primeiras instalações; a atualização funciona igual.

## Atualização no computador do usuário
- O instalador detecta a instalação existente (registro) e mostra **"Atualização: versão instalada → nova"**; usa a mesma pasta e o mesmo escopo.
- Roteiros, preferências, perfis, histórico e modelos de voz (`%APPDATA%` e `%LOCALAPPDATA%`) **não são tocados**.
- Programas do ARCA abertos na pasta (app ou desinstalador) são fechados; arquivo ainda em uso é renomeado e substituído, e o antigo é apagado depois.
- Instalar uma versão **mais antiga** por cima de uma mais nova exige confirmação.
- Compatibilidade de dados: `.tps` v1 (protótipo) e v2 continuam abrindo; configurações antigas passam pelas migrações ao carregar.

## Histórico
Ver [CHANGELOG.md](../CHANGELOG.md).
