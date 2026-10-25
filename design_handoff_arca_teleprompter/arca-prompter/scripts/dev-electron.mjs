// Desenvolvimento do app desktop: servidor do Vite + Electron apontando para ele.
//   npm run dev:electron                  → editor
//   ARCA_MODE=setup npm run dev:electron  → assistente de instalação (sem instalar de verdade: só a tela)
import { createServer } from 'vite';
import { spawn } from 'node:child_process';
import { createRequire } from 'node:module';

await import('./build-electron.mjs');
const server = await createServer({ server: { port: 5173 } });
await server.listen();
const url = `http://localhost:${server.config.server.port}`;
const electron = createRequire(import.meta.url)('electron');
const child = spawn(electron, ['.'], { stdio: 'inherit', env: { ...process.env, VITE_DEV_SERVER_URL: url } });
child.on('exit', code => { server.close(); process.exit(code ?? 0); });
