// Gera o par de chaves que assina as licenças (ECDSA P-256) e o token do administrador.
//   node tools/gen-keys.mjs
// Escreve .dev.vars (segredos para o servidor local — NÃO vai para o repositório) e mostra a chave pública,
// que vai dentro do app (src/ArcaTeleprompter/Services/LicenseService.cs → PublicKey).
// Se .dev.vars já existe, não troca as chaves (licenças já emitidas continuariam valendo só com a chave antiga).
import { webcrypto as crypto } from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const dir = path.join(path.dirname(fileURLToPath(import.meta.url)), '..');
const vars = path.join(dir, '.dev.vars');
const b64 = buf => Buffer.from(buf).toString('base64');

if (fs.existsSync(vars) && !process.argv.includes('--novo')) {
  const priv = /LICENSE_PRIVATE_KEY="?([^"\n]+)/.exec(fs.readFileSync(vars, 'utf8'))?.[1];
  const key = await crypto.subtle.importKey('pkcs8', Buffer.from(priv, 'base64'), { name: 'ECDSA', namedCurve: 'P-256' }, true, ['sign']);
  const jwk = await crypto.subtle.exportKey('jwk', key);
  const pub = await crypto.subtle.importKey('jwk', { kty: jwk.kty, crv: jwk.crv, x: jwk.x, y: jwk.y }, { name: 'ECDSA', namedCurve: 'P-256' }, true, ['verify']);
  console.log('.dev.vars já existe (use --novo para trocar as chaves).');
  console.log('Chave pública (SPKI, base64):\n' + b64(await crypto.subtle.exportKey('spki', pub)));
  process.exit(0);
}
const pair = await crypto.subtle.generateKey({ name: 'ECDSA', namedCurve: 'P-256' }, true, ['sign', 'verify']);
const priv = b64(await crypto.subtle.exportKey('pkcs8', pair.privateKey));
const pub = b64(await crypto.subtle.exportKey('spki', pair.publicKey));
const admin = Buffer.from(crypto.getRandomValues(new Uint8Array(24))).toString('base64url');
fs.writeFileSync(vars, `LICENSE_PRIVATE_KEY="${priv}"\nADMIN_TOKEN="${admin}"\n`);
console.log('Gerado ' + vars);
console.log('Chave pública (SPKI, base64) — vai no app:\n' + pub);
console.log('Token do administrador (página /admin):\n' + admin);
