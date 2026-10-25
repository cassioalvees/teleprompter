# 03 — Arquitetura

## Contexto (C4 nível 1)

```
            ┌──────────────┐  teclado/clicker   ┌──────────────────────────┐
 Operador ─▶│  PC Windows  │◀───────────────────│ Apresentador (talento)   │
            │  ARCA 2.0    │──── monitores ────▶│ prompter físico (espelho)│
            └─┬───┬───┬──┬─┘                    └──────────────────────────┘
   WebSocket  │   │   │  │ MOS TCP 10540/41
  8420/8421   │   │   │  └──────────────▶ Sistema de redação (NRCS)
 iPad / PWA ◀─┘   │   └─ NDI ──────────▶ Mixer / vMix / OBS / TriCaster
                  └─ HTTP/TCP legenda ─▶ vMix / gerador de caracteres
```

## Contêineres e DLLs (C4 nível 2)

```
ArcaTeleprompter.exe (WPF, MVVM)
 ├── Arca.Core.dll      domínio puro: ScriptDocument, TpsFile, HtmlScript, PrompterLayout,
 │                      ScrollEngine, VoiceAligner, Keymap, Stores/JsonStore, Protocol
 ├── Arca.Import.dll    DOCX (OpenXml), PDF (PdfPig), HTML, TXT → ScriptDocument
 ├── Arca.Audio.dll     AudioInput (NAudio WASAPI/ASIO/arquivo) → SpeechRecognizer (Vosk)
 ├── Arca.Ndi.dll       NdiSender (P/Invoke Processing.NDI.Lib.x64.dll)
 ├── Arca.Server.dll    PrompterServer (Kestrel, WebSocket, HTTPS, PWA embutido)
 └── Arca.Mos.dll       MosDevice (TCP, MOS 2.8 running order)
ArcaSetup.exe / ArcaUninstall.exe (WPF) — mesmo código; o desinstalador usa o runtime da pasta do app
```

Regra de dependência: `Arca.Core` não referencia nenhuma outra DLL do projeto nem WPF. As DLLs de
infraestrutura referenciam só `Arca.Core`. O app compõe tudo nos *Services*.

## Componentes do app (C4 nível 3)

| Componente | Arquivo | Responsabilidade |
|---|---|---|
| MainWindow | `MainWindow*.cs` (partial: Editor, Keys, Dialogs, Integrations) | Orquestra UI, laço de renderização, abas, sessão |
| MainViewModel | `ViewModels/MainViewModel.cs` | Estado observável (CommunityToolkit.Mvvm) |
| PrompterSurface | `Controls/PrompterSurface.cs` | Texto do prompter (RichTextBox somente leitura, 1000 unidades de largura, escala/translação) + HUD |
| PrompterOverlay | `Controls/PrompterOverlay.cs` | Faixa, fade, destaque, linha de leitura, indicador |
| ScriptFlow | `Editor/ScriptFlow.cs` | ScriptDocument ↔ FlowDocument (editor e prompter) |
| OutputManager | `Services/OutputManager.cs` | Janelas de saída por monitor |
| WebService | `Services/WebService.cs` | Liga/desliga o servidor; publica estado (~30 Hz) |
| NdiService | `Services/NdiService.cs` | Thread de render própria (Dispatcher separado) → NDI |
| VoiceService | `Services/VoiceService.cs` | Captura + ASR + alinhador → alvo de rolagem |
| VideoService | `Services/VideoService.cs` | Prévia de câmera e gravação MP4 (MediaCapture) |
| CaptionService, MosService | `Services/IntegrationServices.cs` | Legenda e espelho MOS |
| Native | `Services/Native.cs` | Monitores (DisplayConfig), hotkeys globais, manter tela acesa |

## Espaço de layout
Todo o texto é diagramado num espaço de **1000 unidades de largura**; a escala para cada saída é
`largura_px / 1000`. Fonte, margens e altura de linha derivam de `PrompterLayout.Compute(settings, fontFactor)`
(palavras por linha → tamanho de fonte). Assim operador, monitores, NDI e web mostram **as mesmas quebras de linha**.

A sincronização entre superfícies usa **âncora** (índice do bloco + fração/deslocamento dentro do bloco),
e não pixels, porque cada superfície pode medir o bloco com pequena diferença.

## Threads

| Thread | Trabalho |
|---|---|
| UI (Dispatcher principal) | Editor, laço `CompositionTarget.Rendering` (ScrollEngine.Tick), saídas em monitores, leituras a 5 Hz |
| NDI render | Dispatcher próprio com PrompterSurface offscreen: renderiza o **texto em faixas** da altura do quadro (só quando a rolagem se aproxima de uma faixa nova) e o **overlay** uma vez, com fundo transparente |
| NDI envio | Relógio próprio (Stopwatch + timer de 1 ms): monta cada quadro copiando linhas das faixas e compondo o overlay (≈ 2 ms em 1080p) e envia no ritmo exato do formato (`NDIlib_send_send_video_async_v2`, buffer duplo) |
| Áudio (NAudio) | Callback de captura → nível, gate, reamostragem 16 kHz → fila |
| Reconhecimento | Worker com `BlockingCollection` → Vosk → palavras novas → UI (`BeginInvoke`) |
| Kestrel | Conexões HTTP/WebSocket; comandos remotos voltam à UI por evento |
| MOS | Listeners TCP 10540/10541 assíncronos |

Regra: só a thread de UI altera o documento e o motor de rolagem. As demais publicam eventos.

## Fluxo de rolagem
1. `ScrollEngine.Tick(dt)` integra a velocidade (LPS × altura de linha), aplica pausas (`Stops`), fim (`EndY`) e alvo de voz.
2. O operador e as janelas de saída recebem `Y`; NDI e web recebem `Y` + velocidade e extrapolam entre amostras.
   O navegador recebe também o relógio do servidor e a **fração do bloco** (não pixels): compensa a variação de atraso do Wi-Fi e
   as pequenas diferenças de diagramação do navegador, e suaviza a posição exibida (sem trancos).
3. `SlowTick` (5 Hz): leituras, marcador atual, HUD do talento, estado web, legenda.

## Persistência
`%APPDATA%\ARCA Teleprompter\`: `settings.json`, `session.json`, `versions.json`, `profiles.json`, `https.pfx`.
`%LOCALAPPDATA%\ARCA Teleprompter\`: `logs\`, `models\` (Vosk). Gravação atômica (arquivo temporário + `File.Replace`).
