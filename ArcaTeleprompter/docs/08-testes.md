# 08 — Plano e resultados de testes

## Testes automatizados (`tests/Arca.Core.Tests`, xUnit)

| ID | Cobertura |
|---|---|
| T-HTML | `HtmlScript` ida e volta: marcadores, pausas, apresentadores, pulados, formatação |
| T-TPS | `.tps` v1 (protótipo) e v2; configurações |
| T-TXT | Texto puro → marcadores (`*`) e pausas (`[PAUSA]`) |
| T-RUN | Rundown, mover bloco, remover apresentador |
| T-LAY | `PrompterLayout` (palavras por linha → fonte) |
| T-SCR | `ScrollEngine`: play, pausas, fim, reverso cruzando zero, duração-alvo |
| T-VOZ | `VoiceAligner`: seguir, pular palavras curtas, improviso e ressincronia, salto falso ("gente"/"vinte") |
| T-UTL | Normalização, similaridade, formatação de tempo |
| T-MOS | `MosDevice.Process` (roCreate, roStorySend, roElementAction) e TCP UTF-16BE ponta a ponta |

Executar: `dotnet test tests/Arca.Core.Tests`. Também roda no passo 1 do `build.ps1`; falha interrompe o build.

## Ferramentas de verificação (`tools/`)

| Ferramenta | Verifica |
|---|---|
| `NdiTest` | Envio NDI sintético |
| `NdiProbe` | Receptor NDI independente: encontra a fonte do ARCA e grava um quadro PNG; `--medir N` (regularidade); `--movimento N [csv]` (quanto o texto anda por quadro: saltos, tremor, paradas, recuos) |
| `VoiceTest` | WAV → Vosk ou Whisper (`--engine`) → alinhador, palavra a palavra; `--ref=longa.wav` mede cobertura e latência por palavra; `--degrade` gera microfone fraco/ruidoso/com eco |
| `ServerTest` | Cliente WebSocket: autenticação, html/state/y, comandos |
| `MakeSamples` | Gera os roteiros de exemplo do instalador |
| `SetupTest` | Instalador sobre uma "instalação anterior" numa pasta temporária: arquivo preso o tempo todo (falha com o nome do programa e devolve a pasta byte a byte) e preso por 2 s (espera e instala) |

## Procedimentos manuais

| ID | Procedimento | Resultado esperado |
|---|---|---|
| P-INI | Apagar `session.json` e abrir o app | Aba "Sem título.tps" vazia, lista de marcadores vazia, janela normal (não maximizada) ocupando ~90% da tela |
| P-EDT | Criar marcadores, depois **Novo** | O novo roteiro não herda marcadores nem pausas do anterior |
| P-PLAY | Play/pausa | "Fim previsto" só mostra horário durante a rolagem; parado mostra `--:--:--` |
| P-LIN | Próxima/anterior linha e marcador perto de um marcador com apresentador | A seta para só em linhas de texto; título do marcador, nome do apresentador e pausa são pulados |
| P-CAR | Clicar no editor | Cursor visível (cor de destaque, 3 px, piscando) |
| P-CAM | Ligar a câmera | Imagem inteira, proporcional, sem faixa preta |
| P-EDT2 | Rolar o editor até o topo com a roda | Sem tremulação |
| P-TEL | Saída em 2º monitor espelhada | Texto igual ao operador, mesmas quebras |
| P-WEB | iPad: modo limpo e modo controle | Limpo sem contadores; controle comanda o app |
| P-NDI | NDI ligado + `NdiProbe` | Quadro 1920×1080 recebido |
| P-VOZ | Arquivo de leitura e microfone | Seta acompanha a palavra na linha de leitura |
| P-BAR | Janela do talento, Telas › Nova janela de prompter (mexer o mouse sobre ela) e navegador no modo controle | Mesma barra nas três: parágrafo ↑, linha ↑/↓, parágrafo ↓, play, início, ré, −, velocidade, + |
| P-ROT | Navegador › ⚙ › Girar o texto 90° e 180° | Texto girado ocupando a tela; barras continuam de pé; arrastar acompanha a rotação |
| P-FS2 | Chrome/Edge (https ou localhost) › ⚙ › Tela cheia › Outro monitor… | Lista os monitores; o escolhido abre em tela cheia sem bordas |
| P-AUTO | `ArcaTeleprompter.exe --autoteste=r.txt` com `ARCA_DATA_DIR` de teste | "TUDO OK" (54 verificações: editor e transporte, todos os atalhos de teclado, os botões da barra de controle, a suavidade quadro a quadro e o tamanho da janela) |
| P-VOZ2 | Voz › Motor › Whisper · preciso, *Baixar modelo*, *Usar microfone* | Modelo baixado; registro "Whisper pronto: … Vulkan"; palavras aparecem com < 1 s de atraso |
| P-FNT | Abrir lista de fontes | Fontes do pacote + todas as do Windows |
| P-INS | Instalar em pasta personalizada, abrir, desinstalar | Arquivos na pasta escolhida; após desinstalar: pasta, registro, atalhos e associação removidos |

## Resultados (versão 2.4.0) — voz

| Áudio (mesma leitura de 31 s, 55 palavras do roteiro) | Vosk (gramática) | Whisper base (padrão) | Whisper small |
|---|---|---|---|
| `longa.wav` — limpo | 51/55 · 0,18 s | 54/55 · 0,68 s (p90 1,06) | 55/55 · 0,9–1,3 s |
| `fraca.wav` — microfone fraco (−24 dB + ruído) | 46/55 · 0,24 s | 54/55 · 0,65 s (p90 1,18) | 53–55/55 · 0,9–1,3 s |
| `sala.wav` — eco de sala (RT60 0,6 s) −12 dB + ruído | 38/55 · 0,27 s | 52/55 · 0,70 s (p90 1,22) | 50–52/55 · 1,0–1,1 s |
| `ruim.wav` — fala quase no nível do ruído | 6/55, perde-se na 8ª palavra | 28/55 · 0,64 s, segue até o fim | 33–36/55 · 0,8–0,9 s |

Whisper com prompt do roteiro na placa de vídeo (Vulkan); detalhes em [06](06-voz.md#vosk--whisper-24).

## Resultados (site, 27/10/2026) — LGPD: política, baixar dados e excluir conta

Servidor local e depois o publicado, 13 verificações, todas OK: baixar meus dados vem como arquivo para download, com cadastro, licença (computador PC-LGPD) e histórico (cadastro, ativação), sem o hash da senha; sem entrar não baixa nada; excluir com senha errada é recusado e a conta continua; com a senha, a conta é excluída e a sessão encerrada; não dá mais para entrar; o computador perde a licença na próxima conferência; o mesmo e-mail pode criar conta de novo (nada sobrou); `/privacidade` abre. Pela tela: senha → confirmação "Excluir sua conta?" → volta para a política com "Sua conta e todos os dados ligados a ela foram excluídos". Os 31 testes de conta repetidos depois: TUDO OK.

## Resultados (versão 2.9.0) — revisão de segurança

| Verificação | Resultado |
|---|---|
| Testes automatizados (xUnit) | 34/34 — 15 novos: cor só como cor (9 casos), assinatura de atualização aceita e recusada se versão, arquivo ou tamanho mudarem, sem assinatura, com outra chave ou malformada |
| Monitor web com senha (app de teste, porta 8430) | conexão com Origin de outro site → recusada (403); da própria página e sem Origin → aceitas; 4 senhas erradas → conexão segue; 5ª → encerrada pelo app; após 10 em 10 min → nem a senha certa entra |
| Atualização assinada (servidor local, ARCA_UPDATE_URL) | latest.json 9.9.9 assinado com a chave real → "versão 9.9.9 disponível"; o mesmo com o tamanho alterado em 1 byte → ignorado |
| Pastas do instalador (`tools/SetupTest`) | aceitas: Arquivos de Programas\ARCA Teleprompter, D:\Apps\…, AppData\Local\Programs\…; recusadas: C:\, C:\Windows, C:\Windows\System32, C:\Program Files, C:\ProgramData, C:\Users, a pasta do usuário, caminho relativo e caracteres inválidos; arquivo preso e reinstalação continuam OK |
| Servidor de licenças publicado | 31 testes de conta OK; login: e-mail com conta ~466 ms, sem conta ~460 ms (antes o sem-conta respondia sem calcular hash) |
| Autoteste do app | Rodado com o computador parado havia 44 min (monitor desligado pela economia de energia): os itens de movimento falham — a janela desenha ~3 quadros/s — e o **código da 2.8.8 falha igual** nas mesmas condições (20 quadros em 7 s). Os demais itens passam. Os de movimento passaram com a tela ligada nas versões anteriores e não dependem das mudanças desta versão |

## Resultados (versão 2.8.8) — Preferências e palavras por linha

Preferências abertas em cada aba (autoteste): Geral 520 · Atalhos 520 · Dispositivos 520 · Integrações 520 px (antes variava entre 400 e 560).

Palavras por linha no roteiro do usuário (teleprompterteste.tps, em inglês; ajuste 5,25, Atkinson, fonte 58), `--cmd=linhas`: editor, telas de prompter e janela do talento com as mesmas 29 linhas, quebradas nas mesmas palavras — média 5,07 palavras por linha, de 2 a 7 (linha com uma palavra longa, como "teleprompter", fica com menos). O ajuste define o tamanho da letra pela média; cada linha varia com o tamanho das palavras.

## Resultados (versão 2.8.7) — NDI a 60p e monitor web atualizado sozinho

**Diagnóstico no computador do usuário** (i5-1135G7, 4 núcleos/8 threads, na tomada): registro do ARCA com NDI 1080p60 e fundo transparente — 136–250 quadros a cada 5 s (meta 300), montagem 20–33 ms por quadro, quase todos atrasados (até 116 ms). Gravação das posições enviadas às telas enquanto o usuário arrastava no iPad: o texto andava em pulsos (salto grande, desliza ~0,4 s, para) — o padrão do monitor web antigo, que só mandava o arrasto ao soltar o dedo: o iPad ainda estava com o código anterior à 2.8.6.

**NDI 1080p60 com fundo transparente** (app de teste licenciado, prompt rodando, com o ARCA do usuário aberto ao lado):

| Montagem | Montagem por quadro | Quadros em 5 s (meta 300) | Atrasados |
|---|---|---|---|
| 1 núcleo (`ARCA_NDI_BANDS=1`, como antes) | 3,8–13 ms (máx 40) | 228–300 | 11–88 |
| Faixas paralelas | 1,2–1,7 ms (máx 4) | 300 · 300 · 300 | 1 · 0 · 0 |
| Faixas paralelas + janela do talento em tela cheia | 1,6–2,1 ms (máx 5) | 302 · 300 (após abrir a janela) | 3 · 0 |

**Recarga automática do monitor web**: página aberta e marcada; app reaberto sem mudança → reconectou, sem recarregar (marca mantida); app reaberto com um arquivo do monitor web alterado → a página recarregou sozinha ao reconectar e já com o arquivo novo.

Autoteste: as duas primeiras rodadas (logo depois de compilações, com o ARCA do usuário mandando NDI a 60p ao lado) falharam nos 3 itens de suavidade do editor (8 quadros acima de 50 ms); o código da 2.8.6 nas mesmas condições passou, e a 2.8.7 recompilada passou 2 de 2 — variação de carga do computador, não a mudança (que não toca no desenho do editor). Não testado: num iPad físico.

## Resultados (versão 2.8.6) — arrastar pelo iPad com o NDI acompanhando

App licenciado com NDI ligado (nome próprio "ARCA-TESTE-ARRASTO") e monitor web na porta 8430; arrasto de dedo simulado no navegador (toque, parado): 3 s subindo ~130 px/s, ~60 movimentos por segundo. Receptor NDI independente (`tools/NdiProbe --movimento 300`), 300 quadros a 30 qps:

| Janela | NDI |
|---|---|
| 5,0–7,5 s (durante o arrasto) | 238–248 px/s em todas as meias-janelas; 0 quadros parados |
| fim | 195 px/s → 4 px/s: para junto com o dedo, sem salto |
| total | 3,3 s em movimento, 735 px; 0 quadros para trás; maior salto de velocidade entre quadros 60 px/s; tremor p95 0,74 px |

Antes: o app só recebia o arrasto ao soltar o dedo (um único "nudge"), e o NDI ficava parado e pulava de uma vez. Não medido: a tela do próprio iPad (o painel do navegador de testes estava escondido e não desenhava); não testado num iPad físico.

## Resultados (versão 2.8.5) — tela cheia no iPad

Regra que reconhece iPad/iPhone conferida com os textos reais dos navegadores: iPad com iPadOS 17 (que se apresenta como Mac, com toque), iPad antigo e iPhone → iPad/iPhone; Mac de verdade, Windows/Chrome e Android → outro. No monitor web (porta de teste 8430, 820×1180): no computador, botão de tela cheia e "Outro monitor…" continuam; no modo iPad, "Outro monitor…" some e aparece o texto da Tela de Início; aberto como app, o botão de tela cheia some; o aviso "Tela cheia no iPad" abre com os 3 passos. Não testado num iPad físico.

## Resultados (versão 2.8.4) — janela do talento em tela cheia

Monitor 1366×768 (área útil 1366×720, barra de tarefas de 48 px). Jornal da Noite aberto, `--talent`: a janela do talento abriu com **1366×768** (cobre a barra de tarefas), barra do talento no topo e controles embaixo. Autoteste completo: TUDO OK (F5 abre e fecha; Esc fecha mesmo em tela cheia).

## Resultados (versão 2.8.3) — linha a linha e editor na mesma linha das telas

| Verificação | Antes | Depois |
|---|---|---|
| Próxima linha a partir do 1º parágrafo, com linha em branco no meio (autoteste) | 40 toques sem sair do 1º parágrafo ("Última linha") | 8 toques até o 2º parágrafo; 1 toque volta à última linha do 1º |
| Editor × telas de prompter, bloco a bloco (texto, marcador, texto, pausa, texto) | até 19,8 px de diferença (parágrafo antes do marcador 18 px mais alto no editor) | maior diferença 1,4 px |
| Captura parada logo após o marcador "Chuvas no litoral" (Jornal da Noite) | editor com a seta no título do marcador; tela e navegador já na 1ª linha do texto | editor e tela com a seta no mesmo ponto |
| No meio de um parágrafo | editor, tela e navegador iguais (mesmas quebras de linha e espaços) | iguais |

Autoteste completo: TUDO OK (rodado com a licença ilimitada de testes, sem telas de ativação).

**Licença ilimitada** (servidor publicado): criada pelo admin numa conta de teste; ativou em dois computadores, os dois continuam valendo na conferência; a conta mostra a gratuita + a ilimitada (último computador); "remover" pela conta não a derruba; não abre vaga para outra gratuita; bloqueada pelo admin deixa de valer. Testes de conta (31) repetidos depois da mudança: TUDO OK.

## Resultados (versão 2.8.2) — barra do monitor web com mouse

App licenciado, monitor web na porta de teste 8430, eventos de mouse e teclado simulados na página:

| Caso | Barra de cima |
|---|---|
| Ao abrir | Aparece (some sozinha depois) |
| Mouse mexendo no meio da tela (10 movimentos) | Some em 0,5 s e continua escondida; controles de baixo visíveis |
| Clique no + dos controles de baixo · seta do teclado | Continua escondida |
| Mouse no topo | Aparece; ao sair do topo, some em 0,5 s |
| Toque na tela (iPad) | Aparece |

Sem erros de script no navegador (módulo conferido com `node --check`).

## Resultados (versão 2.8.1) — atualização automática pelo GitHub de verdade

Computador do usuário com a 2.8.0 instalada em Arquivos de Programas; v2.8.1 publicada no GitHub Releases.

| Passo | Resultado |
|---|---|
| Ajuda › Procurar atualizações (1ª tentativa) | Aviso da 2.8.1; o download falhou com "618 (jwt:jwt-not-provided)": o endereço do instalador era montado a partir do endereço temporário assinado do latest.json |
| Correção publicada | latest.json da v2.8.1 reenviado com o endereço completo (`publish-release.ps1 -SoLatestJson`); o cache do GitHub levou ~1 min para entregar o novo |
| 2ª tentativa | Baixou, o Windows pediu permissão (UAC), instalou a 2.8.1 e o app reabriu sozinho |

## Resultados (versão 2.8.0) — licença, Minha conta e instalador

**Servidor e conta** (`wrangler dev` local, 31 verificações, todas OK): senha curta recusada; conta criada com cookie HttpOnly/SameSite=Lax; e-mail repetido (maiúsculas) recusado; a conta nasce com um código ARCA-XXXX-XXXX-XXXX livre; sem sessão → 401; segundo código recusado; o app ativa no computador 1 e a conta mostra nome e versão; computador 2 recusado com a dica de remover pela conta; outra conta não remove licença alheia; chamada vinda de outro site → 403; **remover pela conta** → a conferência do computador 1 responde "removida pela sua conta" e o computador 2 ativa; senha errada → "E-mail ou senha incorretos"; perfil salvo; trocar a senha exige a atual e encerra os outros navegadores; senha provisória do administrador encerra as sessões e entra; sair apaga o cookie; a rota antiga de cadastro sem conta não existe mais.

**De ponta a ponta com o app**: conta criada pelo formulário do site → Minha conta com o código; `ArcaTeleprompter --ativar=CÓDIGO` (servidor local) → a conta mostra WORKSTATION01; **Remover deste computador** (confirmação) → "Livre para ativar"; o app aberto de novo avisa "A licença foi removida deste computador pela sua conta no site. O app voltou ao modo demonstração" e a barra mostra "Demonstração · 10 min hoje · Ativar". Página da conta no celular (375 px) sem rolagem lateral.

**Instalador** (`tools/SetupTest`, 452 arquivos): arquivo preso por outro processo o tempo todo → espera 11 s, falha com "O arquivo WinRT.Runtime.dll está aberto em: Windows PowerShell…" e a pasta volta idêntica (SHA-256 de todos os arquivos, sem sobras); preso por 2 s → instala depois de esperar, arquivo novo no lugar, sem sobras.

**No servidor publicado** (https://arca-licencas.cassiosantos-csa.workers.dev, 24/10): as mesmas 31 verificações, todas OK; segredos conferidos como `secret_text`; o app (sem endereço de teste) ativou com `--ativar`, a conta mostrou WORKSTATION01 · 2.8.0, e depois de **Remover deste computador** o app aberto de novo apagou a licença. Contas de teste apagadas do banco depois. Tela Ajuda › Licença já ativada (`--cmd=licenca`): os três botões cabem (janela 487 px; antes, 460 px cortava o "Fechar").

**Monitor web** (app licenciado, porta de teste 8430): 6 s de mousemove na mesma posição (o que o navegador manda quando a barra some debaixo do cursor) → a barra some mesmo assim; movimento de verdade → volta. Ajustes numa janela de 560 px de altura: título fixo, conteúdo rola com barra fina.

Não testado aqui: o download automático depois de criar a conta (sem versão publicada no GitHub, a página só mostra "Conta criada").

## Resultados (versão 2.7.0) — atualização automática

Teste de ponta a ponta: 2.7.0 instalada numa pasta de teste (só para o usuário), servidor local no papel do GitHub (`ARCA_UPDATE_URL`) anunciando a versão "9.9.9" com o mesmo instalador.

| Passo | Resultado |
|---|---|
| Ao abrir | Consulta o latest.json 8 s depois; aviso "ARCA Teleprompter 9.9.9" com novidades e tamanho (224 MB) |
| Lembrar depois | Fecha o aviso; ao reabrir o app, o aviso volta |
| Atualizar agora | Tela de download com progresso, MB/s e tempo; SHA-256 conferido; o app fecha |
| Instalador `--update` | Abre direto em "Atualizando para a versão 2.7.0", mesma pasta e componentes, sem perguntas |
| Fim | Instalado às 12:27:49; app reaberto sozinho às 12:27:50 |
| SHA-256 errado | "O arquivo baixado não confere… Nada foi instalado"; arquivo apagado; instalação intacta; instalador não abre |

Não testado aqui: o pedido de permissão do Windows (UAC) numa instalação em Arquivos de Programas (o teste usou uma pasta do usuário) e o GitHub de verdade (ainda sem versão publicada).

## Resultados (versão 2.6.2) — editor ao maximizar e restaurar

| Caso | Sem a correção (largura visível → texto) | Com a correção |
|---|---|---|
| Janela normal | 598 → 598 | 598 → 598 |
| Maximizar | 866 → **598** (não preenche) | 866 → 866 |
| Restaurar | 598 → **866** (texto enorme) | 598 → 598 |
| Mais larga | 882 → **598** | 882 → 882 |
| Mais estreita | 522 → **882** | 522 → 522 |

Autoteste completo: 54/54 (editor e transporte 11, atalhos 25, botões 9, suavidade 4, janela 5).

## Resultados (versão 2.6.1) — suavidade no NDI e no navegador

Comandos pelos botões do navegador (play, +×4, −×4, pausa, linha ↑×2, linha ↓×2, play, pausa); NDI medido com `NdiProbe --movimento` (1080p30) e navegador gravando cada quadro.

| Item | Antes (2.6.0) | Depois (2.6.1) |
|---|---|---|
| NDI — maior salto de velocidade na mudança de velocidade | 148 px/s (um quadro fora do lugar em cada mudança) | 33 px/s (rampa) |
| NDI — quadros atrasados | até 4 seguidos (66–133 ms) | 0 em 840 (intervalo 33,4 ms) |
| NDI — recuo ao pausar | 1,7 px | 0 |
| NDI — linha (parado) | pulava a linha num quadro | desliza: 7 → 39 → 42 → 32 → 24 → … → 1 px/quadro |
| Navegador — recuo ao pausar | ~1,2 px | 0 |
| Navegador — linha | desliza | desliza, 0 quadros contra o movimento |
| App (autoteste) | — | 403 quadros/6,7 s, 0 acima de 50 ms, salto máx 6,2 u/s |

Observação: comandos mandados por linha de comando (`--cmd`) abrem um processo novo do app para entregar o comando e disputam o processador — atrasam alguns quadros do NDI. Para medir suavidade, mande os comandos pelo navegador.

## Resultados (versão 2.6.0) — cor, voz e suavidade

| Item | Resultado |
|---|---|
| Autoteste | 49/49 (editor e transporte 11, atalhos 25, botões 9, suavidade 4) |
| Suavidade (talento + janela de prompter abertos; W×4, S×4, W×8, S×8) | 409 quadros em 6,7 s (16,7 ms), 0 acima de 50 ms; maior salto de velocidade entre quadros 6,9 u/s (máx 144 u/s); aceleração constante ≤ 208 u/s², 0 inversões; 0 quadros para trás |
| Seletor de cor | Abre com a cor atual (fundo #000000, texto #FFFFFF); HEX digitado atualiza RGB e o quadro; Aplicar fecha; cor aparece em "Últimas usadas" |
| Partida da voz | Ver docs/06-voz.md — Partida rápida |

### Whisper — modelos (cobertura das 55 palavras · latência mediana)

| Áudio | Base, tempo real | Turbo, tempo real | Turbo, PC 5× mais rápido | Small, PC 5× mais rápido |
|---|---|---|---|---|
| longa (limpo) | 52 · 0,59 s | 21 · 3,1 s | 43 · 0,68 s | 53 · 0,34 s |
| fraca | 53 · 0,64 s | 23 · 3,0 s | 49 · 0,80 s | 53 · 0,39 s |
| sala (eco) | 46 · 0,68 s | 29 · 3,3 s | 29 · 1,24 s | 55 · 0,41 s |
| ruim | 32 · 0,66 s | 15 · 2,7 s | 37 · 1,02 s | 44 · 0,27 s |

## Resultados (versão 2.5.1) — nome do apresentador e alinhamento

| Item | Resultado |
|---|---|
| Nome do apresentador | Editor centralizado (Entrevista com dois apresentadores): ~18 px do nome até a própria fala, ~27 px até a fala anterior (antes ficava mais perto da anterior) |
| Alinhamento | Centralizado: marcador e nomes centralizados no editor; janela do talento à esquerda: marcador e nomes à esquerda |
| Janela do talento | Botões minimizar, maximizar/restaurar e fechar na barra |

## Resultados (versão 2.5.0) — atalhos e barras

| Item | Resultado |
|---|---|
| Autoteste (`--autoteste`) | 45/45: editor e transporte (11) + 25 atalhos de teclado + 9 botões da barra de controle |
| Atalhos | Todos com efeito conferido no motor; **Page Down e ] estavam quebrados** (nome alternativo da tecla no Windows) e foram corrigidos |
| Ré | Frear com S até o mínimo: velocidade 0, direção continua para a frente; R inverte e desinverte |
| Barra da janela principal | Mesma ordem das outras telas (captura conferida) |

## Resultados (versão 2.4.0) — telas e editor

| Item | Resultado |
|---|---|
| Testes automatizados | 19/19 aprovados |
| Autoteste do app (`--autoteste`) | 11/11: colar 3 linhas → 3 parágrafos com cursor no fim; apresentador só na linha do cursor; desfazer/refazer; desfazer digitação; seta pula linha em branco (antes parava nela); slider 0,2 → 0,6 = 52 → 125 u/s; pausa sem deslocamento (0,00 u); play continua de onde parou |
| Barra de controle | Mesma barra (com ré, − e +) na janela do talento, na janela de prompter flutuante e no navegador; no talento, bloco/tempo/relógio acima da barra |
| Navegador | Girar 90°/180° confirmados (área do texto gira, barras de pé, preferência salva); ré alterna e o botão fica vermelho; "Outro monitor…" mostra o aviso quando o navegador nega a permissão (escolha de monitor real a conferir no Chrome/Edge com 2 monitores) |

## Resultados (versão 2.3.0)

| Item | Resultado |
|---|---|
| Testes automatizados | 19/19 aprovados (inclui frenagem com aceleração constante, sem ré ao frear e movimento da voz sem tranco nem ultrapassar o alvo) |
| Frenagem em velocidade alta | Medida no fluxo de posição: desaceleração constante ≈ −417 u/s² de 198 a 52 u/s em 0,35 s |
| Navegador | +/−, espaço e roda alteram o app; barra some em 4 s (controle) e não existe no ?obs; fundo transparente |
| Janela do talento | Uma barra só; controle de velocidade ligado ao app; informações maiores em zona reservada |
| NDI | Texto + overlay recebidos por `NdiProbe`; envio com 150 quadros a cada 5 s (29,97 qps), montagem 1,5–2 ms, envio ≈ 1 ms, atraso máximo 0,3 ms, 0 quadros atrasados (antes: ≈ 24 ms por quadro com renderização completa e relógio de 15 ms) |
| Web | Velocidade exibida no navegador medida quadro a quadro: média 87,9 u/s, desvio 1,2 (antes: desvio 43, picos de 7,8 a 214 u/s) |
| Web/PWA | Limpo e controle verificados; comando do navegador moveu o app |
| Voz | Leitura sintetizada seguida; improviso detectado e ressincronizado |
| Instalação personalizada | 426 arquivos, registro, associação, atalhos, exemplos e modelo de voz OK |
| Desinstalação | Registro, associação e atalhos removidos; arquivos do runtime do desinstalador removidos após o fechamento |
