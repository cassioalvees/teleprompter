# 10 — Segurança e privacidade

Revisão completa em 2.9.0 (25/10/2026): monitor web, servidor de licenças e conta, atualização automática, instalador e licença no app. O que foi corrigido está marcado com **(2.9.0)**; o que ficou como risco conhecido está no fim, com a recomendação.

## Superfície de rede (app)
| Porta | Serviço | Padrão | Proteções |
|---|---|---|---|
| 8420 TCP | HTTP + WebSocket (monitor web) | desligado | Senha opcional; lista fixa de comandos aceitos; valores numéricos validados (`IsFinite`); só aceita conexões da própria página **(2.9.0)**; limite de senhas erradas **(2.9.0)** |
| 8421 TCP | HTTPS + WSS | junto com o web | Certificado autoassinado RSA 2048, SAN com IPs da LAN, 5 anos |
| 10540/10541 TCP | MOS | desligado | Apenas LAN; XML sem DTD (sem XXE) |
| NDI (mDNS + TCP dinâmico) | NDI | desligado | Controlado pelo runtime NDI |

### Monitor web
- **Origem da conexão (2.9.0)**: o WebSocket só é aceito com `Origin` igual ao endereço do próprio monitor (ou sem `Origin`, caso do OBS e de programas). Antes, qualquer site aberto num navegador da rede podia abrir `ws://IP:8420/ws` e controlar o prompter (*cross-site WebSocket hijacking*) — com o padrão "sem senha", sem nenhuma barreira.
- **Senha (2.9.0)**: comparada com `CryptographicOperations.FixedTimeEquals`; 5 senhas erradas encerram a conexão; 10 em 10 minutos bloqueiam o endereço por 10 minutos. Antes não havia limite (uma senha curta caía em segundos).
- **Aviso no app (2.9.0)**: inspetor › Telas mostra em vermelho quando o controle pelo navegador está liberado sem senha.
- **Conteúdo**: o texto do roteiro é escapado (`HtmlScript.Enc`); a cor de cada trecho só entra se for uma cor de verdade — #hex, rgb()/rgba() ou nome **(2.9.0)** (uma cor vinda de arquivo importado podia carregar CSS, ex.: `url(…)` para um endereço externo).
- **Identidade dos arquivos (2.8.7)**: a página recarrega sozinha quando o ARCA muda de versão (o iPad não fica com código antigo).
- O servidor só serve arquivos embutidos (`wwwroot` em recurso), não o disco.

## Servidor de licenças e conta (Cloudflare Workers + D1)
- Senhas: PBKDF2-SHA256 com sal, 100 mil iterações (máximo do Workers). Nunca guardadas em texto.
- Sessão: cookie `HttpOnly`, `SameSite=Lax`, `Secure` (no https), 30 dias; no banco só o SHA-256 do token. Trocar a senha encerra as outras sessões. Sessões vencidas são apagadas a cada entrada **(2.9.0)**.
- Rotas da conta só aceitam POST da própria página (`Origin`); limite de 5 cadastros/hora e 10 senhas erradas a cada 15 min por endereço.
- **Tempo de resposta do login (2.9.0)**: e-mail sem conta também calcula um hash — o tempo não revela quem tem conta.
- Licença: ECDSA P-256 assinada no servidor; o app confere com a chave pública embutida. Código de 12 caracteres em alfabeto de 32 (≈ 10¹⁸ combinações): adivinhar não é viável.
- Administração: token longo e aleatório comparado em tempo constante; página `/admin` sem indexação.
- Segredos: `LICENSE_PRIVATE_KEY` e `ADMIN_TOKEN` como *secrets* da Cloudflare; cópia local em `server/licencas/.dev.vars` (fora do git). **Faça backup desse arquivo em lugar seguro** — perder a chave impede emitir licenças que as versões instaladas aceitem.

## Atualização automática
- **Assinatura do latest.json (2.9.0)**: `publish-release.ps1` assina "versão|arquivo|SHA-256|tamanho" com uma chave ECDSA P-256 **só de atualizações** (`UPDATE_PRIVATE_KEY` em `.dev.vars`, separada da chave de licenças); o app confere com a chave pública embutida (`Arca.Core/Net/UpdateSignature.cs`) e recusa o que não estiver assinado. Antes, o SHA-256 vinha do mesmo lugar que o instalador: quem conseguisse publicar no GitHub (conta invadida) mandaria um instalador falso que o app aceitaria. `ARCA_UPDATE_ALLOW_UNSIGNED=1` só para testes locais.
- **Arquivo travado até instalar (2.9.0)**: o instalador baixado fica aberto só para leitura e o SHA-256 é conferido de novo na hora de abrir — outro programa não consegue trocá-lo entre o download e a execução.
- Download por https (GitHub); SHA-256 conferido durante o download.

## Instalador
- Executa como usuário comum (`asInvoker`); só eleva quando a pasta ou o escopo exigem.
- **Pasta de destino (2.9.0)**: recusa a pasta do Windows (e tudo dentro), a raiz do disco, "Arquivos de Programas"/ProgramData em si e a pasta de usuários — também na continuação elevada (`--resume`), que lê as opções de um arquivo temporário: pasta perigosa ali faz o instalador recomeçar pela tela.
- Proteção contra *zip slip*; desfaz por inteiro se algo falhar (2.8.0).
- `SHA256SUMS.txt` publicado junto com o instalador.

## Privacidade
- **Áudio**: processado em memória, localmente (Vosk/Whisper); nada é gravado nem enviado (exceto o diagnóstico de 15 s, gravado só quando o operador pede, nos registros locais). A gravação de vídeo é explícita e salva em `Vídeos\ARCA Teleprompter`.
- **Roteiros**: ficam na máquina (`.tps`, `session.json`, `versions.json`); nunca vão para o servidor.
- **Conta (servidor)**: nome, e-mail, empresa, telefone, senha cifrada; por licença, nome do computador, versão do app e última conferência; registro de eventos (cadastro, entrada, ativação) com endereço IP. Para LGPD: publicar uma política de privacidade no site e atender pedidos de exclusão (hoje pelo administrador).
- Nenhuma telemetria. O app acessa a internet para licença, atualização e download de modelos de voz.

## Riscos conhecidos
| Risco | Situação / recomendação |
|---|---|
| Monitor web sem senha por padrão | Aviso no app; recomendação no manual. Exigir senha por padrão mudaria o uso atual — decisão do produto |
| http (8420) sem criptografia na rede local | Usar https (8421) com o certificado instalado no aparelho |
| Certificado autoassinado gera alerta no navegador | Instalar `/certificado.cer` como confiável no aparelho |
| App .NET pode ser descompilado e a checagem de licença removida | Inerente a apps .NET; ofuscação (ex.: ConfuserEx, pago: Dotfuscator) dificulta, não impede. Com licença gratuita, risco baixo |
| Contagem da demonstração (10 min/dia) apagável | Guardada assinada em dois lugares; apagar os dois zera o dia. Sem servidor, não há como garantir |
| Instalador sem assinatura de código (Authenticode) | Windows mostra "Editor desconhecido" e o SmartScreen avisa. Certificado de assinatura de código (≈ US$ 200–400/ano) resolve |
| Senha esquecida só pelo administrador | Envio por e-mail precisa de domínio + serviço de e-mail |
| `.dev.vars` com as chaves privadas em disco | Backup cifrado; nunca no git (já no `.gitignore`) |
| Runtime NDI de terceiros | Carregado só se presente; falha não afeta o app |
