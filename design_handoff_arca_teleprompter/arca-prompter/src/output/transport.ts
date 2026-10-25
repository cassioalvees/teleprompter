// Canal das páginas de saída com o editor: IPC (Electron), BroadcastChannel (pop-up na web)
// ou WebSocket (monitor web servido pelo app).
import type { BusCmd, BusMsg } from '../platform/types';
import { CHANNEL } from '../platform/web';

export interface Transport { onMsg(cb: (m: BusMsg) => void): void; send(c: BusCmd): void; close?(): void }

export function localTransport(): Transport {
  const arca = window.arca;
  if (arca) return { onMsg: cb => arca.on('bus', cb), send: c => arca.send('bus:cmd', c) };
  const bc = new BroadcastChannel(CHANNEL);
  return {
    onMsg: cb => bc.addEventListener('message', e => { if (e.data && e.data.msg) cb(e.data.msg); }),
    send: c => bc.postMessage({ cmd: c }),
    close: () => bc.close()
  };
}

export interface WsHandlers { onMsg(m: BusMsg): void; onAuth(ok: boolean, needPass: boolean): void; onState(open: boolean): void }

/** WebSocket com reconexão automática. Protocolo em electron/server.ts. */
export function wsTransport(pass: string, h: WsHandlers): Transport & { setPass(p: string): void } {
  let ws: WebSocket | null = null, closed = false, retry: any = 0, pw = pass;
  const url = (location.protocol === 'https:' ? 'wss://' : 'ws://') + location.host + '/ws';
  const open = () => {
    ws = new WebSocket(url);
    ws.onopen = () => { h.onState(true); ws!.send(JSON.stringify({ t: 'auth', pass: pw })); };
    ws.onmessage = e => {
      let m: any; try { m = JSON.parse(e.data); } catch (err) { return; }
      if (m.t === 'auth') return h.onAuth(!!m.ok, !!m.needPass);
      h.onMsg(m);
    };
    ws.onclose = () => { h.onState(false); if (!closed) retry = setTimeout(open, 1500); };
  };
  open();
  return {
    onMsg: () => {},
    send: c => { if (ws && ws.readyState === 1) ws.send(JSON.stringify({ t: 'cmd', cmd: c })); },
    setPass: p => { pw = p; if (ws && ws.readyState === 1) ws.send(JSON.stringify({ t: 'auth', pass: p })); },
    close: () => { closed = true; clearTimeout(retry); ws && ws.close(); }
  };
}
