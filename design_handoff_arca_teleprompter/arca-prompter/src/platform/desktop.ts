// Implementação para o app Windows (Electron). Fala com o processo principal pela ponte
// `window.arca` exposta em electron/preload.ts.
import type { BusCmd, BusMsg, DisplayInfo, OpenedFile, Platform } from './types';

export interface ArcaBridge {
  invoke(ch: string, ...a: any[]): Promise<any>;
  send(ch: string, ...a: any[]): void;
  on(ch: string, cb: (...a: any[]) => void): void;
  store: Record<string, string>;
}
declare global { interface Window { arca?: ArcaBridge } }

const toFile = (f: { name: string; data: Uint8Array; path?: string }): OpenedFile => ({ file: new File([f.data], f.name), path: f.path });

export function createDesktopPlatform(arca: ArcaBridge): Platform {
  const cache = { ...arca.store };
  const saveT: Record<string, any> = {};
  let lastY = 0;
  return {
    kind: 'desktop',
    storage: {
      get: k => (k in cache ? cache[k] : null),
      set: (k, v) => { cache[k] = v; clearTimeout(saveT[k]); saveT[k] = setTimeout(() => arca.send('store:set', k, v), 150); }
    },
    listDisplays: () => arca.invoke('displays:list') as Promise<DisplayInfo[]>,
    onDisplaysChanged: cb => arca.on('displays:changed', cb),
    openOutput: d => arca.send('output:open', d),
    closeOutput: id => arca.send('output:close', id),
    onOutputClosed: cb => arca.on('output:closed', cb),
    publish: (msg: BusMsg) => {
      if (msg.t === 'y') { if (Math.abs(msg.y - lastY) < 0.01 && !msg.v) return; lastY = msg.y; }
      arca.send('bus:publish', msg);
    },
    onCommand: (cb: (c: BusCmd) => void) => arca.on('bus:cmd', cb),
    openFile: async () => { const f = await arca.invoke('file:open'); return f ? toFile(f) : null; },
    saveFile: (name, data, mime, path, saveAs) => arca.invoke('file:save', { name, data, mime, path, saveAs }),
    printHtml: (html, mode, name) => arca.invoke('print:html', { html, mode, name }),
    monitor: {
      start: o => arca.invoke('monitor:start', o),
      stop: () => arca.invoke('monitor:stop'),
      update: o => arca.send('monitor:update', o),
      onViewers: cb => arca.on('monitor:viewers', cb)
    },
    mos: {
      connect: (host, port) => arca.invoke('mos:connect', { host, port }),
      disconnect: () => arca.send('mos:disconnect')
    },
    win: {
      minimize: () => arca.send('win:minimize'),
      maximize: () => arca.send('win:maximize'),
      close: () => arca.send('win:close')
    },
    onOpenPath: cb => { arca.on('file:opened', f => cb(toFile(f))); arca.send('file:ready'); },
    setTitle: t => { document.title = t; },
    openExternal: url => arca.send('shell:open', url)
  };
}
