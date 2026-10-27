// Gera public/index.html (site do ARCA Teleprompter) a partir do conteúdo abaixo.
//   node tools/build-site.mjs
// O desenho veio do Claude Design (canvas "Site ARCA Teleprompter"); os textos ficam aqui para editar fácil.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const out = path.join(path.dirname(fileURLToPath(import.meta.url)), '..', 'public', 'index.html');
const esc = s => String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');

const roll = [
  ['ÂNCORA', '#FFD60A', 'Boa noite. Hoje a gente conversa sobre o que muda na cidade a partir de amanhã.'],
  ['REPÓRTER', '#5EB1EF', 'E eu vou mostrando cada ponto enquanto você lê, direto do estúdio.'],
  ['ÂNCORA', '#FFD60A', 'Primeiro, o trânsito: três avenidas mudam de sentido no centro.'],
  ['REPÓRTER', '#5EB1EF', 'A mudança vale das seis da manhã às dez da noite, de segunda a sábado.'],
  ['ÂNCORA', '#FFD60A', 'Voltamos já, com a previsão do tempo para o fim de semana.']
];
const ticker = ['Rolagem suave', 'Seta de leitura', 'Marcadores e pausas', 'Apresentadores coloridos', 'NDI para vMix e OBS', 'iPad pela rede', 'Voz offline', 'Redação MOS', 'Espelhamento', 'Atalhos remapeáveis', 'Stream Deck', 'Atualização automática'];
const facts = [
  ['5', '', 'formatos de roteiro: TPS, DOCX, PDF, RTF e TXT'],
  ['3', '', 'idiomas no acompanhamento por voz: português, espanhol e inglês'],
  ['60', 'p', 'na saída NDI, também em 25p e 30p, com fundo transparente'],
  ['8', '', 'apresentadores com cor e nome próprios no mesmo roteiro']
];
const features = [
  ['Prompter de estúdio', 'Rolagem com aceleração constante, sem trancos. Seta de leitura, marcadores, pausas, contagem regressiva e tempo previsto por bloco.', 'M2.5 3.5h11v8h-11zM6 13.5h4M5 6.5h6M5 8.5h4'],
  ['Acompanhamento por voz', 'O texto segue a fala do apresentador, sem internet. Motores Vosk (rápido) e Whisper (preciso), com comandos falados.', 'M8 2.5a2 2 0 0 1 2 2v3a2 2 0 0 1-4 0v-3a2 2 0 0 1 2-2zM4 7.5a4 4 0 0 0 8 0M8 11.5v2'],
  ['Saída NDI', 'Envie o prompter para vMix, OBS e qualquer receptor NDI da rede, em 1080p ou 720p, com fundo transparente.', 'M2.5 8h3l2-4 2 8 2-4h2'],
  ['iPad e navegador', 'Qualquer tablet ou navegador da rede vira monitor ou controle remoto. Tela cheia, rotação e modo limpo para OBS.', 'M4 2.5h8v11H4zM7 11.5h2'],
  ['Telas e espelhamento', 'Janela do talento, monitores extras e janelas flutuantes, cada uma com espelho horizontal e vertical e alinhamento próprio.', 'M2 3.5h7v5H2zM7 7.5h7v5H7z'],
  ['Apresentadores', 'Cada fala com a cor e o nome de quem lê. Destaque um apresentador e esmaeça os outros com um clique.', 'M5.5 6a2 2 0 1 0 0-.01M2.5 13a3 3 0 0 1 6 0M11 6.5a1.6 1.6 0 1 0 0-.01M9.5 12.5a2.6 2.6 0 0 1 4 0'],
  ['Redação e legendas', 'Integração MOS com o sistema da redação: os roteiros chegam prontos no ARCA.', 'M3 4.5h10M3 8h10M3 11.5h6'],
  ['Controle total', 'Atalhos remapeáveis, atalhos globais, clickers e pedais, Stream Deck e controle pelo navegador.', 'M2.5 5.5h11v6h-11zM5 8.5h.01M8 8.5h.01M11 8.5h.01'],
  ['Sempre atualizado', 'O app avisa quando há versão nova e se atualiza com um clique, conferindo o arquivo antes de instalar.', 'M13 8a5 5 0 1 1-1.5-3.5M13 3v2.5h-2.5']
];
const shots = [
  ['Editor', 'editor.png', 'Editor do ARCA com marcadores, roteiro em fundo preto e inspetor de aparência', 'O editor já mostra o texto como no prompter: marcadores à esquerda, apresentadores coloridos e a barra de controle embaixo.'],
  ['Talento', 'talento.png', 'Janela do talento com texto grande, seta, tempo do bloco e relógio', 'A janela do talento: texto grande, seta de leitura, tempo do bloco, relógio e a mesma barra de controle.'],
  ['iPad e navegador', 'web.png', 'Monitor web no navegador mostrando o prompter e a barra de controle', 'Pela rede, no iPad ou em qualquer navegador: acompanhe e controle o prompter, em tela cheia e com rotação.'],
  ['Voz', 'voz.png', 'Painel de acompanhamento por voz com entrada de áudio, nível e idioma', 'Acompanhamento por voz offline: escolha o microfone (WASAPI ou ASIO), o idioma e o motor de reconhecimento.'],
  ['Telas', 'telas.png', 'Painel de telas com prévia da saída e monitores conectados', 'Monitores conectados, janelas de prompter extras e as informações que aparecem na tela do talento.'],
  ['Overlay', 'overlay.png', 'Painel de overlay com tipo de indicador, cor e posição da linha de leitura', 'Indicador de leitura, faixa de destaque, posição da linha e imagem de fundo — tudo ajustável.'],
  ['Cores', 'cor.png', 'Seletor de cor do app com cores prontas e campos HEX e RGB', 'Seletor de cor próprio, com cores prontas para leitura em estúdio e as últimas usadas.']
];
const audience = [
  ['Telejornal e emissoras', 'Vários apresentadores, marcadores por bloco, integração com a redação e saída para o switcher.'],
  ['Igrejas e cultos', 'Leituras, avisos e mensagens com letra grande, controle pelo tablet e tela para o púlpito.'],
  ['Eventos e palestras', 'Monitor de retorno, contagem regressiva e controle remoto por clicker ou pedal.'],
  ['Vídeos e podcasts', 'Gravação com a câmera junto do prompter e acompanhamento por voz, sem operador.'],
  ['Ensino e cursos', 'Aulas gravadas com roteiro na tela e leitura natural, sem decorar o texto.']
];
const steps = [
  ['1', 'Crie sua conta', 'Nome, e-mail, empresa, telefone e uma senha. O download do instalador começa em seguida.'],
  ['2', 'Instale', 'O instalador coloca tudo em Arquivos de Programas em cerca de um minuto. Até ativar, o app funciona em modo demonstração.'],
  ['3', 'Ative com o código da conta', 'O código fica em Minha conta › Licenças. Cole no app e pronto. Trocou de computador? Remova a licença do antigo pela conta e ative no novo.']
];
const specs = [
  ['Sistema', 'Windows 10 21H2 ou mais novo, ou Windows 11 · 64 bits · 4 GB de memória'],
  ['Roteiros', 'Abre TPS, DOCX, PDF, RTF e TXT · salvamento automático e histórico de versões'],
  ['Saídas', 'Janela do talento, monitores extras, janelas flutuantes, NDI e navegador'],
  ['NDI', '1920×1080 a 25, 30 ou 60p · 1280×720 a 30p · fundo transparente opcional'],
  ['Navegador e iPad', 'Wi-Fi ou rede local · modo limpo e modo controle · senha opcional · endereço para OBS'],
  ['Voz', 'Offline · Vosk e Whisper · português, espanhol e inglês · microfone WASAPI ou ASIO'],
  ['Integrações', 'Redação MOS · legendas · Stream Deck e automação por linha de comando'],
  ['Controle', 'Atalhos remapeáveis e globais · clickers e pedais · roda do mouse e trackpad'],
  ['Vídeo', 'Câmera na tela do operador e gravação junto com o prompt'],
  ['Licença', 'Gratuita com conta no site · vitalícia · um computador por vez, com troca pela conta']
];
const compare = [
  ['Prompter', '10 minutos por dia', 'Sem limite'],
  ['Roteiros abertos', 'Um por vez', 'Vários, em abas'],
  ['Telas de prompter', 'Uma', 'Todas'],
  ['Saída NDI', '—', 'Incluída'],
  ['iPad e navegador', '—', 'Incluído'],
  ['Acompanhamento por voz', '—', 'Incluído'],
  ['Redação MOS e gravação', '—', 'Incluídas']
];
const faq = [
  ['Precisa de internet?', 'Só para ativar a licença e para baixar atualizações. Depois de ativado, o ARCA funciona sem internet — inclusive o acompanhamento por voz, que roda no próprio computador.'],
  ['Posso trocar de computador?', 'Sim. Cada licença fica ativa em um computador por vez. Em Minha conta › Licenças, clique em Remover deste computador e ative no novo com o mesmo código. Também dá para remover pelo próprio app, em Ajuda › Licença.'],
  ['O que a demonstração permite?', 'Sem licença, o ARCA abre em modo demonstração: 10 minutos de prompter por dia, um roteiro e uma tela de prompter. NDI, navegador e iPad, voz, redação (MOS) e gravação ficam liberados com a licença da conta.'],
  ['Funciona com vMix e OBS?', 'Sim. Pela saída NDI (1080p a 25, 30 ou 60 quadros, com fundo transparente opcional) ou pelo monitor web, que tem um endereço próprio para OBS sem nenhuma interface na tela.'],
  ['Consigo abrir meus roteiros atuais?', 'O ARCA abre arquivos TPS, DOCX, PDF, RTF e TXT. Marcadores e pausas podem ser escritos direto no texto (*Título e [PAUSA]).'],
  ['Como recebo as atualizações?', 'Ao abrir, o app confere se há versão nova. Se houver, mostra as novidades: um clique baixa, instala e reabre o app, mantendo roteiros e preferências.'],
  ['Onde fica meu código?', 'Em Minha conta › Licenças, no site. Lá você também vê em qual computador ele está ativado. Esqueceu a senha? Fale com o suporte informando o e-mail da conta.'],
  ['Quais dados vocês guardam?', 'Nome, e-mail, empresa e telefone, e o computador onde a licença está ativa. Seus roteiros nunca saem do seu computador. Em Minha conta você baixa todos os seus dados ou exclui a conta; os detalhes estão na política de privacidade (link no rodapé).'],
  ['A licença tem custo?', 'Por enquanto o cadastro é gratuito e a licença é vitalícia.']
];

const dl = '<svg width="18" height="18" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.6" aria-hidden="true"><path d="M8 2v8M4.5 6.8 8 10.2l3.5-3.4M2.5 13.5h11"/></svg>';
const eyebrow = t => `<span class="eyebrow">${esc(t)}</span>`;
const rollHtml = roll.concat(roll).map(([who, c, t]) => `<div class="roll-item"><span class="roll-who" style="color:${c}">${esc(who)}</span><p style="color:${c}">${esc(t)}</p></div>`).join('');

// Campos do cadastro: no início do site e na aba "Criar conta" de Minha conta.
const signupFields = `<label for="f-nome">Nome</label><input id="f-nome" name="name" autocomplete="name" placeholder="Seu nome" required>
<label for="f-email">E-mail</label><input id="f-email" name="email" type="email" autocomplete="email" placeholder="voce@empresa.com" required>
<label for="f-empresa">Empresa</label><input id="f-empresa" name="company" autocomplete="organization" placeholder="Empresa, emissora ou igreja" required>
<label for="f-tel">Telefone (com DDD)</label><input id="f-tel" name="phone" type="tel" autocomplete="tel" placeholder="(11) 99999-0000" required>
<label for="f-senha">Senha</label><input id="f-senha" name="password" type="password" autocomplete="new-password" placeholder="Pelo menos 8 caracteres" minlength="8" required>`;

// Estilo comum ao site e à página Minha conta.
const css = `:root{--bg:#0B0B0D;--panel:#101012;--card:#131315;--line:#1F1F23;--line2:#242428;--text:#ECECEF;--muted:#A8A8B2;--faint:#8A8A94;--accent:#E5484D;--accent2:#FF8A8D}
*{box-sizing:border-box}[hidden]{display:none!important}html{scroll-behavior:smooth;scroll-padding-top:80px}
body{margin:0;background:var(--bg);color:var(--text);font:16px/1.6 Inter,system-ui,sans-serif;overflow-x:hidden;-webkit-font-smoothing:antialiased}
a{color:var(--text);text-decoration:none}a:hover{color:var(--accent2)}
img{max-width:100%}
.wrap{max-width:1200px;margin:0 auto;padding:0 24px}
.eyebrow{font-size:12px;font-weight:600;letter-spacing:.14em;text-transform:uppercase;color:var(--accent2)}
h1,h2,h3{margin:0}h2{font:700 42px/1.12 Sora,sans-serif;letter-spacing:-.015em;margin:10px 0 12px}
.lead{color:var(--muted);max-width:40em;margin:0 0 40px}
.btn{display:inline-flex;align-items:center;justify-content:center;gap:10px;min-height:52px;padding:0 28px;border-radius:999px;background:var(--accent);color:#fff;font-weight:600;font-size:16px;border:0;cursor:pointer;transition:transform .2s,box-shadow .2s;font-family:inherit}
.btn:hover{transform:translateY(-2px);box-shadow:0 10px 30px rgba(229,72,77,.35);color:#fff}
.btn-ghost{display:inline-flex;align-items:center;gap:10px;min-height:52px;padding:0 26px;border-radius:999px;border:1px solid #3A3A40;color:var(--text);font-weight:600}
.band{background:var(--panel);border-top:1px solid var(--line);border-bottom:1px solid var(--line)}
section{padding:80px 0}
/* topo */
header{position:sticky;top:0;z-index:20;background:rgba(11,11,13,.82);backdrop-filter:blur(14px);border-bottom:1px solid var(--line)}
header .wrap{padding-top:16px;padding-bottom:16px;display:flex;flex-wrap:wrap;align-items:center;justify-content:space-between;gap:16px}
.brand{display:flex;align-items:center;gap:14px}.brand b{font:800 17px Sora,sans-serif;letter-spacing:.32em}.brand i{width:1px;height:18px;background:#34343A}.brand span{font-size:14px;font-weight:500;color:var(--muted)}
nav{display:flex;flex-wrap:wrap;align-items:center;gap:26px;font-size:14px;font-weight:500}
nav .btn{min-height:38px;padding:0 18px;font-size:14px}
/* abertura */
.hero{position:relative;overflow:hidden;padding:96px 0 64px}
.hero .wrap{position:relative;display:flex;flex-wrap:wrap;align-items:center;gap:56px}
.hero-text{flex:1 1 460px;min-width:0;display:flex;flex-direction:column;gap:22px}
.pill{align-self:flex-start;display:inline-flex;align-items:center;gap:8px;font-size:12px;font-weight:600;letter-spacing:.14em;text-transform:uppercase;color:var(--accent2);padding:6px 14px;border:1px solid #4A2326;border-radius:999px;background:#1A1112}
.dot{width:7px;height:7px;border-radius:4px;background:var(--accent)}
h1{font:800 60px/1.04 Sora,sans-serif;letter-spacing:-.025em}
.hero p.sub{margin:0;font-size:19px;color:#B4B4BE;max-width:34em}
.hero .note{margin:0;font-size:13.5px;color:var(--faint)}
.row{display:flex;flex-wrap:wrap;gap:12px}
.glow{position:absolute;border-radius:50%;pointer-events:none;background:radial-gradient(closest-side,rgba(229,72,77,.28),rgba(229,72,77,0))}
.prompter-wrap{flex:1 1 480px;min-width:0;position:relative}
.prompter{position:relative;border-radius:20px;border:1px solid #2A2A30;background:#000;overflow:hidden;box-shadow:0 40px 120px rgba(0,0,0,.6)}
.chrome{display:flex;align-items:center;gap:8px;padding:10px 14px;background:#161619;border-bottom:1px solid #26262B;font-size:12px;color:var(--faint)}
.chrome i{width:10px;height:10px;border-radius:5px;background:#3A3A40}
.chrome .live{margin-left:auto;display:inline-flex;align-items:center;gap:6px;font-size:11px;font-weight:600;letter-spacing:.1em;color:var(--accent2)}
.stage{position:relative;height:360px;overflow:hidden}
.roll{padding:0 28px 0 56px;display:flex;flex-direction:column}
.roll-item{padding:14px 0;display:flex;flex-direction:column;gap:4px}.roll-who{font-size:11px;letter-spacing:.14em}
.roll-item p{margin:0;font-size:30px;line-height:1.25;font-weight:500}
.fade-top,.fade-bottom{position:absolute;left:0;right:0;pointer-events:none}.fade-top{top:0;height:90px;background:linear-gradient(#000,rgba(0,0,0,0))}.fade-bottom{bottom:0;height:110px;background:linear-gradient(rgba(0,0,0,0),#000)}
.arrow{position:absolute;left:18px;top:118px}
.hud{position:absolute;left:16px;right:16px;bottom:14px;display:flex;justify-content:space-between;font:13px Sora,sans-serif}.hud span{padding:4px 10px;border-radius:8px;background:#1A1A1D}
.tag{position:absolute;padding:8px 14px;border-radius:12px;background:#17171A;border:1px solid #2E2E34;font-size:13px;font-weight:600;box-shadow:0 10px 30px rgba(0,0,0,.5)}
/* faixa */
.ticker{border-top:1px solid var(--line);border-bottom:1px solid var(--line);background:var(--panel);overflow:hidden;padding:18px 0}
.ticker-track{display:flex;gap:40px;width:max-content;font:600 15px Sora,sans-serif;color:var(--faint);white-space:nowrap}
.ticker-track span{display:inline-flex;align-items:center;gap:40px}.ticker-track em{font-style:normal;color:var(--accent)}
/* grades e cartões */
.grid{display:grid;gap:16px}
.facts{grid-template-columns:repeat(auto-fit,minmax(220px,1fr))}
.fact{padding:24px;border-radius:16px;border:1px solid #222226;background:#111113}.fact b{font:800 44px/1 Sora,sans-serif}.fact b span{color:var(--accent)}.fact p{margin:10px 0 0;font-size:14.5px;color:var(--muted)}
.cards{grid-template-columns:repeat(auto-fit,minmax(300px,1fr))}
.card{padding:26px;border-radius:18px;background:var(--card);border:1px solid var(--line2);display:flex;flex-direction:column;gap:10px;transition:transform .25s,border-color .25s,background .25s}
.card:hover{transform:translateY(-6px);border-color:#5A2A2D;background:#1B1517}
.card h3{font-size:18px;font-weight:600;margin-top:6px}.card p{margin:0;font-size:14.5px;color:var(--muted)}
.ico{width:44px;height:44px;border-radius:12px;background:#241517;color:var(--accent2);display:flex;align-items:center;justify-content:center}
/* galeria */
.gal-head{display:flex;flex-wrap:wrap;align-items:flex-end;justify-content:space-between;gap:20px;margin-bottom:28px}
.tabs{display:flex;flex-wrap:wrap;gap:8px}
.tab{min-height:44px;padding:0 16px;border-radius:999px;border:1px solid #34343A;background:transparent;color:#C8C8D0;font:600 14px Inter,sans-serif;cursor:pointer;transition:background .2s,color .2s,border-color .2s}
.tab:hover{border-color:#5A2A2D}.tab[aria-selected=true]{background:var(--accent);color:#fff;border-color:var(--accent)}
.viewer{position:relative;border-radius:20px;border:1px solid #2A2A30;background:#000;overflow:hidden;aspect-ratio:1440/820;box-shadow:0 40px 120px rgba(0,0,0,.55)}
.viewer img{position:absolute;inset:0;width:100%;height:100%;object-fit:contain;background:var(--bg);opacity:0;transform:scale(1.02);transition:opacity .6s,transform .6s}
.viewer img.on{opacity:1;transform:none}
.caption{display:flex;flex-wrap:wrap;justify-content:space-between;gap:16px;margin-top:18px}.caption p{margin:0;color:#C8C8D0;max-width:46em}.caption span{font:14px Sora,sans-serif;color:var(--faint)}
/* passos */
.step{position:relative;overflow:hidden}.step .big{position:absolute;right:18px;top:4px;font:800 96px/1 Sora,sans-serif;color:#1E1E22}
.step .num{position:relative;width:38px;height:38px;border-radius:19px;background:var(--accent);color:#fff;font:700 16px Sora,sans-serif;display:flex;align-items:center;justify-content:center}
.step h3,.step p{position:relative}
/* especificações */
.specs{display:grid;grid-template-columns:repeat(auto-fit,minmax(min(100%,460px),1fr));gap:0 56px}
.spec{display:flex;gap:20px;padding:16px 0;border-bottom:1px solid #222226;font-size:14px}.spec b{flex:0 0 150px;font-weight:600}.spec span{color:var(--muted)}
/* licença */
.lic{display:flex;flex-wrap:wrap;gap:40px;align-items:flex-start}
.lic-text{flex:1 1 440px;min-width:0;display:flex;flex-direction:column;gap:18px}
.cmp{border-radius:16px;border:1px solid var(--line2);overflow:hidden;font-size:14px}
.cmp div{display:grid;grid-template-columns:repeat(3,minmax(0,1fr))}.cmp div+div{border-top:1px solid var(--line)}.cmp span{padding:12px 16px}
.cmp .head{background:#151517;font-weight:600;color:var(--faint)}.cmp .head span:last-child{color:var(--accent2)}.cmp .demo{color:var(--faint)}
.form-card{flex:1 1 400px;min-width:0;padding:32px;border-radius:22px;background:#151517;border:1px solid #2A2A30;box-shadow:0 30px 80px rgba(0,0,0,.45)}
form{display:flex;flex-direction:column;gap:6px}
form h3,.done h3{font:700 23px Sora,sans-serif}
label{font-size:13px;color:#B4B4BE;margin-top:8px}
input{height:46px;border-radius:10px;border:1px solid #34343A;background:#0D0D0F;color:var(--text);padding:0 14px;font:inherit}
input::placeholder{color:#6E6E78}input:focus{outline:2px solid var(--accent);outline-offset:1px;border-color:transparent}
.err{color:#FF9A9A;font-size:14px;min-height:1.2em;margin:6px 0 0}
.small{font-size:12.5px;color:var(--faint);margin:6px 0 0}
.done{display:flex;flex-direction:column;gap:14px}
.code{display:flex;align-items:center;gap:10px;padding:14px 14px 14px 18px;border-radius:12px;border:1px dashed var(--accent);background:#0D0D0F}
.code span{flex:1 1 auto;font:600 24px Consolas,ui-monospace,monospace;letter-spacing:.06em;user-select:all;word-break:break-all}
.copy{flex:none;min-height:44px;padding:0 16px;border-radius:999px;border:1px solid #3A3A40;background:transparent;color:var(--text);font:600 13.5px Inter,sans-serif;cursor:pointer}
.done ol{margin:4px 0 0;padding-left:20px;color:#B4B4BE;font-size:14.5px;display:flex;flex-direction:column;gap:4px}.done b{color:var(--text)}
/* dúvidas */
.faq{max-width:860px}.faq-list{border-top:1px solid var(--line2)}
.faq-item{border-bottom:1px solid var(--line2)}
.faq-item button{width:100%;min-height:60px;display:flex;align-items:center;justify-content:space-between;gap:16px;padding:16px 8px;border:0;background:transparent;color:var(--text);font:600 17px Inter,sans-serif;text-align:left;cursor:pointer;transition:background .2s}
.faq-item button:hover{background:#17171A}
.faq-item button i{flex:none;width:30px;height:30px;border-radius:15px;border:1px solid #34343A;display:flex;align-items:center;justify-content:center;color:var(--accent2);font-style:normal;font-size:18px;transition:transform .25s}
.faq-item.open button i{transform:rotate(45deg)}
.faq-item p{margin:0;padding:0 8px;color:var(--muted);font-size:15px;max-width:46em;max-height:0;overflow:hidden;transition:max-height .35s ease,padding .35s ease}
.faq-item.open p{max-height:260px;padding:0 8px 20px}
.final{position:relative;overflow:hidden;border-top:1px solid var(--line);text-align:center}
.final .wrap{position:relative;max-width:860px;display:flex;flex-direction:column;align-items:center;gap:20px;padding-top:16px;padding-bottom:16px}
.final h2{font:800 48px/1.08 Sora,sans-serif;letter-spacing:-.02em;margin:0}
footer{border-top:1px solid var(--line)}footer .wrap{padding-top:32px;padding-bottom:32px;display:flex;flex-wrap:wrap;align-items:center;justify-content:space-between;gap:16px;font-size:13.5px;color:var(--faint)}
footer b{font:800 14px Sora,sans-serif;letter-spacing:.3em;color:var(--text)}
/* movimento */
@keyframes scrollUp{from{transform:translateY(0)}to{transform:translateY(-50%)}}
@keyframes marquee{from{transform:translateX(0)}to{transform:translateX(-50%)}}
@keyframes pulse{0%,100%{opacity:1;transform:translateX(0)}50%{opacity:.55;transform:translateX(4px)}}
@keyframes bob{0%,100%{transform:translateY(0)}50%{transform:translateY(-10px)}}
@keyframes blink{0%,100%{opacity:1}50%{opacity:.25}}
@keyframes breathe{0%,100%{opacity:.45}50%{opacity:.8}}
@keyframes rise{from{opacity:0;transform:translateY(28px)}to{opacity:1;transform:none}}
.roll{animation:scrollUp 38s linear infinite}.ticker-track{animation:marquee 40s linear infinite}
.arrow{animation:pulse 1.6s ease-in-out infinite}.dot{animation:blink 1.4s ease-in-out infinite}.glow{animation:breathe 6s ease-in-out infinite}
.tag.a{animation:bob 5s ease-in-out infinite}.tag.b{animation:bob 6s ease-in-out -2s infinite}.tag.c{animation:bob 7s ease-in-out -4s infinite}
.in{animation:rise .9s cubic-bezier(.2,.7,.2,1) both}.in2{animation:rise .9s cubic-bezier(.2,.7,.2,1) .15s both}.in3{animation:rise .9s cubic-bezier(.2,.7,.2,1) .3s both}
.reveal{opacity:0;transform:translateY(28px);transition:opacity .8s cubic-bezier(.2,.7,.2,1),transform .8s cubic-bezier(.2,.7,.2,1)}.reveal.seen{opacity:1;transform:none}
@media (prefers-reduced-motion:reduce){*{animation:none!important;transition:none!important}.reveal{opacity:1;transform:none}html{scroll-behavior:auto}}
@media (max-width:700px){h1{font-size:40px}h2{font-size:32px}.final h2{font-size:34px}nav{gap:16px}nav a:not(.btn){display:none}.tag{display:none}.roll-item p{font-size:22px}.spec{flex-direction:column;gap:2px}.spec b{flex:none}section{padding:56px 0}.hero{padding-top:56px}}`;

const html = `<!doctype html>
<html lang="pt-BR">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>ARCA Teleprompter — teleprompter para estúdio, eventos e jornalismo</title>
<meta name="description" content="Teleprompter para Windows com editor de roteiro, janela do talento, monitores extras, saída NDI para vMix e OBS, iPad pela rede e acompanhamento por voz offline. Gratuito com cadastro.">
<meta property="og:title" content="ARCA Teleprompter">
<meta property="og:description" content="O teleprompter que acompanha a operação ao vivo. Gratuito com cadastro.">
<meta property="og:image" content="/img/editor.png">
<meta name="theme-color" content="#0B0B0D">
<link rel="icon" href="/favicon.ico">
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Sora:wght@400;600;700;800&family=Inter:wght@400;500;600&display=swap" rel="stylesheet">
<style>
${css}
</style>
</head>
<body>
<header><div class="wrap">
<a class="brand" href="#inicio"><b>ARCA</b><i></i><span>Teleprompter</span></a>
<nav aria-label="Principal"><a href="#recursos">Recursos</a><a href="#telas">Telas</a><a href="#especificacoes">Especificações</a><a href="#licenca">Licença</a><a href="#perguntas">Dúvidas</a><a href="/conta">Entrar</a><a class="btn" href="#licenca">Criar conta grátis</a></nav>
</div></header>

<main>
<section id="inicio" class="hero">
<div class="glow" style="left:50%;top:-260px;width:900px;height:600px;margin-left:-450px"></div>
<div class="wrap">
<div class="hero-text">
<span class="pill in"><span class="dot"></span>Para Windows · <span data-version>versão atual</span></span>
<h1 class="in2">O teleprompter que acompanha a operação ao vivo.</h1>
<p class="sub in3">Roteiro, prompter, monitores extras, NDI para vMix e OBS, iPad pela rede e acompanhamento por voz — num app só, feito para estúdio, eventos e jornalismo.</p>
<div class="row in3"><a class="btn" href="#licenca">${dl}Baixar grátis</a><a class="btn-ghost" href="#telas"><svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor" aria-hidden="true"><path d="M4.5 3v10l8.5-5z"/></svg>Ver o app por dentro</a></div>
<p class="note">Conta gratuita · licença vitalícia · Windows 10 e 11 (64 bits)</p>
</div>
<div class="prompter-wrap">
<div class="prompter" role="img" aria-label="Prompter rolando: texto grande em fundo preto com a seta de leitura">
<div class="chrome"><i></i><i></i><i></i><span style="margin-left:10px">Janela do talento</span><span class="live"><span class="dot"></span>AO VIVO</span></div>
<div class="stage">
<div class="roll">${rollHtml}</div>
<div class="fade-top"></div><div class="fade-bottom"></div>
<svg class="arrow" width="22" height="26" viewBox="0 0 22 26" aria-hidden="true"><path d="M0 0 22 13 0 26z" fill="#E5484D"/></svg>
<div class="hud"><span>Abertura <span style="color:#FF8A8D;padding:0;background:none">00:42</span></span><span>1,20 linhas/s</span></div>
</div>
</div>
<span class="tag a" style="right:-14px;top:64px">NDI · 1080p60</span>
<span class="tag b" style="left:-18px;bottom:96px"><span style="color:#3DD68C">●</span> Voz offline</span>
<span class="tag c" style="right:40px;bottom:-16px">iPad pela rede</span>
</div>
</div>
</section>

<div class="ticker" aria-hidden="true"><div class="ticker-track">${ticker.concat(ticker).map(t => `<span>${esc(t)}<em>✦</em></span>`).join('')}</div></div>

<section style="padding-bottom:24px"><div class="wrap"><div class="grid facts">
${facts.map(([n, u, t]) => `<div class="fact reveal"><b>${n}<span>${u}</span></b><p>${esc(t)}</p></div>`).join('\n')}
</div></div></section>

<section id="recursos" style="padding-top:56px"><div class="wrap">
<div class="reveal">${eyebrow('Recursos')}<h2>Tudo o que a operação precisa, sem plugins.</h2><p class="lead">Do roteiro escrito no editor até a saída no switcher: o ARCA cuida da leitura, das telas e da integração com o resto do estúdio.</p></div>
<div class="grid cards">
${features.map(([t, d, ic]) => `<div class="card reveal"><span class="ico"><svg width="22" height="22" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="${ic}"/></svg></span><h3>${esc(t)}</h3><p>${esc(d)}</p></div>`).join('\n')}
</div>
</div></section>

<section id="telas" class="band"><div class="wrap">
<div class="gal-head reveal"><div>${eyebrow('O app por dentro')}<h2 style="margin-bottom:0">Telas reais do ARCA.</h2></div>
<div class="tabs" role="tablist" aria-label="Telas do app">
${shots.map(([t], i) => `<button type="button" class="tab" role="tab" id="tab-${i}" aria-controls="viewer" aria-selected="${i === 0}" data-i="${i}">${esc(t)}</button>`).join('')}
</div></div>
<div class="viewer reveal" id="viewer" role="tabpanel">
${shots.map(([, f, alt], i) => `<img src="/img/${f}" alt="${esc(alt)}" width="1440" height="820" loading="${i ? 'lazy' : 'eager'}"${i ? '' : ' class="on"'}>`).join('\n')}
</div>
<div class="caption"><p id="caption">${esc(shots[0][3])}</p><span id="counter">1 / ${shots.length}</span></div>
</div></section>

<section><div class="wrap">
<div class="reveal">${eyebrow('Para quem é')}<h2 style="margin-bottom:36px">Feito para quem lê ao vivo.</h2></div>
<div class="grid" style="grid-template-columns:repeat(auto-fit,minmax(210px,1fr));gap:14px">
${audience.map(([t, d]) => `<div class="card reveal" style="padding:22px;gap:6px"><h3 style="font-size:16.5px;margin:0">${esc(t)}</h3><p style="font-size:14px">${esc(d)}</p></div>`).join('\n')}
</div>
</div></section>

<section id="como-funciona" style="padding-top:16px"><div class="wrap">
<div class="reveal">${eyebrow('Como começar')}<h2 style="margin-bottom:36px">Três passos, cinco minutos.</h2></div>
<div class="grid" style="grid-template-columns:repeat(auto-fit,minmax(280px,1fr))">
${steps.map(([n, t, d]) => `<div class="card step reveal" style="padding:30px"><span class="big" aria-hidden="true">${n}</span><span class="num">${n}</span><h3 style="font-size:19px">${esc(t)}</h3><p>${esc(d)}</p></div>`).join('\n')}
</div>
</div></section>

<section id="especificacoes" class="band"><div class="wrap">
<div class="reveal">${eyebrow('Especificações')}<h2 style="margin-bottom:36px">Os detalhes técnicos.</h2></div>
<div class="specs">
${specs.map(([k, v]) => `<div class="spec reveal"><b>${esc(k)}</b><span>${esc(v)}</span></div>`).join('\n')}
</div>
</div></section>

<section id="licenca"><div class="wrap lic">
<div class="lic-text reveal">
${eyebrow('Licença')}<h2 style="margin:0">Libere o app completo — de graça.</h2>
<p style="margin:0;color:var(--muted)">Sem licença, o ARCA abre em modo demonstração. Com a conta gratuita, tudo fica liberado em um computador, sem prazo de validade — e você troca de computador quando quiser.</p>
<div class="cmp"><div class="head"><span></span><span>Demonstração</span><span>Com a conta</span></div>
${compare.map(([a, b, c]) => `<div><span>${esc(a)}</span><span class="demo">${esc(b)}</span><span>${esc(c)}</span></div>`).join('\n')}
</div>
</div>
<div class="form-card reveal">
<form id="signup" novalidate>
<h3>Crie sua conta grátis</h3>
<p style="margin:0 0 4px;font-size:14px;color:var(--muted)">O download começa em seguida. Sua licença fica guardada na conta.</p>
${signupFields}
<button class="btn" type="submit" id="go" style="margin-top:16px">${dl}Criar conta e baixar</button>
<p class="err" id="err" role="alert"></p>
<p class="small">Usamos seus dados só para a licença e para avisos sobre o app. <a href="/privacidade" style="text-decoration:underline">Política de privacidade</a></p>
<p class="small" style="border-top:1px solid #26262B;padding-top:14px;margin-top:10px;font-size:13.5px">Já tem conta? <a href="/conta" style="text-decoration:underline">Entrar</a> · Windows 10 e 11, 64 bits<span data-size></span></p>
</form>
</div>
</div></section>

<section id="perguntas" class="band"><div class="wrap faq">
<div class="reveal">${eyebrow('Dúvidas')}<h2 style="margin-bottom:28px">Perguntas frequentes</h2></div>
<div class="faq-list">
${faq.map(([q, a], i) => `<div class="faq-item${i === 0 ? ' open' : ''}"><button type="button" aria-expanded="${i === 0}" aria-controls="faq-${i}">${esc(q)}<i aria-hidden="true">+</i></button><p id="faq-${i}">${esc(a)}</p></div>`).join('\n')}
</div>
</div></section>

<section class="final">
<div class="glow" style="left:50%;bottom:-320px;width:1000px;height:600px;margin-left:-500px;background:radial-gradient(closest-side,rgba(229,72,77,.25),rgba(229,72,77,0))"></div>
<div class="wrap reveal"><h2>Pronto para o próximo ao vivo?</h2><p style="margin:0;color:#B4B4BE;font-size:18px">Crie a conta, baixe e ative em cinco minutos.</p>
<div class="row" style="justify-content:center"><a class="btn" href="#licenca">Criar conta grátis</a><a class="btn-ghost" href="#telas">Ver o app por dentro</a></div></div>
</section>
</main>

<footer><div class="wrap"><span><b>ARCA</b> · Teleprompter para estúdio, eventos e jornalismo</span>
<nav aria-label="Rodapé" style="gap:22px"><a href="/conta">Minha conta</a><a href="#licenca">Download</a><a href="#perguntas">Dúvidas</a><a href="/privacidade">Privacidade</a><a href="https://github.com/cassioalvees/teleprompter/releases">Versões</a></nav></div></footer>

<script>
(() => {
  const $ = s => document.querySelector(s), $$ = s => [...document.querySelectorAll(s)];
  // Seções aparecem ao rolar
  const io = 'IntersectionObserver' in window ? new IntersectionObserver(es => es.forEach(e => { if (e.isIntersecting) { e.target.classList.add('seen'); io.unobserve(e.target); } }), { threshold: 0.12 }) : null;
  $$('.reveal').forEach(el => io ? io.observe(el) : el.classList.add('seen'));
  // Galeria: troca sozinha a cada 4,8 s até a pessoa escolher uma aba
  const caps = ${JSON.stringify(shots.map(s => s[3]))};
  const tabs = $$('.tab'), imgs = $$('#viewer img'); let cur = 0, auto = true;
  const show = i => { cur = i; tabs.forEach((t, k) => t.setAttribute('aria-selected', String(k === i))); imgs.forEach((im, k) => im.classList.toggle('on', k === i)); $('#caption').textContent = caps[i]; $('#counter').textContent = (i + 1) + ' / ' + caps.length; };
  tabs.forEach(t => t.addEventListener('click', () => { auto = false; show(+t.dataset.i); }));
  setInterval(() => { if (auto && !document.hidden) show((cur + 1) % caps.length); }, 4800);
  // Perguntas: uma aberta por vez
  $$('.faq-item button').forEach(b => b.addEventListener('click', () => {
    const it = b.parentElement, open = !it.classList.contains('open');
    $$('.faq-item').forEach(x => { x.classList.remove('open'); x.querySelector('button').setAttribute('aria-expanded', 'false'); });
    if (open) { it.classList.add('open'); b.setAttribute('aria-expanded', 'true'); }
  }));
  // Versão e tamanho da última versão publicada
  fetch('/api/latest').then(r => r.ok ? r.json() : null).then(d => {
    if (!d || !d.version) return;
    $$('[data-version]').forEach(e => e.textContent = 'versão ' + d.version);
    if (d.size) $$('[data-size]').forEach(e => e.textContent = ' · ' + Math.round(d.size / 1048576) + ' MB');
  }).catch(() => {});
  // Criar conta: depois vai para Minha conta, que mostra o código e inicia o download.
  $('#signup').addEventListener('submit', async e => {
    e.preventDefault(); const f = e.target; $('#err').textContent = ''; $('#go').disabled = true;
    try {
      const r = await fetch('/api/account/signup', { method: 'POST', headers: { 'content-type': 'application/json' }, body: JSON.stringify(Object.fromEntries(new FormData(f))) });
      const d = await r.json();
      if (!d.ok) { $('#err').textContent = d.error || 'Não foi possível criar a conta.'; return; }
      location.href = '/conta?novo=1';
    } catch { $('#err').textContent = 'Sem conexão com o servidor. Tente de novo.'; }
    finally { $('#go').disabled = false; }
  });
})();
</script>
</body>
</html>
`;
fs.writeFileSync(out, html);
console.log('Gerado ' + out + ' (' + Math.round(html.length / 1024) + ' KB)');

// ——— Minha conta (public/conta.html): entrar, criar conta, Licenças e Meus dados ———
const monitor = '<svg width="22" height="22" viewBox="0 0 16 16" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="1.5" y="2.5" width="13" height="8.5" rx="1.5"/><path d="M5.5 14h5M8 11v3"/></svg>';
const conta = `<!doctype html>
<html lang="pt-BR">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Minha conta — ARCA Teleprompter</title>
<meta name="robots" content="noindex">
<meta name="theme-color" content="#0B0B0D">
<link rel="icon" href="/favicon.ico">
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Sora:wght@400;600;700;800&family=Inter:wght@400;500;600&display=swap" rel="stylesheet">
<style>
${css}
/* Minha conta */
.acc{min-height:calc(100vh - 160px);padding:56px 0 80px;position:relative;overflow:hidden}
.acc .wrap{position:relative;max-width:980px}
.auth{max-width:460px;margin:0 auto}
.seg{display:flex;padding:4px;border-radius:999px;background:#151517;border:1px solid #2A2A30;margin-bottom:18px}
.seg button{flex:1;min-height:42px;border:0;border-radius:999px;background:transparent;color:#C8C8D0;font:600 14px Inter,sans-serif;cursor:pointer}
.seg button[aria-selected=true]{background:var(--accent);color:#fff}
.auth .form-card{padding:28px}
.acc-head{display:flex;flex-wrap:wrap;align-items:flex-end;justify-content:space-between;gap:16px;margin-bottom:26px}
.acc-head h1{font:800 40px/1.1 Sora,sans-serif;letter-spacing:-.02em;margin:8px 0 6px}.acc-head p{margin:0;color:var(--muted)}
.ghost{min-height:42px;padding:0 18px;border-radius:999px;border:1px solid #3A3A40;background:transparent;color:var(--text);font:600 14px Inter,sans-serif;cursor:pointer}
.ghost:hover{border-color:#5A2A2D;color:var(--accent2)}
.banner{display:flex;flex-wrap:wrap;align-items:center;gap:10px 16px;padding:16px 18px;border-radius:14px;border:1px solid #1F4A33;background:#0F1F17;color:#BFEFD3;margin-bottom:22px;font-size:15px}
.banner b{color:#fff}.banner a{color:#7EE0AD;text-decoration:underline}
.acc .tabs{margin-bottom:22px}
.lic-card{border-radius:20px;border:1px solid #2A2A30;background:#131315;overflow:hidden}
.lic-top{display:flex;flex-wrap:wrap;align-items:flex-start;justify-content:space-between;gap:16px;padding:26px 26px 22px}
.lic-top .code{margin-top:10px;min-width:min(100%,420px)}
.state{display:inline-flex;align-items:center;gap:8px;padding:6px 12px;border-radius:999px;font-size:13px;font-weight:600;white-space:nowrap}
.state i{width:8px;height:8px;border-radius:4px;background:currentColor}
.state.on{background:rgba(48,164,108,.14);color:#7EE0AD}.state.free{background:#1E1E22;color:#C8C8D0}.state.off{background:rgba(229,72,77,.14);color:#FF9A9A}
.machine{display:flex;flex-wrap:wrap;align-items:center;gap:16px;padding:20px 26px;border-top:1px solid var(--line2);background:#101012}
.machine .ico{flex:none}.machine div{flex:1 1 260px;min-width:0}.machine b{display:block;font-size:16px}.machine small{display:block;color:var(--muted);font-size:13.5px;margin-top:2px}
.danger{min-height:44px;padding:0 18px;border-radius:999px;border:1px solid #5A2A2D;background:transparent;color:#FF9A9A;font:600 14px Inter,sans-serif;cursor:pointer}
.danger:hover{background:#2A1416}
.how{padding:20px 26px 24px;border-top:1px solid var(--line2)}.how ol{margin:8px 0 0;padding-left:20px;color:#B4B4BE;font-size:14.5px;display:flex;flex-direction:column;gap:4px}.how b{color:var(--text)}
.empty{padding:36px 26px;text-align:center;display:flex;flex-direction:column;align-items:center;gap:12px}.empty p{margin:0;color:var(--muted)}
.panes{display:grid;grid-template-columns:repeat(auto-fit,minmax(min(100%,380px),1fr));gap:16px}
.panes .form-card{padding:26px;box-shadow:none}
.ok{color:#7EE0AD;font-size:14px;min-height:1.2em;margin:6px 0 0}
input[readonly]{color:var(--muted)}
dialog{border:1px solid #2A2A30;border-radius:20px;background:#151517;color:var(--text);padding:26px;max-width:min(440px,calc(100vw - 32px))}
dialog::backdrop{background:rgba(0,0,0,.6);backdrop-filter:blur(3px)}
dialog h3{font:700 21px Sora,sans-serif;margin:0 0 8px}dialog p{margin:0 0 20px;color:var(--muted);font-size:15px}
dialog .row{justify-content:flex-end}
.btn.red{background:var(--accent)}
.loading{color:var(--faint);text-align:center;padding:80px 0}
@media (max-width:700px){.acc-head h1{font-size:30px}.lic-top,.machine,.how{padding-left:18px;padding-right:18px}.lic-top .code{flex-wrap:wrap}.lic-top .code span{flex-basis:100%;font-size:21px;word-break:normal;white-space:nowrap}}
</style>
</head>
<body>
<header><div class="wrap">
<a class="brand" href="/"><b>ARCA</b><i></i><span>Teleprompter</span></a>
<nav aria-label="Principal"><a href="/#recursos">Recursos</a><a href="/#perguntas">Dúvidas</a><a href="/download" class="btn">${dl}Baixar</a></nav>
</div></header>

<main class="acc">
<div class="glow" style="left:50%;top:-320px;width:900px;height:560px;margin-left:-450px"></div>
<div class="wrap">
<p class="loading" id="loading">Carregando…</p>

<div class="auth" id="auth" hidden>
<div style="text-align:center;margin-bottom:24px">${eyebrow('Minha conta')}<h2 style="margin:10px 0 6px">Suas licenças em um lugar.</h2><p style="margin:0;color:var(--muted)">Veja o código de ativação e troque de computador quando precisar.</p></div>
<div class="seg" role="tablist"><button type="button" role="tab" id="t-login" aria-selected="true">Entrar</button><button type="button" role="tab" id="t-signup" aria-selected="false">Criar conta</button></div>
<div class="form-card">
<form id="login" novalidate>
<label for="l-email">E-mail</label><input id="l-email" name="email" type="email" autocomplete="email" placeholder="voce@empresa.com" required>
<label for="l-senha">Senha</label><input id="l-senha" name="password" type="password" autocomplete="current-password" required>
<button class="btn" type="submit" style="margin-top:16px">Entrar</button>
<p class="err" role="alert"></p>
<p class="small">Esqueceu a senha? Fale com o suporte informando o e-mail da conta.</p>
</form>
<form id="signup" novalidate hidden>
${signupFields}
<button class="btn" type="submit" style="margin-top:16px">${dl}Criar conta e baixar</button>
<p class="err" role="alert"></p>
<p class="small">Gratuita · licença vitalícia · um computador por vez. <a href="/privacidade" style="text-decoration:underline">Política de privacidade</a></p>
</form>
</div>
</div>

<div id="panel" hidden>
<div class="acc-head"><div>${eyebrow('Minha conta')}<h1 id="hello"></h1><p id="who"></p></div><button type="button" class="ghost" id="logout">Sair</button></div>
<div class="banner" id="novo" hidden></div>
<div class="tabs" role="tablist"><button type="button" class="tab" role="tab" aria-selected="true" data-pane="lics">Licenças</button><button type="button" class="tab" role="tab" aria-selected="false" data-pane="dados">Meus dados</button></div>
<div id="lics"></div>
<div id="dados" hidden><div class="panes">
<form class="form-card" id="profile" novalidate><h3 style="font:700 19px Sora,sans-serif">Dados da conta</h3>
<label for="p-email">E-mail</label><input id="p-email" readonly>
<label for="p-nome">Nome</label><input id="p-nome" name="name" autocomplete="name" required>
<label for="p-empresa">Empresa</label><input id="p-empresa" name="company" autocomplete="organization" required>
<label for="p-tel">Telefone</label><input id="p-tel" name="phone" type="tel" autocomplete="tel" required>
<button class="btn" type="submit" style="margin-top:16px">Salvar</button><p class="err" role="alert"></p></form>
<form class="form-card" id="password" novalidate><h3 style="font:700 19px Sora,sans-serif">Trocar a senha</h3>
<label for="s-atual">Senha atual</label><input id="s-atual" name="current" type="password" autocomplete="current-password" required>
<label for="s-nova">Nova senha</label><input id="s-nova" name="password" type="password" autocomplete="new-password" placeholder="Pelo menos 8 caracteres" required>
<button class="btn" type="submit" style="margin-top:16px">Trocar a senha</button><p class="err" role="alert"></p>
<p class="small">Os outros navegadores onde a conta estiver aberta precisam entrar de novo.</p></form>
<form class="form-card" id="lgpd" novalidate><h3 style="font:700 19px Sora,sans-serif">Seus dados</h3>
<p class="small" style="margin:4px 0 12px;font-size:13.5px">Veja tudo o que guardamos sobre você ou exclua a conta. Saiba mais na <a href="/privacidade" style="text-decoration:underline">política de privacidade</a>.</p>
<a class="ghost" href="/api/account/export" download style="display:inline-flex;align-items:center;justify-content:center">Baixar meus dados</a>
<label for="x-senha" style="margin-top:18px">Excluir minha conta — confirme com a senha</label><input id="x-senha" name="password" type="password" autocomplete="current-password" required>
<button class="danger" type="submit" style="margin-top:12px">Excluir minha conta</button><p class="err" role="alert"></p>
<p class="small">Apaga a conta, as licenças (os computadores voltam ao modo demonstração) e o histórico. Não dá para desfazer.</p></form>
</div></div>
</div>
</div>
</main>

<dialog id="confirm"><h3 id="c-title"></h3><p id="c-text"></p><div class="row"><button type="button" class="ghost" value="no">Cancelar</button><button type="button" class="btn red" value="yes" style="min-height:44px">Remover</button></div></dialog>

<footer><div class="wrap"><span><b>ARCA</b> · Teleprompter para estúdio, eventos e jornalismo</span>
<nav aria-label="Rodapé" style="gap:22px"><a href="/">Início</a><a href="/#perguntas">Dúvidas</a><a href="/privacidade">Privacidade</a><a href="https://github.com/cassioalvees/teleprompter/releases">Versões</a></nav></div></footer>

<script>
(() => {
  const $ = s => document.querySelector(s), $$ = s => [...document.querySelectorAll(s)];
  const esc = s => String(s ?? '').replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]));
  const day = iso => iso ? new Date(iso).toLocaleDateString('pt-BR') : '';
  const ago = iso => { if (!iso) return ''; const d = Math.floor((Date.now() - new Date(iso)) / 864e5); return d <= 0 ? 'hoje' : d === 1 ? 'ontem' : 'há ' + d + ' dias'; };
  const api = async (path, data) => {
    try {
      const r = await fetch(path, data ? { method: 'POST', headers: { 'content-type': 'application/json' }, body: JSON.stringify(data) } : {});
      return await r.json();
    } catch { return { ok: false, error: 'Sem conexão com o servidor. Tente de novo.' }; }
  };
  const view = v => { $('#loading').hidden = true; $('#auth').hidden = v !== 'auth'; $('#panel').hidden = v !== 'panel'; };
  const novo = new URLSearchParams(location.search).has('novo');
  let me = null;

  // ——— Entrar / criar conta ———
  const authTab = which => {
    $('#t-login').setAttribute('aria-selected', String(which === 'login')); $('#t-signup').setAttribute('aria-selected', String(which === 'signup'));
    $('#login').hidden = which !== 'login'; $('#signup').hidden = which !== 'signup';
  };
  $('#t-login').onclick = () => authTab('login'); $('#t-signup').onclick = () => authTab('signup');
  const submit = (form, path, done) => form.addEventListener('submit', async e => {
    e.preventDefault(); const b = form.querySelector('button[type=submit]'), err = form.querySelector('.err');
    err.textContent = ''; err.className = 'err'; b.disabled = true;
    const d = await api(path, Object.fromEntries(new FormData(form)));
    b.disabled = false;
    if (!d.ok) { err.textContent = d.error || 'Não foi possível concluir.'; return; }
    done(d, err);
  });
  submit($('#login'), '/api/account/login', () => load());
  submit($('#signup'), '/api/account/signup', () => { history.replaceState(null, '', '/conta?novo=1'); load(true); });

  // ——— Painel ———
  async function load(fresh) {
    const d = await api('/api/account');
    if (!d.ok) { view('auth'); authTab(novo ? 'signup' : 'login'); return; }
    me = d; view('panel');
    $('#hello').textContent = 'Olá, ' + d.user.name.split(' ')[0];
    $('#who').textContent = d.user.email + (d.user.company ? ' · ' + d.user.company : '');
    $('#p-email').value = d.user.email; $('#p-nome').value = d.user.name; $('#p-empresa').value = d.user.company; $('#p-tel').value = d.user.phone;
    renderLics();
    if (fresh || novo) welcome();
  }

  // Conta recém-criada: começa o download (quando há versão publicada) e explica o próximo passo.
  async function welcome() {
    const b = $('#novo'); b.hidden = false;
    b.innerHTML = '<span>✓ <b>Conta criada.</b> Seu código de ativação está logo abaixo.</span>';
    const l = await api('/api/latest');
    if (l.ok) { b.innerHTML = '<span>✓ <b>Conta criada.</b> O download do instalador começou (versão ' + esc(l.version) + ').</span><a href="/download">Baixar de novo</a>'; setTimeout(() => { location.href = '/download'; }, 700); }
    history.replaceState(null, '', '/conta');
  }

  function renderLics() {
    const box = $('#lics');
    if (!me.licenses.length) {
      box.innerHTML = '<div class="lic-card"><div class="empty"><span class="ico">' + ${JSON.stringify(monitor)} + '</span><h3 style="font:700 20px Sora,sans-serif;margin:0">Você ainda não tem um código</h3><p>Gere o código de ativação da sua licença gratuita e vitalícia.</p><button type="button" class="btn" id="gen">Gerar código</button><p class="err" role="alert"></p></div></div>';
      $('#gen').onclick = async () => { const d = await api('/api/account/license', {}); if (!d.ok) { box.querySelector('.err').textContent = d.error; return; } load(); };
      return;
    }
    box.innerHTML = me.licenses.map((l, i) => {
      const blocked = l.status !== 'ativa', unlimited = l.kind === 'ilimitada';
      const state = unlimited && !blocked ? '<span class="state on"><i></i>Vários computadores</span>' : blocked ? '<span class="state off"><i></i>Bloqueada</span>' : l.active ? '<span class="state on"><i></i>Ativa em 1 computador</span>' : '<span class="state free"><i></i>Livre para ativar</span>';
      const machine = unlimited
        ? '<div class="machine"><span class="ico">' + ${JSON.stringify(monitor)} + '</span><div><b>Ativa em quantos computadores quiser</b><small>' + (l.machine_name ? 'Último usado: ' + esc(l.machine_name) + (l.app_version ? ' · versão ' + esc(l.app_version) : '') + (l.last_seen ? ' · ' + ago(l.last_seen) : '') : 'Ainda não ativada em nenhum computador.') + '</small></div></div>'
        : l.active
        ? '<div class="machine"><span class="ico">' + ${JSON.stringify(monitor)} + '</span><div><b>' + esc(l.machine_name || 'Computador sem nome') + '</b><small>Ativada em ' + day(l.activated_at) + (l.app_version ? ' · versão ' + esc(l.app_version) : '') + (l.last_seen ? ' · último contato ' + ago(l.last_seen) : '') + '</small></div><button type="button" class="danger" data-release="' + i + '">Remover deste computador</button></div>'
        : '<div class="machine"><span class="ico">' + ${JSON.stringify(monitor)} + '</span><div><b>Nenhum computador</b><small>' + (blocked ? 'Esta licença está bloqueada. Fale com o suporte.' : 'Abra o ARCA no computador onde vai usar e cole o código.') + '</small></div>' + (blocked ? '' : '<a class="ghost" href="/download" style="display:inline-flex;align-items:center;gap:8px">${dl}Baixar instalador</a>') + '</div>';
      return '<article class="lic-card"><div class="lic-top"><div style="flex:1 1 300px;min-width:0"><span class="eyebrow">Licença ' + (l.kind === 'vitalicia' ? 'vitalícia' : l.kind === 'ilimitada' ? 'ilimitada' : esc(l.kind)) + ' · desde ' + day(l.created_at) + '</span>'
        + '<div class="code"><span>' + esc(l.code) + '</span><button type="button" class="copy" data-copy="' + esc(l.code) + '">Copiar</button></div></div>' + state + '</div>' + machine
        + '<div class="how"><span class="eyebrow" style="color:var(--faint)">Como ativar</span><ol><li>Instale e abra o ARCA Teleprompter.</li><li>Na tela de ativação (ou em <b>Ajuda › Licença</b>), cole o código e clique em <b>Ativar</b>.</li><li>Para trocar de computador, clique em <b>Remover deste computador</b> aqui e ative no novo.</li></ol></div></article>';
    }).join('');
  }

  $('#lics').addEventListener('click', async e => {
    const c = e.target.closest('[data-copy]');
    if (c) { try { await navigator.clipboard.writeText(c.dataset.copy); c.textContent = 'Copiado'; setTimeout(() => c.textContent = 'Copiar', 1800); } catch {} return; }
    const r = e.target.closest('[data-release]'); if (!r) return;
    const l = me.licenses[+r.dataset.release];
    if (!(await ask('Remover de ' + (l.machine_name || 'este computador') + '?', 'O ARCA nesse computador volta ao modo demonstração assim que abrir com internet. O código ' + l.code + ' fica livre para ativar em outro computador.'))) return;
    r.disabled = true;
    const d = await api('/api/account/release', { code: l.code });
    if (!d.ok) { r.disabled = false; alert(d.error); return; }
    load();
  });

  // Confirmação (diálogo da página, não o do navegador)
  const ask = (title, text, yes = 'Remover') => new Promise(res => {
    const dlg = $('#confirm'); $('#c-title').textContent = title; $('#c-text').textContent = text; dlg.querySelector('[value=yes]').textContent = yes;
    dlg.querySelectorAll('button').forEach(b => b.onclick = () => { dlg.close(); res(b.value === 'yes'); });
    dlg.oncancel = () => res(false);
    dlg.showModal();
  });

  $$('.tab').forEach(t => t.addEventListener('click', () => {
    $$('.tab').forEach(x => x.setAttribute('aria-selected', String(x === t)));
    $('#lics').hidden = t.dataset.pane !== 'lics'; $('#dados').hidden = t.dataset.pane !== 'dados';
  }));
  const saved = (err, msg) => { err.className = 'ok'; err.textContent = msg; setTimeout(() => { err.textContent = ''; err.className = 'err'; }, 3000); };
  submit($('#profile'), '/api/account/profile', (d, err) => { saved(err, 'Dados salvos.'); load(); });
  submit($('#password'), '/api/account/password', (d, err) => { $('#password').reset(); saved(err, 'Senha trocada.'); });
  // Excluir a conta: confirma de novo antes de mandar (não dá para desfazer).
  $('#lgpd').addEventListener('submit', async e => {
    e.preventDefault(); const f = e.target, err = f.querySelector('.err'); err.textContent = '';
    if (!f.password.value) { err.textContent = 'Digite a sua senha para confirmar.'; return; }
    if (!(await ask('Excluir sua conta?', 'A conta, as licenças e o histórico serão apagados. Os computadores com o ARCA ativado voltam ao modo demonstração. Não dá para desfazer.', 'Excluir'))) return;
    const d = await api('/api/account/delete', { password: f.password.value });
    if (!d.ok) { err.textContent = d.error || 'Não foi possível excluir.'; return; }
    location.href = '/privacidade?excluida=1';
  });
  $('#logout').onclick = async () => { await api('/api/account/logout', {}); me = null; view('auth'); authTab('login'); };

  load();
})();
</script>
</body>
</html>
`;
const outConta = path.join(path.dirname(out), 'conta.html');

fs.writeFileSync(outConta, conta);
console.log('Gerado ' + outConta + ' (' + Math.round(conta.length / 1024) + ' KB)');

// ——— Política de privacidade (public/privacidade.html) ———
const ATUALIZADA = '27 de outubro de 2026';
// Contato do controlador (pedidos de titulares, LGPD).
const CONTATO = 'cassiosantos.csa@gmail.com';
const priv = [
  ['Quem somos', 'O ARCA Teleprompter é um aplicativo para Windows. Este site cuida das contas e das licenças do app. Esta política explica quais dados pessoais tratamos, para quê e como você controla esses dados, conforme a Lei Geral de Proteção de Dados (LGPD, Lei 13.709/2018).'],
  ['Quais dados guardamos', '<ul><li><b>Cadastro:</b> nome, e-mail, empresa e telefone.</li><li><b>Senha:</b> guardada cifrada (PBKDF2 com sal) — nunca a senha em si.</li><li><b>Licenças:</b> o código, o tipo, e para cada ativação o nome do computador, uma identificação técnica do computador (um código derivado do Windows, não o seu nome nem arquivos), a versão do app e a data da última conferência.</li><li><b>Segurança:</b> registro de cadastro, entradas, ativações e tentativas de senha errada, com data e endereço IP.</li><li><b>Sessão:</b> um cookie técnico que mantém você conectado em Minha conta por até 30 dias.</li></ul>'],
  ['O que NÃO guardamos', 'Seus <b>roteiros nunca saem do seu computador</b>. O áudio do acompanhamento por voz é processado só no seu computador. O app não envia telemetria nem dados de uso; ele acessa a internet apenas para ativar e conferir a licença, procurar atualizações e baixar modelos de voz.'],
  ['Para que usamos', '<ul><li>Criar e manter a sua conta e a sua licença (execução do serviço que você pediu).</li><li>Ativar a licença em um computador e permitir a troca de computador.</li><li>Segurança: impedir abuso, como tentativas repetidas de senha (legítimo interesse).</li><li>Avisos sobre o app, como versões novas e mudanças importantes. Não vendemos nem cedemos seus dados para publicidade.</li></ul>'],
  ['Com quem compartilhamos', 'Os dados ficam na <b>Cloudflare</b> (hospedagem do site e do banco de dados), que trata os dados em nosso nome. Os instaladores são baixados do <b>GitHub</b>. Não compartilhamos seus dados com mais ninguém, exceto quando a lei exigir.'],
  ['Por quanto tempo', 'Enquanto a conta existir. Ao excluir a conta, apagamos na hora o cadastro, as licenças, as sessões e o histórico ligado a você. Fica apenas um registro anônimo de que uma conta foi excluída.'],
  ['Seus direitos', 'Em <a href="/conta">Minha conta</a> você pode, a qualquer momento:<ul><li><b>ver e corrigir</b> nome, empresa e telefone (Meus dados);</li><li><b>baixar todos os seus dados</b> num arquivo (Meus dados › Baixar meus dados);</li><li><b>excluir a conta</b> e tudo o que está ligado a ela (Meus dados › Excluir minha conta);</li><li><b>remover a licença de um computador</b> (Licenças).</li></ul><p>Para qualquer outro pedido sobre seus dados — inclusive se não conseguir entrar na conta — escreva para <a href="mailto:' + CONTATO + '">' + CONTATO + '</a>.</p>'],
  ['Segurança', 'Conexões por https, senha cifrada, sessão em cookie protegido (HttpOnly), limite de tentativas de senha e licenças assinadas digitalmente. Detalhes técnicos no documento de segurança do projeto.'],
  ['Contato', 'Dúvidas, pedidos sobre seus dados pessoais ou sobre esta política: <a href="mailto:' + CONTATO + '">' + CONTATO + '</a>. Respondemos em até 15 dias, como prevê a LGPD.'],
  ['Mudanças nesta política', 'Quando esta política mudar, a data abaixo é atualizada e, se a mudança for importante, avisamos pelo app ou por e-mail.'],
];
const privacidade = `<!doctype html>
<html lang="pt-BR">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Política de privacidade — ARCA Teleprompter</title>
<meta name="description" content="Como o ARCA Teleprompter trata seus dados pessoais (LGPD).">
<meta name="theme-color" content="#0B0B0D">
<link rel="icon" href="/favicon.ico">
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Sora:wght@400;600;700;800&family=Inter:wght@400;500;600&display=swap" rel="stylesheet">
<style>
${css}
.doc{max-width:820px;padding:56px 0 80px}.doc h1{font:800 40px/1.1 Sora,sans-serif;letter-spacing:-.02em;margin:10px 0 8px}
.doc h2{font:700 22px Sora,sans-serif;margin:34px 0 8px}.doc p,.doc li{color:#C8C8D0;font-size:16px}.doc ul{padding-left:22px}.doc li{margin:4px 0}
.doc b{color:var(--text)}.doc a{text-decoration:underline}.upd{color:var(--faint);font-size:14px}
.ok{display:block;padding:14px 18px;border-radius:14px;border:1px solid #1F4A33;background:#0F1F17;color:#BFEFD3;margin:18px 0}
@media (max-width:700px){.doc h1{font-size:30px}}
</style>
</head>
<body>
<header><div class="wrap">
<a class="brand" href="/"><b>ARCA</b><i></i><span>Teleprompter</span></a>
<nav aria-label="Principal"><a href="/">Início</a><a href="/conta">Minha conta</a></nav>
</div></header>
<main class="wrap doc">
<span class="ok" id="excluida" hidden>✓ Sua conta e todos os dados ligados a ela foram excluídos.</span>
${eyebrow('LGPD')}<h1>Política de privacidade</h1>
<p class="upd">Atualizada em ${ATUALIZADA}</p>
${priv.map(([t, b]) => `<h2>${t}</h2>${b.startsWith('<') ? b : '<p>' + b + '</p>'}`).join('\n')}
</main>
<footer><div class="wrap"><span><b>ARCA</b> · Teleprompter para estúdio, eventos e jornalismo</span>
<nav aria-label="Rodapé" style="gap:22px"><a href="/">Início</a><a href="/conta">Minha conta</a></nav></div></footer>
<script>if (new URLSearchParams(location.search).has('excluida')) document.getElementById('excluida').hidden = false;</script>
</body>
</html>
`;
const outPriv = path.join(path.dirname(out), 'privacidade.html');
fs.writeFileSync(outPriv, privacidade);
console.log('Gerado ' + outPriv + ' (' + Math.round(privacidade.length / 1024) + ' KB)');
