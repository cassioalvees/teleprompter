# 04 — Modelo de dados

## Domínio (Arca.Core.Model)

```
ScriptDocument
 └── Blocks: List<ScriptBlock>
       ScriptBlock { Kind: Text | Bookmark | Stop, Runs: List<TextRun>, Who: int (0 = ninguém), Skip: bool }
       TextRun     { Text, Bold, Italic, Underline, Color: "#rrggbb" | null }
PrompterSettings  (aparência e rolagem — ver tabela abaixo)
Presenter         { Name, Color }
```

Regras:
- Um `Bookmark` abre um bloco do *rundown*; os blocos de texto seguintes pertencem a ele até o próximo marcador.
- `Skip` num marcador pula o bloco inteiro; num parágrafo, só o parágrafo.
- `Who` é herdado pelos parágrafos seguintes até mudar (`WhoStartMask`).
- Texto puro: linha iniciada por `*` = marcador; `[PAUSA]` = pausa (`ScriptDocument.FromPlainText`).

## Arquivo `.tps`

JSON UTF-8.

**v1** (protótipo web) — `{ "app": "ARCA Teleprompter", "v": 1, "html": "<p>…</p>", "settings": {…} }`
O HTML usa `data-bm` (marcador), `data-stop` (pausa), `data-who="N"`, `data-skip`.

**v2** (este app) — `{ "app": "ARCA Teleprompter", "v": 2, "blocks": [ … ], "html": "…", "settings": {…} }`
`blocks` é a fonte da verdade; `html` continua sendo gravado para abrir no protótipo.

Exemplo de bloco: `{ "Kind": "Text", "Runs": [ { "Text": "Boa noite", "Bold": true } ], "Who": 1, "Skip": false }`

### `settings`

| Campo | Tipo | Padrão | Descrição |
|---|---|---|---|
| `wpl` | número | 5 | Palavras por linha (define o tamanho da fonte) |
| `margin` / `mPos` | número | 0 / −1 | Margem e posição (−1 esq., 0 centro, +1 dir.) |
| `lineH` | número | 1.3 | Altura de linha |
| `fontKey` | texto | `atk` | `atk`, `sans`, `serif`, `mono` ou `sys:Família` (fonte instalada) |
| `bg` / `fg` | cor | `#000000` / `#ffffff` | Fundo / texto |
| `align` | texto | `left` | `left`, `center`, `right`, `justify` |
| `readPct` | número | 30 | Linha de leitura (% da altura) |
| `ovType` / `ovOp` | texto / número | `nenhum` / 0.55 | Overlay `faixa`, `fade`, `destaque`, `nenhum` |
| `indType` / `indSize` / `indColor` | texto / 0-2 / cor | `seta` / 1 / `#ffffff` | Indicador |
| `readingLine` | bool | false | Linha horizontal de leitura |
| `speedT`, `minS`, `maxS` | número | 0.3, 0.3, 3 | Posição do controle de velocidade e limites (LPS) |
| `pres` | lista | Âncora, Repórter | Apresentadores |
| `presNames` / `presOnly` | bool / texto | true / `"0"` | Mostrar nomes / destacar só um |
| `progSec` | inteiro | 600 | Duração-alvo do programa (s) |

## Arquivos de estado (`%APPDATA%\ARCA Teleprompter`)

| Arquivo | Classe | Conteúdo |
|---|---|---|
| `settings.json` | `AppSettings` | Tema (`light`), atalhos, telas, web, NDI, áudio, voz, MOS, legenda, aparência padrão |
| `session.json` | `Session` | Abas abertas: nome, caminho, blocos, posição, alterado |
| `versions.json` | `List<ScriptVersion>` | Até 25 versões por roteiro (manual, automática a cada 2 min, ao fechar) |
| `profiles.json` | `List<Profile>` | Perfis de aparência salvos |
| `https.pfx` | — | Certificado autoassinado do monitor web (5 anos) |

Instalação: `install.json` na pasta do app (versão, escopo, caminho, componentes) e chave
`Software\Microsoft\Windows\CurrentVersion\Uninstall\ARCATeleprompter` (HKCU ou HKLM).
