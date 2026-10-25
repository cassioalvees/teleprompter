// Modo aplicativo: janela do editor, janelas de prompter por monitor, arquivos, impressão,
// monitor web e conexão MOS.
import { app, BrowserWindow, dialog, ipcMain, screen, session, shell, type Display } from 'electron';
import { readFileSync, writeFileSync, existsSync } from 'node:fs';
import { basename, join } from 'node:path';
import { Socket } from 'node:net';
import { readAll, write } from './store';
import { MonitorServer, lanAddresses } from './server';
import { appIcon, distDir, loadPage, preloadPath } from './paths';

let main: BrowserWindow | null = null;
const outputs = new Map<string, BrowserWindow>();
// Últimas mensagens do barramento: repassadas a saídas e monitores que se conectam depois.
const last: { html?: any; state?: any; y?: any } = {};
let monitor: MonitorServer | null = null;
let mos: Socket | null = null;
let pendingOpen: string[] = [];
let rendererReady = false;

const fileArg = (argv: string[]) => argv.slice(1).filter(a => !a.startsWith('-') && /\.(tps|json|txt|docx|pdf|html?)$/i.test(a) && existsSync(a));

function displayList() {
  const prim = screen.getPrimaryDisplay();
  return screen.getAllDisplays().map((d: Display, i) => ({
    id: String(d.id),
    name: (d as any).label || `Monitor ${i + 1}`,
    res: `${Math.round(d.size.width * d.scaleFactor)} × ${Math.round(d.size.height * d.scaleFactor)}`,
    operator: d.id === prim.id
  }));
}

function openOutput(d: { id: string; nick?: string; windowed?: boolean }) {
  const ex = outputs.get(d.id);
  if (ex && !ex.isDestroyed()) { ex.show(); ex.focus(); return; }
  const disp = screen.getAllDisplays().find(x => String(x.id) === d.id);
  const b = disp ? disp.bounds : { x: undefined as any, y: undefined as any, width: 960, height: 540 };
  const win = new BrowserWindow({
    x: b.x, y: b.y, width: b.width, height: b.height,
    frame: !disp, fullscreen: !!disp, autoHideMenuBar: true, backgroundColor: '#000000',
    title: 'ARCA Teleprompter · ' + (d.nick || 'Saída'), icon: appIcon(),
    webPreferences: { preload: preloadPath(), backgroundThrottling: false }
  });
  win.setMenu(null);
  // Garante a tela certa mesmo quando o Windows reposiciona a janela antes do fullscreen.
  if (disp) win.once('ready-to-show', () => { win.setBounds(disp.bounds); win.setFullScreen(true); });
  loadPage(win, '#/output?d=' + encodeURIComponent(d.id));
  outputs.set(d.id, win);
  win.on('closed', () => { outputs.delete(d.id); main?.webContents.send('output:closed', d.id); });
}

function sendToOutputs(msg: any) {
  for (const w of outputs.values()) if (!w.isDestroyed()) w.webContents.send('bus', msg);
  monitor?.broadcast(msg);
}

function openFile(p: string) {
  try { const data = readFileSync(p); main?.webContents.send('file:opened', { name: basename(p), path: p, data }); } catch (e) { /* ignora */ }
}

export function startApp() {
  if (!app.requestSingleInstanceLock()) { app.quit(); return; }
  pendingOpen = fileArg(process.argv);
  app.on('second-instance', (_e, argv) => {
    if (argv.includes('--quit-for-uninstall')) { app.quit(); return; }
    const files = fileArg(argv);
    if (main) { if (main.isMinimized()) main.restore(); main.focus(); }
    files.forEach(f => (rendererReady ? openFile(f) : pendingOpen.push(f)));
  });

  app.whenReady().then(() => {
    // Câmera, microfone e fontes locais: o app só carrega as próprias páginas.
    session.defaultSession.setPermissionRequestHandler((_wc, _perm, cb) => cb(true));
    session.defaultSession.setPermissionCheckHandler(() => true);

    main = new BrowserWindow({
      width: 1440, height: 900, minWidth: 1024, minHeight: 640, frame: false, show: false,
      backgroundColor: '#f2f2f0', title: 'ARCA Teleprompter', icon: appIcon(),
      webPreferences: { preload: preloadPath(), backgroundThrottling: false, spellcheck: false }
    });
    main.setMenu(null);
    main.once('ready-to-show', () => { main!.maximize(); main!.show(); });
    loadPage(main, '');
    // Links externos (ex.: URL do monitor) abrem no navegador padrão; window.open interno vira janela.
    main.webContents.setWindowOpenHandler(({ url }) => {
      if (/^https?:/.test(url) && !url.startsWith('http://localhost:5173')) { shell.openExternal(url); return { action: 'deny' }; }
      return { action: 'allow', overrideBrowserWindowOptions: { autoHideMenuBar: true, icon: appIcon() } };
    });
    // Fechar o editor fecha as saídas e o servidor.
    main.on('closed', () => { main = null; for (const w of outputs.values()) if (!w.isDestroyed()) w.close(); monitor?.stop(); mos?.destroy(); app.quit(); });

    screen.on('display-added', () => main?.webContents.send('displays:changed', displayList()));
    screen.on('display-removed', (_e, d) => { const w = outputs.get(String(d.id)); if (w && !w.isDestroyed()) w.close(); main?.webContents.send('displays:changed', displayList()); });
  });
  app.on('window-all-closed', () => app.quit());

  ipcMain.on('store:getAll', e => { e.returnValue = readAll(); });
  ipcMain.on('store:set', (_e, k: string, v: string) => { try { write(k, v); } catch (err) { console.error('store', err); } });

  ipcMain.on('win:minimize', e => BrowserWindow.fromWebContents(e.sender)?.minimize());
  ipcMain.on('win:maximize', e => { const w = BrowserWindow.fromWebContents(e.sender); if (w) w.isMaximized() ? w.unmaximize() : w.maximize(); });
  ipcMain.on('win:close', e => BrowserWindow.fromWebContents(e.sender)?.close());
  ipcMain.on('shell:open', (_e, url: string) => { if (/^https?:\/\//.test(url)) shell.openExternal(url); });

  ipcMain.handle('displays:list', () => displayList());
  ipcMain.on('output:open', (_e, d) => openOutput(d));
  ipcMain.on('output:close', (_e, id: string) => { const w = outputs.get(id); if (w && !w.isDestroyed()) w.close(); });

  ipcMain.on('bus:publish', (_e, msg) => { if (msg.t === 'html') last.html = msg; else if (msg.t === 'state') last.state = msg; else if (msg.t === 'y') last.y = msg; sendToOutputs(msg); });
  ipcMain.on('bus:cmd', (e, cmd) => {
    if (cmd && cmd.t === 'hello') { const w = e.sender; [last.html, last.state, last.y].forEach(m => m && w.send('bus', m)); return; }
    main?.webContents.send('bus:cmd', cmd);
  });

  ipcMain.on('file:ready', () => { rendererReady = true; pendingOpen.forEach(openFile); pendingOpen = []; });
  ipcMain.handle('file:open', async () => {
    const r = await dialog.showOpenDialog(main!, { title: 'Abrir roteiro', properties: ['openFile'], filters: [{ name: 'Roteiros e documentos', extensions: ['tps', 'json', 'txt', 'docx', 'pdf', 'html', 'htm'] }, { name: 'Todos os arquivos', extensions: ['*'] }] });
    if (r.canceled || !r.filePaths[0]) return null;
    const p = r.filePaths[0]; return { name: basename(p), path: p, data: readFileSync(p) };
  });
  ipcMain.handle('file:save', async (_e, o: { name: string; data: string; mime: string; path?: string; saveAs?: boolean }) => {
    let p = o.path && !o.saveAs && o.name.endsWith('.tps') ? o.path : '';
    if (!p) {
      const ext = (o.name.split('.').pop() || 'tps').toLowerCase();
      const r = await dialog.showSaveDialog(main!, { title: 'Salvar', defaultPath: join(app.getPath('documents'), o.name), filters: [{ name: ext === 'tps' ? 'Roteiro ARCA (.tps)' : ext.toUpperCase(), extensions: [ext] }] });
      if (r.canceled || !r.filePath) return null; p = r.filePath;
    }
    writeFileSync(p, o.data, 'utf8'); app.addRecentDocument(p);
    return { name: basename(p), path: p };
  });

  ipcMain.handle('print:html', async (_e, o: { html: string; mode: 'print' | 'pdf'; name: string }) => {
    const w = new BrowserWindow({ show: false, webPreferences: { offscreen: false } });
    await w.loadURL('data:text/html;charset=utf-8,' + encodeURIComponent(o.html));
    try {
      if (o.mode === 'pdf') {
        const r = await dialog.showSaveDialog(main!, { title: 'Exportar PDF', defaultPath: join(app.getPath('documents'), o.name + '.pdf'), filters: [{ name: 'PDF', extensions: ['pdf'] }] });
        if (r.canceled || !r.filePath) return false;
        writeFileSync(r.filePath, await w.webContents.printToPDF({ pageSize: 'A4', printBackground: true, margins: { marginType: 'default' } }));
        shell.openPath(r.filePath); return true;
      }
      return await new Promise<boolean>(res => w.webContents.print({ printBackground: true }, ok => res(ok)));
    } finally { w.destroy(); }
  });

  ipcMain.handle('monitor:start', async (_e, o: { port: number; pass: string; ctrl: boolean }) => {
    if (!monitor) monitor = new MonitorServer(distDir(), () => [last.html, last.state, last.y].filter(Boolean), cmd => main?.webContents.send('bus:cmd', cmd), n => main?.webContents.send('monitor:viewers', n));
    monitor.pass = o.pass; monitor.ctrl = o.ctrl;
    try { await monitor.start(o.port); } catch (err: any) {
      monitor = null;
      return { ok: false, error: err && err.code === 'EADDRINUSE' ? `porta ${o.port} em uso` : String(err && err.message || err) };
    }
    const ips = lanAddresses();
    return { ok: true, url: `http://${ips[0] || 'localhost'}:${o.port}/monitor`, urls: ips.map(ip => `http://${ip}:${o.port}/monitor`) };
  });
  ipcMain.handle('monitor:stop', async () => { await monitor?.stop(); monitor = null; });
  ipcMain.on('monitor:update', (_e, o) => { if (!monitor) return; if ('pass' in o) monitor.setPass(o.pass); if ('ctrl' in o) monitor.ctrl = o.ctrl; });

  // MOS: só a conexão TCP ao servidor da redação. O protocolo (XML MOS 2.8) é a próxima etapa.
  ipcMain.handle('mos:connect', (_e, o: { host: string; port: number }) => new Promise(res => {
    mos?.destroy();
    const s = new Socket(); let done = false;
    const fin = (r: any) => { if (!done) { done = true; res(r); } };
    s.setTimeout(4000, () => { s.destroy(); fin({ ok: false, error: 'Tempo esgotado ao conectar em ' + o.host }); });
    s.once('error', (err: any) => fin({ ok: false, error: err.code === 'ECONNREFUSED' ? 'Conexão recusada por ' + o.host + ':' + o.port : String(err.message) }));
    s.connect(o.port, o.host, () => { s.setTimeout(0); mos = s; fin({ ok: true }); });
    s.on('close', () => { if (mos === s) { mos = null; } });
  }));
  ipcMain.on('mos:disconnect', () => { mos?.destroy(); mos = null; });
}
