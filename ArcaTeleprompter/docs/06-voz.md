# 06 — Acompanhamento por voz

## Cadeia
```
Microfone (WASAPI)  ─┐                    VoiceFrontEnd (comum aos dois motores)
Interface (ASIO, canal N) ─┼─▶ passa-altas 80 Hz ─▶ AGC + gate ─▶ 16 kHz mono (WDL) ─┬─▶ Vosk (gramática do roteiro)        ─┐
Arquivo WAV/MP3 (tempo real) ─┘                                                       └─▶ Whisper (janela 2–4 s + VAD + prompt) ─┤
                                                                                                              palavras novas ▼
                                       VoiceAligner ─▶ palavra do roteiro ─▶ Y alvo (linha de leitura) ─▶ ScrollEngine
```

- **Motores** (Voz › Motor; `IVoiceRecognizer` em `Arca.Audio`): **Vosk · rápido** (`SpeechRecognizer`, padrão) e
  **Whisper · preciso** (`WhisperRecognizer`, 2.4). Os dois recebem o mesmo áudio tratado (`VoiceFrontEnd`) e emitem os
  mesmos eventos (`Word`, `Level`, `Error`); o resto do app não sabe qual está ligado.
- **Modelo Vosk**: *small* (pt-BR 31 MB, es, en) em `%LOCALAPPDATA%\ARCA Teleprompter\models` ou na pasta `models` do app. O instalador inclui o pt-BR; os demais são baixados em *Voz › Modelo*.
- **Modelo Whisper**: multilíngue quantizado `ggml-base-q5_1.bin` (57 MB, padrão) ou `ggml-small-q5_1.bin` (181 MB), baixado
  sob demanda (botão *Baixar modelo*) do repositório oficial do whisper.cpp para a mesma pasta. Não vai no instalador.
- **Gramática**: o vocabulário do roteiro + `[unk]` restringe o reconhecimento e aumenta a precisão. O JSON da gramática é gerado com escape relaxado para preservar acentos.
- **ASIO**: abre 1 canal a partir de `InputChannelOffset` (escolha do canal da mesa), tenta 48 kHz e depois 44,1 kHz.

## Alinhador (máquina de estados)

| Estado | Comportamento |
|---|---|
| Ouvindo | Aguarda a primeira palavra igual ao início do trecho atual |
| Seguindo | Procura cada palavra ouvida nas próximas `Window` = 20 palavras (Levenshtein normalizado ≥ 0,75, sem acentos). Avança direto até `MaxSkip` = 3 palavras; saltos maiores exigem confirmação pela palavra seguinte |
| Improviso | Após `Tolerance` = 3 erros seguidos a rolagem para. Procura entre −10 e +30 palavras (só palavras com 3+ letras) |
| Ressincronizando | Achou uma candidata; volta a Seguindo quando a próxima palavra também casa |

Palavras com menos de 2 letras são ignoradas (o ASR erra muito artigos e preposições).

**Linha falada na seta** (2.3): a linha fica parada na seta enquanto é lida e desliza (curva suave) para a próxima perto do fim; movimento por mola criticamente amortecida com aceleração ≤ 6 linhas/s² e velocidade ≤ 3 linhas/s. Comandos falados com o prefixo "comando" (voltar, próximo, início, parar). Navegação manual com a voz ligada recomeça o acompanhamento no ponto escolhido.

**Rolagem contínua** (`PrompterSurface.ReadingY`): o alvo não salta de linha em linha — avança conforme a fração da linha já lida
(do início da linha até o fim da palavra ouvida), interpolando até a próxima linha **de texto** (título de marcador e pausa são pulados).
No fim de um parágrafo, a primeira linha do próximo já chega à linha de leitura: o olhar do apresentador não desce para buscar o texto.
A **antecipação** (Voz › Sensibilidade, padrão 0,35 linha; 1 = a linha lida fica uma linha acima da seta) adianta esse movimento.
O motor aproxima o alvo com constante de tempo ≈ 0,3 s e velocidade limitada (4 linhas/s).

## Tratamento do áudio
Passa-altas de 80 Hz (ronco, vento, zumbido de rede) → **controle automático de ganho** (leva a fala a −20 dBFS, até +32 dB; aprende só
durante a fala) → **gate suave** (atenua 16 dB abaixo do ruído de fundo medido e do limiar, sem cortar a fala) → 16 kHz.

## Whisper (2.4)
O Whisper não é de streaming: `WhisperRecognizer` retranscreve uma **janela deslizante** do áudio tratado.

- **VAD**: usa a mesma detecção de fala do gate (nível acima do ruído de fundo medido e do limiar). Sem fala na janela o modelo
  **nem roda** (guarda só 0,3 s antes da fala) — é a principal defesa contra alucinação no silêncio. Estalo curto (< 0,25 s) seguido de
  silêncio é descartado.
- **Janela**: começa no início da fala e cresce até `MaxWindow`; passando disso, é cortada no fim de uma palavra já emitida
  (fica ≈ `Keep` s). Transcrição a cada `Hop` = 0,3 s (na prática, assim que a anterior termina). O codificador roda com
  **contexto de áudio do tamanho da janela** (`audio_ctx` ≈ 50 quadros/s) em vez dos 30 s fixos — 2,6 s → 0,3–0,5 s por transcrição.
- **Só palavras novas**: tempos por **DTW** (cabeças de alinhamento do modelo) marcam onde cada palavra está; o que termina antes da
  última palavra emitida (com 0,15 s de folga) ou repete as últimas 1–5 emitidas é descartado. Uma palavra sai quando **duas
  transcrições seguidas concordam** nela (LocalAgreement-2) **ou** quando terminou há `StableLag` s no áudio (o fim da janela é
  instável; o meio, não; `StableLag` = 0,35 s). Na **pausa de 0,5 s** (fim da frase) sai o resto e a janela recomeça.
- **Alucinações**: segmento com probabilidade de "sem fala" > 0,6 e confiança < 0,5 é descartado; frases-fantasma conhecidas
  ("Obrigado.", "Legendas pela comunidade Amara.org", "Inscreva-se"…), texto só entre colchetes/parênteses e ♪ também; laços de
  repetição ("nesta edição nesta edição…") são cortados na primeira vez; no máximo `MaxWindow`×10+8 tokens por transcrição.
- **Prompt do roteiro**: com *Reconhecer só o vocabulário do roteiro* ligado, o trecho de 10 palavras antes até 35 depois da
  posição vira o prompt do Whisper (trocado a cada 30 palavras lidas, como a gramática do Vosk). Puxa a transcrição para as
  palavras do texto; o improviso continua sendo transcrito (o alinhador o detecta como fora do roteiro).
- **Desempenho**: placa de vídeo via **Vulkan** quando houver (Whisper.net escolhe sozinho; *Usar a placa de vídeo* desligado força o
  processador e nem carrega a biblioteca Vulkan). Se as 15 primeiras transcrições levarem > 0,8 s em média, o inspetor sugere o
  modelo Base (ou o Vosk). O modelo é carregado na thread do reconhecedor (a interface não trava) e compartilhado entre sessões.

## Partida rápida (2.6)
- `VoiceService.Warmup()` prepara o motor escolhido numa thread de fundo ao abrir o app e ao trocar motor/modelo/idioma: `SpeechRecognizer.Preload` (modelo Vosk) ou `WhisperRecognizer.Preload` (modelo + 1 s de transcrição de aquecimento — na placa de vídeo, a 1ª transcrição compila os shaders).
- O Vosk não carrega mais nada no construtor: a captura começa no `Start()` e o modelo/reconhecedor ficam prontos na thread do reconhecedor; a fila (≈ 10 s) guarda o áudio até lá.
- Medido (`VoiceTest longa.wav --grammar`, com e sem `--preload`): Vosk — pré-carga 0,47 s, primeira palavra 0,84 s no áudio; Whisper base — pré-carga 1,32 s, primeira palavra 1,84 → 1,40 s. Primeira carga do Vosk após ligar o computador: 2,5 s (registro do app).

## Gramática em janela
O reconhecedor usa como vocabulário só as palavras do roteiro **em volta da posição** (40 para trás, 220 para frente), trocadas a cada
30 palavras lidas, sempre entre frases (sem perder palavras). Vocabulário pequeno = reconhecimento bem mais preciso.

## Verificação
`tools/VoiceTest` roda um WAV pelo Vosk e pelo alinhador e imprime cada palavra com o estado (`--quiet` só o resumo;
`--degrade=entrada,saída,ganhoDb,ruído` gera um WAV de microfone fraco/ruidoso para testar).

| Áudio (leitura sintetizada pt-BR) | Antes (gate fixo, sem AGC) | Agora |
|---|---|---|
| Limpo, 68 palavras | 55 de 59 seguidas | 54 de 59 seguidas, até o fim |
| Microfone fraco (−24 dB + ruído leve) | não medido (o gate fixo de −48 dB cortava esse nível) | 48 de 52 seguidas, até o fim |
| Muito ruidoso (fala quase no nível do ruído) | 0 | 15 ouvidas (limite prático) |
| Com improviso | improviso detectado | improviso detectado e ressincronizado |

### Vosk × Whisper (2.4)
`VoiceTest <wav> --grammar --engine=vosk|whisper --whisper=base|small --ref=longa.wav --quiet`. **Cobertura** = palavras do
roteiro que o alinhador seguiu; **latência** = do fim da palavra no áudio (tempos do Vosk offline em `longa.wav`) até o alinhador
segui-la — mediana (p90) —, antes da suavização da rolagem. Notebook i5-1135G7 com Intel Iris Xe (Vulkan); `sala.wav` gerado com
`--degrade=longa.wav,sala.wav,-12,0.004,0.6`.

| Áudio (mesma leitura de 31 s, 55 palavras do roteiro) | Vosk (gramática) | Whisper base (padrão) | Whisper small |
|---|---|---|---|
| `longa.wav` — limpo | 51/55 · 0,18 s | 54/55 · 0,68 s (p90 1,06) | 55/55 · 0,9–1,3 s |
| `fraca.wav` — microfone fraco (−24 dB + ruído) | 46/55 · 0,24 s | 54/55 · 0,65 s (p90 1,18) | 53–55/55 · 0,9–1,3 s |
| `sala.wav` — eco de sala (RT60 0,6 s) −12 dB + ruído | 38/55 · 0,27 s | 52/55 · 0,70 s (p90 1,22) | 50–52/55 · 1,0–1,1 s |
| `ruim.wav` — fala quase no nível do ruído | 6/55, perde-se na 8ª palavra | 28/55 · 0,64 s, segue até o fim | 33–36/55 · 0,8–0,9 s |

- Por transcrição: base 220–270 ms e small 470–600 ms na placa de vídeo; base 550 ms só no processador; com o contexto de áudio
  completo (30 s) o base levava 2,6 s — por isso o contexto do tamanho da janela.
- **Prompt do roteiro** vale muito: sem ele o small cai para 47/55 (limpo), 49/55 (fraco) e 22/55 (ruim).
- Ajustes descartados: janela de 3 s (mais rápida, porém 42/55 na sala e o base se perde no `ruim.wav`); atenção "flash" (passes
  30% mais rápidos, mas o dobro de palavras fora do roteiro).
- Variação entre execuções: ±2 palavras e ±0,2 s (o reconhecimento corre em tempo real, concorrendo com o resto da máquina).
- Não havia gravações `logs\voz-diagnostico-*.wav` do operador para medir; quando houver, rodar o mesmo comando sobre elas.
