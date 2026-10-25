import { app, type BrowserWindow } from 'electron';
import { existsSync } from 'node:fs';
import { join } from 'node:path';

const devUrl = process.env.VITE_DEV_SERVER_URL;
export const distDir = () => join(app.getAppPath(), 'dist');
export const preloadPath = () => join(__dirname, 'preload.cjs');
export const appIcon = () => { const p = join(app.getAppPath(), 'build', 'icon.ico'); return existsSync(p) ? p : undefined; };

/** Carrega a página do renderer (servidor do Vite em desenvolvimento, dist/ empacotado). */
export function loadPage(win: BrowserWindow, hash: string) {
  if (devUrl) win.loadURL(devUrl + (hash ? '/' + hash : ''));
  else win.loadFile(join(distDir(), 'index.html'), hash ? { hash: hash.replace(/^#/, '') } : undefined);
}
