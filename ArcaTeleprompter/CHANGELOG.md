# Histórico de versões — ARCA Teleprompter

Formato baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/); numeração [SemVer](https://semver.org/lang/pt-BR/)
(regras em [docs/12-versionamento.md](docs/12-versionamento.md)). A versão mais recente fica no topo.

## [2.8.8] — 2026-10-25

### Corrigido
- **Preferências cresciam e encolhiam ao trocar de aba**: a janela ajustava a altura ao conteúdo de cada aba (entre 400 e 560 px). Agora tem altura fixa — a mesma em todas as abas, cabendo na janela do app (até 560 px) — e o conteúdo rola dentro. Autoteste: Geral, Atalhos, Dispositivos e Integrações com 520 px.

### Adicionado
- `--cmd=linhas` registra as palavras de cada linha no editor, nas telas de prompter e na janela do talento (diagnóstico de "palavras por linha").

## [2.8.7] — 2026-10-25

### Corrigido
- **NDI sem fluidez a 60p** (relatado com 1080p60 e fundo transparente): a montagem de cada quadro (texto com posição em subpixel + camada sobreposta) rodava num núcleo só e, no uso real, passava de 16,7 ms — o NDI saía com ~45 quadros irregulares por segundo, quase todos atrasados. Agora o quadro é montado em faixas paralelas (uma por núcleo, até 8) e a mistura da camada sobreposta ficou mais leve (sem divisão por pixel). Medido a 1080p60 com fundo transparente: montagem de 3,8–13 ms (picos de 40) → **1,2–2,1 ms**; 300 de 300 quadros em 5 s, 0 atrasados — também com a janela do talento aberta em tela cheia.
- **iPad continuava com o monitor web antigo depois de atualizar o ARCA**: ao reabrir o app, a página só reconectava e mantinha o código anterior na memória (por isso o arrasto da 2.8.6 não chegava ao iPad e as telas ainda andavam aos trancos). O app agora informa a identidade dos arquivos do monitor web ao conectar; se mudou, a página recarrega sozinha. Sem atualização, só reconecta.

### Alterado
- **Desfeito (2.8.5): tela cheia no iPad volta a ser a do Safari**, pelo botão de tela cheia, como antes — sem aviso para abrir pela Tela de Início.
- Registro do NDI mostra o nome da saída e o formato em cada linha de métrica; `ARCA_NDI_BANDS` muda o número de faixas (medições).

## [2.8.6] — 2026-10-25

### Corrigido
- **Arrastar o texto no iPad (parado): o NDI, as telas e o app só mexiam ao soltar o dedo**, e pulavam de uma vez ("como se fosse quadro"). O monitor web mandava o arrasto inteiro ao app só no fim. Agora manda aos poucos (a cada ~40 ms) enquanto o dedo anda, e o app aplica pela mesma mola suave da roda do mouse: app, telas e NDI deslizam junto com o dedo. A tela do próprio iPad segue o dedo na hora e, ao soltar, espera o app terminar de deslizar (~0,45 s) em vez de recuar. A roda do mouse no navegador também passou a deslizar no app em vez de pular. Cache do app web v14.

## [2.8.5] — 2026-10-25

### Alterado
- **Monitor web no iPad/iPhone: tela cheia pela Tela de Início.** A tela cheia do Safari mostra "arraste para baixo para sair" e sai com esse gesto — o mesmo que no ARCA freia o texto (rodando) e rola o texto (parado). No iPad/iPhone, o botão de tela cheia (e a tecla F) agora mostra como abrir o ARCA pela Tela de Início (abre como app, em tela cheia, sem esse gesto) em vez de usar a tela cheia do Safari; aberto pela Tela de Início, o botão some. Em Ajustes, "Outro monitor…" não aparece no iPad. No computador nada muda. Cache do app web v13.

## [2.8.4] — 2026-10-25

### Alterado
- **Janela do talento abre em tela cheia** no monitor em que abre, cobrindo a barra de tarefas do Windows (antes abria como janela no centro; o "maximizar" do Windows deixava a barra de tarefas por cima do texto). O botão □ da barra, o duplo clique na barra e o F11 alternam entre tela cheia e janela; o botão de tela cheia repetido na barra de ferramentas saiu. **Esc fecha a janela do talento** de uma vez, também em tela cheia.

## [2.8.3] — 2026-10-24

### Corrigido
- **Próxima linha / linha anterior paravam no fim do parágrafo** dizendo "Última linha" (ou "Primeira linha"): com uma linha em branco entre parágrafos, o editor de texto não informa a linha seguinte a partir dela. Agora pula direto para o parágrafo com texto mais perto, nos dois sentidos (subindo, cai na última linha do parágrafo de cima).
- **Editor fora de passo das telas de prompter e do navegador perto de marcadores e pausas**: no editor o espaço acima do marcador somava com o do parágrafo anterior (nas telas, vale o maior dos dois) e o bloco do marcador começava antes da linha divisória; a pausa também tinha espaço diferente embaixo. Com o prompt rodando, a seta do editor apontava para o título do marcador enquanto as telas já estavam na primeira linha do texto. Agora marcador e pausa têm as mesmas medidas em todos os lugares (diferença medida: de ~20 px para 1,4 px).

### Adicionado
- **Licença ilimitada** (servidor): ativa em quantos computadores quiser com o mesmo código, para o dono e para os testes. Criada pelo administrador (`/api/admin/unlimited`) numa conta; aparece em Minha conta › Licenças como "Vários computadores" e no app como "licença ilimitada". Bloquear pelo /admin continua valendo.
- Autotestes: próxima/anterior atravessando a linha em branco; editor e telas de prompter com os mesmos blocos (texto, marcador e pausa).

## [2.8.2] — 2026-10-24

### Alterado
- **Monitor web com mouse: a barra de cima só aparece com o mouse no topo da tela** (como num player de vídeo) e some ao sair dele. Mexer o mouse no resto da tela — por exemplo, para mudar a velocidade nos controles de baixo —, clicar nos controles de baixo, usar a roda do mouse ou o teclado mostra só os controles de baixo e o cursor, não a barra de cima. Toque (iPad) continua mostrando tudo. Cache do app web v12.

### Corrigido
- **Atualização falhava no download com "Response status code does not indicate success: 618 (jwt:jwt-not-provided)"**: o GitHub entrega o latest.json por um endereço temporário assinado, e o app montava o endereço do instalador a partir dele, perdendo a assinatura. Agora monta a partir do endereço original. Para as versões já instaladas, `publish-release.ps1` passou a publicar o latest.json com o endereço completo do instalador (corrigido também na v2.8.1 já publicada, com `-SoLatestJson`).

## [2.8.1] — 2026-10-24

Correções que chegaram depois das primeiras cópias da 2.8.0 — com número novo para a atualização automática entregar a quem já tem a 2.8.0.

### Corrigido
- **Monitor web: a barra de cima não sumia em tela cheia** com o mouse parado em cima dela — ao começar a sumir, o navegador mandava um movimento "falso" do mouse (mesma posição), que a mostrava de novo. Agora só movimento de verdade mostra a barra; ao entrar ou sair da tela cheia ela volta a contar para sumir.
- **Monitor web: rolagem dos Ajustes** com a barra branca do navegador e o título rolando junto. Agora o título fica fixo e a barra de rolagem é fina, no visual do app (cache do app web v11).
- **Diálogos cortavam botões**: com três botões longos (ex.: Licença › Fechar · Minha conta · Desativar este computador) o primeiro ficava para fora. A janela agora cresce para caber todos.
- `publish-release.ps1`: o GitHub recusava a versão no Windows PowerShell (as novidades iam com propriedades extras do arquivo).

### Adicionado
- `--cmd=licenca` abre Ajuda › Licença (automação e testes).

## [2.8.0] — 2026-10-23

### Adicionado
- **Licença e modo demonstração.** Sem licença o app abre em demonstração: 10 minutos de prompter por dia, um roteiro e uma tela de prompter; NDI, monitor web/iPad, voz, redação (MOS), câmera/gravação e impressão pedem a licença (aviso com "Ativar licença"). O tempo do dia é guardado assinado na pasta de dados e no registro do usuário. Barra de status: "Demonstração · N min hoje · Ativar" (ou "Licenciado").
- **Ativação online, um computador por licença.** Ajuda › Licença (e a tela que aparece ao abrir na demonstração): cola o código, o servidor prende a licença a este computador e devolve uma licença assinada (ECDSA P-256), conferida pelo app com a chave pública embutida. Depois de ativado o app funciona sem internet; ao abrir com internet ele confere se a licença continua valendo. "Desativar este computador" libera o código. `--ativar=CÓDIGO` ativa sem interface.
- **Servidor de licenças e site** (`server/licencas`, Cloudflare Workers + D1), publicado em **https://arca-licencas.cassiosantos-csa.workers.dev** (endereço padrão do app): página do produto, download da última versão do GitHub, administração (`/admin`: buscar, liberar computador, bloquear, senha provisória, exportar CSV).
- **Minha conta, no site** (`/conta`): conta com e-mail e senha. O código de ativação fica em **Licenças**, que mostra em qual computador ele está ativado (nome, data, versão, último contato) e tem **Remover deste computador** — o código fica livre para ativar em outro, e o computador antigo volta à demonstração assim que abrir com internet ("A licença foi removida deste computador pela sua conta no site"). **Meus dados**: nome, empresa, telefone e troca de senha (encerra as outras sessões).
- Site: **Criar conta e baixar** no lugar de "Gerar código e baixar" — a pessoa cria a conta, vai para Minha conta (com o código) e o download começa sozinho.

### Corrigido
- **Instalação falhava com "The process cannot access the file because it is being used by another process"** (relatado em `WinRT.Runtime.dll`, em Arquivos de Programas): quando outro programa segura um arquivo da pasta por um instante (antivírus, indexador, o app terminando de fechar), o instalador agora espera e tenta de novo por até ~10 s. Se continuar preso, diz **qual programa** está com o arquivo e oferece **Tentar de novo**.
- **"As alterações foram desfeitas" não era verdade numa reinstalação ou atualização**: os arquivos já sobrescritos ficavam na versão nova. Agora cada arquivo existente é guardado antes de ser trocado; se algo falhar, a pasta volta exatamente como estava, e os novos são apagados.

### Alterado
- Textos do app: "versão licenciada" no lugar de "versão com cadastro"; a tela de ativação leva para **Minha conta** (onde está o código) e a tela da licença ativa tem o botão **Minha conta**.

## [2.7.0] — 2026-10-23

### Adicionado
- **Atualização automática** pelo GitHub Releases (`cassioalvees/teleprompter`). Ao abrir, o app consulta a última versão em segundo plano (8 s depois de abrir, sem atrasar nada). Havendo versão nova, mostra **"ARCA Teleprompter X disponível"** com as novidades e o tamanho: **Atualizar agora** ou **Lembrar depois** (pergunta de novo na próxima vez que o app abrir). Nunca aparece com o prompt rodando, a voz ligada, outro diálogo aberto ou o app em segundo plano — espera ficar livre.
- **Tela de download** da atualização: começa sozinha, com progresso, MB/s e tempo restante; o SHA-256 é conferido no caminho (arquivo corrompido ou alterado é recusado, nada é instalado). Ao terminar, o app fecha e o instalador abre em **modo atualização**.
- **Instalador `--update`**: usa a pasta, o tipo de instalação e os componentes da instalação existente, sem perguntas (vai direto para "Atualizando para a versão X"); o Windows pede a permissão de administrador quando a pasta exige (Arquivos de Programas); no fim, **reabre o app**, que mostra as novidades.
- Ajuda › **Procurar atualizações…** e Preferências › Geral › **Procurar atualizações ao abrir o app**.
- `build.ps1` gera `dist\latest.json` (versão, arquivo, tamanho, SHA-256, novidades); `publish-release.ps1` cria a versão no GitHub e anexa o instalador e o latest.json.

## [2.6.2] — 2026-10-23

### Corrigido
- **Editor não se ajustava ao maximizar e o texto ficava enorme ao restaurar** (relatado num monitor 4K): o editor recalculava o tamanho do texto lendo a largura da área de rolagem antes de ela ser atualizada, ficando sempre um passo atrás — ao maximizar continuava com a largura antiga; ao restaurar, usava a de tela cheia. Agora recalcula quando a largura visível de fato muda (e depois do passe de layout ao redimensionar, e ao mudar de monitor com outra escala do Windows).

### Adicionado
- Autoteste do tamanho da janela: normal, maximizar, restaurar, mais larga e mais estreita — o texto tem de preencher exatamente a largura visível.

## [2.6.1] — 2026-10-23

Suavidade conferida quadro a quadro no NDI (receptor próprio) e no navegador, com comandos vindos do próprio navegador.

### Corrigido
- **Tranco ao mudar a velocidade no talento, nas telas de prompter, no NDI e no navegador** (no editor não aparecia): cada mudança de velocidade — tecla, botão ou controle deslizante — reaplicava o visual de todas as telas. Agora muda só a velocidade. No NDI, o maior salto de velocidade entre quadros caiu de 148 para 33 px/s e o maior desvio de 3,8 para 0,8 px.
- **NDI perdia quadros** (até 4 seguidos, 66–133 ms): cada faixa de texto criava um bitmap e um bloco de 8 MB novos, e as limpezas de memória paravam o envio. Bitmap e faixas agora são reaproveitados, e com o NDI no ar o .NET usa limpeza de memória de baixa latência. Medido: 0 quadros atrasados em 28 s com mudanças de velocidade, pausas e linhas.
- **Recuo de ~1–2 px ao pausar** no NDI e no navegador (eles preveem a posição um pouco à frente; ao saber da pausa, voltavam). Parado no mesmo ponto, o texto fica onde está; andando, uma correção pequena nunca puxa contra o movimento.
- Se a interface travar um instante, o texto alcança o tempo real (limite por quadro de 0,1 → 0,25 s, o mesmo da previsão do NDI e do navegador).

### Alterado
- **Linha anterior/próxima (parado) desliza** no app, no talento, nas telas e no NDI — como a roda do mouse — em vez de pular. Rolagem manual com mola amortecida (sai devagar, acelera e chega devagar em ≈ 0,25 s; antes começava na velocidade máxima e, no NDI, a linha pulava meia altura no primeiro quadro). O NDI e o navegador recebem a velocidade do deslize para prever a posição entre os quadros.
- **Seletor de cor menor** (≈ 330 × 400 em vez de 460 × 500): quadro 280 × 120, HEX e RGB numa linha, cores prontas em duas fileiras.

### Adicionado
- `tools/NdiProbe --movimento N [saida.csv]`: mede quanto o texto anda entre quadros recebidos por NDI (correlação do perfil de brilho, subpixel) — saltos de velocidade, tremor, paradas e quadros para trás.
- Registro do NDI a cada 5 s com montagem e envio separados (média e máximo) e limpezas de memória.

## [2.6.0] — 2026-10-23

### Adicionado
- **Seletor de cor do app** no lugar do seletor do Windows (fundo, texto, apresentadores e texto selecionado): quadro de saturação × brilho, faixa de matiz, antes/depois (clique no "antes" volta à cor atual), HEX e RGB, 20 cores prontas para prompter e as **últimas usadas** (guardadas nas preferências).
- **Outra cor na barra de formatação**: bolinha colorida depois das cinco cores, abre o seletor para o texto selecionado.
- **Voz preparada ao abrir o app**: o motor escolhido (Vosk ou Whisper) carrega em segundo plano quando o modelo existe — e de novo ao trocar motor, modelo ou idioma. No Whisper, uma transcrição curta de aquecimento prepara a placa de vídeo.
- Autoteste da **suavidade**: rola com o talento e uma janela de prompter abertos, acelera e freia pelo teclado e mede quadro a quadro.
- `tools/VoiceTest`: `--pace=N` (simula um computador N× mais rápido), `--preload` e tempo de partida (primeira palavra depois de ligar).

### Corrigido
- **Microfone demorava a começar a ouvir**: o modelo do Vosk carregava no clique, travando a interface (2,5 s na primeira vez após ligar o computador). Agora carrega antes, fora da interface, e a captura começa na hora — o áudio fica na fila enquanto o reconhecedor fica pronto, sem perder o começo da fala. No Whisper, a primeira palavra sai 0,4 s mais cedo (1,84 → 1,40 s no teste).

### Desempenho
- Whisper **large-v3-turbo** avaliado e **não adotado**: neste computador (i5-1135G7 + Iris Xe) cada transcrição leva 1,2–1,5 s (o Base, 0,2 s) e ele acompanha só 15–33 das 55 palavras em tempo real; mesmo simulando um computador 5× mais rápido, segue menos palavras que o Base (43/49/29/37 contra 52/53/46/32). O **Small** num computador mais rápido é o melhor resultado medido (53/53/55/44).

## [2.5.1] — 2026-10-23

### Corrigido
- **Nome do apresentador colado no texto de cima**: na 2.3 o nome subiu (0,6 da fonte acima do parágrafo) e ficou mais perto da fala anterior do que da própria. Voltou para o lugar do protótipo e das versões 2.0–2.2 — encostado na própria fala (0,45 da fonte), com o espaço acima dele.
- **Marcador e nome com alinhamentos diferentes em cada tela**: no editor o marcador centralizava e o nome não; no prompter os dois ficavam sempre à esquerda; no navegador os dois centralizavam. Agora, como no protótipo, **marcador e nome seguem o alinhamento do texto** em todas as telas (editor, prompter, talento, NDI e navegador). A pausa continua à esquerda.
- **Janela do talento sem minimizar e maximizar**: botões ao lado do fechar.

## [2.5.0] — 2026-10-23

Conferência de todos os atalhos de teclado e dos botões de controle de cada tela.

### Alterado
- **Barra de baixo da janela principal igual às outras telas** (talento, janelas de prompter e navegador), na mesma ordem: parágrafo ↑, linha ↑, linha ↓, parágrafo ↓ · play · início · ré │ **−** velocidade **+**. Os botões "segure para recuar/avançar" saíram — linha e parágrafo fazem esse papel, iguais em todas as telas.

### Corrigido
- **Page Down (próximo marcador) e ] (próximo parágrafo) não funcionavam pelo teclado**: o Windows dá dois nomes para a mesma tecla (Page Down = "Next", ] = "Oem6") e o app comparava o nome. Agora compara a tecla — vale também para atalhos trocados em Preferências › Atalhos.

### Adicionado
- Autoteste (`--autoteste`) confere todos os atalhos — Espaço, ↑/↓ parado e rodando, W/S, R, 0, 1–9 e teclado numérico, Home/End/Backspace, Page Up/Down, [ ], M, F5, Esc, E, F11, Ctrl+Enter, Ctrl+1…9, Ctrl+E, Ctrl+, — e cada botão da barra de controle; também confere que **frear até o mínimo não vira ré** (a ré é só pelo botão ou pela tecla R) e que, digitando no editor, R e Espaço são texto.

## [2.4.0] — 2026-10-22

Reconhecimento de voz com microfones reais (o operador relatou que, mesmo com sinal forte, a fala quase não era reconhecida).

### Adicionado
- **Motor de voz Whisper · preciso** (inspetor › Voz › Motor), ao lado do **Vosk · rápido** (padrão): whisper.cpp via Whisper.net (MIT) com modelo multilíngue quantizado — **Base** (57 MB, padrão: atraso < 1 s) ou **Small** (181 MB, um pouco mais preciso em áudio muito ruim, mais lento). O modelo é baixado sob demanda pelo botão *Baixar modelo*.
- Transcrição quase em tempo real: janela deslizante de 2–4 s retranscrita a cada ~0,3 s **só quando há fala** (VAD do gate); emite apenas palavras novas e estáveis (acordo entre transcrições seguidas ou palavra já terminada há 0,35 s, com tempos por DTW). Proteções contra alucinação: nada roda no silêncio, laços de repetição cortados, frases-fantasma ("Obrigado.", créditos de legenda) descartadas.
- O trecho do roteiro em volta da posição vira o **prompt** do Whisper (com "Reconhecer só o vocabulário do roteiro" ligado), puxando a transcrição para as palavras do texto.
- **Placa de vídeo (Vulkan)** usada pelo Whisper quando houver — Intel Iris Xe: transcrição ≈ 2× mais rápida que no processador; opção *Usar a placa de vídeo* para desligar. Aviso automático quando o Whisper está lento no computador (sugere o Base).
- `tools/VoiceTest`: `--engine=vosk|whisper`, `--whisper=base|small`, `--ref=limpo.wav` (latência por palavra contra os tempos do Vosk offline no áudio limpo), `--trace`, e reverberação de sala opcional em `--degrade`.

### Desempenho
Medido com `tools/VoiceTest` (cobertura das 55 palavras do roteiro lidas · latência mediana do fim da palavra até o alinhador),
i5-1135G7 + Iris Xe:

| Áudio (mesma leitura de 31 s, 55 palavras do roteiro) | Vosk (gramática) | Whisper base (padrão) | Whisper small |
|---|---|---|---|
| `longa.wav` — limpo | 51/55 · 0,18 s | 54/55 · 0,68 s (p90 1,06) | 55/55 · 0,9–1,3 s |
| `fraca.wav` — microfone fraco (−24 dB + ruído) | 46/55 · 0,24 s | 54/55 · 0,65 s (p90 1,18) | 53–55/55 · 0,9–1,3 s |
| `sala.wav` — eco de sala (RT60 0,6 s) −12 dB + ruído | 38/55 · 0,27 s | 52/55 · 0,70 s (p90 1,22) | 50–52/55 · 1,0–1,1 s |
| `ruim.wav` — fala quase no nível do ruído | 6/55, perde-se na 8ª palavra | 28/55 · 0,64 s, segue até o fim | 33–36/55 · 0,8–0,9 s |

### Alterado
- Tratamento do sinal (passa-altas 80 Hz, AGC, gate, 16 kHz) extraído para `VoiceFrontEnd`, comum aos dois motores (`IVoiceRecognizer`); o Vosk funciona igual.
- O pacote leva só as bibliotecas nativas win-x64 do Whisper (processador e Vulkan).
- **Barra de controle igual em todas as telas** — janela do talento, janelas de prompter (flutuantes e de monitor) e navegador, na mesma ordem: parágrafo ↑, linha ↑, linha ↓, parágrafo ↓ · play · início · **ré** │ **−** velocidade **+**. Nas janelas de prompter ela aparece ao mexer o mouse e some sozinha em 3 s.
- Janela do talento: com a barra visível, bloco/tempo/relógio sobem para cima dela (antes o relógio ficava atrás da barra).

### Adicionado (telas e navegador)
- **Navegador: girar o texto** 0°/90°/180°/270° (⚙ › Tela › Girar o texto, ou endereço `?girar=90`) — para monitor de pé ou de cabeça para baixo; arrastar com o dedo segue a rotação.
- **Navegador: tela cheia sem bordas** (como o F11) nesta tela ou **em outro monitor** (⚙ › Tela cheia › Outro monitor…, Chrome/Edge pelo endereço https ou localhost); tecla **F** alterna.
- Botão de **ré** na barra do navegador (fica vermelho com o texto em ré).
- Autoteste do app (`--autoteste=relatorio.txt`): colar, apresentador por linha, desfazer/refazer, slider de velocidade, pausa, play e linha em branco.

### Corrigido
- **Seta parava em linha em branco** ao voltar ao início, ir a um parágrafo ou marcador: agora vai sempre para a próxima linha com texto (também com a voz).

## [2.3.0] — 2026-10-22

Terceira rodada de testes do operador (versão 2.2.0 instalada).

### Adicionado
- **Sensibilidade do mouse e do trackpad** (Preferências › Geral): linhas por giro com o prompt parado, passo de velocidade por giro com ele rodando e opção de **inverter** a roda.
- **Rolagem manual suave**: roda/trackpad deslizam o texto até o alvo (sem pular linhas); o deslizar do trackpad é proporcional.
- **Espaço entre parágrafos** ajustável (inspetor › Roteiro › Parágrafos, em linhas).
- **Voz**: volume de entrada (dB) e ganho automático liga/desliga; **comandos falados** — "comando voltar", "comando próximo", "comando início", "comando parar"; com a voz ligada, rolar, ir a um marcador ou voltar ao início recomeça o acompanhamento dali (dá para voltar a um trecho).
- **Monitor web para OBS/vMix**: endereço `?obs` (sem barra, sem controles, sem cursor) e `&fundo=transparente`; teclas + e −, F (tela cheia), V (voz); roda do mouse e arrastar mudam a velocidade enquanto roda.
- **NDI**: a fonte se identifica como "ARCA Teleprompter" nos receptores (metadado do SDK NDI 6); usa o SDK instalado se faltar o runtime.

### Alterado
- **Frear e acelerar** com aceleração constante (≈ 0,35 s por mudança): sem trancos em velocidade alta.
- **Mais devagar não passa mais para a ré**: para na velocidade mínima; a ré é só pelo botão/tecla de inverter.
- **Pausa** para exatamente onde foi pausado (sem o "encaixe" que fazia saltos).
- **Voz**: a linha falada fica centrada na seta e desliza para a próxima só no fim da linha; movimento com mola amortecida e aceleração limitada (sem arrancos entre parágrafos).
- **Espaçamentos iguais no editor, no prompter, no NDI e no iPad**: entre parágrafos ¼ de linha (ajustável); linha em branco digitada = meia linha; nome do apresentador junto da própria fala, afastado do texto anterior.
- **Informações na tela** maiores (proporcionais à tela) e em **zona reservada**: o texto passa por baixo, sem disputar com bloco/tempo/relógio.
- **Monitor web**: barra de cima some sozinha (1,5 s no modo limpo, 4 s no controle) e volta ao mexer o mouse; **controle pelo navegador vem liberado**; layout para celular e telas baixas.
- Abrir roteiro, voltar ao início e ir a marcador/parágrafo levam a seta à **primeira linha de texto** (nunca ao título do marcador).

### Corrigido
- **Janela do talento com duas barras** e dois botões de fechar — agora só a barra do app (arrastar move, duplo clique maximiza).
- **Controle de velocidade da janela do talento** não respondia (a janela separada perdeu a ligação com o app).
- **Desfazer/Refazer** não funcionavam (o histórico interno registrava as formatações automáticas); agora o histórico é do roteiro, com o cursor de volta ao lugar.
- **Apresentador pegava o texto todo** quando as linhas eram quebras dentro de um mesmo parágrafo — cada linha vira parágrafo próprio.
- **Cursor pulando ao colar** texto (agora fica no fim do texto colado e o editor rola até ele).
- Gestos e atalhos do navegador "não funcionavam" (o controle vinha desligado e o aviso ficava escondido no modo limpo).
- Tela de boas-vindas mostrava a versão anterior durante os testes.

## [2.2.0] — 2026-10-19

Segunda rodada de testes do operador, com a versão 2.1.0 instalada.

### Adicionado
- **Tela de boas-vindas**: na primeira execução e depois de uma instalação nova (ações rápidas, primeiros passos); depois de uma atualização mostra as **novidades da versão**. Também em Ajuda › Boas-vindas.
- **Janela do talento separada** (F5): abre em janela própria — o editor continua visível, dá para levar a outro monitor; F11 ou o botão da barra põem só ela em tela cheia; Esc ou X fecham.
- **Diagnóstico do microfone** (Voz › Gravar diagnóstico, 15 s): grava o áudio como ele chega (todos os canais) nos registros, para análise.
- Pasta de dados separada para testes e suporte (variável `ARCA_DATA_DIR`).

### Alterado
- **Instalação nova abre limpa**: sessão, histórico e preferências que sobraram de antes vão para `%APPDATA%\ARCA Teleprompter\anterior-AAAAMMDD-HHMM` (nada é apagado). Atualizações mantêm tudo.
- **Microfone**: usa o canal com mais voz (microfones "array" de notebook têm 2–4 canais e a média podia cancelar a voz) e o dispositivo padrão de gravação do Windows (não o de comunicações).
- **Roda do mouse na velocidade**: passo maior e proporcional ao giro (touchpad incluído), sem descartar giros rápidos; rampa de velocidade mais curta (0,15 s).
- **Linhas em branco** (comuns ao colar de sites) não viram espaço extra no prompter, no NDI e no iPad.
- "Abrir o app" ao final da instalação abre como usuário comum (antes herdava o administrador do instalador).

### Corrigido
- **Apresentador aplicado ao texto todo**: parágrafos criados com Enter ou colando compartilhavam o mesmo marcador interno; agora cada parágrafo tem o seu.
- **Barra da janela do talento sem resposta** (os botões ficavam na área de arrastar a janela).
- Instalação nova vinha com abas e dados de uso anterior.

## [2.1.0] — 2026-10-19

Rodada de ajustes a partir dos testes do operador.

### Adicionado
- **Informações na tela configuráveis** (inspetor › Telas): bloco atual, tempo do bloco e relógio, embaixo (padrão) ou em cima — vale para a janela do talento e as telas do prompter.
- **Voz:** tecla **V** liga/desliga o acompanhamento (remapeável, atalho global e comando `voice` no controle web); ajustes **Suavidade** (tempo para alcançar a palavra) e **Exigência** (semelhança mínima); **Antecipação** (quanto o texto sobe antes de terminar a linha).
- **Nome do apresentador no editor**, acima de cada fala, como no prompter.
- **Diálogos no visual do app** (confirmação, erro, renomear), no lugar das caixas do Windows.
- **Automação por linha de comando:** `--cmd=play|faster|nextBm|gotoBm:N|voice…`, `--talent`, `--snap=arquivo.png` (captura da janela para suporte).
- Ferramentas de verificação: `NdiProbe --medir N` (regularidade e quadros sem texto) e `VoiceTest --quiet/--degrade` (precisão com microfone fraco/ruído).

### Alterado
- **Velocidade inicial** de 1,11 para 0,55 linha/s (≈ 165 palavras/min) e faixa 0,2–2,5 linhas/s; preferências e roteiros com o padrão antigo são migrados (velocidades escolhidas pelo usuário não mudam).
- **Aceleração e frenagem suaves:** a velocidade desliza até o novo valor (≈ 0,2 s), inclusive no play e ao inverter.
- **Leitura por voz contínua:** o texto sobe conforme a linha é lida e o próximo parágrafo já chega à seta; título de marcador e pausa são pulados.
- **Reconhecimento de voz:** filtro passa-altas, ganho automático, gate suave e vocabulário em janela em volta da posição de leitura.
- **Aviso de velocidade** no formato do protótipo (pílula compacta), só para o operador (editor e janela do talento) — não aparece nas telas do prompter.
- **Inspetor:** rótulos sem corte (colunas automáticas), inspetor mais largo, botões dos marcadores aparecem ao passar o mouse, caixas de seleção quebram a linha.
- **Texto mais nítido** no editor e nas telas (modo de renderização certo para texto escalado).
- O app começa **limpo** (roteiro vazio) e em **janela normal** (não maximizada); exemplos ficam em Documentos\ARCA Teleprompter\Exemplos.

### Corrigido
- **NDI sem texto** (só a seta) e fundo "transparente" saindo preto.
- **NDI piscando** ao mudar a velocidade.
- **Monitor web/iPad travando** (degraus na troca de bloco e variação do Wi-Fi).
- **Voz subindo e descendo sozinha** (palavras repetidas e ressincronia); o texto agora só avança.
- **Apresentador aplicado ao texto inteiro** ao selecionar só um trecho.
- **Texto não chegava à seta** depois de colar conteúdo formatado.
- Próxima/anterior linha e salto de marcador paravam no **título do marcador ou no nome do apresentador**.
- **Marcadores do roteiro anterior** apareciam num roteiro novo.
- **"Fim previsto"** corria com o relógio mesmo parado.
- **Cursor de edição invisível** no editor.
- **Câmera** cortada e com faixa preta.
- Lista de formato NDI mostrando "NdiFormat { Id = … }".
- Formato "pílula" achatado (raio de canto no WPF).
- **Instalador:** falha ao instalar para todos os usuários sem pedir administrador; falha quando o app ou um desinstalador antigo estava aberto (agora fecha e substitui arquivos em uso); desinstalador deixava arquivos e mostrava "Erro inesperado".

### Desempenho
- **NDI:** texto renderizado em faixas + relógio próprio de envio + movimento em subpixel — 29,97 qps exatos, ≈ 2 ms por quadro (antes ≈ 24 ms e irregular).
- **Web:** desvio de velocidade medido no navegador de 43 para 1,2 unidade/s.
- **Voz:** leitura com microfone fraco (−24 dB) passou de nenhuma palavra reconhecida para 48 de 52 seguidas.

## [2.0.0] — 2026-10-19

Primeira versão nativa (substitui o protótipo React/Electron).

### Adicionado
- Aplicativo Windows em C#/.NET 8 + WPF, com bibliotecas separadas: Arca.Core, Arca.Import, Arca.Audio, Arca.Ndi, Arca.Server, Arca.Mos.
- Editor de roteiro com marcadores, pausas, apresentadores, trechos pulados, formatação, histórico de versões e perfis.
- Prompter com rolagem suave, contagem regressiva, linha de leitura, indicador e overlay; saídas em monitores e janela do talento.
- Monitor web e PWA para iPad (HTTP 8420 / HTTPS 8421, modos limpo e controle, QR code, senha).
- Saída NDI 1080p/720p com alfa e tally.
- Acompanhamento por voz offline (Vosk) com WASAPI e ASIO (canal), arquivo e simulação.
- MOS 2.8 (running order) e legendas (vMix/TCP).
- Importação DOCX, PDF, HTML, RTF, TXT; impressão e PDF.
- Instalador e desinstalador próprios; documentação de engenharia em `docs/`.

[2.7.0]: #270--2026-10-23
[2.6.2]: #262--2026-10-23
[2.6.1]: #261--2026-10-23
[2.6.0]: #260--2026-10-23
[2.5.1]: #251--2026-10-23
[2.5.0]: #250--2026-10-23
[2.4.0]: #240--2026-10-22
[2.3.0]: #230--2026-10-22
[2.2.0]: #220--2026-10-19
[2.1.0]: #210--2026-10-19
[2.0.0]: #200--2026-10-19
