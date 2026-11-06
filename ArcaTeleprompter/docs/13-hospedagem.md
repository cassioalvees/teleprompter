# 13 — Hospedagem: site, licenças e atualizações

O ARCA usa três coisas fora do computador do usuário. Cada uma pode ficar num lugar diferente.

| Parte | Onde está hoje | O que faz | Endereço gravado no app |
|---|---|---|---|
| **Site + servidor de licenças** | Cloudflare Workers + banco D1 (plano gratuito) | página inicial, cadastro, Minha conta, política de privacidade, ativação e conferência de licenças, painel `/admin` | `https://arca-licencas.cassiosantos-csa.workers.dev` (`LicenseService.DefaultServer`) |
| **Atualizações** | GitHub Releases (`cassioalvees/teleprompter`) | instalador de cada versão e `latest.json` assinado | `https://github.com/cassioalvees/teleprompter/releases/latest/download/latest.json` (`UpdateService.DefaultUrl`) |
| **Chaves** | `server/licencas/.dev.vars` (no seu computador) e *secrets* da Cloudflare | assinar licenças (`LICENSE_PRIVATE_KEY`), entrar no painel (`ADMIN_TOKEN`), assinar atualizações (`UPDATE_PRIVATE_KEY`) | as chaves **públicas** correspondentes estão dentro do app |

## Atualizações: manter no GitHub (recomendado)

- **É de graça e rápido no mundo todo**: os arquivos saem da rede de entrega do GitHub, sem limite prático para o tamanho do instalador (224 MB).
- **Segurança não depende de quem hospeda**: o app só aceita um `latest.json` assinado com a chave de atualizações e confere o SHA-256 do instalador. Mesmo que alguém troque o arquivo no servidor, o app recusa.
- **Já está no app**: trocar de lugar exigiria uma versão nova com outro endereço, e quem não atualizasse ficaria sem atualizações.
- O site já usa o GitHub para o botão "Baixar" (`/download` redireciona para o instalador da última versão).

## Site e licenças em outra hospedagem

O servidor é um **Cloudflare Worker** (JavaScript) com banco **D1** (SQLite). Hospedagens compartilhadas como Hostinger e HostGator rodam PHP + MySQL e não executam esse código como está.

| Opção | Trabalho | Custo | Observação |
|---|---|---|---|
| **A. Continuar na Cloudflare com domínio próprio** (recomendado) | pequeno: apontar um subdomínio, ex. `licencas.arcaaudiovisual.com.br`, para o Worker | grátis (plano gratuito cobre milhares de usuários) | o domínio pode continuar registrado na Hostinger/Registro.br; os e-mails e o site institucional podem ficar onde estão |
| B. VPS (Hostinger VPS, DigitalOcean…) com Node.js | médio: adaptar o código para Node (banco SQLite em arquivo, servidor HTTP) | ~R$ 30–60/mês | você cuida de atualizações do sistema, HTTPS e backup |
| C. Hospedagem compartilhada (PHP + MySQL) | grande: reescrever o servidor em PHP | o do plano | mesma lógica (contas, licenças assinadas, LGPD) em outra linguagem; precisa de PHP com OpenSSL para assinar ECDSA P-256 |

### Por que um domínio próprio é o passo mais importante
O app procura o servidor pelo endereço gravado nele. Hoje é o endereço `workers.dev`, ligado à conta da Cloudflare. Com um domínio seu, uma mudança futura de hospedagem é só trocar para onde o domínio aponta, e nenhum app precisa ser atualizado.

Passos da opção A:
1. Colocar o domínio (ou só o subdomínio) na Cloudflare: em Websites › Add a domain, trocar os servidores DNS no registro do domínio. Se o domínio está na Hostinger, os servidores DNS se trocam no painel dela.
2. No Worker `arca-licencas`: Settings › Domains & Routes › Add › Custom domain › `licencas.arcaaudiovisual.com.br`.
3. Numa versão nova do app: trocar `LicenseService.DefaultServer` para o domínio novo.
4. Manter o endereço `workers.dev` funcionando por um tempo. Os apps antigos ainda o usam para ativar e conferir.

### O que precisa ir junto em qualquer mudança
1. **A chave privada de licenças (`LICENSE_PRIVATE_KEY`).** O app confere a assinatura com a chave pública embutida: com outra chave, nenhuma licença emitida pelo servidor novo é aceita.
2. **O banco**: exportar com `npx wrangler d1 export arca-licencas --remote --output=backup.sql` e importar no novo (SQLite aceita direto; MySQL precisa ajustar tipos).
3. **`ADMIN_TOKEN`** (painel) e as páginas do site (`server/licencas/public`).
4. As mesmas regras de segurança: senha com PBKDF2 e sal, cookie `HttpOnly`/`Secure`, limites de tentativas, cabeçalhos de segurança (CSP e outros), conforme [10 — Segurança](10-seguranca-privacidade.md).

### E se o servidor ficar fora do ar?
- **Licenças já ativadas continuam valendo sem internet** (licença vitalícia, assinatura conferida no próprio computador). A conferência online só serve para aplicar uma licença removida no site, e falha em silêncio.
- **Ativar um computador novo** e **criar conta** precisam do servidor.
- **Atualizações** não dependem dele (vêm do GitHub).

## Ligar o "Esqueci minha senha" por e-mail

O site já tem a página e o servidor pronto (2.11.0). Falta só o envio do e-mail, que precisa de um domínio seu na Cloudflare. Enquanto estiver desligado, a página diz para escrever para o suporte, e o painel `/admin` gera a senha provisória.

1. Domínio na Cloudflare (o mesmo passo 1 da opção A acima).
2. Ativar o envio de e-mail do domínio: Cloudflare › Email › Email Sending, ou `npx wrangler email sending enable seudominio.com.br`. A Cloudflare cria os registros SPF/DKIM no DNS.
3. Em `server/licencas/wrangler.toml`, tirar o `#` das 4 linhas do final e trocar o endereço:
   ```toml
   [[send_email]]
   name = "EMAIL"
   [vars]
   MAIL_FROM = "nao-responda@seudominio.com.br"
   ```
4. `npx wrangler deploy` em `server/licencas`.
5. Testar com o seu e-mail em Minha conta › Esqueci minha senha (confira também o spam na primeira vez).

O link do e-mail usa o endereço por onde a pessoa entrou no site, então já sai certo com o domínio novo.

## Backup (faça hoje)
- `server/licencas/.dev.vars`: guarde num lugar seguro fora deste computador, como um gerenciador de senhas ou um pendrive guardado. **Sem a `LICENSE_PRIVATE_KEY` não dá para emitir licenças que os apps aceitem; sem a `UPDATE_PRIVATE_KEY` não dá para publicar atualizações que os apps aceitem.**
- Banco de licenças: `npx wrangler d1 export arca-licencas --remote --output=licencas-AAAA-MM-DD.sql` de tempos em tempos.
- Código: está no GitHub (`cassioalvees/teleprompter`).
