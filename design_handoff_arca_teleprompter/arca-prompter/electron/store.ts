// Persistência em %APPDATA%\ARCA Teleprompter\data\<chave>.json (substitui o localStorage do protótipo).
import { app } from 'electron';
import { existsSync, mkdirSync, readdirSync, readFileSync, renameSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';

export const dataDir = () => join(app.getPath('userData'), 'data');
const safe = (k: string) => k.replace(/[^a-z0-9._-]/gi, '_');

export function readAll(): Record<string, string> {
  const dir = dataDir(), out: Record<string, string> = {};
  if (!existsSync(dir)) return out;
  for (const f of readdirSync(dir)) {
    if (!f.endsWith('.json')) continue;
    try { out[f.slice(0, -5)] = readFileSync(join(dir, f), 'utf8'); } catch (e) { /* arquivo corrompido: ignora */ }
  }
  return out;
}

export function write(key: string, value: string) {
  const dir = dataDir(); mkdirSync(dir, { recursive: true });
  const file = join(dir, safe(key) + '.json'), tmp = file + '.tmp';
  // Grava num temporário e renomeia: um desligamento no meio não corrompe o roteiro.
  writeFileSync(tmp, value, 'utf8'); renameSync(tmp, file);
}
