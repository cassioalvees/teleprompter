// Monitor web: servidor HTTP + WebSocket na rede local (porta 8420).
// GET /monitor → a mesma build do app (MonitorPage). WS /ws → estado, HTML e posição em tempo real.
// Protocolo: cliente envia {t:'auth', pass}; servidor responde {t:'auth', ok, needPass};
// depois recebe mensagens do barramento ({t:'html'|'state'|'y'}) e, se permitido, envia {t:'cmd', cmd}.
import { createServer, type Server } from 'node:http';
import { createReadStream, existsSync, statSync } from 'node:fs';
import { extname, join, normalize } from 'node:path';
import { networkInterfaces } from 'node:os';
import { WebSocketServer, type WebSocket } from 'ws';

const MIME: Record<string, string> = { '.html': 'text/html; charset=utf-8', '.js': 'text/javascript', '.mjs': 'text/javascript', '.css': 'text/css', '.woff2': 'font/woff2', '.woff': 'font/woff', '.png': 'image/png', '.svg': 'image/svg+xml', '.json': 'application/json', '.ttf': 'font/ttf' };

export function lanAddresses(): string[] {
  const out: string[] = [];
  for (const list of Object.values(networkInterfaces())) for (const a of list || []) if (a.family === 'IPv4' && !a.internal) out.push(a.address);
  // Endereços de rede privada primeiro (192.168.x, 10.x, 172.16-31.x).
  return out.sort((a, b) => +!/^(192\.168|10\.|172\.(1[6-9]|2\d|3[01]))/.test(a) - +!/^(192\.168|10\.|172\.(1[6-9]|2\d|3[01]))/.test(b));
}

export class MonitorServer {
  private http: Server | null = null;
  private wss: WebSocketServer | null = null;
  private clients = new Set<WebSocket>();
  private authed = new WeakSet<WebSocket>();
  pass = ''; ctrl = false;

  constructor(private root: string, private replay: () => any[], private onCmd: (c: any) => void, private onViewers: (n: number) => void) {}

  start(port: number): Promise<void> {
    return new Promise((resolve, reject) => {
      const http = createServer((req, res) => {
        let p = decodeURIComponent((req.url || '/').split('?')[0]);
        if (p === '/' ) { res.writeHead(302, { Location: '/monitor' }); return res.end(); }
        if (p === '/monitor' || p === '/monitor/') p = '/index.html';
        else if (p.startsWith('/monitor/')) p = p.slice(8);
        const file = normalize(join(this.root, p));
        if (!file.startsWith(normalize(this.root)) || !existsSync(file) || !statSync(file).isFile()) { res.writeHead(404); return res.end('Não encontrado'); }
        res.writeHead(200, { 'Content-Type': MIME[extname(file)] || 'application/octet-stream', 'Cache-Control': 'no-cache' });
        createReadStream(file).pipe(res);
      });
      const wss = new WebSocketServer({ server: http, path: '/ws' });
      wss.on('connection', ws => {
        this.clients.add(ws); this.count();
        ws.on('message', raw => {
          let m: any; try { m = JSON.parse(String(raw)); } catch (e) { return; }
          if (m.t === 'auth') {
            const ok = !this.pass || m.pass === this.pass;
            ws.send(JSON.stringify({ t: 'auth', ok, needPass: !!this.pass }));
            if (ok) { this.authed.add(ws); for (const msg of this.replay()) ws.send(JSON.stringify(msg)); }
            return;
          }
          if (m.t === 'cmd' && this.ctrl && this.authed.has(ws) && m.cmd && m.cmd.t !== 'key') this.onCmd(m.cmd);
        });
        ws.on('close', () => { this.clients.delete(ws); this.count(); });
      });
      http.once('error', reject);
      http.listen(port, '0.0.0.0', () => { this.http = http; this.wss = wss; resolve(); });
    });
  }

  private count() { this.onViewers([...this.clients].length); }

  broadcast(msg: any) {
    if (!this.clients.size) return;
    const data = JSON.stringify(msg);
    for (const ws of this.clients) if (this.authed.has(ws) && ws.readyState === 1 && ws.bufferedAmount < 1 << 20) ws.send(data);
  }

  /** Senha nova derruba quem entrou com a antiga. */
  setPass(p: string) {
    if (p === this.pass) return; this.pass = p;
    for (const ws of this.clients) { this.authed.delete(ws); ws.send(JSON.stringify({ t: 'auth', ok: !p, needPass: !!p })); if (!p) { this.authed.add(ws); for (const msg of this.replay()) ws.send(JSON.stringify(msg)); } }
  }

  stop(): Promise<void> {
    return new Promise(resolve => {
      for (const ws of this.clients) ws.terminate();
      this.clients.clear(); this.count();
      this.wss?.close(); this.wss = null;
      if (this.http) this.http.close(() => resolve()); else resolve();
      this.http = null;
    });
  }
}
