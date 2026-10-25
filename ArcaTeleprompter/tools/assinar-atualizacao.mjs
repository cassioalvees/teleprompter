// Assina o latest.json da atualização automática (usado pelo publish-release.ps1).
//   node tools/assinar-atualizacao.mjs <latest.json de entrada> <latest.json assinado de saída>
// Chave: UPDATE_PRIVATE_KEY em server/licencas/.dev.vars (fora do repositório; faça backup).
// Assina "versão|arquivo|SHA256|tamanho" com ECDSA P-256 (IEEE P1363, base64url) — o app confere com a chave pública
// embutida (Arca.Core/Net/UpdateSignature.cs) e recusa atualização sem assinatura válida.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';

const [, , input, output] = process.argv;
if (!input || !output) { console.error('uso: node tools/assinar-atualizacao.mjs <entrada> <saída>'); process.exit(2); }
const vars = path.join(path.dirname(fileURLToPath(import.meta.url)), '..', 'server', 'licencas', '.dev.vars');
const key = /^UPDATE_PRIVATE_KEY="?([^"\r\n]+)/m.exec(fs.existsSync(vars) ? fs.readFileSync(vars, 'utf8') : '')?.[1];
if (!key) { console.error('UPDATE_PRIVATE_KEY não encontrada em ' + vars); process.exit(1); }
const info = JSON.parse(fs.readFileSync(input, 'utf8').replace(/^﻿/, ''));
const payload = `${info.version}|${info.file}|${String(info.sha256).toUpperCase()}|${info.size}`;
const priv = crypto.createPrivateKey({ key: Buffer.from(key, 'base64'), format: 'der', type: 'pkcs8' });
info.sig = crypto.sign('sha256', Buffer.from(payload, 'utf8'), { key: priv, dsaEncoding: 'ieee-p1363' }).toString('base64url');
fs.writeFileSync(output, JSON.stringify(info, null, 2));
console.log('assinado: ' + payload);
