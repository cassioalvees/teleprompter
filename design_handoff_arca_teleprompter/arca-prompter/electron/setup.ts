// Instalador e desinstalador (telas em src/setup, desenho de Instalador.dc.html).
//
// O Setup.exe é o próprio app empacotado como "portable" do electron-builder: ao rodar, ele se
// extrai numa pasta temporária. Instalar = copiar essa pasta para o destino, criar atalhos,
// associar .tps e registrar o desinstalador em "Aplicativos instalados" (Registro do Windows).
// O desinstalador é o app instalado chamado com --uninstall.
import { app, BrowserWindow, dialog, ipcMain, shell } from 'electron';
import { execFile, execFileSync, spawn } from 'node:child_process';
import { cpSync, existsSync, mkdirSync, readdirSync, readFileSync, rmSync, statSync, statfsSync, writeFileSync, copyFileSync } from 'node:fs';
import { basename, dirname, join, parse } from 'node:path';
import { tmpdir } from 'node:os';
import { appIcon, loadPage, preloadPath } from './paths';
import { dataDir } from './store';
import { SAMPLES } from './samples';

const PRODUCT = 'ARCA Teleprompter';
const EXE = PRODUCT + '.exe';
const REG_KEY = 'Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\ARCATeleprompter';
const PROG_ID = 'ARCA.Script';
const FW_RULE = 'ARCA Teleprompter - Monitor web';

type Scope = 'all' | 'me';
interface InstallOpts { path: string; scope: Scope; comp: Record<string, boolean>; desk: boolean; startMenu: boolean; assoc: boolean }
interface Manifest { version: string; path: string; scope: Scope; shortcuts: string[]; assoc: boolean; firewall: boolean; samples?: string; installedAt: string }

let win: BrowserWindow | null = null;
let cancelFlag = false;
let lastInstall: InstallOpts | null = null;

const sh = (cmd: string, args: string[]) => new Promise<{ ok: boolean; out: string }>(res => execFile(cmd, args, { windowsHide: true }, (err, out, errOut) => res({ ok: !err, out: String(out || '') + String(errOut || '') })));
const hive = (scope: Scope) => (scope === 'all' ? 'HKLM' : 'HKCU');
const regAdd = (key: string, name: string | null, value: string, type = 'REG_SZ') => sh('reg', ['add', key, ...(name == null ? ['/ve'] : ['/v', name]), '/t', type, '/d', value, '/f']);
const regDel = (key: string) => sh('reg', ['delete', key, '/f']);

function isAdmin() { try { execFileSync('net', ['session'], { stdio: 'ignore', windowsHide: true }); return true; } catch (e) { return false; } }
function dirSize(p: string): number {
  let n = 0; if (!existsSync(p)) return 0;
  for (const e of readdirSync(p, { withFileTypes: true })) { const f = join(p, e.name); try { n += e.isDirectory() ? dirSize(f) : statSync(f).size; } catch (err) { /* em uso */ } }
  return n;
}
const mb = (b: number) => (b < 1 << 20 ? '<1 MB' : Math.round(b / (1 << 20)) + ' MB');
function listFiles(root: string, rel = ''): string[] {
  const out: string[] = [];
  for (const e of readdirSync(join(root, rel), { withFileTypes: true })) { const r = rel ? join(rel, e.name) : e.name; if (e.isDirectory()) out.push(...listFiles(root, r)); else out.push(r); }
  return out;
}
const programs = (scope: Scope) => scope === 'all' ? join(process.env.ProgramData || 'C:\\ProgramData', 'Microsoft\\Windows\\Start Menu\\Programs') : join(app.getPath('appData'), 'Microsoft\\Windows\\Start Menu\\Programs');
const desktop = (scope: Scope) => scope === 'all' ? join(process.env.PUBLIC || 'C:\\Users\\Public', 'Desktop') : app.getPath('desktop');
const defaultPaths = () => ({ all: join(process.env.ProgramFiles || 'C:\\Program Files', PRODUCT), me: join(process.env.LOCALAPPDATA || app.getPath('appData'), 'Programs', PRODUCT) });
/** Pasta com os arquivos do app a copiar (a extração do portable, ou a pasta do executável). */
const sourceDir = () => dirname(process.execPath);

function installedManifest(): Manifest | null {
  // 1) ao lado do executável (desinstalador rodando da pasta instalada ou da cópia temporária via --target)
  const target = process.argv.find(a => a.startsWith('--target='))?.slice(9);
  for (const dir of [target, sourceDir()].filter(Boolean) as string[]) {
    const f = join(dir, 'install.json'); if (existsSync(f)) try { return JSON.parse(readFileSync(f, 'utf8')); } catch (e) { /* */ }
  }
  // 2) pelo Registro (instalação anterior, para o Setup avisar/reinstalar)
  for (const h of ['HKCU', 'HKLM']) {
    try {
      const out = execFileSync('reg', ['query', h + '\\' + REG_KEY, '/v', 'InstallLocation'], { windowsHide: true }).toString();
      const m = /InstallLocation\s+REG_SZ\s+(.+)/.exec(out); const f = m && join(m[1].trim(), 'install.json');
      if (f && existsSync(f)) return JSON.parse(readFileSync(f, 'utf8'));
    } catch (e) { /* não instalado nesse escopo */ }
  }
  return null;
}

function progress(p: any) { win?.webContents.send('setup:progress', p); }

async function install(o: InstallOpts) {
  cancelFlag = false; lastInstall = o;
  const target = o.path.replace(/[\\/]+$/, ''), exe = join(target, EXE), undo: (() => Promise<any> | void)[] = [];
  const createdDir = !existsSync(target);
  const step = (task: string, file = '', pct?: number) => progress({ task, file, pct });
  const ok = (log: string, warn = false) => progress({ log, warn });
  const check = () => { if (cancelFlag) throw new Error('__cancel'); };
  try {
    step('Preparando a instalação', 'Verificando espaço em disco e permissões', 1);
    const files = listFiles(sourceDir()), total = files.reduce((a, f) => a + statSync(join(sourceDir(), f)).size, 0);
    try { const fs = statfsSync(parse(target).root); if (fs.bavail * fs.bsize < total * 1.1) throw new Error(`Espaço insuficiente em ${parse(target).root}`); } catch (e: any) { if (/insuficiente/.test(e.message)) throw e; }
    mkdirSync(target, { recursive: true });
    if (createdDir) undo.push(() => rmSync(target, { recursive: true, force: true }));
    writeFileSync(join(target, '.write-test'), ''); rmSync(join(target, '.write-test'));
    ok('Preparando a instalação');

    // Cópia arquivo a arquivo, com progresso de 3% a 80%.
    let done = 0;
    for (const f of files) {
      check();
      const dst = join(target, f); mkdirSync(dirname(dst), { recursive: true });
      copyFileSync(join(sourceDir(), f), dst);
      done += statSync(dst).size;
      step('Copiando o aplicativo', dst, 3 + 77 * (done / total));
      if (files.length < 400 || Math.random() < 0.2) await new Promise(r => setImmediate(r));
    }
    ok('Copiando o aplicativo');
    check();

    const man: Manifest = { version: app.getVersion(), path: target, scope: o.scope, shortcuts: [], assoc: false, firewall: false, installedAt: new Date().toISOString() };

    if (o.comp.web) {
      step('Configurando o monitor web', 'Regra de firewall: porta 8420 (rede privada)', 82);
      if (isAdmin()) {
        await sh('netsh', ['advfirewall', 'firewall', 'delete', 'rule', `name=${FW_RULE}`]);
        const r = await sh('netsh', ['advfirewall', 'firewall', 'add', 'rule', `name=${FW_RULE}`, 'dir=in', 'action=allow', 'protocol=TCP', 'localport=8420', 'profile=private', `program=${exe}`]);
        man.firewall = r.ok; ok(r.ok ? 'Configurando o monitor web' : 'Regra de firewall não criada (o Windows perguntará no primeiro uso)', !r.ok);
        if (r.ok) undo.push(() => sh('netsh', ['advfirewall', 'firewall', 'delete', 'rule', `name=${FW_RULE}`]));
      } else ok('Monitor web: o Windows pedirá permissão de rede no primeiro uso', true);
    }
    check();

    if (o.comp.samples) {
      step('Copiando roteiros de exemplo', '', 85);
      const dir = join(app.getPath('documents'), PRODUCT, 'Exemplos'); mkdirSync(dir, { recursive: true });
      for (const [name, data] of Object.entries(SAMPLES)) { const p = join(dir, name); if (!existsSync(p)) writeFileSync(p, JSON.stringify(data), 'utf8'); }
      man.samples = dir; ok('Copiando roteiros de exemplo');
    }

    if (o.assoc) {
      const base = `${hive(o.scope)}\\Software\\Classes`;
      step('Associando arquivos .tps', `${base}\\.tps → ${PROG_ID}`, 88);
      await regAdd(`${base}\\.tps`, null, PROG_ID);
      await regAdd(`${base}\\${PROG_ID}`, null, 'Roteiro ARCA Teleprompter');
      await regAdd(`${base}\\${PROG_ID}\\DefaultIcon`, null, `"${exe}",0`);
      await regAdd(`${base}\\${PROG_ID}\\shell\\open\\command`, null, `"${exe}" "%1"`);
      undo.push(() => regDel(`${base}\\${PROG_ID}`)); undo.push(() => regDel(`${base}\\.tps`));
      man.assoc = true; ok('Associando arquivos .tps');
    }
    check();

    if (o.desk || o.startMenu) {
      step('Criando atalhos', [o.startMenu ? 'Menu Iniciar' : '', o.desk ? 'Área de trabalho' : ''].filter(Boolean).join(' · '), 92);
      const opts = { target: exe, cwd: target, description: 'Teleprompter para estúdio e jornalismo', icon: exe, iconIndex: 0, appUserModelId: 'com.arca.teleprompter' };
      for (const dir of [o.startMenu && programs(o.scope), o.desk && desktop(o.scope)].filter(Boolean) as string[]) {
        const lnk = join(dir, PRODUCT + '.lnk');
        try { mkdirSync(dir, { recursive: true }); if (shell.writeShortcutLink(lnk, 'replace', opts)) { man.shortcuts.push(lnk); undo.push(() => rmSync(lnk, { force: true })); } } catch (e) { /* sem permissão */ }
      }
      ok('Criando atalhos');
    }
    check();

    step('Registrando o desinstalador', 'Aplicativos instalados → ' + PRODUCT, 96);
    const key = `${hive(o.scope)}\\${REG_KEY}`;
    const vals: [string, string, string?][] = [
      ['DisplayName', PRODUCT], ['DisplayVersion', app.getVersion()], ['Publisher', 'ARCA'], ['DisplayIcon', `"${exe}",0`],
      ['InstallLocation', target], ['UninstallString', `"${exe}" --uninstall`], ['QuietUninstallString', `"${exe}" --uninstall`],
      ['EstimatedSize', String(Math.round(total / 1024)), 'REG_DWORD'], ['NoModify', '1', 'REG_DWORD'], ['NoRepair', '1', 'REG_DWORD']
    ];
    for (const [n, v, t] of vals) await regAdd(key, n, v, t);
    undo.push(() => regDel(key));
    writeFileSync(join(target, 'install.json'), JSON.stringify(man, null, 2));
    writeFileSync(join(target, 'NOVIDADES.txt'), NOTES.replace('{v}', app.getVersion()), 'utf8');
    ok('Registrando o desinstalador');
    progress({ pct: 100, done: true });
  } catch (e: any) {
    // Cancelado ou falhou: desfaz na ordem inversa.
    for (const u of undo.reverse()) { try { await u(); } catch (err) { /* segue */ } }
    if (e && e.message === '__cancel') { progress({ task: 'Instalação cancelada', file: 'Nenhuma alteração foi mantida', pct: 0 }); setTimeout(() => app.quit(), 1400); }
    else progress({ error: e && e.code === 'EPERM' ? 'Sem permissão para gravar nessa pasta. Escolha outra ou instale para todos os usuários.' : String(e && e.message || e) });
  }
}

async function uninstall(o: { keep: Record<string, boolean>; reason: string; feedback: string }) {
  const man = installedManifest();
  const step = (task: string, file = '', pct?: number) => progress({ task, file, pct });
  const ok = (log: string, warn = false) => progress({ log, warn });
  if (!man) { progress({ error: 'Instalação não encontrada.' }); return; }
  step('Fechando o ARCA Teleprompter', 'Encerrando processos', 5);
  await sh('taskkill', ['/IM', EXE, '/T', '/F', '/FI', `PID ne ${process.pid}`]);
  ok('Fechando o ARCA Teleprompter');

  step('Removendo arquivos do programa', man.path + '\\', 15);
  let left = false;
  try { rmSync(man.path, { recursive: true, force: true, maxRetries: 5, retryDelay: 300 }); } catch (e) { left = true; }
  if (existsSync(man.path)) left = true;
  ok(left ? 'Alguns arquivos estavam em uso e serão removidos ao reiniciar' : 'Removendo arquivos do programa', left);

  step('Removendo atalhos e associação .tps', 'Menu Iniciar · Área de trabalho · .tps', 45);
  for (const l of man.shortcuts) rmSync(l, { force: true });
  if (man.assoc) { const base = `${hive(man.scope)}\\Software\\Classes`; await regDel(`${base}\\${PROG_ID}`); await regDel(`${base}\\.tps`); }
  ok('Removendo atalhos e associação .tps');

  if (man.firewall) { step('Removendo regra de firewall', 'Porta 8420', 55); await sh('netsh', ['advfirewall', 'firewall', 'delete', 'rule', `name=${FW_RULE}`]); ok('Removendo regra de firewall'); }

  const ud = app.getPath('userData'), dd = dataDir();
  const rmData = (keys: string[]) => keys.forEach(k => rmSync(join(dd, k + '.json'), { force: true }));
  if (!o.keep.scripts) { step('Apagando roteiros e versões', join(dd, 'arca-prompter-docs.json'), 65); rmData(['arca-prompter-docs', 'arca-prompter-versions']); ok('Apagando roteiros e versões'); }
  if (!o.keep.prefs) { step('Apagando preferências', join(dd, 'arca-prompter-prefs.json'), 72); rmData(['arca-prompter-prefs', 'arca-prompter-displays']); ok('Apagando preferências'); }
  if (!o.keep.profiles) { step('Apagando perfis de apresentador', join(dd, 'arca-prompter-profiles.json'), 78); rmData(['arca-prompter-profiles']); ok('Apagando perfis de apresentador'); }
  if (!o.keep.logs) {
    step('Apagando registros e cache', ud, 84);
    // Tudo em userData fora de data/ é cache do Chromium; com tudo desmarcado, apaga a pasta inteira.
    const all = !o.keep.scripts && !o.keep.prefs && !o.keep.profiles;
    // Este processo usa a mesma pasta de cache: apaga o que der agora e o resto depois de fechar.
    for (const e of existsSync(ud) ? readdirSync(ud) : []) if (all || e !== 'data') try { rmSync(join(ud, e), { recursive: true, force: true }); } catch (err) { /* em uso */ }
    ok('Apagando registros e cache');
  }
  if (o.reason || o.feedback) try { writeFileSync(join(tmpdir(), 'arca-teleprompter-desinstalacao.txt'), `${new Date().toISOString()}\n${o.reason}\n${o.feedback}\n`); } catch (e) { /* */ }

  step('Removendo registro do desinstalador', 'Aplicativos instalados', 95);
  await regDel(`${hive(man.scope)}\\${REG_KEY}`);
  ok('Removendo registro do desinstalador');
  progress({ pct: 100, done: true });
}

/** Relança este mesmo programa como administrador (UAC) com os argumentos dados. */
function relaunchElevated(args: string[]): Promise<boolean> {
  const exe = process.env.PORTABLE_EXECUTABLE_FILE || process.execPath;
  const all = (app.isPackaged ? [] : [app.getAppPath()]).concat(args);
  const list = all.map(a => `'${a.replace(/'/g, "''")}'`).join(',');
  return sh('powershell', ['-NoProfile', '-Command', `Start-Process -FilePath '${exe.replace(/'/g, "''")}' -ArgumentList ${list} -Verb RunAs`]).then(r => r.ok);
}

export function startSetup(mode: 'install' | 'uninstall') {
  // O desinstalador não consegue apagar a pasta de onde está rodando: copia-se para %TEMP% e
  // continua de lá, apontando para a instalação com --target.
  if (mode === 'uninstall' && app.isPackaged && !process.argv.some(a => a.startsWith('--target='))) {
    const man = installedManifest();
    if (man && sourceDir().toLowerCase() === man.path.toLowerCase()) {
      const tmp = join(tmpdir(), 'arca-uninstall-' + Date.now());
      cpSync(man.path, tmp, { recursive: true });
      spawn(join(tmp, EXE), ['--uninstall', `--target=${man.path}`], { detached: true, stdio: 'ignore' }).unref();
      app.quit(); return;
    }
  }

  let resume: InstallOpts | null = null;
  const r = process.argv.find(a => a.startsWith('--resume='));
  if (r) try { resume = JSON.parse(readFileSync(r.slice(9), 'utf8')); rmSync(r.slice(9), { force: true }); } catch (e) { /* */ }

  app.whenReady().then(async () => {
    const man = installedManifest();
    // Instalação para todos os usuários: o desinstalador precisa de administrador.
    if (mode === 'uninstall' && man && man.scope === 'all' && !isAdmin() && !process.argv.includes('--no-elevate')) {
      if (await relaunchElevated(process.argv.slice(app.isPackaged ? 1 : 2).concat('--no-elevate'))) { app.quit(); return; }
    }
    win = new BrowserWindow({
      width: 760, height: 500, resizable: false, maximizable: false, frame: false, show: false, center: true,
      backgroundColor: '#ffffff', title: mode === 'install' ? 'Instalação do ' + PRODUCT : 'Desinstalar ' + PRODUCT, icon: appIcon(),
      webPreferences: { preload: preloadPath() }
    });
    win.setMenu(null);
    win.once('ready-to-show', () => win!.show());
    loadPage(win, mode === 'install' ? '#/setup' : '#/uninstall');
    win.on('closed', () => { win = null; cleanupAndQuit(); });
  });
  app.on('window-all-closed', () => cleanupAndQuit());

  ipcMain.handle('setup:info', () => {
    const man = installedManifest(), ud = app.getPath('userData'), dd = dataDir();
    const sz = (k: string[]) => k.reduce((a, n) => a + (existsSync(join(dd, n + '.json')) ? statSync(join(dd, n + '.json')).size : 0), 0);
    const cache = existsSync(ud) ? readdirSync(ud).filter(e => e !== 'data').reduce((a, e) => a + (statSync(join(ud, e)).isDirectory() ? dirSize(join(ud, e)) : statSync(join(ud, e)).size), 0) : 0;
    return {
      mode, version: app.getVersion(), isAdmin: isAdmin(), paths: defaultPaths(), resume,
      sizeMB: Math.round(dirSize(sourceDir()) / (1 << 20)),
      installed: man ? { path: man.path, version: man.version, sizeMB: Math.round(dirSize(man.path) / (1 << 20)) } : null,
      dataSizes: { scripts: mb(sz(['arca-prompter-docs', 'arca-prompter-versions'])), prefs: mb(sz(['arca-prompter-prefs', 'arca-prompter-displays'])), profiles: mb(sz(['arca-prompter-profiles'])), logs: mb(cache) },
      appRunning: mode === 'uninstall' && appRunning()
    };
  });
  ipcMain.handle('setup:disk', (_e, p: string) => { try { const s = statfsSync(parse(p).root || 'C:\\'); return { freeGB: (s.bavail * s.bsize) / (1 << 30) }; } catch (e) { return { freeGB: null }; } });
  ipcMain.handle('setup:browse', async (_e, cur: string) => {
    const r = await dialog.showOpenDialog(win!, { title: 'Pasta de destino', defaultPath: dirname(cur), properties: ['openDirectory', 'createDirectory'] });
    if (r.canceled || !r.filePaths[0]) return null;
    const p = r.filePaths[0]; return basename(p).toLowerCase() === PRODUCT.toLowerCase() ? p : join(p, PRODUCT);
  });
  ipcMain.handle('setup:elevate', async (_e, o: InstallOpts) => {
    const f = join(tmpdir(), `arca-setup-${Date.now()}.json`); writeFileSync(f, JSON.stringify(o));
    const ok = await relaunchElevated(['--setup', `--resume=${f}`]);
    if (ok) { setTimeout(() => app.quit(), 300); return { ok: true }; }
    rmSync(f, { force: true });
    return { ok: false, error: 'O Windows não concedeu permissão de administrador. Escolha “Somente para mim” para instalar sem ela.' };
  });
  ipcMain.handle('setup:install', (_e, o: InstallOpts) => install(o));
  ipcMain.handle('setup:cancel', () => { cancelFlag = true; });
  ipcMain.handle('setup:uninstall', (_e, o) => uninstall(o));
  ipcMain.handle('setup:closeApp', async () => { await sh('taskkill', ['/IM', EXE, '/T', '/FI', `PID ne ${process.pid}`]); await new Promise(r => setTimeout(r, 1200)); return !appRunning(); });
  ipcMain.on('setup:minimize', () => win?.minimize());
  ipcMain.on('setup:quit', () => win?.close());
  ipcMain.on('setup:finish', (_e, o: { launch: boolean; notes: boolean }) => {
    const target = lastInstall ? lastInstall.path.replace(/[\\/]+$/, '') : '';
    if (target && o.notes) shell.openPath(join(target, 'NOVIDADES.txt'));
    if (target && o.launch) spawn(join(target, EXE), [], { detached: true, stdio: 'ignore', cwd: target }).unref();
    win?.close();
  });
}

function appRunning() {
  try {
    const out = execFileSync('tasklist', ['/FI', `IMAGENAME eq ${EXE}`, '/FO', 'CSV', '/NH'], { windowsHide: true }).toString();
    const own = new Set([process.pid, process.ppid]);
    return out.split(/\r?\n/).some(l => { const m = /^"[^"]+","(\d+)"/.exec(l); return m && !own.has(+m[1]); });
  } catch (e) { return false; }
}

let cleaned = false;
function cleanupAndQuit() {
  if (cleaned) return; cleaned = true;
  // Cópia temporária do desinstalador: apaga-se depois que este processo terminar.
  if (process.argv.some(a => a.startsWith('--target=')) && app.isPackaged) {
    const dir = sourceDir();
    spawn('cmd.exe', ['/c', `ping -n 3 127.0.0.1 >nul & rmdir /s /q "${dir}"`], { detached: true, stdio: 'ignore', windowsHide: true }).unref();
  }
  app.quit();
}

const NOTES = `ARCA Teleprompter {v}

• Editor WYSIWYG com marcadores, pausas, blocos pulados e apresentadores.
• Janela do talento e uma janela de prompter por monitor, com espelho horizontal/vertical por tela.
• Monitor web na rede local (porta 8420), com controle remoto opcional pelo celular.
• Abre .tps, .txt, .html, Word (.docx) e PDF. Exporta .txt e PDF.
• Fontes de leitura incluídas e acesso às fontes instaladas no Windows.
• Salvamento automático e histórico de versões em %APPDATA%\\ARCA Teleprompter.

Em desenvolvimento: saída NDI, protocolo MOS completo, voz offline, closed caption.
`;
