// Implementação para navegador: localStorage, pop-ups para as telas de prompter
// (sincronizadas por BroadcastChannel), download de arquivos e impressão do navegador.
import type { BusCmd, BusMsg, DisplayInfo, Platform } from './types';

export const CHANNEL = 'arca-prompter-bus';

export function createWebPlatform(): Platform {
  const bc = typeof BroadcastChannel !== 'undefined' ? new BroadcastChannel(CHANNEL) : null;
  const pops = new Map<string, Window>();
  const closedCbs: ((id: string) => void)[] = [];
  const cmdCbs: ((c: BusCmd) => void)[] = [];
  bc?.addEventListener('message', e => { const m = e.data; if (m && m.cmd) cmdCbs.forEach(cb => cb(m.cmd)); });
  setInterval(() => { pops.forEach((w, id) => { if (w.closed) { pops.delete(id); closedCbs.forEach(cb => cb(id)); } }); }, 800);

  let screens: any[] = [];
  const res = () => `${screen.width} × ${screen.height}`;
  const list = async (): Promise<DisplayInfo[]> => {
    const out: DisplayInfo[] = [{ id: 'op', name: 'Esta janela do navegador', res: res(), operator: true }];
    try {
      // Window Management API (Chrome/Edge): só lista se o usuário já concedeu a permissão.
      const perm = await (navigator as any).permissions?.query({ name: 'window-management' }).catch(() => null);
      if (perm && perm.state === 'granted' && (window as any).getScreenDetails) {
        const det = await (window as any).getScreenDetails(); screens = det.screens;
        screens.forEach((s: any, i: number) => { if (!s.isPrimary) out.push({ id: 'scr' + i, name: s.label || 'Monitor ' + (i + 1), res: `${s.width} × ${s.height}`, operator: false }); });
      }
    } catch (e) { /* sem permissão */ }
    out.push({ id: 'pop1', name: 'Janela pop-up', res: 'arraste para o monitor do prompter', operator: false, windowed: true });
    return out;
  };

  return {
    kind: 'web',
    storage: {
      get: k => { try { return localStorage.getItem(k); } catch (e) { return null; } },
      set: (k, v) => { try { localStorage.setItem(k, v); } catch (e) { /* cheio / bloqueado */ } }
    },
    listDisplays: list,
    onDisplaysChanged: () => {},
    openOutput: d => {
      const ex = pops.get(d.id); if (ex && !ex.closed) { ex.focus(); return; }
      const url = location.href.split('#')[0] + '#/output?d=' + encodeURIComponent(d.id);
      let feat = 'popup,width=960,height=540';
      const m = /^scr(\d+)$/.exec(d.id), s = m && screens[+m[1]];
      if (s) feat = `popup,left=${s.availLeft},top=${s.availTop},width=${s.availWidth},height=${s.availHeight}`;
      const w = window.open(url, 'arca-out-' + d.id, feat);
      if (w) pops.set(d.id, w); else setTimeout(() => closedCbs.forEach(cb => cb(d.id)), 0);
    },
    closeOutput: id => { const w = pops.get(id); if (w) { pops.delete(id); try { w.close(); } catch (e) { /* */ } } },
    onOutputClosed: cb => { closedCbs.push(cb); },
    publish: (msg: BusMsg) => { bc?.postMessage({ msg }); },
    onCommand: cb => { cmdCbs.push(cb); },
    openFile: async () => null,
    saveFile: async (name, data, mime) => {
      const a = document.createElement('a'); a.href = URL.createObjectURL(new Blob([data], { type: mime })); a.download = name;
      document.body.appendChild(a); a.click(); a.remove(); setTimeout(() => URL.revokeObjectURL(a.href), 5000);
      return { name };
    },
    printHtml: async html => {
      const w = window.open('', '_blank'); if (!w) return false;
      w.document.write(html); w.document.close(); w.focus(); setTimeout(() => w.print(), 400); return true;
    },
    monitor: null,
    mos: null,
    win: null,
    onOpenPath: () => {},
    setTitle: t => { document.title = t; },
    openExternal: url => { window.open(url, '_blank'); }
  };
}

/** Pede ao navegador acesso aos monitores (precisa de um clique do usuário). */
export async function requestScreens(): Promise<boolean> {
  try { if ((window as any).getScreenDetails) { await (window as any).getScreenDetails(); return true; } } catch (e) { /* negado */ }
  return false;
}
