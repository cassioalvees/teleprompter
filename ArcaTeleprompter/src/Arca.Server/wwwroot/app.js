// ARCA Teleprompter · cliente web / PWA (JavaScript puro, sem frameworks).
// Recebe o roteiro e a posição do app pelo WebSocket /ws e desenha o prompter no mesmo
// "espaço de 1000 unidades" do app. Protocolo em docs/05-protocolos.md.

const $ = s => document.querySelector(s);
const body = document.body;
const text = $('#text'), stage = $('#stage'), flip = $('#flip');

// ——— Preferências deste aparelho ———
const prefs = Object.assign({ mode: 'controle', flipH: false, flipV: false, rot: 0, hud: true, wake: true, swipe: true, pass: '' },
  JSON.parse(localStorage.getItem('arca-web') || '{}'));
const savePrefs = () => { try { localStorage.setItem('arca-web', JSON.stringify(prefs)); } catch (e) { /* modo privado */ } };

// ——— Estado recebido ———
let state = null;           // PrompterSnapshot
// y: posição no layout do app; b/o: bloco sob a linha de leitura e deslocamento dentro dele (unidades).
// Usar o deslocamento (e não uma fração) mantém a linha de leitura sobre uma linha do texto mesmo que a
// altura total do parágrafo varie um pouco entre o app e o navegador.
let feed = { y: 0, v: 0, b: -1, o: 0, at: performance.now() };
let clockOff = null;          // performance.now() − relógio do servidor (atraso mínimo)
let shown = null, lastFrame = performance.now(), localV = 0;
let ws = null, authed = false, retry = 0;

// ——— Conexão ———
function connect() {
  const url = (location.protocol === 'https:' ? 'wss://' : 'ws://') + location.host + '/ws';
  ws = new WebSocket(url);
  ws.onopen = () => { retry = 0; body.classList.add('online'); setConn('Conectado'); send({ t: 'auth', pass: prefs.pass }); };
  ws.onclose = () => {
    body.classList.remove('online'); authed = false; setConn('Reconectando…');
    setTimeout(connect, Math.min(5000, 600 * ++retry));
  };
  ws.onmessage = e => { let m; try { m = JSON.parse(e.data); } catch (err) { return; } onMessage(m); };
}
const send = m => { if (ws && ws.readyState === 1) ws.send(JSON.stringify(m)); };
const setConn = s => { $('#conn b').textContent = s; };

function onMessage(m) {
  if (m.t === 'auth') {
    authed = m.ok;
    $('#login').hidden = m.ok || !m.needPass;
    $('#loginErr').hidden = m.ok || !prefs.pass;
    $('#authMsg').textContent = m.ok ? (m.needPass ? 'Acesso liberado.' : 'Este monitor não tem senha.') : 'Senha incorreta.';
    return;
  }
  if (m.t === 'html') { text.innerHTML = m.html; layoutDirty = true; return; }
  if (m.t === 'state') { applyState(m.s); return; }
  if (m.t === 'y') {
    // Instante (no relógio local) em que o app mediu essa posição: o menor atraso visto é a referência,
    // assim mensagens que chegam atrasadas pela rede não fazem o texto recuar e avançar.
    const now = performance.now();
    let at = now;
    if (typeof m.ts === 'number') {
      const est = now - m.ts;
      clockOff = clockOff === null || est < clockOff ? est : clockOff + (est - clockOff) * 0.002;
      at = m.ts + clockOff;
    }
    feed = { y: m.y, v: m.v || 0, b: m.b ?? -1, o: m.o || 0, f: m.f ?? -1, bh: m.bh || 0, at };
    return;
  }
  if (m.t === 'flash') { flash(m.text, m.sub); }
}

// ——— Aparência ———
let layoutDirty = true;
// Instante da última pausa/play: logo depois dela a previsão pode ter passado um pouco do ponto.
let liveChangedAt = -1e9, lastTarget = 0, targetMovedAt = 0;
function applyState(s) {
  const prev = state; state = s;
  if (prev && !!prev.live !== !!s.live) liveChangedAt = performance.now();
  const r = document.documentElement.style;
  r.setProperty('--pbg', s.bg); r.setProperty('--pfg', s.fg);
  // O app envia Name/Color (maiúsculos): sem isso as falas ficavam brancas no navegador e no iPad.
  s.pres.forEach((p, i) => r.setProperty('--who' + (i + 1), p.color || p.Color || '#ffffff'));
  // Espaço entre parágrafos e linha em branco nas mesmas proporções do app (em linhas).
  r.setProperty('--pgap', (s.lineH * (s.paraGap ?? 0.25)) + 'em'); r.setProperty('--blank', (s.lineH * 0.5) + 'em');
  Object.assign(text.style, {
    fontFamily: `'${s.fontFamily}', sans-serif`, fontSize: s.fontSize + 'px', lineHeight: s.lineH,
    padding: `0 ${s.padR}px 0 ${s.padL}px`, textAlign: s.align
  });
  body.classList.toggle('names', s.presNames);
  text.classList.toggle('only', s.presOnly !== '0');
  if (s.presOnly !== '0') text.querySelectorAll('p').forEach(p => p.classList.toggle('focus', p.classList.contains('w' + s.presOnly)));
  body.classList.toggle('live', s.live);
  body.classList.toggle('playing', s.playing);
  body.classList.toggle('readonly', !s.webCtrl);
  $('#ro').hidden = s.webCtrl;
  $('#doc').textContent = s.docName || '';
  $('#playIcon').setAttribute('d', s.live ? 'M3.5 2.5h3v11h-3z M9.5 2.5h3v11h-3z' : 'M4 2.5l9.5 5.5L4 13.5z');
  if (document.activeElement !== $('#speed')) $('#speed').value = s.speedT;
  $('#speedLabel').textContent = s.speed;
  body.classList.toggle('reverse', s.dir < 0);
  $('#hudBlock').textContent = s.block || '—';
  $('#hudLeft').textContent = s.blockLeft; $('#hudLeft').style.color = s.blockLeftWarn ? '#ff6b6b' : '#fff';
  $('#lastBadge').hidden = !s.isLastBlock;
  $('#hudClock').textContent = s.clock;
  $('#count').hidden = !(s.count > 0); $('#count').textContent = s.count;
  if (prev && (prev.fontSize !== s.fontSize || prev.lineH !== s.lineH || prev.padL !== s.padL || prev.fontFamily !== s.fontFamily)) layoutDirty = true;
  if (prev && prev.speedT !== s.speedT && prev.live) flash(s.speed, s.dir < 0 ? 'Ré' : 'Frente');
  overlays();
}

function overlays() {
  const s = state; if (!s) return;
  const k = stage.clientWidth / 1000, rp = s.readPct / 100, lh = s.fontSize * s.lineH * k;
  const H = stage.clientHeight, top = H * rp;
  const dim = hexA(s.bg, s.ovOp);
  const dt = $('#dimTop'), db = $('#dimBottom'), hl = $('#hiLine');
  dt.hidden = db.hidden = !(s.ovType === 'faixa' || s.ovType === 'fade'); hl.hidden = s.ovType !== 'destaque';
  dt.style.height = top + 'px'; db.style.top = (top + lh) + 'px';
  dt.style.background = s.ovType === 'fade' ? `linear-gradient(${dim},transparent)` : dim;
  db.style.background = s.ovType === 'fade' ? `linear-gradient(transparent,${dim})` : dim;
  Object.assign(hl.style, { top: top + 'px', height: lh + 'px', background: hexA(s.indColor, s.ovOp * 0.3) });
  const rl = $('#readline'); rl.hidden = !s.readingLine; Object.assign(rl.style, { top: (top + lh / 2) + 'px', background: s.indColor });
  const ind = $('#indicator');
  ind.hidden = s.indType === 'nenhum';
  const shape = { seta: ['polygon(0 0,100% 50%,0 100%)', '0'], quadrado: ['none', '2px'], ponto: ['none', '50%'], linha: ['none', '1px'] }[s.indType] || ['none', '0'];
  Object.assign(ind.style, { left: s.indLeft * k + 'px', top: (top + lh / 2) + 'px', width: s.indW * k + 'px', height: s.indH * k + 'px', background: s.indColor, clipPath: shape[0], borderRadius: shape[1] });
  flip.style.transform = `scale(${prefs.flipH ? -1 : 1},${prefs.flipV ? -1 : 1})`;
}
const hexA = (h, a) => { const n = parseInt((h || '#000').slice(1), 16); return `rgba(${n >> 16},${(n >> 8) & 255},${n & 255},${a})`; };

// ——— Rolagem: âncora (parágrafo + fração) do app, extrapolada com a velocidade ———
let tops = [];
function measure() {
  // Altura do bloco = até o topo do próximo (com a margem), igual ao mapa do app: sem degrau na troca de bloco.
  const ps = [...text.children];
  tops = ps.map((p, k) => ({ i: +p.dataset.i, top: p.offsetTop, h: k + 1 < ps.length ? ps[k + 1].offsetTop - p.offsetTop : p.offsetHeight }));
  layoutDirty = false;
}
function currentY() {
  const dt = Math.min(0.25, (performance.now() - feed.at) / 1000);
  let y = feed.y, v = feed.v;
  if (feed.b >= 0) {
    const t = tops.find(x => x.i === feed.b);
    // Fração do bloco: o navegador diagrama com alturas um pouco diferentes do app, então a posição
    // vai pela proporção (e a velocidade é convertida para a altura local do bloco).
    if (t && feed.f >= 0 && feed.bh > 0) { y = t.top + feed.f * t.h; v = feed.v * t.h / feed.bh; }
    else if (t) y = t.top + Math.min(feed.o, t.h);
  }
  localV = v;
  const last = tops[tops.length - 1], end = last ? last.top + last.h : 1e9;
  return Math.max(0, Math.min(end, y + v * dt + dragY));
}
function frame() {
  const now = performance.now(), dt = Math.min(0.1, (now - lastFrame) / 1000);
  lastFrame = now;
  if (state) {
    if (layoutDirty) { measure(); overlays(); }
    const k = stage.clientWidth / 1000, rp = state.readPct / 100;
    // Posição mostrada: segue a velocidade e corrige o erro aos poucos (sem trancos); saltos grandes
    // (marcador, início, arrasto) vão direto.
    const target = currentY();
    // O ponto do app está parado (pausado e sem rolar à mão) há 0,25 s?
    if (Math.abs(target - lastTarget) > 0.01) { lastTarget = target; targetMovedAt = now; }
    const resting = !state.live && now - targetMovedAt > 250;
    if (shown === null || dragY || Math.abs(target - shown) > 260) shown = target;
    else {
      const pred = shown + localV * dt;
      let next = pred + (target - pred) * Math.min(1, dt * 6);
      // Logo depois de pausar ou dar play — e enquanto o app fica parado no mesmo ponto — a correção pequena (atraso da
      // rede: a previsão passou um pouco do ponto) não anda contra o movimento: o texto espera em vez de recuar.
      // Rolar à mão no app (o ponto se mexe) e voltar linhas (> 10 unidades) continuam aparecendo normalmente.
      if ((now - liveChangedAt < 600 || resting) && Math.abs(target - shown) < 10) { if (localV >= 0 && next < shown) next = shown; else if (localV < 0 && next > shown) next = shown; }
      shown = next;
    }
    text.style.transform = `scale(${k}) translate3d(0,${(stage.clientHeight / k * rp - shown).toFixed(2)}px,0)`;
  }
  requestAnimationFrame(frame);
}

// ——— Comandos ———
function cmd(name, value) {
  if (!state || !state.webCtrl) return toast('Controle desligado no app: Telas › Monitor web › Permitir controlar');
  send({ t: 'cmd', name, value });
}
// Aviso que aparece também no modo limpo (só some no modo OBS).
let toastT = 0;
function toast(t) {
  let el = $('#toast'); if (!el) { el = document.createElement('div'); el.id = 'toast'; el.className = 'toast'; document.body.appendChild(el); }
  el.textContent = t; el.hidden = false; clearTimeout(toastT); toastT = setTimeout(() => { el.hidden = true; }, 2600);
}
document.querySelectorAll('[data-cmd]').forEach(b => b.addEventListener('click', e => { e.stopPropagation(); cmd(b.dataset.cmd); poke(); }));
$('#speed').addEventListener('input', e => cmd('speed', +e.target.value));
document.querySelectorAll('[data-set-mode]').forEach(b => b.addEventListener('click', e => { e.stopPropagation(); setMode(b.dataset.setMode); }));
function setMode(m) { prefs.mode = m; savePrefs(); body.dataset.mode = m; }

// Teclado / clicker Bluetooth no iPad.
const KEYS = { ' ': 'togglePlay', ArrowDown: 'faster', ArrowUp: 'slower', PageDown: 'nextBm', PageUp: 'prevBm', Home: 'rewind', End: 'toEnd', r: 'reverse', R: 'reverse', ']': 'nextPara', '[': 'prevPara', '+': 'faster', '=': 'faster', '-': 'slower', v: 'voice', V: 'voice' };
addEventListener('keydown', e => {
  if (e.target.tagName === 'INPUT') return;
  if (e.key === 'f' || e.key === 'F') { e.preventDefault(); toggleFs(); return; }
  const c = KEYS[e.key]; if (!c) return;
  e.preventDefault(); poke();
  // Setas com o prompt parado andam linha a linha; + e − sempre mudam a velocidade.
  if ((e.key === 'ArrowDown' || e.key === 'ArrowUp') && state && !state.live) cmd(e.key === 'ArrowDown' ? 'lineDown' : 'lineUp'); else cmd(c);
});

// ——— Toque: mostra as barras; arrasta o texto quando pausado ———
let idleT = 0, dragY = 0, touch = null;
// A barra de cima some sozinha (rápido no modo limpo); volta ao mexer o mouse ou tocar na tela.
function poke() {
  body.classList.remove('hidebar', 'idle'); clearTimeout(idleT);
  idleT = setTimeout(() => { body.classList.add('idle', 'hidebar'); }, prefs.mode === 'limpa' ? 1500 : 4000);
}
// Só movimento de verdade: quando a barra some debaixo de um cursor parado, o navegador manda um mousemove
// "falso" (mesma posição), que mostrava a barra de novo — em tela cheia ela nunca sumia.
let lastMove = 0, lastX = -1, lastY = -1;
addEventListener('mousemove', e => {
  if (e.screenX === lastX && e.screenY === lastY) return;
  lastX = e.screenX; lastY = e.screenY;
  const n = performance.now(); if (n - lastMove > 120) { lastMove = n; poke(); }
});
// Ao entrar ou sair da tela cheia, a barra começa a contar para sumir de novo.
document.addEventListener('fullscreenchange', poke);
document.addEventListener('webkitfullscreenchange', poke);
stage.addEventListener('pointerdown', e => {
  poke();
  if (!prefs.swipe || !state || !state.webCtrl) return;
  touch = { y: along(e), k: stage.clientWidth / 1000, moved: false, live: !!state.live, acc: 0 };
});
addEventListener('pointermove', e => {
  if (!touch) return;
  const dy = (along(e) - touch.y) / touch.k * (prefs.flipV ? -1 : 1);
  if (Math.abs(dy) > 4) touch.moved = true;
  if (!touch.moved) return;
  if (touch.live) {
    // Rodando: arrastar para cima acelera, para baixo freia (um passo a cada ~40 px).
    const steps = Math.trunc((touch.y - along(e)) / 40) - touch.acc;
    if (steps) { touch.acc += steps; for (let i = 0; i < Math.abs(steps); i++) cmd(steps > 0 ? 'faster' : 'slower'); }
  } else dragY = -dy;
});
// Roda do mouse / trackpad: rodando muda a velocidade; parado rola o texto.
let wheelAcc = 0;
addEventListener('wheel', e => {
  if (!state || (e.target instanceof Element && e.target.closest('dialog'))) return;
  e.preventDefault(); poke();
  if (state.live) {
    wheelAcc += e.deltaY;
    while (Math.abs(wheelAcc) >= 100) { cmd(wheelAcc > 0 ? 'faster' : 'slower'); wheelAcc -= Math.sign(wheelAcc) * 100; }
  } else if (state.webCtrl) cmd('nudge', e.deltaY * 0.5);
}, { passive: false });
addEventListener('pointerup', () => {
  if (touch && touch.moved && dragY && !touch.live) {
    // Mantém o texto onde o dedo soltou até o app confirmar a nova posição.
    const y = currentY(); cmd('nudge', dragY);
    feed = { y, v: 0, b: -1, o: 0, at: performance.now() };
  }
  dragY = 0; touch = null;
});

// Coordenada do dedo/mouse ao longo do texto (com a tela girada, "para baixo" no texto é outro eixo da tela).
function along(e) { return { 90: -e.clientX, 180: -e.clientY, 270: e.clientX }[prefs.rot] ?? e.clientY; }

// ——— Tela cheia (sem bordas, como o F11), nesta tela ou em outro monitor ———
function toggleFs() {
  const d = document.documentElement;
  if (document.fullscreenElement || document.webkitFullscreenElement) (document.exitFullscreen || document.webkitExitFullscreen).call(document);
  else (d.requestFullscreen || d.webkitRequestFullscreen)?.call(d, { navigationUI: 'hide' })?.catch?.(() => {});
}
// Window Management API (Chrome/Edge): lista os monitores e abre a tela cheia direto no escolhido.
async function screensList() {
  const box = $('#screens');
  // O aviso vai dentro da janela de ajustes (o toast ficaria atrás dela).
  const say = t => { const p = $('#scrMsg'); p.textContent = t; p.hidden = false; };
  if (!('getScreenDetails' in window)) {
    return say(isSecureContext ? 'Este navegador não escolhe monitor: arraste a janela para o outro monitor e aperte F'
      : 'Para escolher o monitor, abra pelo endereço https (porta 8421) no Chrome ou Edge — ou arraste a janela e aperte F');
  }
  let det;
  try { det = await window.getScreenDetails(); } catch (e) { return say('O navegador negou a permissão de "gerenciar janelas". Libere em Configurações do site (ícone ao lado do endereço) — ou arraste a janela para o outro monitor e aperte F.'); }
  box.querySelectorAll('.scr').forEach(b => b.remove());
  det.screens.forEach((s, i) => {
    const b = document.createElement('button'); b.type = 'button'; b.className = 'scr';
    b.textContent = (s.label || 'Monitor ' + (i + 1)) + ' · ' + s.width + '×' + s.height + (s === det.currentScreen ? ' (esta)' : '');
    b.addEventListener('click', () => { dlg.close(); document.documentElement.requestFullscreen({ navigationUI: 'hide', screen: s }).catch(() => toast('O navegador não deixou abrir a tela cheia')); });
    box.appendChild(b);
  });
  say(det.screens.length < 2 ? 'Só um monitor encontrado.' : 'Escolha o monitor para abrir em tela cheia:');
}
$('#btnFsHere').addEventListener('click', () => { dlg.close(); toggleFs(); });
$('#btnScreens').addEventListener('click', screensList);

// ——— Girar o texto ———
function setRot(r) {
  prefs.rot = [0, 90, 180, 270].includes(+r) ? +r : 0; savePrefs();
  body.dataset.rot = prefs.rot; layoutDirty = true;
  document.querySelectorAll('#optRot [data-rot]').forEach(b => b.classList.toggle('on', +b.dataset.rot === prefs.rot));
}
document.querySelectorAll('#optRot [data-rot]').forEach(b => b.addEventListener('click', () => setRot(b.dataset.rot)));

// ——— Flash de velocidade ———
let flashT = 0;
function flash(t, sub) {
  $('#flash').hidden = false; $('#flashText').textContent = t;
  const s = $('#flashSub'); s.hidden = !sub; s.textContent = sub || ''; s.classList.toggle('re', sub === 'Ré');
  clearTimeout(flashT); flashT = setTimeout(() => { $('#flash').hidden = true; }, 1100);
}

// ——— Ajustes ———
const dlg = $('#settings');
$('#btnSettings').addEventListener('click', e => { e.stopPropagation(); syncChecks(); dlg.showModal(); });
function syncChecks() {
  $('#optFlipH').checked = prefs.flipH; $('#optFlipV').checked = prefs.flipV; $('#optHud').checked = prefs.hud;
  $('#optWake').checked = prefs.wake; $('#optSwipe').checked = prefs.swipe; $('#pass').value = prefs.pass;
}
[['optFlipH', 'flipH'], ['optFlipV', 'flipV'], ['optHud', 'hud'], ['optWake', 'wake'], ['optSwipe', 'swipe']].forEach(([id, k]) =>
  $('#' + id).addEventListener('change', e => { prefs[k] = e.target.checked; savePrefs(); applyPrefs(); }));
$('#btnPass').addEventListener('click', e => { e.preventDefault(); prefs.pass = $('#pass').value; savePrefs(); send({ t: 'auth', pass: prefs.pass }); });
$('#loginForm').addEventListener('submit', e => { e.preventDefault(); prefs.pass = $('#loginPass').value; savePrefs(); send({ t: 'auth', pass: prefs.pass }); });
$('#btnFs').addEventListener('click', e => { e.stopPropagation(); toggleFs(); });
function applyPrefs() { body.classList.toggle('nohud', !prefs.hud); overlays(); wake(); }

// Tela sempre ligada (Screen Wake Lock: Safari/iPadOS 16.4+, Chrome, Edge).
let lock = null;
async function wake() {
  try {
    if (prefs.wake && 'wakeLock' in navigator && !lock && document.visibilityState === 'visible') { lock = await navigator.wakeLock.request('screen'); lock.addEventListener('release', () => { lock = null; }); }
    else if (!prefs.wake && lock) { await lock.release(); lock = null; }
  } catch (e) { /* sem suporte ou sem gesto do usuário */ }
}
document.addEventListener('visibilitychange', wake);
addEventListener('pointerdown', wake, { once: true });
addEventListener('resize', () => { layoutDirty = true; });

// Service worker (só em contexto seguro: https ou localhost) para abrir como app mesmo sem rede.
if ('serviceWorker' in navigator && isSecureContext) navigator.serviceWorker.register('sw.js').catch(() => {});

// Endereço para OBS/vMix: ?obs (tela limpa, sem nenhuma interface) e ?fundo=transparente.
const qs = new URLSearchParams(location.search);
if (qs.has('obs') || qs.get('modo') === 'limpa') { prefs.mode = 'limpa'; }
if (qs.has('obs')) body.classList.add('obs');
if (qs.get('fundo') === 'transparente') body.classList.add('transparente');
if (qs.get('senha')) prefs.pass = qs.get('senha');
if (qs.has('girar')) prefs.rot = +qs.get('girar');
setRot(prefs.rot);
setMode(prefs.mode);
poke();
applyPrefs();
poke();
connect();
requestAnimationFrame(frame);
