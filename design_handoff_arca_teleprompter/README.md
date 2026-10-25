# Handoff: ARCA | Teleprompter (Windows)

Aplicativo de teleprompter para Windows, para estúdio e jornalismo. As referências funcionais são o **PowerPrompter** (Suborbital), para o editor e a janela do talento, e o **Autoscript**, para o fluxo de rundown/newsroom, saídas de rede e operação ao vivo.

---

## 1. Sobre os arquivos de design

`Teleprompter.dc.html` é um **protótipo de referência em HTML**: mostra visual, layout e comportamento pretendidos. **Não é código de produção.** A tarefa é **recriar este design como app desktop Windows**, usando a stack recomendada abaixo ou a que o time preferir, com os padrões dessa stack.

Para abrir o protótipo: abra `Teleprompter.dc.html` no Chrome ou Edge, com `support.js` na mesma pasta. O protótipo é um único componente. A lógica fica no `<script data-dc-script>`, na classe `Component` (estado React-like + `renderVals()`), e o template HTML com estilos inline fica dentro de `<x-dc>`.

## 2. Fidelidade

**Alta fidelidade (hi-fi).** Cores, tipografia, espaçamentos, raios, estados e interações são finais. Recrie a UI fielmente.

Várias integrações são **simuladas** no protótipo e precisam de implementação real (ver §9).

## 3. Stack recomendada

- **Electron + React + TypeScript** (Vite). Motivos: multi-janela nativa (editor + N janelas de prompter em monitores diferentes), acesso a `screen` para detectar monitores, módulos nativos para NDI e servidor HTTP/WebSocket embutido para o monitor web.
- Alternativas válidas:
  - **Tauri 2** (Rust): mais leve; NDI via bindings Rust.
  - **WPF/.NET 8**: NDI via NewTek.NDI .NET.
- Renderização da rolagem: um `<div>` com `transform: translate3d` dirigido por `requestAnimationFrame` (como no protótipo) é suficiente a 60 fps. Para a saída NDI, renderize a janela de prompter em canvas offscreen (`webContents.beginFrameSubscription` no Electron, ou `OffscreenCanvas`) e envie os frames ao NDI SDK.

## 4. Telas / janelas

### 4.1 Janela principal (editor)

Layout vertical, do topo para baixo:

1. **Barra de título/menu (36px)**
   - Esquerda: marca "ARCA" (`--f-brand`, 15px, letter-spacing 3.5px), um divisor de 1px × 16px, "Teleprompter" (600, 12.5px) e o nome do documento (12px, `--t-muted`).
   - Menus: Arquivo, Editar, Formatar, Marcadores, Prompter, Exibir, Janela, Ajuda.
   - Os dropdowns têm `min-width:270px` e `width:max-content`, padding 6px, raio 12px, `--t-sh2`; os itens têm 30px de altura e raio 8px, com check à esquerda e atalho à direita em `--faint`.
2. **Toolbar (50px)**, fundo `--t-ui`, borda inferior `--t-border`:
   - Botões de formatação (32×30, raio 980px), com rolagem horizontal sem scrollbar quando falta espaço.
   - Seletor **Apresentador** (pílula com bolinha de cor, nome e chevron; abre lista com Alt+N).
   - Botão **Bloquear/Editável** (estados "Editável" / "Bloqueado" / "Bloqueado · no ar").
   - Botão primário **Prompt** (`--t-accent`).
3. **Corpo**: um grid `clamp(190px,20%,230px) | minmax(0,1fr) | clamp(272px,30%,320px)`.
   - **Coluna esquerda – Marcadores / Rundown** (fundo `--panel`, borda direita):
     - Cabeçalho "MARCADORES" (eyebrow: Sora 10px, letter-spacing 1.5px, uppercase, `--t-faint`) e um botão "+".
     - Cada linha (mín. 38px, raio 9px, ativa com `--t-accent-soft`) tem:
       - grid `14px | 1fr | 22px | 22px`;
       - nº do atalho (1–9) e o nome;
       - uma sublinha com posição · tempo estimado · tempo de ensaio ("E 0:42");
       - um botão olho (pular bloco) e um × (excluir).
     - As linhas são arrastáveis para reordenar; uma linha de 2px `--t-accent` indica onde o bloco vai cair.
     - Seção **Pausas**: mesmas linhas, com ícone de pausa.
     - Rodapé compacto:
       - "Programa" com input mm:ss (52px) e a diferença (−mm:ss em `--t-ok`, ou +mm:ss em `#e5484d`);
       - checkbox **Modo ensaio**, com "Zerar";
       - "N palavras · est. mm:ss".
   - **Centro – Editor WYSIWYG**:
     - Barra de **abas de roteiro** (38px, abas de 28px com raio 8px; a ativa usa `--raised` + `--shadow`, um ponto `--t-accent` indica alterações não salvas, × fecha, + cria).
     - Abaixo, a área do editor com o mesmo fundo e a mesma fonte do prompter, escalada de um "espaço de 1000 unidades" (ver §6.1).
     - Sobreposições do editor: indicador de leitura (seta), overlay, barra "Editor bloqueado durante a reprodução — pause para editar" com o botão "Pausar e editar", HUD de velocidade e contagem regressiva.
   - **Coluna direita – Inspetor**, com abas segmentadas: **Roteiro · Overlay · Vídeo · Telas · Voz**.
     - **Roteiro**:
       - Prévia do prompter (16:9).
       - Fundo / Texto: 4 presets, mais o poço "Outra cor…" (tracejado com +, abre o seletor de cor nativo) e um input HEX de 84px.
       - Fonte (Atkinson Hyperlegible, Source Sans 3, Source Serif 4, IBM Plex Mono) e alinhamento (esquerda, centro, direita).
       - Palavras por linha (2–12, passo 0,25), **Margem** (0–400), **Posição** da margem (−1 = Esq., 0 = Centro, +1 = Dir.; padrão Esq., duplo clique reseta), entrelinha (1.0–2.0).
       - Tempo total automático: hh:mm:ss ou mm:ss → calcula a velocidade para terminar no tempo.
       - Velocidade mín./máx. e unidade (linhas/s, linhas/min, palavras/s, palavras/min).
       - Seção **Apresentadores**: lista (cor, nome, Alt+N, ×), "Adicionar" (máx. 8), checkbox "Mostrar o nome no início de cada fala" e "Destacar" (chips Todas / um por apresentador).
     - **Overlay**:
       - Tipo: faixa, fade, destaque ou nenhum, com opacidade 0–100%. Com "nenhum" não há guia no editor.
       - Indicador: seta, quadrado, ponto, linha ou nenhum; tamanho P/M/G; cor (presets + espectro + HEX); posição da linha de leitura (10–70%).
       - Linha de leitura e imagem de fundo (PNG/JPG/WebP/**SVG**; SVG como data URL utf-8) com opacidade.
     - **Vídeo**: câmera, gravação junto com o prompt, gravação/exportação.
     - **Telas**:
       - Lista de janelas de prompter (monitor, espelhamento H/V, alinhamento) e "Nova janela de prompter".
       - **Saída NDI**: toggle, status, nome da fonte, formato (1080p30/60/25, 720p30), opções "Espelhar" e "Fundo transparente (alpha)".
       - **Monitor web**: toggle, status, URL com "Copiar", senha opcional, "Permitir controlar pelo navegador" e "Abrir monitor web".
     - **Voz**: acompanhamento por voz (estados DESLIGADO, OUVINDO, SEGUINDO, IMPROVISO, RESSINC.), driver (WASAPI/ASIO), canal, gate em dB, janela e tolerância de palavras, idioma.
4. **Barra de transporte/leituras** (embaixo do editor):
   - Play/pausa (52×36) e linha anterior/próxima.
   - Slider de velocidade.
   - Leituras: Posição, Restante, Duração, Relógio, Fim previsto, Linha N/M.
5. **Barra de status**: palavras, voz, NDI/Web/MOS ativos, "Salvo automaticamente às hh:mm" e a última tecla.

### 4.1.1 Barra do talento
Na barra flutuante da janela do talento, os botões ficam nesta ordem:
- parágrafo anterior (seta dupla para cima);
- linha anterior;
- próxima linha;
- próximo parágrafo (seta dupla para baixo);
- play;
- voltar ao início;
- slider e leitura da velocidade.

### 4.2 Janela do talento (prompter)

- Ocupa a tela inteira. O texto é escalado do espaço de 1000 unidades para a largura da janela (`container-type:inline-size`, unidades `cqw`).
- **Espelhamento**: a janela do talento no monitor do apresentador é **normal (não espelhada)**. Só as saídas para o vidro do prompter (janelas em outros monitores, NDI) aplicam espelho H/V, configurável por tela, como no PowerPrompter.
- **HUD superior** (`top:60px`, `pointer-events:none`, pílulas `rgba(0,0,0,.62)` com raio 12px):
  - à esquerda, o badge "ÚLTIMO BLOCO" (`#e5484d`) + o nome do bloco + o tempo restante do bloco (vermelho abaixo de 10 s);
  - à direita, o relógio.
  - Liga em Exibir → "Informações na tela do talento".
- **Barra de controle flutuante** (embaixo, centralizada, `rgba(30,30,30,.92)`, raio 12px, botões de 36px; some quando o texto está rolando):
  - parágrafo anterior · linha anterior · próxima linha · próximo parágrafo · play · voltar ao início · slider de velocidade · leitura da velocidade.
- **HUD de velocidade**: pílula compacta em uma linha, centrada em `bottom:76px`, com:
  - badge "Frente" ou "Ré" (Ré em `#e5484d`, com seta);
  - valor + unidade (Sora 600, 13px);
  - barra de 72×3px.
  - Some em 1.1 s.
- **Contagem regressiva**: círculo de 160px no centro (3-2-1), opcional.
- Duplo clique liga a **edição inline**, só quando pausado.

### 4.3 Diálogos

Todos com cabeçalho e título, raio 14px e `--t-sh3`.

- **Preferências**:
  - abas Geral, Atalhos, Dispositivos, Integrações;
  - em Atalhos, cada ação tem a tecla capturável e há "Restaurar padrão" + um texto-resumo dos gestos;
  - em Integrações:
    - Newsroom MOS: sistema (ENPS, Avid iNEWS, Octopus, Dalet, Ross Inception), servidor, ID MOS, Conectar;
    - Estúdio: closed caption.
- **Localizar e substituir** (Ctrl+F).
- **Novo roteiro** (confirmação).
- **Histórico de versões** (lista com data · tipo · palavras e "Restaurar", que abre em aba nova).
- **Perfis de apresentador** (salvar atual com nome, aplicar, excluir).
- **Monitor web** (760px): barra de URL, prévia ao vivo 16:9 com o tag "AO VIVO · MONITOR"/"NO AR", Bloco/Posição/Velocidade e Play se o controle estiver permitido.
- **Controle remoto**.
- **Sobre**.

## 5. Interações e comportamento

### 5.1 Rolagem e velocidade

- `lps` (linhas/s) = `minS + speedT·(maxS−minS)`, com `speedT ∈ [0,1]`; padrão `minS=0.3`, `maxS=3`, `speedT=0.3`.
- A rolagem é `y += dir · lps · lineHeightPx · dt`. Ao cruzar um `[data-stop]` (pausa), a rolagem para nele. No fim (ou no início, em ré), ela pausa.
- Ajuste de velocidade (`adjSpeed`): passo 0.05 (0.15 com Shift). Abaixo de 0 a direção **inverte** (ré), e vice-versa.
- Presets: teclas 1–9 → `speedT = (n−1)/8`.
- **Roda do mouse**:
  - pausado, rola o texto;
  - rodando, ajusta a velocidade (±0.03, com throttle de 60 ms);
  - **Ctrl+roda** muda as palavras por linha.
- **Arrastar** na janela do talento (pausado) rola o texto à mão.
- "Velocidade travada" (`speedLock`) bloqueia os ajustes na janela do talento.

### 5.2 Edição × reprodução

- **Durante a reprodução ou contagem, o editor fica sempre bloqueado** (`contentEditable=false`).
- Só é possível editar pausando:
  - pelo botão "Pausar e editar";
  - pela tecla E na janela do talento, que pausa, fecha o talento e posiciona o cursor no parágrafo atual;
  - pelo duplo clique inline, quando pausado.
- O botão Editável/Bloqueado só alterna quando está pausado; durante a reprodução mostra o aviso "Pause o prompt para editar".

### 5.3 Marcadores, pausas, blocos (rundown)

- **Marcador** = parágrafo `[data-bm]`; inicia um bloco. **Pausa** = `[data-stop]`.
- Inserir: Ctrl+K (marcador) e menu Marcadores. Excluir: o × da linha, botão direito, ou "Excluir marcador atual" / "Excluir todas as pausas".
- No import de texto, uma linha começando com `*` vira marcador e `[PAUSA]` vira pausa.
- **Pular bloco**: `data-skip` no marcador. Os parágrafos seguintes, até o próximo marcador, recebem `data-skipped`: ficam ocultos no prompter e riscados (opacidade .32) no editor.
- **Tempo estimado por bloco** = palavras ÷ (lps · palavras_por_linha). O total soma só os blocos não pulados e é comparado com a "Duração do programa", mostrando sobra ou estouro.
- **Modo ensaio**: enquanto rola, soma 0.5 s a cada 500 ms no bloco atual.
- **Reordenar**: arrastar move o marcador e todo o seu conteúdo para antes do bloco de destino.

### 5.4 Apresentadores

- `data-who="N"` (1–8) no parágrafo. Aplicar com Alt+N, o seletor da toolbar ou Formatar → nome; Alt+0 remove.
- A cor de cada apresentador fica em uma CSS var `--whoN`; o nome fica em `--whoN-name`, como string CSS.
- O primeiro parágrafo de cada sequência do mesmo apresentador recebe `data-who-start`. Nele aparece o nome (Plex Mono, .3em, uppercase), posicionado **acima** do parágrafo (`position:absolute; bottom:100%`) com `margin-top:.55em`, **para não deslocar a linha de leitura**.
- "Destacar só falas de N" deixa os demais parágrafos com opacidade .22 no prompter.
- Ao remover um apresentador, as falas dele ficam sem apresentador e os índices seguintes são renumerados.

### 5.5 Indicador e margem

- O tamanho do indicador é `k = fontSize · 0.028 · [0.65, 1, 1.45][P|M|G]`.
- Reserva do indicador: `ind = (0.4k + largura·k)·10 + 14` (ou 24 sem seta/linha), de modo que **o texto nunca fica sob a seta**.
- Margem distribuída pela posição `p`:
  - esquerda efetiva = `ind + margem·(1+p)/2`;
  - direita efetiva = `24 + margem·(1−p)/2`.
  - Com p = −1 (padrão), a margem toda vai para a direita e o início do texto não se move.
- A fonte é recalculada para manter as palavras por linha:
  `fs = (1000 − esq − dir) / (wpl · 3.05 · fatorFonte)`.
- Fatores de fonte: atk 1, sans 1.1, serif 1.02, mono 0.82.

### 5.6 Documentos, abas, salvamento

- **Abas**: vários roteiros abertos. Ao trocar de aba, o HTML e a posição y da aba atual são salvos. Fechar uma aba com alterações pede confirmação e guarda uma versão.
- **Autosave**: debounce de 1.2 s no `localStorage` (`arca-prompter-docs`); no app real, use um arquivo em `%APPDATA%/ARCA Prompter/`. A sessão é restaurada ao abrir.
- **Versões**: criadas ao salvar, a cada 2 min de edição e ao fechar uma aba sem salvar. Máximo de 25 (`arca-prompter-versions`).
- Formato **.tps** (JSON):

  ```json
  { "app": "ARCA Prompter", "v": 1, "html": "<p>…</p>",
    "settings": { "wpl":5, "margin":0, "mPos":-1, "lineH":1.3, "fontKey":"atk", "bg":"#000000", "fg":"#ffffff",
      "align":"left", "readPct":30, "ovType":"faixa", "indType":"seta", "speedT":0.3, "minS":0.3, "maxS":3,
      "pres":[{"name":"Âncora","color":"#ffd60a"}], "progSec":600 } }
  ```

- Importar: .tps/.json, .txt, .html e **.docx** (lido de `word/document.xml` via ZIP + DecompressionStream; no app real, use uma lib como `mammoth`). **PDF** ainda não; sugestão: `pdfjs-dist`.
- Exportar: .txt; **Imprimir/PDF** (Ctrl+P) abre um documento formatado com os blocos e tempos, os nomes dos apresentadores, as pausas e os blocos pulados riscados. No Electron, use `webContents.printToPDF`.
- **Perfis de apresentador**: `{name, settings}` salvos em `arca-prompter-profiles`.

### 5.7 Atalhos (padrão, remapeáveis)

| Ação | Tecla |
|---|---|
| Play / pausa | Espaço (Ctrl+Enter no editor) |
| Mais rápido / mais devagar (pausado: próxima/anterior linha) | ↓ / ↑ (Shift = passo maior) |
| Mais rápido / devagar (alternativo) | W / S |
| Inverter direção | R |
| Parar | 0 |
| Velocidade predefinida | 1–9 |
| Início / fim | Home (ou Backspace) / End |
| Próximo / anterior marcador | PageDown / PageUp (clickers) |
| Próximo / anterior parágrafo | ] / [ |
| Editar trecho atual | E |
| Espelhar tela do prompter | M |
| Janela do talento | F5 |
| Ir para marcador N | Ctrl+1…9 |
| Inserir marcador | Ctrl+K |
| Bloquear editor | Ctrl+E |
| Localizar | Ctrl+F |
| Novo / Abrir / Salvar / Fechar aba / Imprimir | Ctrl+N / O / S / W / P |
| Apresentador N / remover | Alt+1…8 / Alt+0 |
| Preferências | Ctrl+, |

## 6. Estado (principais variáveis)

- **Reprodução**: `playing`, `count` (contagem), `dir` (±1), `speedT`, `minS`, `maxS`, `unit`, `speedLock`, `follow`, `locked`, `y` (fora do estado, no loop rAF).
- **Layout do texto**: `wpl`, `margin`, `mPos`, `lineH`, `fontKey`, `bg`, `fg`, `align`, `readPct`, `ovType`, `ovOp`, `indType`, `indSize`, `indColor`, `bgImg`, `bgImgOp`.
- **Telas**: `displays[]` com `{ id, name, monitor, flipH, flipV, align, find }`, `talent`, `talentHud`.
- **Documento**: `docs[]` com `{ id, name, html, y, dirty }`, `active`, `docName`, `dirty`, `versions[]`, `savedAt`.
- **Rundown**: `bms[]` `{name, y}`, `stops[]`, `cur`, `blocks[]` `{words, skip}`, `progSec`, `rehearse`, `reh{idx:seg}`.
- **Apresentadores**: `pres[]` `{name, color}`, `presNames`, `presOnly`, `curWho`; `profiles[]`.
- **Rede**: `ndiOn`, `ndiName`, `ndiFmt`, `ndiMirror`, `ndiAlpha`, `webOn`, `webPass`, `webCtrl`, `webViewers`, `mosSys`, `mosHost`, `mosId`, `mosOn`, `ccOut`.
- **Voz**: `vMode`, `vState`, `driver`, `asioCh`, `gate`, `win`, `tol`, `vLang`.
- **Atalhos**: `keymap` (ação → tecla).

### 6.1 Espaço de 1000 unidades

Todo o conteúdo do prompter (editor, prévia, talento, monitor web, NDI) é diagramado num contêiner de **1000 px de largura** (`box-sizing:border-box`) e escalado com `transform: scale(larguraReal/1000)`. Isso garante quebra de linha **idêntica** em todas as saídas. A posição `y` é medida nesse espaço.

## 7. Design tokens

**Tema claro** (`[data-theme]`):

- **Superfícies**: bg `#f2f2f0`, ui `#ffffff`, soft `#f7f7f5`, sunk `#eeeeec`.
- **Bordas**: border `#e2e2de`, hair `#ebebeb`.
- **Texto**: text `#1a1a18`, muted `#5a5a55`, faint `#67675f`.
- **Accent**: accent `#d42b1e`, accent-ink `#ffffff`, accent-soft `rgba(212,43,30,.08)`, accent-line `rgba(212,43,30,.25)`.
- **Sucesso**: ok `#2f8f5b`, ok-soft `#e9f5ee`.
- **Sombras**:
  - sh1 `0 1px 2px rgba(26,26,24,.05)`;
  - sh2 `0 4px 16px rgba(26,26,24,.08)`;
  - sh3 `0 18px 50px rgba(26,26,24,.16)`.

**Tema escuro** (`[data-theme=dark]`):

- **Superfícies**: bg `#0e0e10`, ui `#1b1b1e`, soft `#26262a`, sunk `#141416`.
- **Bordas**: border `#34343a`, hair `#2b2b30`.
- **Texto**: text `#ececef`, muted `#a0a0a8`, faint `#91919a`.
- **Accent**: accent `#ff5a4a`, accent-ink `#1a0705`, accent-soft `rgba(255,90,74,.12)`, accent-line `rgba(255,90,74,.3)`.
- **Sucesso**: ok `#3ec27f`, ok-soft `#0f3d2e`.
- **Sombras**:
  - sh1 `0 1px 2px rgba(0,0,0,.4)`;
  - sh2 `0 4px 16px rgba(0,0,0,.45)`;
  - sh3 `0 18px 50px rgba(0,0,0,.6)`.

**Outras cores**:

- **Perigo/no ar**: `#e5484d`.
- **Cores padrão dos apresentadores**: `#ffd60a`, `#5ac8fa`, `#ff9f0a`, `#a2e05c`, `#ff6b9d`, `#bf9bff`, `#4cd9c4`, `#ff8a65`.
- **Presets de fundo**: `#000000`, `#1c1c1c`, `#f4f1ea`, `#ffffff`.
- **Presets de texto**: `#ffffff`, `#ffe14a`, `#d9d9d9`, `#111111`.
- **Presets do indicador**: `#ffffff`, `#ffd54a`, `#ff6b6b`, `#6aa6dc`.

**Tipografia**:

- **Famílias**:
  - `--f-brand`: GC Epic Pro → Sora (marca);
  - `--f-sans`: Raleway (UI, 13px base);
  - `--f-disp` / `--f-num`: Sora (números, eyebrows, leituras).
- **Fontes do roteiro**: Atkinson Hyperlegible (padrão), Source Sans 3, Source Serif 4, IBM Plex Mono.
- **Escala da UI**: eyebrow 10px/1.5px uppercase · rótulos 11–12px · corpo 13px · títulos de diálogo 15px/600 · leituras 14–15px Sora 600.

**Raios**: pílula 980px · diálogos 14px · menus/cartões 12px · campos 10px · itens de lista 8–9px · swatches 6px.

**Espaçamentos**: base de 4px. Padding de painel 16px, gaps de 6/8/10/12/16px, altura de controles 24/28/30/32/36px.

**Swatches de cor**: 24×24 (raio 6, borda interna `rgba(128,128,128,.45)`, anel de seleção `0 0 0 2px panel, 0 0 0 3.5px text`); o poço "Outra cor…" é tracejado com +, e o input HEX tem 84px.

## 8. Assets

- **Fontes**: Google Fonts (Raleway, Sora, Atkinson Hyperlegible, Source Sans 3, Source Serif 4, IBM Plex Mono). Empacote-as localmente no app (offline).
- **GC Epic Pro** (marca): o arquivo `fonts/gc-epic-pro.ttf` **não está no pacote**; o protótipo cai para Sora. Obtenha a licença/arquivo com o cliente.
- **Ícones**: SVG inline, traço de 1.4–1.7px em viewBox 16×16 (play, pause, stop, chevrons, lock/unlock, olho, ×, +, alinhamento). Podem ser trocados por uma biblioteca de ícones de traço equivalente (ex.: Lucide).

## 9. O que é simulado no protótipo e precisa de implementação real

1. **Saída NDI**: renderizar a janela de prompter offscreen e enviar via NDI SDK (fonte nomeada, formato escolhido, espelho e canal alpha). No protótipo, o status "recebida por vMix" é falso.
2. **Monitor web**:
   - servidor HTTP + WebSocket local (porta 8420), com página `/monitor` que espelha o prompter (o HTML + `y` + configurações via WS);
   - senha opcional;
   - comandos play/pausa/velocidade, se `webCtrl` estiver ligado;
   - contagem de conexões.
3. **MOS (newsroom)**: protocolo MOS 2.8/3.x (TCP 10540/10541) para receber rundowns e stories de ENPS, iNEWS, Octopus, Dalet e Ross. Cada story vira um bloco (marcador + texto).
4. **Closed caption**: enviar o texto da linha de leitura para o encoder de legenda.
5. **Acompanhamento por voz**: ASR em streaming (ex.: Whisper local, Azure Speech ou Vosk) e alinhamento das palavras reconhecidas com o roteiro (janela `win`, tolerância `tol`), com os estados OUVINDO → SEGUINDO → IMPROVISO → RESSINC.
6. **Multi-monitor**: enumerar monitores e abrir uma janela de prompter por monitor (fullscreen, sem moldura), cada uma com seu espelho H/V.
7. **Câmera e gravação**: getUserMedia + MediaRecorder (já funcional no protótipo, com permissão).
8. **Importar PDF**.
9. **Persistência**: trocar o `localStorage` por arquivos em `%APPDATA%`, e associar a extensão `.tps` ao app.

**Fora do escopo** (decisão do cliente): pedal/controles HID e tally/GPI.

## 9.1 Instalador e desinstalador (`Instalador.dc.html`)
Janela de 760×500, raio 10px. Barra lateral escura de 232px (`#141416`), com a marca, a lista de passos (círculos de 22px: atual com anel `#ff5a4a`, concluído preenchido com ✓) e a versão. À direita, o conteúdo: eyebrow `#d42b1e`, título Sora 21px/600 e rodapé de 60px com Voltar · Cancelar · ação primária.

- **Instalação** (6 passos):
  1. Boas-vindas: versão, requisitos e espaço.
  2. Licença: o botão Avançar só libera com o aceite.
  3. Local: pasta validada por `^[A-Z]:\\`, "Procurar…", escopo Todos os usuários (Program Files) ou Somente para mim (`%LOCALAPPDATA%\Programs`), e espaço necessário/disponível/unidade.
  4. Componentes: App (obrigatório, 186 MB), NDI Runtime 48, Monitor web 6, Voz pt-BR 64, Fontes 14, Exemplos 1. Atalhos: Menu Iniciar, Área de trabalho, associação .tps.
  5. Instalando: barra, tarefa atual, arquivo e log. As tarefas variam conforme os componentes (NDI, regra de firewall da porta 8420, modelo de voz, fontes, HKCR\.tps, atalhos, registro do desinstalador).
  6. Concluído: "Abrir agora" e "Ver novidades".
- **Desinstalação** (4 passos):
  1. Confirmar: manter roteiros/versões, preferências, perfis ou registros/cache, mais o aviso "app aberto → Fechar agora".
  2. Motivo: chips + comentário opcional.
  3. Removendo: as tarefas variam conforme o que é mantido em `%APPDATA%\ARCA Teleprompter`.
  4. Concluído.
- **Cancelar / ×**: abre uma confirmação. Durante a cópia, cancelar reverte a instalação.
- **Implementação sugerida**: electron-builder + NSIS com páginas customizadas (ou um WiX/MSI com UI própria), reproduzindo este layout. Desinstalador registrado em "Aplicativos instalados".

## 10. Arquivos

- `Teleprompter.dc.html`: protótipo completo (fonte de verdade para visual e comportamento). Funções-chave na classe `Component`:
  - rolagem e pausas: `frame`, `setY`, `syncUi`;
  - velocidade e gestos: `adjSpeed`, `flashSpeed`, `onWheelCtl`, `onPtrDown`/`Move`/`Up`;
  - atalhos: `key`, `doAction`;
  - navegação: `jumpPara`, `jumpBm`;
  - blocos e marcadores: `pushHtml` (pulos, blocos, início de fala), `moveBlock`, `toggleSkip`, `removeBlock`;
  - apresentadores: `setWho`, `removePres`;
  - documentos e versões: `openInTab`, `switchTab`, `closeTab`, `scheduleSave`, `addVersion`;
  - importação, exportação e impressão: `readDocx`, `printScript`, `saveDoc`, `onOpenFile`, `exportTxt`;
  - margem e fonte: `prM` (esquerda), `prR` (direita), `fs`;
  - rede: `toggleNdi`, `toggleWeb`.
- `Instalador.dc.html`: telas de instalação e desinstalação (alternar no topo).
- `support.js`: runtime necessário para abrir o protótipo no navegador (não faz parte do produto).
