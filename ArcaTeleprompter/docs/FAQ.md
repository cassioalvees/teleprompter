# ARCA Teleprompter — Perguntas frequentes

As mesmas perguntas estão no Manual do Operador (seção 13), que vai junto com o app.

## Licença e conta

**O ARCA é pago?**
Não. A licença é gratuita e vitalícia: crie a conta no site, copie o código em **Minha conta › Licenças** e ative em **Ajuda › Licença**.

**Posso usar em mais de um computador?**
A licença gratuita fica ativa em um computador por vez. Para trocar, clique em **Remover deste computador** em Minha conta › Licenças (ou Ajuda › Licença › Desativar este computador) e ative no novo com o mesmo código. Licenças **ilimitadas** (equipe, dono) ativam em quantos computadores quiser.

**Precisa de internet para usar?**
Só para ativar e para atualizar. Depois de ativado, o ARCA funciona offline — inclusive a voz. Ao abrir com internet, ele confere se a licença continua valendo.

**O que a demonstração permite?**
10 minutos de prompter por dia, um roteiro e uma tela de prompter. NDI, monitor web/iPad, voz, redação (MOS), câmera e impressão pedem a licença.

**Esqueci a senha da conta. E agora?**
Fale com o suporte informando o e-mail da conta: o administrador gera uma senha provisória, e você troca em Minha conta › Meus dados.

## Instalação e atualização

**Como atualizo?**
O app avisa ao abrir quando há versão nova. **Atualizar agora** baixa, instala e reabre o app sozinho; o Windows pede permissão de administrador quando o ARCA está em Arquivos de Programas. Para procurar na hora: Ajuda › Procurar atualizações.

**A atualização é segura?**
Sim. O app só instala versões assinadas pela ARCA (assinatura digital conferida com uma chave embutida no app) e confere o SHA-256 do instalador logo antes de abrir. Arquivo alterado no caminho é recusado e nada é instalado.

**A instalação disse que um arquivo está em uso.**
Outro programa estava com um arquivo da pasta aberto (às vezes o antivírus). O instalador espera alguns segundos, diz qual programa é e oferece **Tentar de novo**. Se falhar, a instalação anterior volta inteira.

**Perco meus roteiros ao atualizar ou reinstalar?**
Não. Roteiros, preferências e modelos de voz ficam na sua pasta de usuário e são mantidos.

## No ar

**O texto do editor, das telas e do iPad está na mesma linha?**
Sim. Todas as telas quebram as linhas nas mesmas palavras e seguem a mesma posição — inclusive perto de marcadores e pausas.

**"Palavras por linha" não dá exatamente esse número em cada linha.**
É uma média: o ajuste define o tamanho da letra. Cada linha quebra onde a próxima palavra não cabe, então uma linha com palavras longas tem menos palavras.

**A janela do talento cobre a barra do Windows?**
Sim, ela abre em tela cheia. O botão □ (ou `F11`) alterna com janela; `Esc` fecha.

**O trackpad está sensível demais.**
Ajuste em Preferências › Geral › Roda do mouse e trackpad: **Rolagem (parado)** e **Velocidade (rodando)**.

## iPad e navegador

**Para que serve a senha do monitor web?**
Sem senha, qualquer aparelho na mesma rede Wi-Fi que abrir o endereço vê o roteiro e — com o controle liberado — controla o prompter. Com senha, o navegador/iPad pede a senha antes de mostrar o texto. Use sempre em eventos, igrejas e redes compartilhadas; o app mostra um aviso em vermelho enquanto o controle estiver liberado sem senha.

**Errei a senha e o iPad não entra mais.**
Por segurança, depois de 5 senhas erradas a conexão é encerrada, e depois de 10 em 10 minutos aquele aparelho fica bloqueado por 10 minutos. Espere e tente com a senha certa.

**Um site pode controlar meu prompter pela rede?**
Não. O app só aceita conexões vindas da própria página do monitor (ou de programas como o OBS), não de outros sites abertos num navegador da rede.

**Atualizei o ARCA e o iPad continua com o comportamento antigo.**
A partir da 2.8.7 a página recarrega sozinha quando o ARCA muda de versão. Se o iPad ainda estiver numa página aberta antes disso, recarregue uma vez (ou feche e abra o app da Tela de Início).

**Arrastar o texto no iPad move as outras telas?**
Sim: parado, arrastar rola o texto e o app, as telas e o NDI acompanham o dedo; rodando, arrastar para cima acelera e para baixo freia.

## NDI

**O NDI está sem fluidez.**
Use 1080p 29,97/30 se o computador for modesto; 59,94/60 dá mais fluidez e pede mais do processador. O registro (Ajuda › Registro de eventos) mostra a cada 5 s quantos quadros saíram e quantos atrasaram.

**Posso usar fundo transparente?**
Sim, em inspetor › Telas › NDI › Fundo transparente, para sobrepor o texto ao vídeo no vMix ou OBS.

## Voz

**A voz funciona sem internet?**
Sim. O reconhecimento (Vosk ou Whisper) roda no próprio computador; o áudio não sai da máquina.

**Qual motor de voz usar?**
**Vosk · rápido** é leve e já vem instalado. **Whisper · preciso** entende melhor salas com eco, ruído e voz longe do microfone; baixe o modelo Base (57 MB) na primeira vez.

## Segurança e privacidade

**Quais dados a conta guarda?**
Nome, e-mail, empresa, telefone, a senha cifrada (nunca a senha em si), e para cada licença o nome do computador, a versão do app e a última conferência. Os roteiros nunca saem do seu computador.

**Como deixar o monitor web mais seguro?**
Defina uma senha, desligue **Permitir controlar o prompter pelo navegador** quando não precisar e prefira o endereço **https** (porta 8421): no http a senha e o texto passam pela rede sem criptografia.
