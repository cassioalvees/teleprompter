# 05 — Protocolos

## 1. Monitor web / PWA (WebSocket)

- HTTP `http://<ip>:8420/` e HTTPS `https://<ip>:8421/` (certificado autoassinado; baixe em `/certificado.cer`
  para instalar como confiável no iPad, o que habilita o service worker e "Adicionar à Tela de Início").
- `GET /api/info` → `{ app, viewers, control, needPass, https }`.
- WebSocket em `/ws`. Mensagens JSON com campo `t`.

### Cliente → servidor
| Mensagem | Quando |
|---|---|
| `{t:"auth", pass}` | Ao conectar (senha vazia se não houver) |
| `{t:"cmd", name, value?}` | Comando remoto, aceito só se autenticado **e** o controle web estiver liberado no app |

Comandos (`RemoteCommands.Allowed`): `play`, `pause`, `togglePlay`, `faster`, `slower`, `reverse`, `rewind`, `toEnd`,
`nextBm`, `prevBm`, `nextPara`, `prevPara`, `lineUp`, `lineDown`, `speed` (value 0–1), `gotoBm` (value = índice), `nudge` (value = unidades).

### Servidor → cliente
| Mensagem | Conteúdo |
|---|---|
| `{t:"auth", ok, needPass}` | Resultado; se ok, o servidor reenvia `html`, `state` e `y` |
| `{t:"html", html}` | Texto (classes `bm`, `stop`, `w N`, `ws`; `data-i` = índice do bloco) |
| `{t:"state", s}` | `PrompterSnapshot`: aparência, play, velocidade, bloco atual, tempos, marcadores |
| `{t:"y", y, v, b, o, f, bh, ts}` | Posição (unidades), velocidade (unid./s), bloco âncora, deslocamento no bloco, **fração do bloco** (0–1), altura do bloco no app e relógio do servidor (ms) — a cada quadro do app |
| `{t:"flash", text, sub}` | Aviso rápido (ex.: "Marcador 3") |

O cliente posiciona pelo bloco `b` e pela fração `f` medidos no próprio DOM (altura do bloco = até o topo do próximo, igual ao app),
converte a velocidade para a altura local (`v · h_local / bh`), usa `ts` com o menor atraso observado como referência de tempo
e suaviza a posição exibida (segue a velocidade e corrige o erro aos poucos; saltos grandes vão direto).

Modos do cliente, escolhidos no menu ⚙ do próprio aparelho: **limpo** (só texto, sem HUD/contadores) e **controle** (barra igual à do talento). Preferências por aparelho em `localStorage` (espelhar H/V, HUD, tela acesa, gesto, senha).

## 2. MOS 2.8 (subconjunto running order)

- O app atua como **MOS device** escutando TCP 10540 (lower port) e 10541 (upper port); codificação UTF-16BE (detecta BOM/UTF-8 também).
- Suportado: `heartbeat`, `reqMachInfo` (→ `listMachInfo`), `roCreate`, `roReplace`, `roDelete`, `roStorySend`,
  `roElementAction` (INSERT, REPLACE, MOVE, DELETE, SWAP), `roMetadataReplace`; `roReq`, `roReadyToAir`, `roStoryMoveMultiple` recebem `roAck` OK.
- Resposta: `<mos><mosID/><ncsID/><messageID/><roAck><roID/><roStatus>OK</roStatus></roAck></mos>`.
- Cada story vira um marcador com o slug; `storyBody` → parágrafos; `<pi>` (instrução ao apresentador) → `[texto]`.
- O espelho recebido aparece em *Integrações › MOS* e pode ser aplicado ao roteiro ativo.

## 3. NDI

- Runtime NDI 6 (`NDI_RUNTIME_DIR_V6`), carregado por `NativeLibrary` em tempo de execução — sem o runtime, a opção fica indisponível e o app funciona normalmente.
- Fonte: `<MÁQUINA> (ARCA Teleprompter)`; formatos 1920×1080 a 29,97/59,94/25 e 1280×720 a 29,97; BGRA (alfa) ou BGRX.
- Envio assíncrono com buffer duplo; tally (programa/preview) e número de receptores aparecem no app.

## 4. Legenda

| Modo | Envio |
|---|---|
| vMix | `GET http://<host>:8088/api/?Function=SetText&Input=<input>&SelectedName=<campo>&Value=<linha>` |
| TCP | Linha UTF-8 terminada em `\r\n` para `<host>:<porta>` |

Enviada quando a linha de leitura muda de linha, durante a rolagem.
