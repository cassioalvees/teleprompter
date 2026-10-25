# 02 — Requisitos

Prioridade: **M** = obrigatório, **D** = desejável. A coluna *Verif.* aponta o teste ou procedimento (ver [08](08-testes.md)).

## Requisitos funcionais

| ID | Requisito | Pri. | Verif. |
|---|---|---|---|
| RF-01 | Criar, abrir, salvar e salvar como roteiros `.tps` (v1 do protótipo e v2). | M | T-TPS, P-ARQ |
| RF-02 | O app começa limpo na primeira execução (roteiro vazio "Sem título"); exemplos ficam em Documentos\ARCA Teleprompter\Exemplos. | M | P-INI |
| RF-03 | Restaurar a sessão anterior (abas, posição, alterações não salvas). | M | P-INI |
| RF-04 | Editar texto com negrito, itálico, sublinhado e cor. | M | P-EDT |
| RF-05 | Inserir, renomear, mover e remover marcadores; lista lateral reflete apenas o roteiro ativo. | M | T-RUN, P-EDT |
| RF-06 | Inserir e remover pausas; a rolagem para nelas. | M | T-SCR |
| RF-07 | Atribuir trechos a apresentadores (nome e cor); filtrar por apresentador. | M | P-EDT |
| RF-08 | Pular trechos (não aparecem no prompter). | M | T-RUN |
| RF-09 | Importar DOCX, PDF, HTML, RTF, TXT; colar texto preservando formatação básica. | M | P-IMP |
| RF-10 | Rolagem com velocidade ajustável, reverso, parar, início/fim, linha a linha, parágrafo, marcador. | M | T-SCR, P-PLAY |
| RF-11 | Contagem regressiva configurável antes do play. | D | P-PLAY |
| RF-12 | Leituras: posição/restante, duração, relógio, **fim previsto (só durante a rolagem; parado mostra "--:--:--")**, linha. | M | P-PLAY |
| RF-13 | Duração-alvo: calcular a velocidade para terminar no tempo do programa. | D | T-SCR |
| RF-14 | Saída em qualquer monitor (tela cheia, espelhamento H/V) e em janela. | M | P-TEL |
| RF-15 | Modo talento com barra de controle e edição rápida do trecho atual. | M | P-TAL |
| RF-16 | Monitor web/PWA: modo limpo (sem contadores/HUD) e modo controle; QR code; senha. | M | T-SRV, P-WEB |
| RF-17 | PWA instalável no iPad (tela de início, tela cheia, tela sempre acesa). | M | P-WEB |
| RF-18 | Saída NDI 1080p30/60/25 e 720p30, fundo opaco ou alfa, tally. | M | P-NDI |
| RF-19 | Acompanhamento por voz offline com microfone WASAPI/ASIO (canal), arquivo ou simulação. | M | T-VOZ, P-VOZ |
| RF-20 | Improviso: parar quando o apresentador sai do texto e ressincronizar ao voltar. | M | T-VOZ |
| RF-21 | Receber espelho MOS (roCreate/roReplace/roStorySend/roElementAction…) e responder roAck. | D | T-MOS |
| RF-22 | Enviar a linha atual como legenda (vMix ou TCP). | D | P-INT |
| RF-23 | Atalhos de teclado remapeáveis; atalhos globais opcionais. | M | P-KEY |
| RF-24 | Histórico de versões (até 25 por roteiro) e perfis de aparência. | D | P-VER |
| RF-25 | Imprimir e exportar PDF/TXT. | D | P-ARQ |
| RF-26 | Fontes: as do pacote e **todas as instaladas no Windows**, listadas automaticamente. | M | P-FNT |
| RF-27 | Instalar em qualquer pasta escolhida (por usuário ou todos os usuários) e desinstalar removendo arquivos, registro, atalhos e associação. | M | P-INS |

## Requisitos não funcionais

| ID | Requisito | Meta |
|---|---|---|
| RNF-01 Desempenho | Rolagem sem trepidação | 60 qps no monitor do operador; extrapolação por velocidade nas saídas |
| RNF-02 Latência web | Posição até o tablet | < 50 ms em LAN; envio limitado a ~30 Hz |
| RNF-03 Latência voz | Palavra falada → rolagem | < 400 ms (resultados parciais do Vosk) |
| RNF-04 Robustez | Erro de UI não derruba o roteiro no ar | Exceções registradas e a operação continua |
| RNF-05 Persistência | Nenhuma perda de texto | Sessão salva a cada alteração (gravação atômica) + versões automáticas |
| RNF-06 Privacidade | Áudio não sai da máquina | Reconhecimento 100% local |
| RNF-07 Segurança | Controle remoto só autorizado | Controle web desligado por padrão; senha com comparação em tempo constante |
| RNF-08 Usabilidade | Primeira execução sem dados; tema claro; overlay "nenhum" | Padrões de fábrica |
| RNF-09 Portabilidade | Windows 10 1809+ x64 | Self-contained, sem .NET pré-instalado |
| RNF-10 Acessibilidade | Leitores de tela | Botões com nome de automação (ToolTip) |
| RNF-11 Manutenibilidade | Domínio testável sem UI | `Arca.Core` sem dependência de WPF; testes xUnit |
| RNF-12 Escala de tela | DPI misto | PerMonitorV2 |
