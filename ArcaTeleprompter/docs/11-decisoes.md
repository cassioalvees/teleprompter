# 11 — Registro de decisões de arquitetura (ADR)

## ADR-001 — C#/.NET 8 + WPF em vez de Electron/React
**Contexto:** o protótipo era React/Electron; o pedido é um software nativo, com DLLs e bibliotecas públicas.
**Decisão:** .NET 8 self-contained, WPF (renderização DirectX), MVVM com CommunityToolkit.Mvvm.
**Consequências:** rolagem suave sincronizada ao vsync (`CompositionTarget.Rendering`), acesso direto a WASAPI/ASIO/NDI/Win32; instalador maior (~200 MB com runtime e modelo de voz).

## ADR-002 — Domínio em DLL separada (Arca.Core)
**Decisão:** modelo, layout, motor de rolagem e alinhador sem dependência de UI.
**Consequências:** testável com xUnit; reutilizável por ferramentas (`VoiceTest`) e por um futuro serviço headless.

## ADR-003 — Web sem framework (JS puro) + PWA
**Contexto:** "a parte online também não quero React".
**Decisão:** HTML/CSS/JS puros embutidos na DLL do servidor; WebSocket próprio; service worker apenas em contexto seguro (HTTPS 8421).
**Consequências:** sem build de front-end; carga mínima no iPad.

## ADR-004 — Reconhecimento de voz offline (Vosk)
**Decisão:** Vosk com gramática restrita ao vocabulário do roteiro; nada de nuvem.
**Consequências:** privacidade e funcionamento sem internet; precisão menor que serviços em nuvem, compensada pela gramática e pelo alinhador tolerante (Levenshtein, improviso).

## ADR-005 — NAudio para WASAPI e ASIO
**Decisão:** NAudio (MIT), com ASIO abrindo 1 canal por `InputChannelOffset`.
**Consequências:** uso direto de interfaces de áudio profissionais e da mesa (canal do microfone do apresentador).

## ADR-006 — NDI por P/Invoke ao runtime instalado
**Decisão:** não redistribuir o SDK; carregar `Processing.NDI.Lib.x64.dll` do runtime NDI 6.
**Consequências:** sem questões de licença no pacote; o operador instala o runtime (link no app).

## ADR-007 — Espaço de layout de 1000 unidades e âncora por bloco
**Decisão:** diagramar o texto em largura fixa e escalar por saída; sincronizar por bloco + deslocamento.
**Consequências:** mesmas quebras de linha em todas as telas, inclusive web; tolerância a diferenças de fonte.

## ADR-008 — Instalador próprio em WPF
**Decisão:** instalador/desinstalador em .NET, com o visual do protótipo, em vez de MSI/Inno.
**Consequências:** identidade visual e detecção de NDI/voz; responsabilidade de manter registro, atalhos, desfazer e elevação no próprio código.

## ADR-009 — Primeira execução limpa
**Decisão:** o app abre com um roteiro vazio; exemplos vão para `Documentos\ARCA Teleprompter\Exemplos`.
**Consequências:** nenhum dado de demonstração entra na sessão do usuário.
