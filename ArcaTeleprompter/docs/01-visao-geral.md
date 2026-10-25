# 01 — Visão geral

## Propósito
O ARCA Teleprompter é um sistema de teleprompter para telejornalismo, eventos e gravações. O operador
edita e roda o roteiro num PC Windows; o texto aparece, espelhado ou não, em monitores de prompter,
em tablets/iPads (PWA), em mixers de vídeo (NDI) e pode seguir a fala do apresentador
automaticamente (voz offline).

Referências de mercado: Autoscript (VoiceTracking, WinPlus) e PowerPrompter (suborbital).

## Escopo da versão 2.0
- Editor de roteiro com marcadores, pausas, apresentadores, trechos pulados, formatação e histórico de versões.
- Prompter com rolagem suave (DirectX/WPF), velocidade variável, reverso, contagem regressiva,
  linha de leitura, indicador (seta/quadrado/ponto/linha), overlay (faixa/fade/destaque).
- Telas: qualquer monitor (tela cheia, espelhamento H/V) e janela; modo talento na própria máquina.
- Monitor web e **PWA** (iPad/Android/PC) em modo *limpo* (só texto) ou *controle* (barra igual à do talento),
  HTTP 8420 e HTTPS 8421, QR code, senha opcional.
- Saída **NDI** 1080p/720p com fundo opaco ou transparente, tally.
- **Voz**: microfone WASAPI ou ASIO (escolha de canal), arquivo de áudio ou simulação; Vosk offline pt/es/en.
- **MOS 2.8** (running order) para sistemas de redação; **legendas** para vMix/TCP.
- Importação DOCX, PDF, HTML, RTF, TXT, TPS; exportação TXT, impressão e PDF.
- Instalador e desinstalador próprios (por usuário ou para todos), associação `.tps`, atalhos, regra de firewall.

## Fora do escopo
- Controle por pedal/USB HID dedicado (usa teclado e clickers que emulam teclado).
- Reconhecimento de fala em nuvem (decisão de privacidade, ver ADR-004).
- Edição colaborativa simultânea.

## Glossário
| Termo | Significado |
|---|---|
| Roteiro | Documento `.tps`: blocos de texto, marcadores e pausas. |
| Marcador | Título de bloco (`*Título` no editor). Navegável por Page Up/Down e Ctrl+1..9. |
| Pausa | Ponto em que a rolagem para sozinha (`[PAUSA]`). |
| Linha de leitura | Altura da tela (% `readPct`) onde o apresentador lê. |
| Talento | Visão de tela cheia do apresentador, com barra de controle própria. |
| Espelho (MOS) | Running order enviado pelo sistema de redação. |
| LPS | Linhas por segundo — unidade interna de velocidade. |
