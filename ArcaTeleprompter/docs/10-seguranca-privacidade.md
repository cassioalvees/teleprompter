# 10 — Segurança e privacidade

## Superfície de rede
| Porta | Serviço | Padrão | Proteções |
|---|---|---|---|
| 8420 TCP | HTTP + WebSocket | desligado | Senha opcional; controle remoto desligado por padrão; lista fixa de comandos aceitos; valores numéricos validados (`IsFinite`) |
| 8421 TCP | HTTPS + WSS | junto com o web | Certificado autoassinado RSA 2048, SAN com IPs da LAN, 5 anos |
| 10540/10541 TCP | MOS | desligado | Apenas LAN; XML sem DTD (sem XXE) |
| NDI (mDNS + TCP dinâmico) | NDI | desligado | Controlado pelo runtime NDI |

- Senha comparada com `CryptographicOperations.FixedTimeEquals`. Não há contas nem armazenamento de senhas de terceiros.
- O servidor só serve arquivos embutidos (`wwwroot` em recurso), não o disco.
- Nenhuma telemetria. O app só acessa a internet quando o operador pede o download de um modelo de voz.

## Privacidade
- **Áudio**: processado em memória, localmente (Vosk); nada é gravado nem enviado. A gravação de vídeo é explícita e salva em `Vídeos\ARCA Teleprompter`.
- **Roteiros**: ficam na máquina (`.tps`, `session.json`, `versions.json`).
- **Preferências por aparelho** (web): `localStorage` do navegador do aparelho.

## Instalador
- Executa como usuário comum (`asInvoker`); só eleva quando a pasta ou o escopo exigem.
- Proteção contra *zip slip* (caminho final precisa ficar dentro da pasta de destino).
- `SHA256SUMS.txt` publicado junto com o instalador. Recomenda-se assinar o executável (Authenticode) na distribuição.

## Riscos conhecidos
| Risco | Mitigação |
|---|---|
| Certificado autoassinado gera alerta no navegador | Instalar `/certificado.cer` como confiável no aparelho |
| Rede de convidados exposta | Usar senha e manter o controle remoto desligado |
| Runtime NDI de terceiros | Carregado só se presente; falha não afeta o app |
