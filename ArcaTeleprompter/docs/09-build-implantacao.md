# 09 — Build, implantação e operação

## Ambiente de desenvolvimento
- Windows 10/11 x64, .NET SDK 8.0.3xx. Opcional: NDI Tools/runtime 6 para testar NDI.
- Solução: `ArcaTeleprompter.sln`. Configuração comum em `Directory.Build.props` (versão, nullable, x64).

## Pipeline (`build.ps1`)
1. **Testes** — `dotnet test` (falha interrompe).
2. **App** — `dotnet publish src/ArcaTeleprompter -r win-x64 --self-contained -p:PublishReadyToRun=true` → `out\app`.
3. **Desinstalador** — `ArcaSetup` publicado como `ArcaUninstall.exe` na mesma pasta (usa o runtime do app).
4. **Pacote** — `payload.zip` com `app\`, `docs\`, `samples\` e `models\` (Vosk pt-BR).
5. **Instalador** — `ArcaSetup` em arquivo único com o pacote embutido → `dist\ARCA Teleprompter Setup <versão>.exe` + `SHA256SUMS.txt`.

Versão: altere `<Version>` em `Directory.Build.props` e siga o checklist de [12 — Versionamento](12-versionamento.md).

## Instalação
| Item | Por usuário | Todos os usuários |
|---|---|---|
| Pasta padrão | `%LOCALAPPDATA%\Programs\ARCA Teleprompter` | `%ProgramFiles%\ARCA Teleprompter` |
| Registro | HKCU | HKLM (pede UAC) |
| Pasta personalizada | qualquer; se não houver permissão de escrita, pede UAC e continua (`--resume`) | idem |

O instalador: copia arquivos (com proteção contra *zip slip*), cria atalhos, associa `.tps` (ProgId `ARCA.Script`),
cria regra de firewall para 8420–8421 (quando elevado), copia exemplos para `Documentos\ARCA Teleprompter\Exemplos`,
grava `install.json` e a chave de desinstalação. Em erro, desfaz tudo (pilha de desfazer).

Instalação silenciosa: não suportada na 2.0 (planejado).

## Desinstalação
`ArcaUninstall.exe` (Configurações › Aplicativos ou menu Iniciar). Opções: manter roteiros, preferências e perfis.
Os arquivos do próprio runtime são apagados por um processo auxiliar logo após o desinstalador fechar.

## Operação
- Logs: `%LOCALAPPDATA%\ARCA Teleprompter\logs\arca-AAAA-MM-DD.log` (também em Ajuda › Registro de eventos).
- Rede: liberar TCP 8420/8421 (web) e 10540/10541 (MOS) no firewall corporativo, se usados.
- NDI: instalar o NDI Runtime 6 (link no instalador e no app).
- Backup: copiar `%APPDATA%\ARCA Teleprompter`.
