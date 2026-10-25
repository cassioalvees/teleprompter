// @ts-nocheck — lógica portada do protótipo (classe Component de Teleprompter.dc.html).
// Mantém os nomes e o comportamento originais; as integrações reais passam por `platform`.
import React from 'react';
import { renderTemplate } from './Template';
import { SCRIPT, I, IMPROV, KEYMAP, ACTIONS, esc } from './constants';
import { platform, isDesktop } from '../platform';
import { applyWhoVars } from '../shared/who';
import { docxToText, pdfToText } from './importers';
import { listSystemFonts, widthFactor } from './systemFonts';

const store = platform.storage;
const readJSON = (k, d) => { try { const v = store.get(k); return v == null ? d : JSON.parse(v); } catch (e) { return d; } };
// Preferências que sobrevivem entre sessões (o protótipo não persistia; o app real precisa).
const PREF_KEYS = ['theme', 'keymap', 'lockOnPlay', 'countdownPref', 'talentHud', 'unit', 'minS', 'maxS', 'speedT', 'wpl', 'margin', 'mPos', 'lineH', 'fontKey', 'bg', 'fg', 'align', 'ovType', 'ovOp', 'indType', 'indSize', 'indColor', 'readPct', 'readingLine', 'bgImgOp', 'pres', 'presNames', 'ndiName', 'ndiFmt', 'ndiMirror', 'ndiAlpha', 'webPass', 'webCtrl', 'mosSys', 'mosHost', 'mosId', 'ccOut', 'vLang', 'gate', 'win', 'tol', 'recWithPrompt', 'ctrlOn', 'stampOn', 'ctrlOp', 'showBm', 'showInsp', 'progSec', 'progInput'];
const loadPrefs = () => { const p = readJSON('arca-prompter-prefs', {}); const o = {}; PREF_KEYS.forEach(k => { if (k in p) o[k] = p[k]; }); if (o.keymap) o.keymap = { ...KEYMAP, ...o.keymap }; return o; };
const WEB_PORT = 8420;

export default class App extends React.Component<any, any> {
  static defaultProps = { simWordMs: 340, improvEvery: 22 };
  state = {
    theme: 'light', menu: null, dlg: null, prefTab: 'geral', keymap: { ...KEYMAP }, capture: null, lastKey: '',
    talent: false, playing: false, locked: false, follow: true, dir: 1, hold: 0, count: 0, lockOnPlay: true, countdownPref: false,
    speedT: 0.3, minS: 0.3, maxS: 3, unit: 0, wpl: 5, margin: 0, mPos: -1, lineH: 1.3, fontKey: 'atk',
    bg: '#000000', fg: '#ffffff', align: 'left', edFlipH: false, edFlipV: false,
    ovType: 'faixa', ovOp: 0.55, indType: 'seta', indSize: 1, indColor: '#ffffff', readPct: 30, readingLine: false,
    bgImg: null, bgImgOp: 0.2, autoRun: false, runTarget: '01:30', tab: 'roteiro', stampMode: 0, showBm: true, showInsp: true,
    displays: [],
    winFlipH: false, winFlipV: false, winAlign: 'left', ctrlOn: true, stampOn: true, ctrlOp: 0.9, speedLock: false, tbShow: true, inline: false, blink: false,
    vMode: 'off', vState: 'off', heard: [], vErr: '', driver: 'wasapi', asioCh: 1, gate: -48, win: 20, tol: 3, level: -70, vLang: 'pt-BR', audioDevs: [], sysFonts: [], sysFontsErr: '',
    ndiOn: false, ndiName: 'ARCA-PROMPTER (Saída 1)', ndiFmt: '1080p30', ndiMirror: false, ndiAlpha: false, ndiTally: true, webOn: false, webBusy: false, webErr: '', webUrl: '', webViewers: 0, webPass: '', webCtrl: false, copied: false, mosSys: 'ENPS', mosHost: '192.168.0.10', mosId: 'ARCA.PROMPTER.1', mosOn: false, mosBusy: false, mosErr: '', gpiAuto: false, ccOut: false, clock: '', endAt: '', flashText: '', flashPct: -1, camOn: false, camMsg: 'Câmera desligada', recording: false, recSec: 0, recUrl: '', lastRec: 0, recWithPrompt: false,
    findQ: '', replQ: '', findInfo: '', docName: 'Jornal da Noite.tps', dirty: false, docs: [{ id: 1, name: 'Jornal da Noite.tps', html: '', y: 0, dirty: false, path: '' }], active: 1, blocks: [], progSec: 600, talentHud: true, profiles: [], profName: '', dragBm: -1, dropBm: -1, pres: [{ name: 'Âncora', color: '#ffd60a' }, { name: 'Repórter', color: '#5ac8fa' }], presNames: true, presOnly: '0', presMenu: false, curWho: 0, hexEdit: {}, progInput: '10:00', rehearse: false, reh: {}, savedAt: '', versions: [],
    words: 0, bms: [], stops: [], cur: -1, t: { line: 1, lines: 1, y: 0, end: 1 },
    ...loadPrefs()
  };
  y = 0; anchor = { i: 0, frac: 0, text: '' }; wheel = 0; lastSet = -1; userY = null; fi = -1;
  W = []; vp = 0; misses = 0; pending = null; vTarget = null; vel = 0;
  edScroll = React.createRef(); edSizer = React.createRef(); edZoom = React.createRef(); edRef = React.createRef();
  tvRef = React.createRef(); tcRef = React.createRef(); pvRef = React.createRef(); pcRef = React.createRef();
  bgFile = React.createRef(); openFile = React.createRef(); videoRef = React.createRef(); mvRef = React.createRef(); mcRef = React.createRef();
  fonts = { atk: ["'Atkinson Hyperlegible', sans-serif", 1], sans: ["'Source Sans 3', sans-serif", 1.1], serif: ["'Source Serif 4', serif", 1.02], mono: ["'IBM Plex Mono', monospace", 0.82] };

  componentDidMount() {
    const ed = this.edRef.current;
    const saved = readJSON('arca-prompter-docs', null);
    const pf = readJSON('arca-prompter-profiles', []); if (pf.length) this.setState({ profiles: pf });
    const vers = readJSON('arca-prompter-versions', []);
    if (saved && saved.docs && saved.docs.length) { const a = saved.docs.find(d => d.id === saved.active) || saved.docs[0]; ed.innerHTML = a.html || SCRIPT; this.setState({ docs: saved.docs, active: a.id, docName: a.name, versions: vers, savedAt: 'Restaurado da última sessão' }); }
    else { ed.innerHTML = SCRIPT; this.setState({ versions: vers }); }
    ed.contentEditable = 'true';
    this.rehT = setInterval(() => { const s = this.state; if (s.rehearse && s.playing && s.cur >= 0) this.setState(x => ({ reh: { ...x.reh, [x.cur]: (x.reh[x.cur] || 0) + 0.5 } })); }, 500);
    try { document.execCommand('defaultParagraphSeparator', false, 'p'); document.execCommand('styleWithCSS', false, true); } catch (e) {}
    this.pushHtml(); this.setState({ dirty: false });
    this.last = performance.now(); this.tick = 0;
    const loop = t => { this.frame(Math.min(0.1, (t - this.last) / 1000)); this.last = t; this.raf = requestAnimationFrame(loop); };
    this.raf = requestAnimationFrame(loop);
    this.onKey = e => this.key(e); window.addEventListener('keydown', this.onKey);
    this.onSel = () => { const ed = this.edRef.current, sel = window.getSelection(); if (!ed || !sel.rangeCount || !ed.contains(sel.anchorNode)) return; let n = sel.anchorNode; while (n && n.parentElement !== ed) n = n.parentElement; const w = n && n.getAttribute ? +(n.getAttribute('data-who') || 0) : 0; if (w !== this.state.curWho) this.setState({ curWho: w }); };
    document.addEventListener('selectionchange', this.onSel);
    this.blinkT = setInterval(() => { if (this.state.displays.some(d => d.find)) this.setState(s => ({ blink: !s.blink })); }, 450);
    // Telas reais (Electron: screen.getAllDisplays; web: esta janela + pop-ups).
    this.loadDisplays(); platform.onDisplaysChanged(list => this.mergeDisplays(list));
    platform.onOutputClosed(id => this.updDisp(id, { on: false, find: false }));
    platform.onCommand(cmd => this.onBusCmd(cmd));
    platform.onOpenPath(f => this.loadFile(f.file, f.path));
    if (platform.monitor) platform.monitor.onViewers(n => this.setState({ webViewers: n }));
    this.listAudio();
    // App: lista as fontes do Windows ao abrir. Navegador: carrega sozinho se a permissão já foi dada;
    // senão, no primeiro clique no seletor de fonte (o Chrome/Edge exige um gesto do usuário).
    if (isDesktop) this.loadSysFonts();
    else (navigator as any).permissions?.query({ name: 'local-fonts' }).then(p => { if (p.state === 'granted') this.loadSysFonts(); }).catch(() => {});
    this.onBeforeUnload = () => { clearTimeout(this.autoT); this.persistDocs(); };
    window.addEventListener('beforeunload', this.onBeforeUnload);
  }
  componentDidUpdate(pp, ps) {
    const s = this.state, ed = this.edRef.current; if (ed) ed.contentEditable = String(!s.locked);
    { const w = JSON.stringify([s.pres, s.presNames, s.presOnly]); if (w !== this.lastWho) { this.lastWho = w; applyWhoVars(document.documentElement, s.pres, s.presNames, s.presOnly); } }
    const v = this.videoRef.current; if (v && this.cam && v.srcObject !== this.cam) v.srcObject = this.cam;
    { const ar = s.autoRun ? [s.runTarget, s.wpl, s.margin, s.minS, s.maxS].join('|') : ''; if (ar !== this.lastAr) { this.lastAr = ar; if (ar) setTimeout(() => this.applyAutoRun(), 50); } }
    // Abre / fecha as janelas de prompter conforme as telas marcadas.
    if (ps.displays !== s.displays) {
      s.displays.forEach(d => { const o = ps.displays.find(x => x.id === d.id); if (d.on && !(o && o.on)) platform.openOutput(d); if (!d.on && o && o.on) platform.closeOutput(d.id); });
      ps.displays.forEach(o => { if (o.on && !s.displays.find(x => x.id === o.id)) platform.closeOutput(o.id); });
      store.set('arca-prompter-displays', JSON.stringify(Object.fromEntries(s.displays.map(d => [d.id, { nick: d.nick, flipH: d.flipH, flipV: d.flipV, align: d.align }]))));
    }
    if (platform.monitor && s.webOn && (ps.webPass !== s.webPass || ps.webCtrl !== s.webCtrl)) platform.monitor.update({ pass: s.webPass, ctrl: s.webCtrl });
    { const p = {}; PREF_KEYS.forEach(k => { p[k] = s[k]; }); const j = JSON.stringify(p); if (j !== this.lastPrefs) { if (this.lastPrefs) store.set('arca-prompter-prefs', j); this.lastPrefs = j; } }
    if (ps.docName !== s.docName || ps.dirty !== s.dirty) platform.setTitle(`${s.docName}${s.dirty ? ' •' : ''} — ARCA Prompter`);
    this.publishState();
  }
  componentWillUnmount() { document.removeEventListener('selectionchange', this.onSel); clearInterval(this.rehT); clearTimeout(this.autoT); cancelAnimationFrame(this.raf); clearInterval(this.blinkT); clearInterval(this.cdT); clearInterval(this.recT); window.removeEventListener('keydown', this.onKey); window.removeEventListener('beforeunload', this.onBeforeUnload); this.stopVoice(); if (this.cam) this.cam.getTracks().forEach(t => t.stop()); }

  render() { const v = this.renderVals(); this.vals = v; return renderTemplate(v); }

  // ——— Saídas: janelas de prompter, monitor web ———
  publishState() {
    const v = this.vals, s = this.state; if (!v) return;
    const snap = {
      bg: v.bg, fg: v.fg, fontFam: v.fontFam, fontSize: v.fontSize, lineH: v.lineH, prMargin: v.prMargin, prRight: v.prRight, align: s.align, readPct: s.readPct,
      lineCq: v.lineCq, halfLineCq: v.halfLineCq, ovType: s.ovType, dimColor: v.dimColor, hiColor: v.hiColor, readingLine: s.readingLine,
      indColor: v.indColorV, indLeft: v.indLeft, indW: v.indW, indH: v.indH, indClip: v.indClip, indRadius: v.indRadius, indDisplay: v.indDisplay,
      bgImg: '', bgImgOp: s.bgImgOp, pres: s.pres, presNames: s.presNames, presOnly: s.presOnly,
      displays: s.displays.map(d => ({ id: d.id, flipH: d.flipH, flipV: d.flipV, align: d.align, find: d.find })), blink: s.blink,
      live: s.playing || s.count > 0, count: s.count, hud: s.talentHud, hudBlock: v.hudBlock, blockLeft: v.blockLeft, blockLeftColor: v.blockLeftColor, isLastBlock: v.isLastBlock, clock: v.clockV,
      flashText: s.flashText, flashSub: s.flashSub || '', flashPct: s.flashPct, curName: v.remoteCur, stamp: v.stampValue, speed: v.remoteSpeed, webCtrl: s.webCtrl
    };
    const sig = JSON.stringify(snap);
    if (sig === this.lastSnap && s.bgImg === this.lastBgImg) return;
    this.lastSnap = sig; this.lastBgImg = s.bgImg;
    snap.bgImg = s.bgImg || '';
    platform.publish({ t: 'state', s: snap });
  }
  onBusCmd(cmd) {
    if (!cmd) return;
    if (cmd.t === 'hello') { this.lastSnap = null; platform.publish({ t: 'html', html: this.edRef.current.innerHTML }); this.publishState(); platform.publish({ t: 'y', y: this.y, v: 0 }); return; }
    if (cmd.t === 'togglePlay') return this.togglePlay();
    if (cmd.t === 'action') { if (cmd.name === 'faster' || cmd.name === 'slower') return this.adjSpeed(cmd.name === 'faster' ? 0.05 : -0.05); return this.doAction(cmd.name); }
    if (cmd.t === 'key') {
      // Tecla pressionada numa janela de prompter (ex.: clicker apontado para o monitor do talento).
      if (cmd.key === 'Escape' || cmd.key === 'F5') return;
      this.key({ key: cmd.key, shiftKey: !!cmd.shiftKey, ctrlKey: !!cmd.ctrlKey, metaKey: false, altKey: !!cmd.altKey, target: document.body, preventDefault() {} });
    }
  }
  async loadDisplays() { try { this.mergeDisplays(await platform.listDisplays()); } catch (e) {} }
  mergeDisplays(list) {
    const saved = readJSON('arca-prompter-displays', {});
    let ext = 0;
    this.setState(st => ({ displays: [
      ...list.map(d => {
        const cur = st.displays.find(x => x.id === d.id), sv = saved[d.id] || {};
        if (!d.operator && !d.windowed) ext++;
        const nick = d.operator ? (isDesktop ? 'Tela do operador' : 'Navegador') : d.windowed ? 'Janela flutuante' : 'Prompter ' + ext;
        return { id: d.id, name: d.name, res: d.res, operator: d.operator, windowed: !!d.windowed, nick: sv.nick || nick, on: cur ? cur.on : false, flipH: sv.flipH ?? (!d.operator && !d.windowed), flipV: !!sv.flipV, align: sv.align || 'left', find: false, ...(cur ? { nick: cur.nick, flipH: cur.flipH, flipV: cur.flipV, align: cur.align, find: cur.find } : {}) };
      }),
      ...st.displays.filter(x => x.extra && !list.find(d => d.id === x.id))
    ] }));
  }
  addWindowOutput = () => {
    const n = this.state.displays.filter(d => d.extra).length + 2, id = 'win' + Date.now();
    this.setState(x => ({ displays: [...x.displays, { id, name: 'Janela', res: 'redimensionável', operator: false, windowed: true, extra: true, nick: 'Janela flutuante ' + n, on: true, flipH: false, flipV: false, align: x.align, find: false }] }));
  };
  fontInfo(key) {
    if (key && key.startsWith('sys:')) { const fam = key.slice(4); return [JSON.stringify(fam) + ', sans-serif', widthFactor(fam)]; }
    return this.fonts[key] || this.fonts.atk;
  }
  async loadSysFonts() {
    try { const list = await listSystemFonts(); this.setState({ sysFonts: list, sysFontsErr: list.length ? '' : 'Nenhuma fonte encontrada' }); }
    catch (e) { this.setState({ sysFontsErr: (e && e.message === 'unsupported') ? 'Fontes do computador: use Chrome/Edge ou o app' : 'Acesso às fontes negado' }); }
  }
  async listAudio() {
    try { const ds = await navigator.mediaDevices.enumerateDevices(); const ins = ds.filter(d => d.kind === 'audioinput').map((d, i) => d.label || (d.deviceId === 'default' ? 'Microfone padrão' : 'Entrada de áudio ' + (i + 1))); this.setState({ audioDevs: ins }); } catch (e) {}
  }

  prM(fsz) { const s = this.state; let ind = 24; if (s.indType !== 'nenhum' && s.indType !== 'linha') { const w = { seta: 1.8, quadrado: 1.7, ponto: 1.8 }[s.indType] || 0, k = fsz * 0.028 * [0.65, 1, 1.45][s.indSize]; ind = Math.round((k * 0.4 + w * k) * 10 + 14); } return ind + Math.round(s.margin * (1 + s.mPos) / 2); }
  prR() { const s = this.state; return 24 + Math.round(s.margin * (1 - s.mPos) / 2); }
  fs() { const s = this.state, d = s.wpl * 3.05 * this.fontInfo(s.fontKey)[1], b = (1000 - 48 - s.margin) / d; return Math.round((1000 - this.prR() - this.prM(b)) / d); }
  lineHpx() { return this.fs() * this.state.lineH; }
  lps() { const s = this.state; return s.minS + s.speedT * (s.maxS - s.minS); }
  blocks() { const c = this.tcRef.current; return c ? c.children : []; }
  endY() { const b = this.blocks(), l = b[b.length - 1]; return l ? Math.max(0, l.offsetTop + l.offsetHeight - this.lineHpx()) : 0; }
  anchorAt(y) {
    const b = this.blocks(); if (!b.length) return { i: 0, frac: 0, text: '' };
    let i = 0; for (let k = 0; k < b.length; k++) { if (b[k].offsetTop <= y) i = k; else break; }
    const top = b[i].offsetTop, h = Math.max(1, (b[i + 1] ? b[i + 1].offsetTop : top + b[i].offsetHeight) - top);
    return { i, frac: Math.max(0, Math.min(1, (y - top) / h)), text: b[i].textContent.slice(0, 50) };
  }
  yOf(a) {
    const b = this.blocks(); if (!b.length) return 0;
    let i = Math.min(a.i, b.length - 1);
    if (a.text && b[i].textContent.slice(0, 50) !== a.text) {
      let best = -1, d = 1e9;
      for (let k = 0; k < b.length; k++) if (b[k].textContent.slice(0, 50) === a.text && Math.abs(k - a.i) < d) { best = k; d = Math.abs(k - a.i); }
      if (best >= 0) i = best;
    }
    const top = b[i].offsetTop, h = Math.max(1, (b[i + 1] ? b[i + 1].offsetTop : top + b[i].offsetHeight) - top);
    return top + a.frac * h;
  }
  setY(y) { this.y = Math.max(0, Math.min(y, this.endY())); this.anchor = this.anchorAt(this.y); this.vTarget = null; this.syncUi(); }

  frame(dt) {
    const s = this.state, tc = this.tcRef.current; if (!tc) return;
    const lh = this.lineHpx();
    let y = this.yOf(this.anchor), vel = 0;
    if (this.userY != null) { y = this.userY; this.userY = null; this.vTarget = null; }
    if (s.vMode !== 'off') { if (this.vTarget != null) y += (this.vTarget - y) * Math.min(1, dt * 5); }
    else if (s.playing && !s.inline) {
      vel = (this.lps() * s.dir + s.hold * this.lps() * 2) * lh;
      const v = vel * dt, ny = y + v; let hit = false;
      if (v > 0) for (const st of tc.querySelectorAll('[data-stop]')) { const t = st.offsetTop - lh * 0.2; if (t > y + 0.5 && t <= ny) { y = t; hit = true; break; } }
      if (hit) { this.pause(); vel = 0; } else y = ny;
    } else if (s.hold) { vel = s.hold * this.lps() * 3 * lh; y += vel * dt; }
    if (this.wheel) { y += this.wheel; this.wheel = 0; }
    const end = this.endY(); y = Math.max(0, Math.min(y, end));
    if (s.playing && s.vMode === 'off' && ((y >= end && s.dir > 0) || (y <= 0 && s.dir < 0))) this.pause();
    if (y <= 0 || y >= end) vel = 0;
    this.y = y; this.anchor = this.anchorAt(y);
    this.apply(y);
    // As saídas extrapolam com a velocidade entre mensagens (rolagem lisa mesmo com IPC/rede).
    platform.publish({ t: 'y', y, v: vel });
    this.tick += dt; if (this.tick > 0.2) { this.tick = 0; this.syncUi(); }
  }
  apply(y) {
    const s = this.state, rp = s.readPct / 100;
    [[this.tvRef, this.tcRef], [this.pvRef, this.pcRef], [this.mvRef, this.mcRef]].forEach(([v, c]) => {
      if (!v.current || !c.current) return;
      const k = v.current.clientWidth / 1000; if (!k) return;
      c.current.style.transform = `scale(${k}) translateY(${((v.current.clientHeight / k) * rp - y).toFixed(2)}px)`;
    });
    const sc = this.edScroll.current, z = this.edZoom.current, sz = this.edSizer.current;
    if (sc && z && sz) {
      const k = sc.clientWidth / 1000, vh = sc.clientHeight / k;
      z.style.transform = `scale(${k})`; z.style.setProperty('--k', k.toFixed(3)); z.style.paddingTop = (vh * rp) + 'px'; z.style.paddingBottom = vh + 'px';
      sz.style.height = (z.offsetHeight * k) + 'px';
      if (s.follow) { const t = y * k; if (Math.abs(sc.scrollTop - t) > 0.5) sc.scrollTop = t; this.lastSet = sc.scrollTop; }
    }
  }
  syncUi() {
    const tc = this.tcRef.current, pc = this.pcRef.current; if (!tc) return;
    if (pc && !pc.innerHTML) pc.innerHTML = tc.innerHTML;
    const mc = this.mcRef.current; if (mc && !mc.innerHTML) mc.innerHTML = tc.innerHTML;
    { const p2 = n => String(n).padStart(2, '0'), hm = d => p2(d.getHours()) + ':' + p2(d.getMinutes()) + ':' + p2(d.getSeconds()), now = new Date();
      const rem = (this.endY() - this.y) / (this.lps() * this.lineHpx()), clock = hm(now), endAt = hm(new Date(now.getTime() + Math.max(0, rem) * 1000));
      if (clock !== this.state.clock || endAt !== this.state.endAt) this.setState({ clock, endAt }); }
    const lh = this.lineHpx(), y = this.y, end = Math.max(1, this.endY());
    const bms = [...tc.querySelectorAll('[data-bm]')].map(el => ({ name: el.textContent.trim() || 'Sem nome', y: el.offsetTop }));
    const stops = [...tc.querySelectorAll('[data-stop]')].map(el => ({ name: el.textContent.trim() || 'Pausa', y: el.offsetTop }));
    let cur = -1; bms.forEach((b, i) => { if (b.y <= y + lh * 0.5) cur = i; });
    const t = { line: Math.floor(y / lh) + 1, lines: Math.max(1, Math.ceil(end / lh) + 1), y: Math.round(y), end: Math.round(end) };
    const sig = JSON.stringify([bms, stops, cur, t]);
    if (sig !== this.lastSig) { this.lastSig = sig; this.setState({ bms, stops, cur, t }); }
    const lv = this.level ?? -70; if (Math.abs(lv - this.state.level) > 1) this.setState({ level: lv });
  }

  pushHtml() {
    const ed = this.edRef.current; let skip = false; const blocks = [];
    [...ed.children].forEach(ch => {
      if (ch.hasAttribute('data-bm')) { skip = ch.hasAttribute('data-skip'); blocks.push({ words: 0, skip }); ch.removeAttribute('data-skipped'); return; }
      if (skip) ch.setAttribute('data-skipped', '1'); else ch.removeAttribute('data-skipped');
      if (blocks.length && !ch.hasAttribute('data-stop')) blocks[blocks.length - 1].words += ch.textContent.split(/\s+/).filter(Boolean).length;
    });
    let prev = null; [...ed.children].forEach(ch => { const w = ch.getAttribute('data-who'); if (ch.hasAttribute('data-bm') || ch.hasAttribute('data-stop')) { prev = null; return; } if (w && w !== prev) ch.setAttribute('data-who-start', '1'); else ch.removeAttribute('data-who-start'); prev = w; });
    if (JSON.stringify(blocks) !== JSON.stringify(this.state.blocks)) this.setState({ blocks });
    this.scheduleSave();
    const html = ed.innerHTML;
    [this.tcRef, this.pcRef, this.mcRef].forEach(r => { if (r.current && r.current.contentEditable !== 'true') r.current.innerHTML = html; });
    platform.publish({ t: 'html', html });
    let total = 0; [...ed.children].forEach(ch => { if (!ch.hasAttribute('data-bm') && !ch.hasAttribute('data-stop')) total += ch.textContent.split(/\s+/).filter(Boolean).length; });
    this.buildWords(); this.setState({ words: total, dirty: true }); this.syncUi();
  }
  onEdInput = () => {
    const ed = this.edRef.current, ci = this.caretIndex();
    [...ed.children].forEach((ch, i) => {
      const tx = ch.textContent;
      if (i !== ci && !ch.hasAttribute('data-bm') && !ch.hasAttribute('data-stop') && /^\*\S/.test(tx)) { ch.setAttribute('data-bm', '1'); ch.contentEditable = 'false'; ch.textContent = tx.slice(1).trim(); }
    });
    this.pushHtml();
  };
  caretIndex() {
    const ed = this.edRef.current, sel = window.getSelection(); if (!sel.rangeCount) return -1;
    let n = sel.anchorNode; while (n && n.parentNode !== ed) n = n.parentNode;
    return n ? [...ed.children].indexOf(n) : -1;
  }
  insertBlock(kind) {
    const ed = this.edRef.current; let i = this.caretIndex(); if (i < 0) i = this.anchor.i;
    const ref = ed.children[i]; const el = document.createElement('p');
    el.setAttribute(kind, '1'); el.contentEditable = 'false';
    el.textContent = kind === 'data-bm' ? ((ref && ref.textContent.split(/\s+/).slice(0, 3).join(' ')) || 'Novo marcador') : 'Ponto de pausa';
    ed.insertBefore(el, ref || null); this.pushHtml();
  }
  exec(cmd, val) { if (this.state.locked) return; this.edRef.current.focus({ preventScroll: true }); document.execCommand(cmd, false, val); this.pushHtml(); }
  upperSel() { const t = window.getSelection().toString(); if (t) this.exec('insertText', t.toUpperCase()); }
  removeBlock(kind, i) { const el = this.edRef.current.querySelectorAll('[' + kind + ']')[i]; if (el) { el.remove(); this.pushHtml(); this.flash(kind === 'data-bm' ? 'Marcador excluído' : 'Pausa excluída'); } }
  jumpPara(d) {
    const tc = this.tcRef.current; if (!tc) return;
    const ps = [...tc.children].filter(el => !el.hasAttribute('data-bm') && !el.hasAttribute('data-stop') && el.offsetParent !== null && el.textContent.trim());
    if (!ps.length) return; const y = this.y, eps = 2;
    let t = d > 0 ? ps.find(p => p.offsetTop > y + eps) : [...ps].reverse().find(p => p.offsetTop < y - eps);
    if (!t) return this.flash(d > 0 ? 'Último parágrafo' : 'Primeiro parágrafo');
    this.setY(t.offsetTop); this.setState({ follow: true });
  }
  jumpBm(i) { const b = this.state.bms[i]; if (b) { this.setY(b.y); this.setState({ follow: true }); } }

  play() {
    if (this.state.countdownPref && !this.state.playing) {
      clearInterval(this.cdT); this.setState({ count: 3 });
      this.cdT = setInterval(() => { const n = this.state.count - 1; if (n <= 0) { clearInterval(this.cdT); this.setState({ count: 0 }); this.go(); } else this.setState({ count: n }); }, 1000);
      return;
    }
    this.go();
  }
  go() { if (this.state.inline) this.finishInline(); this.setState({ playing: true, locked: true, follow: true }); if (this.state.recWithPrompt && this.state.camOn && !this.state.recording) this.startRec(); }
  pause() { clearInterval(this.cdT); this.setState({ playing: false, locked: false, follow: true, hold: 0, count: 0, tbShow: true }); if (this.state.recWithPrompt && this.state.recording) this.stopRec(); }
  togglePlay = () => { if (this.state.vMode !== 'off') this.stopVoice(); (this.state.playing || this.state.count) ? this.pause() : this.play(); };
  toggleLock = () => {
    const s = this.state;
    if (s.playing || s.count > 0) return this.flash('Pause o prompt para editar');
    if (s.locked) this.setState({ locked: false, follow: true }, () => this.edRef.current.focus({ preventScroll: true }));
    else this.setState({ locked: true, follow: true });
  };
  toggleTalent = () => { if (this.state.inline) this.finishInline(); this.setState(s => ({ talent: !s.talent, tbShow: true })); if (document.fullscreenElement) document.exitFullscreen().catch(() => {}); };
  editHere = () => {
    if (this.state.inline) this.finishInline();
    if (document.fullscreenElement) document.exitFullscreen().catch(() => {});
    const i = this.anchor.i; this.stopVoice();
    this.setState({ talent: false, playing: false, locked: false, follow: true, count: 0 }, () => {
      const ed = this.edRef.current; let el = ed.children[i];
      while (el && (el.hasAttribute('data-bm') || el.hasAttribute('data-stop'))) el = el.nextElementSibling;
      if (!el) return;
      ed.contentEditable = 'true'; ed.focus({ preventScroll: true });
      const r = document.createRange(); r.selectNodeContents(el); r.collapse(false);
      const sel = window.getSelection(); sel.removeAllRanges(); sel.addRange(r);
      if (el.animate) el.animate([{ backgroundColor: 'rgba(128,128,128,.35)' }, { backgroundColor: 'rgba(128,128,128,0)' }], { duration: 1600 });
    });
  };
  cursorToPrompter = e => { if (e && e.preventDefault) e.preventDefault(); const i = this.caretIndex(); const b = this.blocks()[i]; if (b) { this.setY(b.offsetTop); this.setState({ follow: true, locked: this.state.playing && this.state.lockOnPlay }); } };
  startInline = () => { if (this.state.inline || this.state.playing || this.state.count > 0) return; const tc = this.tcRef.current; this.setState({ inline: true, playing: false, tbShow: true }); tc.contentEditable = 'true'; tc.focus({ preventScroll: true }); };
  finishInline = () => { const tc = this.tcRef.current; tc.contentEditable = 'false'; this.edRef.current.innerHTML = tc.innerHTML; this.setState({ inline: false, locked: false }); this.pushHtml(); };
  applyAutoRun() {
    const s = this.state, parts = s.runTarget.trim().split(':').map(Number); if (parts.some(isNaN) || parts.length < 2 || parts.length > 3) return;
    const sec = parts.reduce((a, v) => a * 60 + v, 0); if (!sec) return;
    const need = (this.endY() / this.lineHpx()) / sec, t = Math.max(0, Math.min(1, (need - s.minS) / (s.maxS - s.minS)));
    if (Math.abs(t - s.speedT) > 0.005) this.setState({ speedT: t });
  }

  newDoc() { this.openInTab('Sem título.tps', '<p><br></p>'); this.setState({ dlg: null }); }
  snapshotDocs() { const s = this.state, ed = this.edRef.current; return s.docs.map(d => d.id === s.active ? { ...d, html: ed.innerHTML, y: this.y, name: s.docName, dirty: s.dirty } : d); }
  openInTab(name, html, extra, path) { const docs = this.snapshotDocs(), id = Date.now(); docs.push({ id, name, html, y: 0, dirty: false, path: path || '' }); this.loadDoc(docs, id, extra); }
  loadDoc(docs, id, extra) {
    if (this.state.playing) this.pause();
    const d = docs.find(x => x.id === id); this.edRef.current.innerHTML = d.html || '<p><br></p>';
    this.setState({ docs, active: id, docName: d.name, reh: {}, ...(extra || {}) }, () => { this.pushHtml(); this.setY(d.y || 0); this.setState({ dirty: !!d.dirty }); });
  }
  switchTab(id) { if (id === this.state.active) return; this.loadDoc(this.snapshotDocs(), id); }
  closeTab(id) {
    let docs = this.snapshotDocs(); const d = docs.find(x => x.id === id);
    if (d && d.dirty && !confirm('Fechar "' + d.name + '" sem salvar? A versão automática continua no histórico.')) return;
    if (d && d.dirty) this.addVersion(d.name, d.html);
    docs = docs.filter(x => x.id !== id);
    if (!docs.length) docs = [{ id: Date.now(), name: 'Sem título.tps', html: '<p><br></p>', y: 0, dirty: false }];
    if (id === this.state.active) this.loadDoc(docs, docs[Math.max(0, docs.length - 1)].id); else this.setState({ docs });
  }
  scheduleSave() {
    clearTimeout(this.autoT);
    this.autoT = setTimeout(() => {
      const docs = this.persistDocs();
      const now = Date.now(); if (this.state.dirty && now - (this.lastVer || 0) > 120000) { this.lastVer = now; this.addVersion(this.state.docName, this.edRef.current.innerHTML, 'automática'); }
      const d = new Date(); this.setState({ docs, savedAt: 'Salvo automaticamente às ' + String(d.getHours()).padStart(2, '0') + ':' + String(d.getMinutes()).padStart(2, '0') });
    }, 1200);
  }
  persistDocs() { const docs = this.snapshotDocs(); store.set('arca-prompter-docs', JSON.stringify({ docs, active: this.state.active })); return docs; }
  addVersion(name, html, kind) {
    const v = [{ t: Date.now(), name, html, kind: kind || 'salva', words: (html.replace(/<[^>]+>/g, ' ').match(/\S+/g) || []).length }, ...this.state.versions].slice(0, 25);
    store.set('arca-prompter-versions', JSON.stringify(v)); this.setState({ versions: v });
  }
  setWho(n) {
    const ed = this.edRef.current, sel = window.getSelection(); if (!sel.rangeCount || !ed.contains(sel.anchorNode)) return this.flash('Posicione o cursor no texto');
    const r = sel.getRangeAt(0), blk = n => { while (n && n.parentElement !== ed) n = n.parentElement; return n; };
    let a = blk(r.startContainer), b = blk(r.endContainer); if (!a) return;
    for (let el = a; el; el = el.nextElementSibling) { if (!el.hasAttribute('data-bm') && !el.hasAttribute('data-stop')) { if (n) el.setAttribute('data-who', n); else el.removeAttribute('data-who'); } if (el === b) break; }
    this.pushHtml(); this.setState({ curWho: n });
  }
  removePres(i) {
    const ed = this.edRef.current, n = i + 1;
    ed.querySelectorAll('[data-who]').forEach(el => { const w = +el.getAttribute('data-who'); if (w === n) el.removeAttribute('data-who'); else if (w > n) el.setAttribute('data-who', String(w - 1)); });
    this.setState(x => ({ pres: x.pres.filter((_, j) => j !== i), presOnly: x.presOnly === String(n) ? '0' : (+x.presOnly > n ? String(+x.presOnly - 1) : x.presOnly) }), () => this.pushHtml());
  }
  moveBlock(from, to) {
    const ed = this.edRef.current, bms = [...ed.querySelectorAll('[data-bm]')]; if (from === to || from < 0 || !bms[from]) return;
    const frag = [], start = bms[from]; for (let el = start; el && (el === start || !el.hasAttribute('data-bm')); el = el.nextElementSibling) frag.push(el);
    const ref = to < bms.length ? bms[to] : null; if (ref && frag.includes(ref)) return;
    frag.forEach(el => ed.insertBefore(el, ref)); this.pushHtml(); this.flash('Bloco movido');
  }
  profSettings() { const s = this.state; return { fontKey: s.fontKey, wpl: s.wpl, margin: s.margin, mPos: s.mPos, lineH: s.lineH, bg: s.bg, fg: s.fg, align: s.align, indType: s.indType, indSize: s.indSize, indColor: s.indColor, readPct: s.readPct, speedT: s.speedT, minS: s.minS, maxS: s.maxS }; }
  setProfiles(p) { store.set('arca-prompter-profiles', JSON.stringify(p)); this.setState({ profiles: p }); }
  printScript(mode = 'print') {
    const s = this.state, ed = this.edRef.current, wps = Math.max(0.1, this.lps() * s.wpl); let bi = -1, html = '';
    const nm = n => { const p = s.pres[n - 1]; return p ? (p.name || 'Apresentador ' + n) : ''; };
    [...ed.children].forEach(ch => {
      if (ch.hasAttribute('data-bm')) { bi++; const b = s.blocks[bi] || { words: 0, skip: false }; html += `<h2${b.skip ? ' class="skip"' : ''}><span>${esc(ch.textContent)}${b.skip ? ' — pulado' : ''}</span><em>${this.fmt(b.words / wps)}</em></h2>`; return; }
      if (ch.hasAttribute('data-stop')) { html += `<div class="stop">Pausa · ${esc(ch.textContent)}</div>`; return; }
      const w = +(ch.getAttribute('data-who') || 0), st = ch.hasAttribute('data-who-start');
      html += `<p${ch.hasAttribute('data-skipped') ? ' class="skip"' : ''}>${w && st ? `<b style="color:${s.pres[w - 1] ? s.pres[w - 1].color : '#000'}">${esc(nm(w))}</b>` : ''}${ch.innerHTML}</p>`;
    });
    const total = s.blocks.reduce((a, b) => a + (b.skip ? 0 : b.words / wps), 0);
    const doc = `<!doctype html><html><head><meta charset="utf-8"><title>${esc(s.docName)}</title><style>body{font:12pt/1.55 Georgia,serif;color:#111;max-width:680px;margin:40px auto;padding:0 24px}header{display:flex;justify-content:space-between;align-items:baseline;border-bottom:2px solid #111;padding-bottom:8px;margin-bottom:20px}h1{font:600 18pt system-ui,sans-serif;margin:0}header span{font:10pt system-ui,sans-serif;color:#555}h2{display:flex;justify-content:space-between;font:600 10pt system-ui,sans-serif;letter-spacing:.08em;text-transform:uppercase;border-top:1px solid #bbb;padding-top:10px;margin:26px 0 10px}h2 em{font-style:normal;color:#555}p{margin:0 0 10px}p b{display:block;font:600 8.5pt system-ui,sans-serif;letter-spacing:.1em;text-transform:uppercase;margin-bottom:2px}.stop{font:600 9pt system-ui,sans-serif;border:1px dashed #888;border-radius:4px;padding:3px 8px;display:inline-block;margin:4px 0 12px}.skip{opacity:.4;text-decoration:line-through}</style></head><body><header><h1>${esc(s.docName.replace(/\.tps$/, ''))}</h1><span>${s.words} palavras · estimado ${this.fmt(total)} · programa ${this.fmt(s.progSec)}</span></header>${html}</body></html>`;
    platform.printHtml(doc, mode, s.docName.replace(/\.\w+$/, '')).then(ok => { if (!ok) this.flash(isDesktop ? 'Impressão cancelada' : 'Permita pop-ups para imprimir'); else if (mode === 'pdf' && isDesktop) this.flash('PDF exportado'); });
  }
  toggleSkip(i) { const el = this.edRef.current.querySelectorAll('[data-bm]')[i]; if (!el) return; if (el.hasAttribute('data-skip')) el.removeAttribute('data-skip'); else el.setAttribute('data-skip', '1'); this.pushHtml(); }
  saveDoc(saveAs = false) {
    this.addVersion(this.state.docName, this.edRef.current.innerHTML, 'salva');
    const s = this.state, data = { app: 'ARCA Prompter', v: 1, html: this.edRef.current.innerHTML, settings: { wpl: s.wpl, margin: s.margin, mPos: s.mPos, lineH: s.lineH, fontKey: s.fontKey, bg: s.bg, fg: s.fg, align: s.align, readPct: s.readPct, ovType: s.ovType, indType: s.indType, speedT: s.speedT, minS: s.minS, maxS: s.maxS, pres: s.pres, progSec: s.progSec, progInput: this.fmt(s.progSec) } };
    const doc = s.docs.find(d => d.id === s.active), id = s.active;
    platform.saveFile(s.docName.replace(/\.\w+$/, '') + '.tps', JSON.stringify(data), 'application/json', doc && doc.path, saveAs).then(r => {
      if (!r) return;
      this.setState(x => ({ dirty: false, docName: x.active === id ? r.name : x.docName, docs: x.docs.map(d => d.id === id ? { ...d, name: r.name, path: r.path || d.path, dirty: false } : d) }), () => this.scheduleSave());
      if (isDesktop) this.flash('Salvo · ' + r.name);
    });
  }
  exportTxt() { const ed = this.edRef.current; const txt = [...ed.children].map(c => (c.hasAttribute('data-bm') ? '*' : c.hasAttribute('data-stop') ? '[PAUSA] ' : '') + c.textContent).join('\n\n'); platform.saveFile(this.state.docName.replace(/\.\w+$/, '') + '.txt', txt, 'text/plain', undefined, true); }
  openFileDialog = () => { if (isDesktop) platform.openFile().then(f => f && this.loadFile(f.file, f.path)); else this.openFile.current && this.openFile.current.click(); };
  onOpenFile = e => { const f = e.target.files[0]; if (!f) return; e.target.value = ''; this.loadFile(f); };
  async loadFile(f, path) {
    const toHtml = plain => plain.split(/\r?\n/).map(l => l.trim()).filter(Boolean).map(l => l.startsWith('*') ? `<p data-bm="1" contenteditable="false">${esc(l.slice(1).trim())}</p>` : /^\[pausa\]/i.test(l) ? `<p data-stop="1" contenteditable="false">${esc(l.replace(/^\[pausa\]\s*/i, '') || 'Pausa')}</p>` : `<p>${esc(l)}</p>`).join('');
    const base = f.name.replace(/\.\w+$/, '') + '.tps';
    try {
      if (/\.docx$/i.test(f.name)) return this.openInTab(base, toHtml(await docxToText(f)) || '<p><br></p>');
      if (/\.pdf$/i.test(f.name)) return this.openInTab(base, toHtml(await pdfToText(f)) || '<p><br></p>');
      const txt = await f.text();
      if (/\.(tps|json)$/i.test(f.name)) {
        let html = '', settings = null;
        try { const d = JSON.parse(txt); html = d.html || ''; settings = d.settings; } catch (err) { html = `<p>${esc(txt)}</p>`; }
        // Um .tps já aberto numa aba só ganha foco.
        const open = path && this.state.docs.find(d => d.path === path); if (open) return this.switchTab(open.id);
        return this.openInTab(f.name, html || '<p><br></p>', settings || {}, /\.tps$/i.test(f.name) ? path : '');
      }
      const plain = /\.html?$/i.test(f.name) ? new DOMParser().parseFromString(txt, 'text/html').body.innerText : txt;
      this.openInTab(base, toHtml(plain) || '<p><br></p>');
    } catch (err) { this.flash('Não foi possível ler ' + f.name); }
  }
  textNodes() { const out = [], tw = document.createTreeWalker(this.edRef.current, NodeFilter.SHOW_TEXT); let n; while ((n = tw.nextNode())) out.push(n); return out; }
  findNext() {
    const q = this.state.findQ.toLowerCase(); if (!q) return;
    const hits = []; this.textNodes().forEach(n => { const d = n.data.toLowerCase(); let i = d.indexOf(q); while (i >= 0) { hits.push([n, i]); i = d.indexOf(q, i + q.length); } });
    if (!hits.length) return this.setState({ findInfo: 'Nenhuma ocorrência' });
    this.fi = (this.fi + 1) % hits.length; const [n, i] = hits[this.fi];
    const r = document.createRange(); r.setStart(n, i); r.setEnd(n, i + q.length);
    const sel = window.getSelection(); sel.removeAllRanges(); sel.addRange(r);
    let b = n.parentNode; const ed = this.edRef.current; while (b && b.parentNode !== ed) b = b.parentNode;
    const idx = [...ed.children].indexOf(b); const tb = this.blocks()[idx]; if (tb && !this.state.playing) this.setY(tb.offsetTop);
    this.setState({ findInfo: `${this.fi + 1} de ${hits.length}`, follow: true });
  }
  replaceAll() {
    const q = this.state.findQ; if (!q) return; const re = new RegExp(q.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'), 'gi'); let n = 0;
    this.textNodes().forEach(t => { const m = t.data.match(re); if (m) { n += m.length; t.data = t.data.replace(re, this.state.replQ); } });
    this.pushHtml(); this.setState({ findInfo: n ? `${n} substituída(s)` : 'Nenhuma ocorrência' });
  }

  key(e) {
    const t = e.target, s = this.state, k = e.key, c = e.ctrlKey || e.metaKey;
    if (s.capture) { e.preventDefault(); if (k !== 'Escape') this.setState(x => ({ keymap: { ...x.keymap, [x.capture]: k }, capture: null })); else this.setState({ capture: null }); return; }
    const kn = this.keyName(k); if (kn !== s.lastKey) this.setState({ lastKey: kn });
    if (k === 'Escape' && s.menu) return this.setState({ menu: null });
    if (k === 'Escape' && s.dlg) return this.setState({ dlg: null });
    if (s.dlg) return;
    const typing = t.isContentEditable || t.tagName === 'SELECT' || t.tagName === 'TEXTAREA' || (t.tagName === 'INPUT' && t.type !== 'range');
    if (k === 'F5') { e.preventDefault(); return this.toggleTalent(); }
    if (e.altKey && !c && /^[0-8]$/.test(k) && !s.talent) { e.preventDefault(); if (+k > s.pres.length) return this.flash('Apresentador ' + k + ' não existe'); return this.setWho(+k); }
    if (c) {
      const lk = k.toLowerCase(), map = { p: () => this.printScript(e.shiftKey ? 'pdf' : 'print'), w: () => this.closeTab(s.active), enter: () => this.togglePlay(), e: () => this.toggleLock(), k: () => this.insertBlock('data-bm'), j: () => this.cursorToPrompter(), l: () => this.setState({ follow: true, locked: s.playing && s.lockOnPlay }), f: () => this.setState({ dlg: 'find' }), s: () => this.saveDoc(e.shiftKey), o: () => this.openFileDialog(), n: () => this.setState({ dlg: 'novo' }), ',': () => this.setState({ dlg: 'prefs' }) };
      if (map[lk]) { e.preventDefault(); return map[lk](); }
      if (/^[1-9]$/.test(k)) { e.preventDefault(); return this.jumpBm(+k - 1); }
      return;
    }
    if (s.inline) { if (k === 'Escape') { e.preventDefault(); this.finishInline(); } return; }
    if (k === 'Escape' && s.talent) return this.toggleTalent();
    if (typing) return;
    if (/^[1-9]$/.test(k) && s.keymap.stop !== k) { e.preventDefault(); return this.setSpeedT((+k - 1) / 8, true); }
    if (k === 'Backspace') { e.preventDefault(); return this.setY(0); }
    const act = Object.keys(s.keymap).find(a => s.keymap[a].toLowerCase() === k.toLowerCase()); if (!act) return;
    e.preventDefault(); this.big = e.shiftKey; this.doAction(act); this.big = false;
  }
  doAction(a) {
    const s = this.state, lh = this.lineHpx(), st = this.big ? 0.15 : 0.05, run = s.playing || s.count > 0;
    ({ play: () => this.togglePlay(),
      faster: () => run ? this.adjSpeed(st) : this.setY(this.y + lh * (this.big ? 5 : 1)),
      slower: () => run ? this.adjSpeed(-st) : this.setY(this.y - lh * (this.big ? 5 : 1)),
      faster2: () => this.adjSpeed(st), slower2: () => this.adjSpeed(-st),
      reverse: () => { this.setState(x => ({ dir: -x.dir })); this.flashSpeed(s.speedT, -s.dir); },
      stop: () => { if (s.playing) this.pause(); this.flash('Parado'); },
      rewind: () => this.setY(0), toEnd: () => this.setY(this.endY()),
      editHere: () => s.talent && this.editHere(), nextBm: () => this.jumpBm(s.cur + 1), prevBm: () => this.jumpBm(Math.max(0, s.cur - 1)), nextPara: () => this.jumpPara(1), prevPara: () => this.jumpPara(-1),
      mirror: () => { const d = this.tvDisp(); if (d.id) { this.updDisp(d.id, x => ({ flipH: !x.flipH })); this.flash(d.flipH ? 'Espelho desligado' : 'Espelhado'); } } })[a]();
  }
  adjSpeed(d) {
    if (this.state.speedLock && this.state.talent) return this.flash('Velocidade travada');
    const s = this.state; let v = s.dir * s.speedT + d, dir = s.dir;
    if (v < 0 && s.dir > 0) { dir = -1; v = 0; } else if (v > 0 && s.dir < 0) { dir = 1; v = 0; }
    const t = Math.min(1, Math.abs(v));
    this.setState({ speedT: t, dir, autoRun: false }); this.flashSpeed(t, dir);
  }
  setSpeedT(t, show) { this.setState({ speedT: t, autoRun: false }); if (show) this.flashSpeed(t, this.state.dir); }
  flashSpeed(t, dir) { const s = this.state, lps = s.minS + t * (s.maxS - s.minS), u = [['linhas/s', 2, 1], ['linhas/min', 0, 60], ['palavras/s', 1, s.wpl], ['palavras/min', 0, s.wpl * 60]][s.unit]; this.flash(`${(lps * u[2]).toFixed(u[1])} ${u[0]}`, t, dir < 0 ? 'Ré' : 'Frente'); }
  flash(text, pct, sub) { clearTimeout(this.flashT); this.setState({ flashText: text, flashPct: pct == null ? -1 : pct, flashSub: sub || '' }); this.flashT = setTimeout(() => this.setState({ flashText: '' }), 1100); }
  onWheelCtl(e, area) {
    const s = this.state;
    if (e.ctrlKey) { const n = Math.max(2, Math.min(12, s.wpl + (e.deltaY > 0 ? 1 : -1))); if (n !== s.wpl) { this.setState({ wpl: n }); this.flash(n + ' palavras por linha'); } return; }
    if (s.playing || s.count > 0) { const now = performance.now(); if (now - (this.lastWheel || 0) < 60) return; this.lastWheel = now; this.adjSpeed(e.deltaY > 0 ? 0.03 : -0.03); return; }
    if (area === 'tv' && !s.speedLock) this.wheel += e.deltaY * 0.6;
  }
  onPtrDown(e) { if (e.button !== 0 || this.state.inline || e.detail > 1) return; this.drag = { y: e.clientY, sy: this.y, k: (this.tvRef.current.clientWidth / 1000) || 1, flip: this.state.winFlipV ? -1 : 1, moved: false }; }
  onPtrMove(e) { const d = this.drag; if (!d) return; const dy = e.clientY - d.y; if (Math.abs(dy) > 4) d.moved = true; if (d.moved && !this.state.playing) this.setY(d.sy - dy / d.k * d.flip); }
  onPtrUp() { this.drag = null; }
  keyName(k) { return ({ ' ': 'Espaço', ArrowUp: '↑', ArrowDown: '↓', ArrowLeft: '←', ArrowRight: '→', Backspace: 'Backspace', PageUp: 'Page Up', PageDown: 'Page Down', Escape: 'Esc', Enter: 'Enter' })[k] || (k.length === 1 ? k.toUpperCase() : k); }

  norm(w) { return w.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/[^a-z0-9]/g, ''); }
  buildWords() {
    const tc = this.tcRef.current; this.W = []; if (!tc) return;
    const tw = document.createTreeWalker(tc, NodeFilter.SHOW_TEXT, { acceptNode: n => n.parentElement.closest('[data-bm],[data-stop]') ? 2 : 1 });
    let n; while ((n = tw.nextNode())) { const re = /[\p{L}\p{N}]+/gu; let m; while ((m = re.exec(n.data))) this.W.push({ node: n, i: m.index, len: m[0].length, w: m[0], n: this.norm(m[0]) }); }
  }
  wordY(i) {
    const W = this.W[i], tc = this.tcRef.current; if (!W) return this.y;
    let b = W.node.parentNode; while (b && b.parentNode !== tc) b = b.parentNode; if (!b) return this.y;
    const r = document.createRange(); r.setStart(W.node, W.i); r.setEnd(W.node, W.i + W.len);
    const rr = r.getBoundingClientRect(), br = b.getBoundingClientRect(); if (!br.height) return b.offsetTop;
    const frac = this.state.winFlipV ? (br.bottom - rr.bottom) / br.height : (rr.top - br.top) / br.height;
    return b.offsetTop + frac * b.offsetHeight;
  }
  sim(a, b) {
    if (a === b) return 1; const m = a.length, n = b.length; if (!m || !n) return 0;
    let p = Array.from({ length: n + 1 }, (_, j) => j);
    for (let i = 1; i <= m; i++) { const c = [i]; for (let j = 1; j <= n; j++) c[j] = Math.min(p[j] + 1, c[j - 1] + 1, p[j - 1] + (a[i - 1] === b[j - 1] ? 0 : 1)); p = c; }
    return 1 - p[n] / Math.max(m, n);
  }
  hear(raw) {
    const n = this.norm(raw); if (!n) return;
    const s = this.state, improv = this.misses >= s.tol;
    const from = improv ? Math.max(0, this.vp - 10) : this.vp, to = Math.min(this.W.length, this.vp + (improv ? 30 : s.win));
    let hit = -1;
    if (!(improv && n.length < 3) && n.length >= 2) for (let i = from; i < to; i++) if (this.sim(n, this.W[i].n) >= 0.75) { hit = i; break; }
    let st = s.vState, ok = 0;
    if (hit >= 0 && improv) { if (this.pending != null && hit === this.pending + 1) { this.misses = 0; this.pending = null; } else { this.pending = hit; st = 'ressinc'; ok = 1; } }
    if (hit >= 0 && this.misses < s.tol) { this.vp = hit + 1; this.vTarget = this.wordY(hit); this.misses = 0; st = 'seguindo'; ok = 2; }
    else if (hit < 0) { this.misses++; if (this.misses >= s.tol) { st = 'improviso'; this.pending = null; } }
    this.setState(x => ({ vState: st, heard: [...x.heard, { w: raw, ok }].slice(-16) }));
  }
  voiceStartPos() { let i = 0; while (i < this.W.length && this.wordY(i) < this.y - 2) i++; this.vp = i; this.misses = 0; this.pending = null; this.vTarget = null; }
  startMic = () => {
    if (this.state.vMode === 'mic') return this.stopVoice();
    this.stopVoice();
    const SR = window.SpeechRecognition || window.webkitSpeechRecognition;
    if (!SR) return this.setState({ vErr: 'Reconhecimento de voz indisponível neste navegador. Use “Simular leitura”.' });
    this.voiceStartPos();
    const r = new SR(); r.lang = this.state.vLang; r.continuous = true; r.interimResults = true; this.proc = {};
    r.onresult = e => { for (let i = e.resultIndex; i < e.results.length; i++) { const ws = e.results[i][0].transcript.trim().split(/\s+/).filter(Boolean); for (let k = this.proc[i] || 0; k < ws.length; k++) this.hear(ws[k]); this.proc[i] = ws.length; } };
    r.onend = () => { if (this.rec === r) { this.proc = {}; try { r.start(); } catch (e) {} } };
    r.onerror = ev => {
      // O reconhecimento do Chromium depende do serviço online do Google, ausente no Electron.
      if (ev.error === 'network') { this.rec = null; this.stopVoice(); return this.setState({ vErr: isDesktop ? 'Reconhecimento de voz online indisponível no app. Use a versão web no Chrome/Edge ou “Simular leitura”.' : 'Sem conexão com o serviço de reconhecimento de voz.' }); }
      if (/not-allowed|service-not-allowed/.test(ev.error)) { this.rec = null; this.stopVoice(); this.setState({ vErr: 'Microfone bloqueado. Libere o acesso ou use “Simular leitura”.' }); } };
    this.rec = r; try { r.start(); } catch (e) {}
    if (navigator.mediaDevices) navigator.mediaDevices.getUserMedia({ audio: true }).then(st => {
      this.stream = st; const ac = new (window.AudioContext || window.webkitAudioContext)(); this.ac = ac;
      const an = ac.createAnalyser(); an.fftSize = 1024; ac.createMediaStreamSource(st).connect(an); const buf = new Float32Array(an.fftSize);
      this.levelT = setInterval(() => { an.getFloatTimeDomainData(buf); let q = 0; for (const v of buf) q += v * v; this.level = Math.max(-70, 20 * Math.log10(Math.sqrt(q / buf.length) + 1e-6)); }, 80);
    }).catch(() => {});
    this.setState({ vMode: 'mic', vState: 'ouvindo', heard: [], vErr: '', playing: false, locked: true, follow: true });
  };
  startSim = () => {
    if (this.state.vMode === 'sim') return this.stopVoice();
    this.stopVoice(); this.voiceStartPos();
    let p = this.vp, said = 0, imp = -1; const every = this.props.improvEvery ?? 22;
    this.simT = setInterval(() => {
      if (imp >= 0) { this.hear(IMPROV[imp]); this.level = -22 - Math.random() * 6; imp++; if (imp >= IMPROV.length) { imp = -1; p = this.vp; } return; }
      if (p >= this.W.length) return this.stopVoice();
      this.hear(this.W[p].w); p++; said++; this.level = -18 - Math.random() * 8;
      if (said % every === 0) imp = 0;
    }, this.props.simWordMs ?? 340);
    this.setState({ vMode: 'sim', vState: 'ouvindo', heard: [], vErr: '', playing: false, locked: true, follow: true });
  };
  stopVoice() {
    clearInterval(this.simT); clearInterval(this.levelT); this.level = -70;
    if (this.rec) { const r = this.rec; this.rec = null; try { r.stop(); } catch (e) {} }
    if (this.stream) { this.stream.getTracks().forEach(t => t.stop()); this.stream = null; }
    if (this.ac) { this.ac.close().catch(() => {}); this.ac = null; }
    this.vTarget = null;
    if (this.state.vMode !== 'off') this.setState({ vMode: 'off', vState: 'off', locked: false });
  }
  toggleCam = () => {
    if (this.state.camOn) { if (this.state.recording) this.stopRec(); this.cam.getTracks().forEach(t => t.stop()); this.cam = null; return this.setState({ camOn: false, camMsg: 'Câmera desligada' }); }
    if (!navigator.mediaDevices) return this.setState({ camMsg: 'Câmera indisponível' });
    navigator.mediaDevices.getUserMedia({ video: true, audio: true }).then(st => { this.cam = st; this.setState({ camOn: true }); }).catch(() => this.setState({ camMsg: 'Câmera bloqueada ou indisponível' }));
  };
  startRec() {
    if (!this.cam || !window.MediaRecorder) return;
    this.chunks = []; const mr = new MediaRecorder(this.cam); this.mr = mr;
    mr.ondataavailable = e => e.data.size && this.chunks.push(e.data);
    mr.onstop = () => { const url = URL.createObjectURL(new Blob(this.chunks, { type: 'video/webm' })); this.setState(x => ({ recUrl: url, lastRec: x.recSec })); };
    mr.start(); clearInterval(this.recT); this.setState({ recording: true, recSec: 0 }); this.recT = setInterval(() => this.setState(x => ({ recSec: x.recSec + 1 })), 1000);
  }
  stopRec() { clearInterval(this.recT); if (this.mr && this.mr.state !== 'inactive') this.mr.stop(); this.setState({ recording: false }); }
  // NDI exige o NDI SDK + um módulo nativo (ver electron/ndi.ts). Até lá, não finge transmitir.
  toggleNdi() { this.flash(isDesktop ? 'Saída NDI: módulo nativo ainda não incluído' : 'Saída NDI só no app para Windows'); }
  async toggleWeb(force) {
    const m = platform.monitor, s = this.state;
    if (!m) { this.flash('Monitor web só no app para Windows'); return false; }
    if (s.webOn && force !== true) { await m.stop(); this.setState({ webOn: false, webViewers: 0, webUrl: '' }); return false; }
    if (s.webOn) return true;
    this.setState({ webBusy: true, webErr: '' });
    const r = await m.start({ port: WEB_PORT, pass: s.webPass, ctrl: s.webCtrl });
    this.setState({ webBusy: false, webOn: !!r.ok, webUrl: r.url || '', webErr: r.ok ? '' : (r.error || 'Falha ao iniciar'), webViewers: 0 });
    if (r.ok) this.onBusCmd({ t: 'hello' });
    return !!r.ok;
  }
  async toggleMos() {
    const m = platform.mos, s = this.state;
    if (!m) return this.setState({ mosErr: 'Integração MOS só no app para Windows' });
    if (s.mosOn) { m.disconnect(); return this.setState({ mosOn: false, mosErr: '' }); }
    this.setState({ mosBusy: true, mosErr: '' });
    const r = await m.connect(s.mosHost.trim(), 10540);
    this.setState({ mosBusy: false, mosOn: r.ok, mosErr: r.ok ? '' : (r.error || 'Falha na conexão') });
  }
  tvDisp() { const d = this.state.displays; return d.find(x => x.on && !x.operator) || d.find(x => x.on) || { nick: 'Janela flutuante', name: 'Janela', flipH: false, flipV: false, align: this.state.align, find: false }; }
  fmt(sec) { if (!isFinite(sec)) sec = 0; sec = Math.max(0, Math.round(sec)); return String(Math.floor(sec / 60)).padStart(2, '0') + ':' + String(sec % 60).padStart(2, '0'); }
  updDisp(id, patch) { this.setState(s => ({ displays: s.displays.map(d => d.id === id ? { ...d, ...(typeof patch === 'function' ? patch(d) : patch) } : d) })); }

  renderVals() {
    const s = this.state, fsz = this.fs(), lh = fsz * s.lineH, lps = this.lps(), pxs = lps * lh, td = this.tvDisp();
    const live = s.playing || s.count > 0;
    const ck = on => ({ bg: on ? 'var(--inv)' : 'transparent', bd: on ? 'var(--inv)' : 'var(--faint)', op: on ? 1 : 0 });
    const sw = on => ({ track: on ? 'var(--t-accent)' : 'var(--t-border)', left: on ? '16px' : '2px', knob: '#fff' });
    const seg = on => ({ bg: on ? 'var(--raised)' : 'transparent', shadow: on ? 'var(--shadow)' : 'none', color: on ? 'var(--text)' : 'var(--muted)' });
    const down = fn => e => { e.preventDefault(); fn(); };
    const units = [['linhas/s', 2, 1], ['linhas/min', 0, 60], ['palavras/s', 1, s.wpl], ['palavras/min', 0, s.wpl * 60]];
    const u = units[s.unit], fu = v => (v * u[2]).toFixed(u[1]);
    const pos = s.t.y / pxs, total = s.t.end / pxs, wps = Math.max(0.1, lps * s.wpl), estTotal = s.blocks.reduce((a, b) => a + (b.skip ? 0 : b.words / wps), 0);
    const V = { off: ['DESLIGADO', 'Acompanhamento desligado', 'desligada'], ouvindo: ['OUVINDO', 'Aguardando a primeira palavra do roteiro', 'ouvindo'], seguindo: ['SEGUINDO', 'Rolagem acompanha a voz', 'seguindo'], improviso: ['IMPROVISO', 'Fora do roteiro — rolagem pausada', 'improviso'], ressinc: ['RESSINC.', 'Texto encontrado, confirmando posição', 'ressincronizando'] }[s.vState];
    const ind = { seta: ['1.8', '2.4', 'polygon(0 0,100% 50%,0 100%)', '0'], quadrado: ['1.7', '1.7', 'none', '2px'], ponto: ['1.8', '1.8', 'none', '50%'], linha: ['4', '0.5', 'none', '1px'] }[s.indType] || ['0', '0', 'none', '0'];
    const indK = fsz * 0.1 * 0.28 * [0.65, 1, 1.45][s.indSize];
    const rgba = (h, a) => { const n = parseInt(h.slice(1), 16); return `rgba(${n >> 16},${(n >> 8) & 255},${n & 255},${a})`; };
    const tb = (on, d, title, onClick) => ({ d, title, onClick, bg: on ? 'var(--t-accent-soft)' : 'transparent', bd: on ? 'var(--t-accent-line)' : 'var(--t-border)', fg: on ? 'var(--t-accent)' : 'var(--t-muted)' });
    const it = (label, fn, key, check) => ({ isItem: true, sep: false, label, key: key || '', check: check ? '✓' : '', onClick: () => { this.setState({ menu: null }); fn(); } });
    const SEP = { sep: true, isItem: false };
    const menuDefs = [
      ['Arquivo', [it('Novo', () => this.setState({ dlg: 'novo' }), 'Ctrl+N'), it('Abrir…', () => this.openFileDialog(), 'Ctrl+O'), it('Salvar', () => this.saveDoc(), 'Ctrl+S'), ...(isDesktop ? [it('Salvar como…', () => this.saveDoc(true), 'Ctrl+Shift+S')] : []), SEP, it('Importar Word, PDF, texto ou HTML…', () => this.openFileDialog()), it('Histórico de versões…', () => this.setState({ dlg: 'versoes' })), it('Fechar roteiro', () => this.closeTab(s.active), 'Ctrl+W'), it('Exportar como .txt', () => this.exportTxt()), ...(isDesktop ? [it('Imprimir…', () => this.printScript('print'), 'Ctrl+P'), it('Exportar PDF…', () => this.printScript('pdf'), 'Ctrl+Shift+P')] : [it('Imprimir / exportar PDF…', () => this.printScript(), 'Ctrl+P')]), SEP, it('Perfis de apresentador…', () => this.setState({ dlg: 'perfis' })), SEP, it('Preferências…', () => this.setState({ dlg: 'prefs', prefTab: 'geral' }), 'Ctrl+,')]],
      ['Editar', [it('Desfazer', () => this.exec('undo'), 'Ctrl+Z'), it('Refazer', () => this.exec('redo'), 'Ctrl+Y'), SEP, it('Selecionar tudo', () => this.exec('selectAll'), 'Ctrl+A'), it('Localizar e substituir…', () => this.setState({ dlg: 'find' }), 'Ctrl+F'), SEP, it('Bloquear editor', () => this.toggleLock(), 'Ctrl+E', s.locked)]],
      ['Formatar', [...s.pres.map((p, i) => it(p.name || ('Apresentador ' + (i + 1)), () => this.setWho(i + 1), 'Alt+' + (i + 1))), it('Sem apresentador', () => this.setWho(0), 'Alt+0'), SEP, it('Negrito', () => this.exec('bold'), 'Ctrl+B'), it('Itálico', () => this.exec('italic'), 'Ctrl+I'), it('Sublinhado', () => this.exec('underline'), 'Ctrl+U'), SEP, it('Converter para MAIÚSCULAS', () => this.upperSel()), SEP, ...[['▶', 'Seta de ação'], ['●', 'Ponto de destaque'], ['★', 'Estrela'], ['⚠', 'Atenção'], ['↓', 'Seta para baixo'], ['✓', 'Visto']].map(([c, n]) => it(`Inserir símbolo ${c}  ${n}`, () => this.exec('insertText', c + ' '))), it('Mais símbolos e emojis (Win + .)', () => {}), SEP, it('Limpar formatação', () => this.exec('removeFormat'))]],
      ['Marcadores', [it('Inserir marcador', () => this.insertBlock('data-bm'), 'Ctrl+K'), it('Inserir ponto de pausa', () => this.insertBlock('data-stop')), it('Excluir marcador atual', () => s.cur >= 0 && this.removeBlock('data-bm', s.cur)), it('Excluir todas as pausas', () => { this.edRef.current.querySelectorAll('[data-stop]').forEach(el => el.remove()); this.pushHtml(); }), SEP, ...s.bms.slice(0, 9).map((b, i) => it(b.name, () => this.jumpBm(i), 'Ctrl+' + (i + 1))), SEP, it('Lista de marcadores', () => this.setState(x => ({ showBm: !x.showBm })), '', s.showBm)]],
      ['Prompter', [it(s.playing ? 'Parar prompt' : 'Iniciar prompt', () => this.togglePlay(), 'Ctrl+Enter'), it('Voltar ao início', () => this.setY(0), this.keyName(s.keymap.rewind)), it('Parágrafo anterior', () => this.jumpPara(-1), this.keyName(s.keymap.prevPara || '[')), it('Próximo parágrafo', () => this.jumpPara(1), this.keyName(s.keymap.nextPara || ']')), SEP, it('Janela do talento', () => this.toggleTalent(), 'F5', s.talent), it('Editar trecho atual', () => this.editHere(), this.keyName(s.keymap.editHere)), SEP, it(`Espelhar horizontal · ${td.nick}`, () => td.id && this.updDisp(td.id, x => ({ flipH: !x.flipH })), '', td.flipH), it(`Espelhar vertical · ${td.nick}`, () => td.id && this.updDisp(td.id, x => ({ flipV: !x.flipV })), '', td.flipV), SEP, it('Simular acompanhamento por voz', () => this.startSim(), '', s.vMode === 'sim')]],
      ['Exibir', [it('Informações na tela do talento', () => this.setState(x => ({ talentHud: !x.talentHud })), '', s.talentHud), SEP, it('Tema escuro', () => this.setState({ theme: 'dark' }), '', s.theme === 'dark'), it('Tema claro', () => this.setState({ theme: 'light' }), '', s.theme === 'light'), SEP, it('Marcadores', () => this.setState(x => ({ showBm: !x.showBm })), '', s.showBm), it('Inspetor', () => this.setState(x => ({ showInsp: !x.showInsp })), '', s.showInsp)]],
      ['Janela', [it('Controle remoto…', () => this.setState({ dlg: 'remote' })), it('Monitor web…', () => { if (platform.monitor && !this.state.webOn) this.toggleWeb(true); this.setState({ dlg: 'monitor' }); }), it('Saída NDI', () => this.toggleNdi(), '', s.ndiOn), SEP, it('Telas conectadas', () => this.setState({ showInsp: true, tab: 'telas' })), it('Gravação de vídeo', () => this.setState({ showInsp: true, tab: 'video' }))]],
      ['Ajuda', [it('Atalhos de teclado…', () => this.setState({ dlg: 'prefs', prefTab: 'atalhos' })), it('Sobre o ARCA Prompter', () => this.setState({ dlg: 'about' }))]]
    ];
    const dlgMeta = { prefs: ['Preferências', '640px'], find: ['Localizar e substituir', '420px'], remote: ['Controle remoto', '340px'], about: ['Sobre', '380px'], novo: ['Novo roteiro', '380px'], versoes: ['Histórico de versões', '480px'], perfis: ['Perfis de apresentador', '480px'], monitor: ['Monitor web', '760px'] }[s.dlg] || ['', '400px'];
    return {
      theme: s.theme, docTitle: `${s.docName}${s.dirty ? ' •' : ''}`,
      winCtl: isDesktop ? 'flex' : 'none', winMin: () => platform.win && platform.win.minimize(), winMax: () => platform.win && platform.win.maximize(), winClose: () => platform.win && platform.win.close(),
      openAccept: '.tps,.json,.txt,.html,.htm,.docx,.pdf', aboutVer: `Versão ${__APP_VERSION__} · ${isDesktop ? 'Windows' : 'Web'}`,
      menus: menuDefs.map(([label, items]) => ({ label, items, open: s.menu === label, bg: s.menu === label ? 'var(--hover)' : 'transparent', onClick: () => this.setState({ menu: s.menu === label ? null : label }), onEnter: () => { if (this.state.menu && this.state.menu !== label) this.setState({ menu: label }); } })),
      anyMenu: !!s.menu, closeMenus: () => this.setState({ menu: null }),
      tools: [
        { icon: true, d: I.new, title: 'Novo (Ctrl+N)', onDown: down(() => this.setState({ dlg: 'novo' })) }, { icon: true, d: I.open, title: 'Abrir (Ctrl+O)', onDown: down(() => this.openFileDialog()) }, { icon: true, d: I.save, title: 'Salvar (Ctrl+S)', onDown: down(() => this.saveDoc()) },
        { sep: true }, { icon: true, d: I.undo, title: 'Desfazer (Ctrl+Z)', onDown: down(() => this.exec('undo')) }, { icon: true, d: I.redo, title: 'Refazer (Ctrl+Y)', onDown: down(() => this.exec('redo')) }, { icon: true, d: I.find, title: 'Localizar (Ctrl+F)', onDown: down(() => this.setState({ dlg: 'find' })) },
        { sep: true }, { text: true, label: 'B', title: 'Negrito', font: "700 14px var(--f-sans)", deco: 'none', onDown: down(() => this.exec('bold')) }, { text: true, label: 'I', title: 'Itálico', font: "italic 15px 'Source Serif 4',serif", deco: 'none', onDown: down(() => this.exec('italic')) }, { text: true, label: 'U', title: 'Sublinhado', font: "14px var(--f-sans)", deco: 'underline', onDown: down(() => this.exec('underline')) },
        ...[['#ffffff', 'Cor padrão'], ['#ffd54a', 'Amarelo'], ['#ff6b6b', 'Vermelho'], ['#6ccb5f', 'Verde'], ['#6aa6dc', 'Azul']].map(([c, t]) => ({ swatch: true, color: c, title: t, onDown: down(() => c === '#ffffff' ? this.exec('removeFormat') : this.exec('foreColor', c)) })),
        { sep: true }, { icon: true, d: I.bm, title: 'Inserir marcador (Ctrl+K)', onDown: down(() => this.insertBlock('data-bm')) }, { icon: true, d: I.stopMk, title: 'Inserir ponto de pausa', onDown: down(() => this.insertBlock('data-stop')) }
      ].map(b => ({ sep: !!b.sep, icon: !!b.icon, text: !!b.text, swatch: !!b.swatch, ...b })),
      insertBm: down(() => this.insertBlock('data-bm')),
      toggleLock: this.toggleLock, toggleTalent: this.toggleTalent, togglePlay: this.togglePlay, addWindowOutput: this.addWindowOutput,
      lockIcon: s.locked ? I.lock : I.unlock, lockLabel: s.playing || s.count > 0 ? 'Bloqueado · no ar' : s.locked ? 'Bloqueado' : 'Editável', edLockedNote: s.locked && !s.playing, lockBg: s.locked ? 'var(--t-sunk)' : 'var(--t-soft)',
      promptBg: live ? 'var(--t-accent-soft)' : 'var(--t-accent)', promptFg: live ? 'var(--t-accent)' : 'var(--t-accent-ink)', promptIcon: live ? I.stop : I.play, promptLabel: live ? 'Parar' : 'Prompt',
      gridCols: `${s.showBm ? 'clamp(190px,20%,230px) ' : ''}minmax(0,1fr)${s.showInsp ? ' clamp(272px,30%,320px)' : ''}`, showBm: s.showBm, showInsp: s.showInsp,
      bookmarks: s.bms.map((b, i) => { const bl = s.blocks[i] || { words: 0, skip: false }, est = bl.words / wps, rh = s.reh[i]; return {
        op: bl.skip ? 0.55 : 1, deco: bl.skip ? 'line-through' : 'none', sub: bl.skip ? 'Pulado' : this.fmt(b.y / pxs) + ' · ' + this.fmt(est) + (rh ? ' · E ' + this.fmt(rh) : ''),
        onSkip: e => { e.stopPropagation(); this.toggleSkip(i); }, skipTitle: bl.skip ? 'Voltar o bloco ao roteiro' : 'Pular este bloco (não vai ao ar)', skipColor: bl.skip ? 'var(--t-accent)' : 'var(--t-faint)',
        skipIcon: bl.skip ? 'M2 8s2.2-4.5 6-4.5S14 8 14 8s-2.2 4.5-6 4.5S2 8 2 8z M2.5 2.5l11 11' : 'M2 8s2.2-4.5 6-4.5S14 8 14 8s-2.2 4.5-6 4.5S2 8 2 8z M8 6.3a1.7 1.7 0 1 0 0 3.4a1.7 1.7 0 1 0 0-3.4',
        dropLine: s.dropBm === i && s.dragBm !== i ? 'var(--t-accent)' : 'transparent',
        onDragStart: e => { e.dataTransfer.effectAllowed = 'move'; try { e.dataTransfer.setData('text/plain', String(i)); } catch (er) {} this.setState({ dragBm: i }); },
        onDragOver: e => { if (this.state.dragBm < 0) return; e.preventDefault(); if (this.state.dropBm !== i) this.setState({ dropBm: i }); },
        onDragLeave: () => {}, onDrop: e => { e.preventDefault(); const from = this.state.dragBm; this.setState({ dragBm: -1, dropBm: -1 }); this.moveBlock(from, i); },
        key: i < 9 ? String(i + 1) : '', name: b.name, time: this.fmt(b.y / pxs), bg: i === s.cur ? 'var(--t-accent-soft)' : 'transparent', keyColor: i === s.cur ? 'var(--t-accent)' : 'var(--t-faint)', weight: i === s.cur ? 600 : 500, onJump: () => this.jumpBm(i), onDelete: e => { e.preventDefault(); this.removeBlock('data-bm', i); }, onRemove: e => { e.stopPropagation(); this.removeBlock('data-bm', i); } }; }),
      docTabs: s.docs.map(d => { const on = d.id === s.active; return { name: on ? s.docName : d.name, onClick: () => this.switchTab(d.id), onClose: e => { e.stopPropagation(); this.closeTab(d.id); }, bg: on ? 'var(--raised)' : 'transparent', shadow: on ? 'var(--shadow)' : 'none', color: on ? 'var(--text)' : 'var(--muted)', weight: on ? 600 : 500, dirtyOp: (on ? s.dirty : d.dirty) ? 1 : 0 }; }),
      newTab: () => this.newDoc(), saveLabel: s.savedAt,
      talentHud: s.talentHud && s.talent, hudBlock: s.cur >= 0 && s.bms[s.cur] ? s.bms[s.cur].name : 'Abertura', isLastBlock: s.bms.length > 1 && s.cur === s.bms.length - 1, clockV: s.clock || '--:--:--',
      blockLeft: (() => { const nx = s.bms[s.cur + 1], end = nx ? nx.y : s.t.end; return this.fmt(Math.max(0, (end - s.t.y) / pxs)); })(),
      blockLeftColor: (() => { const nx = s.bms[s.cur + 1], end = nx ? nx.y : s.t.end; return (end - s.t.y) / pxs < 10 ? '#ff6b6b' : '#fff'; })(),
      onBmDragEnd: () => this.setState({ dragBm: -1, dropBm: -1 }),
      profName: s.profName, onProfName: e => this.setState({ profName: e.target.value }), noProfiles: !s.profiles.length,
      saveProfile: () => { const name = s.profName.trim() || ('Perfil ' + (s.profiles.length + 1)); this.setProfiles([{ name, settings: this.profSettings(), t: Date.now() }, ...s.profiles.filter(p => p.name !== name)]); this.setState({ profName: '' }); this.flash('Perfil salvo'); },
      profiles: s.profiles.map((p, i) => ({ name: p.name, meta: ({ atk: 'Atkinson', sans: 'Source Sans', serif: 'Source Serif', mono: 'Plex Mono' }[p.settings.fontKey] || String(p.settings.fontKey).replace(/^sys:/, '')) + ' · ' + p.settings.wpl + ' palavras/linha · margem ' + p.settings.margin, onApply: () => { this.setState({ ...p.settings, dlg: null }); this.flash('Perfil "' + p.name + '" aplicado'); }, onDelete: () => this.setProfiles(s.profiles.filter((_, j) => j !== i)) })),
      noVersions: !s.versions.length, versions: s.versions.map(v => { const d = new Date(v.t); return { name: v.name, meta: d.toLocaleDateString('pt-BR') + ' ' + d.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' }) + ' · ' + v.kind + ' · ' + v.words + ' palavras', onRestore: () => { this.openInTab(v.name.replace(/\.tps$/, '') + ' (restaurado).tps', v.html); this.setState({ dlg: null }); } }; }),
      progInput: s.progInput, onProgInput: e => this.setState({ progInput: e.target.value }), onProgBlur: () => { const m = /^(\d{1,3})(?::(\d{1,2}))?$/.exec(s.progInput.trim()); const sec = m ? (+m[1]) * 60 + (+(m[2] || 0)) : s.progSec; this.setState({ progSec: sec, progInput: this.fmt(sec) }); },
      rundownTotal: this.fmt(estTotal), wordCount: s.words, overLabel: estTotal > s.progSec ? '+' + this.fmt(estTotal - s.progSec) : '−' + this.fmt(s.progSec - estTotal), overTitle: estTotal > s.progSec ? 'Estoura o programa' : 'Sobra no programa', overColor: estTotal > s.progSec ? '#e5484d' : 'var(--t-ok)',
      rehCk: ck(s.rehearse), toggleRehearse: () => this.setState(x => ({ rehearse: !x.rehearse })), hasReh: Object.keys(s.reh).length > 0, clearReh: e => { e.stopPropagation(); this.setState({ reh: {} }); },
      hasStops: s.stops.length > 0, stopsList: s.stops.map((p, i) => ({ name: p.name, time: this.fmt(p.y / pxs), onJump: () => { this.setY(p.y); this.setState({ follow: true }); }, onRemove: e => { e.stopPropagation(); this.removeBlock('data-stop', i); } })),
      wordsLabel: `${s.words} palavras · ${s.t.lines} linhas`,
      edScroll: this.edScroll, edSizer: this.edSizer, edZoom: this.edZoom, edRef: this.edRef, tvRef: this.tvRef, tcRef: this.tcRef, pvRef: this.pvRef, pcRef: this.pcRef, mvRef: this.mvRef, mcRef: this.mcRef, bgFile: this.bgFile, openFile: this.openFile, videoRef: this.videoRef,
      onEdInput: this.onEdInput, onOpenFile: this.onOpenFile,
      onEdScroll: () => { const sc = this.edScroll.current; if (this.state.follow && !this.state.playing && this.state.vMode === 'off' && Math.abs(sc.scrollTop - this.lastSet) > 2) { this.userY = sc.scrollTop / (sc.clientWidth / 1000); this.lastSet = sc.scrollTop; } },
      bg: s.bg, fg: s.fg, fontFam: this.fontInfo(s.fontKey)[0], fontSize: fsz, lineH: s.lineH, lineHLabel: s.lineH.toFixed(2), margin: s.margin, align: s.align,
      edFlip: `scale(${s.edFlipH ? -1 : 1},${s.edFlipV ? -1 : 1})`, edCursor: s.locked ? 'default' : 'text',
      readPct: s.readPct, lineCq: (lh * 0.1).toFixed(2) + 'cqw', halfLineCq: (lh * 0.05).toFixed(2) + 'cqw',
      edBarDisplay: (s.playing || s.count > 0) && !s.talent ? 'flex' : 'none', notFollowing: !s.follow,
      edBarText: 'Editor bloqueado durante a reprodução — pause para editar',
      edBarBtn: 'Pausar e editar', edBarAction: () => { this.pause(); this.setState({ talent: false, locked: false }, () => this.edRef.current.focus({ preventScroll: true })); },
      cursorToPrompter: this.cursorToPrompter, counting: s.count > 0, count: s.count,
      readouts: [
        { label: s.stampMode ? 'Restante' : 'Posição', value: this.fmt(s.stampMode ? total - pos : pos), onClick: () => this.setState(x => ({ stampMode: 1 - x.stampMode })) },
        { label: 'Duração', value: this.fmt(total), onClick: null },
        { label: 'Relógio', value: s.clock || '--:--:--', onClick: null },
        { label: 'Fim previsto', value: s.endAt || '--:--:--', onClick: null },
        { label: 'Linha', value: `${s.t.line} / ${s.t.lines}`, onClick: null }
      ],
      transport: [
        { d: I.rew, title: `Voltar ao início (${this.keyName(s.keymap.rewind)})`, onClick: () => this.setY(0) },
        { d: I.back, title: 'Segure para recuar', onDown: () => this.setState({ hold: -1 }), onUp: () => this.state.hold && this.setState({ hold: 0 }), filled: true },
        { d: live ? I.pause : I.play, title: 'Play / pausa (Espaço)', onClick: this.togglePlay, main: true, filled: true },
        { d: I.fwd, title: 'Segure para avançar', onDown: () => this.setState({ hold: 1 }), onUp: () => this.state.hold && this.setState({ hold: 0 }), filled: true },
        { d: I.dir, title: 'Inverter direção', onClick: () => this.setState(x => ({ dir: -x.dir })), on: s.dir < 0 }
      ].map(b => ({ onClick: null, onDown: null, onUp: null, ...b, w: b.main ? '52px' : '38px', bg: b.main ? (live ? 'var(--t-accent-soft)' : 'var(--t-accent)') : (b.on ? 'var(--t-accent-soft)' : 'var(--t-soft)'), fg: b.main ? (live ? 'var(--t-accent)' : 'var(--t-accent-ink)') : (b.on ? 'var(--t-accent)' : 'var(--t-text)'), border: 'none', fill: b.filled ? 'currentColor' : 'none', stroke: b.filled ? 'none' : 'currentColor' })),
      speedT: s.speedT, onSpeed: e => this.setState({ speedT: +e.target.value, autoRun: false }), speedValue: fu(lps), speedColor: s.dir < 0 ? 'var(--live)' : 'var(--text)',
      unit: String(s.unit), onUnit: e => this.setState({ unit: +e.target.value }), unitName: u[0], minLabel: fu(s.minS), maxLabel: fu(s.maxS), totalLabel: this.fmt(total),
      tabs: [['roteiro', 'Roteiro'], ['overlay', 'Overlay'], ['telas', 'Telas'], ['voz', 'Voz'], ['video', 'Vídeo']].map(([id, label]) => ({ label, ...seg(s.tab === id), onClick: () => this.setState({ tab: id }) })),
      tabRoteiro: s.tab === 'roteiro', tabOverlay: s.tab === 'overlay', tabTelas: s.tab === 'telas', tabVoz: s.tab === 'voz', tabVideo: s.tab === 'video',
      resetLook: () => this.setState({ bg: '#000000', fg: '#ffffff', fontKey: 'atk', align: 'left', edFlipH: false, edFlipV: false }), resetLayout: () => this.setState({ wpl: 5, margin: 0, mPos: -1, lineH: 1.3 }),
      bgSwatches: ['#000000', '#1c1c1c', '#f4f1ea', '#ffffff'].map(v => ({ value: v, ring: s.bg === v ? 'var(--text)' : 'transparent', onClick: () => this.setState({ bg: v, fg: (v === '#f4f1ea' || v === '#ffffff') && s.fg === '#ffffff' ? '#111111' : (v.startsWith('#000') || v === '#1c1c1c') && s.fg === '#111111' ? '#ffffff' : s.fg }) })),
      fgSwatches: ['#ffffff', '#ffe14a', '#d9d9d9', '#111111'].map(v => ({ value: v, ring: s.fg === v ? 'var(--text)' : 'transparent', onClick: () => this.setState({ fg: v }) })),
      bgPickRing: ["#000000","#1c1c1c","#f4f1ea","#ffffff"].includes(String(s.bg).toLowerCase()) ? 'transparent' : 'var(--text)', onBgPick: e => this.setState({ bg: e.target.value }),
      bgHex: s.hexEdit.bg ?? String(s.bg).toUpperCase(), onBgHex: e => this.setState({ hexEdit: { ...s.hexEdit, bg: e.target.value } }), onBgHexBlur: e => { let v = e.target.value.trim(); if (!v.startsWith('#')) v = '#' + v; if (/^#[0-9a-f]{3}$/i.test(v)) v = '#' + v.slice(1).split('').map(c => c + c).join(''); const ok = /^#[0-9a-f]{6}$/i.test(v); this.setState(x => { const h = { ...x.hexEdit }; delete h.bg; return ok ? { bg: v.toLowerCase(), hexEdit: h } : { hexEdit: h }; }); },
      fgPickRing: ["#ffffff","#ffe14a","#d9d9d9","#111111"].includes(String(s.fg).toLowerCase()) ? 'transparent' : 'var(--text)', onFgPick: e => this.setState({ fg: e.target.value }),
      fgHex: s.hexEdit.fg ?? String(s.fg).toUpperCase(), onFgHex: e => this.setState({ hexEdit: { ...s.hexEdit, fg: e.target.value } }), onFgHexBlur: e => { let v = e.target.value.trim(); if (!v.startsWith('#')) v = '#' + v; if (/^#[0-9a-f]{3}$/i.test(v)) v = '#' + v.slice(1).split('').map(c => c + c).join(''); const ok = /^#[0-9a-f]{6}$/i.test(v); this.setState(x => { const h = { ...x.hexEdit }; delete h.fg; return ok ? { fg: v.toLowerCase(), hexEdit: h } : { hexEdit: h }; }); },
      indPickRing: ["#ffffff","#ffd54a","#ff6b6b","#6aa6dc"].includes(String(s.indColor).toLowerCase()) ? 'transparent' : 'var(--text)', onIndPick: e => this.setState({ indColor: e.target.value }),
      indColorHex: s.hexEdit.indColor ?? String(s.indColor).toUpperCase(), onIndHex: e => this.setState({ hexEdit: { ...s.hexEdit, indColor: e.target.value } }), onIndHexBlur: e => { let v = e.target.value.trim(); if (!v.startsWith('#')) v = '#' + v; if (/^#[0-9a-f]{3}$/i.test(v)) v = '#' + v.slice(1).split('').map(c => c + c).join(''); const ok = /^#[0-9a-f]{6}$/i.test(v); this.setState(x => { const h = { ...x.hexEdit }; delete h.indColor; return ok ? { indColor: v.toLowerCase(), hexEdit: h } : { hexEdit: h }; }); },
      presList: s.pres.map((p, i) => ({ n: String(i + 1), name: p.name || ('Apresentador ' + (i + 1)), color: p.color, key: 'Alt+' + (i + 1),
        onColor: e => { const v = e.target.value; this.setState(x => ({ pres: x.pres.map((q, j) => j === i ? { ...q, color: v } : q) })); },
        onName: e => { const v = e.target.value; this.setState(x => ({ pres: x.pres.map((q, j) => j === i ? { ...q, name: v } : q) })); },
        onRemove: () => this.removePres(i) })),
      addPres: () => { if (s.pres.length >= 8) return this.flash('Máximo de 8 apresentadores'); const pal = ['#ffd60a', '#5ac8fa', '#ff9f0a', '#a2e05c', '#ff6b9d', '#bf9bff', '#4cd9c4', '#ff8a65']; this.setState(x => ({ pres: [...x.pres, { name: 'Apresentador ' + (x.pres.length + 1), color: pal[x.pres.length % 8] }] })); },
      presNamesCk: ck(s.presNames), togglePresNames: () => this.setState(x => ({ presNames: !x.presNames })),
      presOnlyOpts: [{ n: '0', name: 'Todas', color: 'transparent' }, ...s.pres.map((p, i) => ({ n: String(i + 1), name: p.name || ('Apresentador ' + (i + 1)), color: p.color }))].map(o => { const on = s.presOnly === o.n; return { ...o, dot: o.n === '0' ? 'none' : 'block', bg: on ? 'var(--inv)' : 'var(--t-soft)', fg: on ? 'var(--invtext)' : 'var(--text)', onClick: () => this.setState({ presOnly: o.n }) }; }),
      presMenu: s.presMenu, togglePresMenu: e => { e.preventDefault(); this.setState(x => ({ presMenu: !x.presMenu })); }, closePresMenu: e => { e.preventDefault(); this.setState({ presMenu: false }); },
      curPresName: s.curWho && s.pres[s.curWho - 1] ? (s.pres[s.curWho - 1].name || 'Apresentador ' + s.curWho) : 'Sem apresentador', curPresColor: s.curWho && s.pres[s.curWho - 1] ? s.pres[s.curWho - 1].color : 'transparent',
      presMenuItems: [...s.pres.map((p, i) => ({ name: p.name || ('Apresentador ' + (i + 1)), color: p.color, key: 'Alt+' + (i + 1), onDown: e => { e.preventDefault(); this.setWho(i + 1); this.setState({ presMenu: false }); } })), { name: 'Sem apresentador', color: 'transparent', key: 'Alt+0', onDown: e => { e.preventDefault(); this.setWho(0); this.setState({ presMenu: false }); } }],
      fontKey: s.fontKey, onFont: e => { const v = e.target.value; if (v === '__sys') return this.loadSysFonts(); this.setState({ fontKey: v }); },
      sysFonts: s.fontKey.startsWith('sys:') && !s.sysFonts.includes(s.fontKey.slice(4)) ? [s.fontKey.slice(4), ...s.sysFonts] : s.sysFonts, sysFontsLabel: s.sysFontsErr || 'Fontes do computador…',
      onFontDown: () => { if (!s.sysFonts.length && !this.sysLoading) { this.sysLoading = true; this.loadSysFonts().finally(() => { this.sysLoading = false; }); } },
      aligns: [['left', 'Esquerda', I.alignL], ['center', 'Centro', I.alignC], ['right', 'Direita', I.alignR]].map(([v, t, d]) => ({ title: t, d, ...seg(s.align === v), onClick: () => this.setState({ align: v, winAlign: v, displays: s.displays.map(x => ({ ...x, align: v })) }) })),
      edFlipChecks: [['edFlipH', 'Horizontal'], ['edFlipV', 'Vertical']].map(([k, l]) => ({ label: l, ...ck(s[k]), onClick: () => this.setState(x => ({ [k]: !x[k] })) })),
      wpl: s.wpl, onWpl: e => this.setState({ wpl: Math.max(2, Math.min(12, +e.target.value || 5)) }), wplLabel: (+s.wpl).toFixed(2).replace(/\.?0+$/, ''), onMargin: e => this.setState({ margin: Math.max(0, Math.min(400, +e.target.value || 0)) }), mPos: s.mPos, onMPos: e => { let v = +e.target.value; if (Math.abs(v) < 0.08) v = 0; this.setState({ mPos: v }); }, resetMPos: () => this.setState({ mPos: -1 }), mPosLabel: s.mPos <= -0.95 ? 'Esq.' : s.mPos >= 0.95 ? 'Dir.' : Math.abs(s.mPos) < 0.05 ? 'Centro' : (s.mPos < 0 ? 'E ' : 'D ') + Math.round(Math.abs(s.mPos) * 100) + '%', onLineH: e => this.setState({ lineH: Math.max(1, Math.min(2, +e.target.value || 1.3)) }),
      minS: s.minS, maxS: s.maxS, onMinS: e => this.setState({ minS: Math.min(+e.target.value, s.maxS - 0.2) }), onMaxS: e => this.setState({ maxS: Math.max(+e.target.value, s.minS + 0.2) }),
      autoRunT: sw(s.autoRun), toggleAutoRun: () => this.setState(x => ({ autoRun: !x.autoRun })), runTarget: s.runTarget, onRunTarget: e => this.setState({ runTarget: e.target.value }),
      ovType: s.ovType, onOvType: e => this.setState({ ovType: e.target.value }), ovOp: s.ovOp, ovOpLabel: Math.round(s.ovOp * 100) + '%', onOvOp: e => this.setState({ ovOp: +e.target.value }),
      ovFaixa: s.ovType === 'faixa', ovFade: s.ovType === 'fade', ovDestaque: s.ovType === 'destaque', dimColor: rgba(s.bg, s.ovOp), hiColor: rgba(s.indColor, s.ovOp * 0.3),
      indType: s.indType, onIndType: e => this.setState({ indType: e.target.value }),
      indSizes: ['P', 'M', 'G'].map((l, i) => ({ label: l, ...seg(s.indSize === i), onClick: () => this.setState({ indSize: i }) })),
      indColors: ['#ffffff', '#ffd54a', '#ff6b6b', '#6aa6dc'].map(v => ({ value: v, ring: s.indColor === v ? 'var(--text)' : 'transparent', onClick: () => this.setState({ indColor: v }) })),
      onRead: e => this.setState({ readPct: Math.max(10, Math.min(70, +e.target.value || 30)) }),
      readingLine: s.readingLine, readingLineCk: ck(s.readingLine), toggleReadingLine: () => this.setState(x => ({ readingLine: !x.readingLine })),
      prMargin: this.prM(fsz), prRight: this.prR(), indW: (+ind[0] * indK).toFixed(2) + 'cqw', indH: (+ind[1] * indK).toFixed(2) + 'cqw', indClip: ind[2], indRadius: ind[3], indLeft: s.indType === 'linha' ? '0' : (indK * 0.4).toFixed(2) + 'cqw',
      indDisplay: s.indType === 'nenhum' ? 'none' : 'block', indColorV: s.indColor,
      hasBgImg: !!s.bgImg, bgImg: s.bgImg || '', bgImgOp: s.bgImgOp, bgImgOpLabel: Math.round(s.bgImgOp * 100) + '%',
      pickBg: () => this.bgFile.current && this.bgFile.current.click(), clearBg: () => this.setState({ bgImg: null }),
      onBgFile: e => { const f = e.target.files[0]; if (!f) return; e.target.value = ''; const isSvg = /\.svg$/i.test(f.name) || f.type === 'image/svg+xml'; const r = new FileReader(); r.onload = () => { const v = isSvg ? 'data:image/svg+xml;charset=utf-8,' + encodeURIComponent(String(r.result)) : r.result; this.setState({ bgImg: v }); }; if (isSvg) r.readAsText(f); else r.readAsDataURL(f); },
      onBgOp: e => this.setState({ bgImgOp: +e.target.value }),
      tvFlip: `scale(${s.winFlipH ? -1 : 1},${s.winFlipV ? -1 : 1})`, tvAlign: s.winAlign, pvFlip: `scale(${td.flipH ? -1 : 1},${td.flipV ? -1 : 1})`, pvAlign: td.align, pvLabel: td.nick + (td.flipH || td.flipV ? ' (espelhada)' : ''), pvBorder: td.find && s.blink ? 'var(--live)' : 'var(--line)',
      displays: s.displays.map(d => ({
        nick: d.nick, desc: `${d.name} · ${d.res}${d.operator ? ' · operador' : ''}`, ck: ck(d.on), rowBg: d.find && s.blink ? 'rgba(229,72,77,.15)' : 'transparent',
        onToggle: () => this.updDisp(d.id, x => ({ on: !x.on })), onNick: e => this.updDisp(d.id, { nick: e.target.value }),
        tools: [tb(d.flipH, I.flipH, 'Espelhar horizontal', () => this.updDisp(d.id, x => ({ flipH: !x.flipH }))), tb(d.flipV, I.flipV, 'Espelhar vertical', () => this.updDisp(d.id, x => ({ flipV: !x.flipV }))), tb(false, d.align === 'center' ? I.alignC : I.alignL, 'Alinhamento nesta tela', () => this.updDisp(d.id, x => ({ align: x.align === 'center' ? 'left' : 'center' }))), tb(d.find, I.target, 'Localizar: pisca uma borda na tela', () => this.updDisp(d.id, x => ({ find: !x.find })))]
      })),
      vCode: V[0], vTitle: s.vErr || V[1], vShort: V[2], vActive: s.vMode !== 'off', vDot: s.vState === 'improviso' ? 'var(--live)' : (s.vMode !== 'off' ? 'var(--text)' : 'var(--faint)'),
      heard: s.heard.map(h => ({ w: h.w, color: h.ok === 2 ? 'var(--text)' : h.ok === 1 ? 'var(--muted)' : 'var(--live)', deco: h.ok ? 'none' : 'line-through' })), noHeard: !s.heard.length,
      vErrOrIdle: s.vErr || 'As palavras reconhecidas aparecem aqui',
      startMic: this.startMic, startSim: this.startSim,
      micLabel: s.vMode === 'mic' ? 'Parar microfone' : 'Usar microfone', micPrimary: s.vMode === 'mic' ? 'var(--live)' : 'var(--inv)', micFg: s.vMode === 'mic' ? '#fff' : 'var(--invtext)',
      simLabel: s.vMode === 'sim' ? 'Parar simulação' : 'Simular leitura', simBg: s.vMode === 'sim' ? 'var(--sel)' : 'var(--raised)',
      drivers: [['wasapi', 'WASAPI'], ['asio', 'ASIO']].map(([v, l]) => ({ label: l, ...seg(s.driver === v), onClick: () => this.setState({ driver: v }) })), isAsio: s.driver === 'asio',
      // ASIO não é acessível pelo Chromium; exige módulo nativo. WASAPI = dispositivos reais do sistema.
      devices: s.driver === 'asio' ? ['ASIO requer módulo nativo (não incluído)'] : (s.audioDevs.length ? s.audioDevs : ['Microfone padrão do sistema']),
      channels: Array.from({ length: 32 }, (_, i) => ({ v: String(i + 1), l: `Canal ${i + 1}` })), asioCh: String(s.asioCh), onAsioCh: e => this.setState({ asioCh: +e.target.value }),
      levelPct: Math.round((s.level + 70) / 70 * 100), gatePct: Math.round((s.gate + 70) / 70 * 100), levelColor: s.level > s.gate ? 'var(--text)' : 'var(--faint)',
      gate: s.gate, onGate: e => this.setState({ gate: +e.target.value }), win: s.win, onWin: e => this.setState({ win: +e.target.value }), tol: s.tol, onTol: e => this.setState({ tol: +e.target.value }),
      vLang: s.vLang, onVLang: e => this.setState({ vLang: e.target.value }),
      camVis: s.camOn ? 'visible' : 'hidden', camOff: !s.camOn, camMsg: s.camMsg, camBtn: s.camOn ? 'Desligar câmera' : 'Ligar câmera', toggleCam: this.toggleCam,
      recording: s.recording, recLabel: this.fmt(s.recSec), recBtn: s.recording ? 'Parar gravação' : 'Gravar', recBg: s.recording ? 'var(--live)' : 'var(--inv)', recFg: s.recording ? '#fff' : 'var(--invtext)',
      toggleRec: () => { if (!this.state.camOn) return this.setState({ camMsg: 'Ligue a câmera primeiro' }); this.state.recording ? this.stopRec() : this.startRec(); },
      recWithCk: ck(s.recWithPrompt), toggleRecWithPrompt: () => this.setState(x => ({ recWithPrompt: !x.recWithPrompt })),
      hasRecUrl: !!s.recUrl, recUrl: s.recUrl, recFile: s.docName.replace(/\.\w+$/, '') + '.webm', lastRecLabel: this.fmt(s.lastRec),
      statusDot: live ? 'var(--live)' : (s.vMode !== 'off' ? 'var(--text)' : 'var(--faint)'), statusLabel: live ? 'No ar' : (s.vMode !== 'off' ? 'Acompanhando por voz' : 'Pronto'),
      screensLabel: `${s.displays.filter(d => d.on).length} tela(s) de prompter`, lastKeyLabel: s.lastKey ? `Última entrada: ${s.lastKey}` : '',
      talentVis: s.talent ? 'visible' : 'hidden', blinkOp: td.find && s.blink ? 1 : 0, tvTitle: s.winFlipH || s.winFlipV ? 'Janela do operador · espelhada' : 'Janela do operador · leitura normal',
      onTvMove: e => { const show = e.clientY < 70; if (show !== this.state.tbShow) this.setState({ tbShow: show }); },
      tbOp: s.tbShow || !s.playing ? 1 : 0, tbPe: s.tbShow || !s.playing ? 'auto' : 'none', tvCursor: s.playing && !s.tbShow ? 'none' : 'default',
      tvTools: [
        tb(s.ctrlOn, I.controls, 'Mostrar controles', () => this.setState(x => ({ ctrlOn: !x.ctrlOn }))), tb(s.stampOn, I.clock, 'Mostrar tempo', () => this.setState(x => ({ stampOn: !x.stampOn }))),
        tb(s.winFlipH, I.flipH, 'Espelhar esta janela (horizontal)', () => this.setState(x => ({ winFlipH: !x.winFlipH }))), tb(s.winFlipV, I.flipV, 'Espelhar esta janela (vertical)', () => this.setState(x => ({ winFlipV: !x.winFlipV }))),
        tb(s.speedLock, I.lock, 'Travar velocidade', () => this.setState(x => ({ speedLock: !x.speedLock }))), tb(false, s.winAlign === 'center' ? I.alignC : I.alignL, 'Alinhamento desta janela', () => this.setState(x => ({ winAlign: x.winAlign === 'center' ? 'left' : 'center' }))),
        tb(false, I.fs, 'Tela cheia', () => { const el = this.tvRef.current && this.tvRef.current.parentNode; if (el && el.requestFullscreen) el.requestFullscreen().catch(() => {}); })
      ],
      ctrlOn: s.ctrlOn, stampOn: s.stampOn, ctrlOp: s.ctrlOp, onCtrlOp: e => this.setState({ ctrlOp: +e.target.value }),
      editHere: this.editHere, startInline: this.startInline, finishInline: this.finishInline, inline: s.inline,
      onTvWheel: e => this.onWheelCtl(e, 'tv'), onEdWheel: e => this.onWheelCtl(e, 'ed'), onTvDown: e => this.onPtrDown(e), onTvPMove: e => this.onPtrMove(e), onTvUp: () => this.onPtrUp(), flashText: s.flashText, hasFlash: !!s.flashText, hasFlashSub: !!s.flashSub, flashSub: s.flashSub, flashSubBg: s.flashSub === 'Ré' ? '#e5484d' : 'rgba(255,255,255,.16)', flashArrow: s.flashSub === 'Ré' ? 'M10 3L4 8l6 5' : 'M6 3l6 5-6 5', flashBar: s.flashPct >= 0, flashW: Math.round(Math.max(0, s.flashPct) * 100) + '%',
      paraPrev: () => this.jumpPara(-1), paraNext: () => this.jumpPara(1), lineUp: () => this.setY(this.y - lh), lineDown: () => this.setY(this.y + lh), rewind: () => this.setY(0),
      playIcon: live ? I.pause : I.play, playBg: live ? 'var(--live)' : '#e6e6e6', playFg: live ? '#fff' : '#1e1e1e',
      onSpeedTv: e => { if (!this.state.speedLock) this.setState({ speedT: +e.target.value, autoRun: false }); }, speedLock: s.speedLock,
      stampValue: (s.stampMode ? '−' : '') + this.fmt(s.stampMode ? total - pos : pos), toggleStamp: () => this.setState(x => ({ stampMode: 1 - x.stampMode })),
      dlgOpen: !!s.dlg, dlgTitle: dlgMeta[0], dlgW: dlgMeta[1], closeDlg: () => this.setState({ dlg: null, capture: null }),
      dlgPrefs: s.dlg === 'prefs', dlgFind: s.dlg === 'find', dlgRemote: s.dlg === 'remote', dlgAbout: s.dlg === 'about', dlgNovo: s.dlg === 'novo', dlgVersoes: s.dlg === 'versoes', dlgPerfis: s.dlg === 'perfis', dlgMonitor: s.dlg === 'monitor',
      prefTabs: [['geral', 'Geral'], ['atalhos', 'Atalhos'], ['disp', 'Dispositivos'], ['integ', 'Integrações']].map(([id, l]) => ({ label: l, bg: s.prefTab === id ? 'var(--t-accent-soft)' : 'transparent', weight: s.prefTab === id ? 600 : 400, onClick: () => this.setState({ prefTab: id, capture: null }) })),
      prefGeral: s.prefTab === 'geral', prefAtalhos: s.prefTab === 'atalhos', prefDisp: s.prefTab === 'disp', prefInteg: s.prefTab === 'integ',
      themes: [['dark', 'Escuro'], ['light', 'Claro']].map(([v, l]) => ({ label: l, ...seg(s.theme === v), onClick: () => this.setState({ theme: v }) })),
      prefChecks: [['lockOnPlay', 'Bloquear o editor ao iniciar o prompt'], ['countdownPref', 'Contagem regressiva de 3 s antes de rolar'], ['recWithPrompt', 'Gravar vídeo junto com o prompt']].map(([k, l]) => ({ label: l, ...ck(s[k]), onClick: () => this.setState(x => ({ [k]: !x[k] })) })),
      shortcutHint: 'Shift + tecla = passo maior · 1–9 = velocidade predefinida · Backspace = início · Roda do mouse: rola (pausado) ou ajusta a velocidade (rodando) · Ctrl + roda = palavras por linha · Arrastar na tela do talento = rolar', shortcuts: ACTIONS.map(([a, l]) => ({ label: l, key: s.capture === a ? 'Pressione uma tecla…' : this.keyName(s.keymap[a]), bg: s.capture === a ? 'var(--sel)' : 'var(--field)', onClick: () => this.setState({ capture: a }) })),
      resetKeys: () => this.setState({ keymap: { ...KEYMAP }, capture: null }),
      devicesList: [['Teclado', 'Conectado'], ['Mouse e roda de rolagem', 'Conectado'], ['Clicker de apresentação', 'Usa Page Up / Page Down'], ['Controle pelo navegador (monitor web)', s.webOn ? (s.webCtrl ? 'Ativo · ' + s.webViewers + ' conectado(s)' : 'Somente visualização') : 'Desligado']].map(([n, st]) => ({ name: n, state: st })),
      lastKeyBig: s.lastKey || '—',
      findQ: s.findQ, replQ: s.replQ, findInfo: s.findInfo, onFindQ: e => { this.fi = -1; this.setState({ findQ: e.target.value, findInfo: '' }); }, onReplQ: e => this.setState({ replQ: e.target.value }),
      findNext: () => this.findNext(), replaceAll: () => this.replaceAll(), onFindKey: e => { if (e.key === 'Enter') { e.preventDefault(); this.findNext(); } },
      remoteCur: (s.bms[s.cur] || {}).name || '—', remoteSpeed: `${fu(lps)} ${u[0]}`,
      rFaster: () => this.adjSpeed(0.05), rSlower: () => this.adjSpeed(-0.05), rNext: () => this.doAction('nextBm'), rPrev: () => this.doAction('prevBm'),
      confirmNew: () => this.newDoc(),
      ndiT: sw(s.ndiOn), toggleNdi: () => this.toggleNdi(), ndiDot: 'var(--t-faint)',
      ndiStatus: isDesktop ? 'Indisponível · requer NDI Runtime + módulo nativo (próxima etapa)' : 'Disponível apenas no app para Windows', tallyOn: false,
      ndiName: s.ndiName, onNdiName: e => this.setState({ ndiName: e.target.value }), ndiFmt: s.ndiFmt, onNdiFmt: e => this.setState({ ndiFmt: e.target.value }),
      ndiChecks: [['ndiMirror', 'Espelhar a saída NDI'], ['ndiAlpha', 'Fundo transparente (alpha) para sobrepor no vMix']].map(([k, l]) => ({ label: l, ...ck(s[k]), onClick: () => this.setState(x => ({ [k]: !x[k] })) })),
      webT: sw(s.webOn), toggleWeb: () => this.toggleWeb(), webDot: s.webOn ? 'var(--t-ok)' : s.webBusy ? 'var(--t-accent)' : s.webErr ? '#e5484d' : 'var(--t-faint)',
      webStatus: !platform.monitor ? 'Disponível apenas no app para Windows' : s.webBusy ? 'Iniciando servidor…' : s.webErr ? 'Erro · ' + s.webErr : !s.webOn ? 'Desligado' : `Ativo · ${s.webViewers} navegador(es) conectado(s)`,
      webUrl: s.webUrl || (platform.monitor ? `http://<ip-deste-pc>:${WEB_PORT}/monitor` : 'Indisponível na versão web'),
      copyUrl: () => { if (!s.webUrl) return this.flash('Ligue o monitor web primeiro'); try { navigator.clipboard.writeText(s.webUrl); } catch (e) {} this.setState({ copied: true }); setTimeout(() => this.setState({ copied: false }), 1500); }, copyLabel: s.copied ? 'Copiado' : 'Copiar',
      webPass: s.webPass, onWebPass: e => this.setState({ webPass: e.target.value }),
      webChecks: [['webCtrl', 'Permitir controlar o prompter pelo navegador']].map(([k, l]) => ({ label: l, ...ck(s[k]), onClick: () => this.setState(x => ({ [k]: !x[k] })) })),
      openMonitor: () => { if (platform.monitor && !this.state.webOn) this.toggleWeb(true); this.setState({ dlg: 'monitor' }); }, webCtrl: s.webCtrl, webViewOnly: !s.webCtrl,
      openMonitorExt: () => { if (s.webUrl) platform.openExternal(s.webUrl); },
      remoteInfo: s.webOn ? (s.webCtrl ? 'Celular/tablet: abra ' + s.webUrl : 'Ative “Permitir controlar pelo navegador” para usar o celular') : (platform.monitor ? 'Ligue o Monitor web para controlar pelo celular' : 'Controle pelo celular disponível no app para Windows'),
      monTag: live ? 'NO AR' : 'AO VIVO · MONITOR', monPlayLabel: live ? 'Pausar' : 'Play',
      mosSys: s.mosSys, onMosSys: e => this.setState({ mosSys: e.target.value }), mosHost: s.mosHost, onMosHost: e => this.setState({ mosHost: e.target.value }), mosId: s.mosId, onMosId: e => this.setState({ mosId: e.target.value }),
      // Só a conexão TCP (porta 10540) é real; o protocolo MOS (roMetadata/roCreate) ainda não é processado.
      mosDot: s.mosOn ? 'var(--t-ok)' : s.mosBusy ? 'var(--t-accent)' : s.mosErr ? '#e5484d' : 'var(--t-faint)', mosStatus: s.mosOn ? `Conectado ao ${s.mosSys} (TCP 10540) · leitura do rundown MOS em desenvolvimento` : s.mosBusy ? 'Conectando…' : s.mosErr || 'Desconectado', mosBtn: s.mosOn ? 'Desconectar' : 'Conectar',
      toggleMos: () => this.toggleMos(),
      integChecks: [['ccOut', 'Enviar o texto lido como legenda (closed caption)']].map(([k, l]) => ({ label: l, ...ck(s[k]), onClick: () => this.setState(x => ({ [k]: !x[k] })) })),
      netLabel: [s.ndiOn ? 'NDI ●' : '', s.webOn ? `Web ${s.webViewers}` : '', s.mosOn ? 'MOS ●' : ''].filter(Boolean).join(' · ')
    };
  }
}
