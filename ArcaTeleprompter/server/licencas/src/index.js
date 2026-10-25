// Servidor de licenças do ARCA Teleprompter — Cloudflare Worker + D1.
//   GET  /                     site (public/index.html, servido como arquivo estático) com o cadastro
//   GET  /download             instalador mais recente (redireciona para o anexo da última versão no GitHub)
//   GET  /api/latest           versão, arquivo e tamanho da última versão (latest.json do GitHub, guardado 5 min)
//   GET  /conta                Minha conta (public/conta.html): entrar, criar conta, Licenças e Meus dados
//   POST /api/account/signup   cria a conta e o código de licença · /login · /logout
//   GET  /api/account          dados e licenças da conta (cookie de sessão)
//   POST /api/account/release  remove a licença do computador atual (para ativar em outro) · /license gera o código
//   POST /api/account/profile  atualiza nome, empresa e telefone · /password troca a senha
//   POST /api/activate         prende o código a um computador e devolve a licença assinada (ECDSA P-256)
//   POST /api/deactivate       libera o código (trocar de computador) — só do próprio computador
//   POST /api/check            o app confere de vez em quando se a licença continua valendo (bloqueio, troca)
//   GET  /admin                página do administrador (token ADMIN_TOKEN)
//   GET  /api/admin/licenses   lista · POST /api/admin/reset (libera o computador) · POST /api/admin/status (bloquear)
// Formato da licença: base64url(JSON) + "." + base64url(assinatura P1363 de 64 bytes sobre o texto antes do ponto).

const ALPHABET = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789'; // sem 0/O e 1/I
const json = (data, status = 200, headers = {}) => new Response(JSON.stringify(data), { status, headers: { 'content-type': 'application/json; charset=utf-8', 'access-control-allow-origin': '*', 'cache-control': 'no-store', ...headers } });
const fail = (status, error) => json({ ok: false, error }, status);
const now = () => new Date().toISOString();
const b64url = bytes => btoa(String.fromCharCode(...new Uint8Array(bytes))).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
const clean = (v, max = 120) => String(v ?? '').replace(/[\u0000-\u001f]/g, ' ').trim().slice(0, max);
const normCode = c => clean(c, 40).toUpperCase().replace(/[^A-Z0-9]/g, '').replace(/^ARCA/, '');
const fmtCode = raw => 'ARCA-' + raw.match(/.{1,4}/g).join('-');

function newCode() {
  const r = crypto.getRandomValues(new Uint8Array(12));
  return fmtCode(Array.from(r, b => ALPHABET[b % ALPHABET.length]).join(''));
}

let keyCache;
async function privateKey(env) {
  if (!keyCache) {
    const der = Uint8Array.from(atob(env.LICENSE_PRIVATE_KEY), c => c.charCodeAt(0));
    keyCache = await crypto.subtle.importKey('pkcs8', der, { name: 'ECDSA', namedCurve: 'P-256' }, false, ['sign']);
  }
  return keyCache;
}

async function sign(env, payload) {
  const body = b64url(new TextEncoder().encode(JSON.stringify(payload)));
  const sig = await crypto.subtle.sign({ name: 'ECDSA', hash: 'SHA-256' }, await privateKey(env), new TextEncoder().encode(body));
  return body + '.' + b64url(sig);
}

async function log(env, code, kind, machine, detail, ip) {
  await env.DB.prepare('INSERT INTO events (code, kind, machine, detail, ip, at) VALUES (?, ?, ?, ?, ?, ?)').bind(code, kind, machine ?? null, detail ?? null, ip ?? null, now()).run();
}

function sameSecret(a, b) {
  if (!a || !b || a.length !== b.length) return false;
  let d = 0; for (let i = 0; i < a.length; i++) d |= a.charCodeAt(i) ^ b.charCodeAt(i);
  return d === 0;
}

async function body(req) { try { return await req.json(); } catch { return {}; } }

// ——— Conta do site (Minha conta) ———
// Cookie de sessão HttpOnly; no banco fica só o SHA-256 do token. Senha em PBKDF2-SHA256 (100 mil iterações, o máximo do Workers).
const SESSION_DAYS = 30, ITER = 100000;
const enc = new TextEncoder();
const hex = buf => Array.from(new Uint8Array(buf), b => b.toString(16).padStart(2, '0')).join('');
const sha256 = async s => hex(await crypto.subtle.digest('SHA-256', enc.encode(s)));
const since = ms => new Date(Date.now() - ms).toISOString();

async function hashPassword(pw, saltHex) {
  const salt = saltHex ? Uint8Array.from(saltHex.match(/../g), h => parseInt(h, 16)) : crypto.getRandomValues(new Uint8Array(16));
  const key = await crypto.subtle.importKey('raw', enc.encode(pw), 'PBKDF2', false, ['deriveBits']);
  const bits = await crypto.subtle.deriveBits({ name: 'PBKDF2', hash: 'SHA-256', salt, iterations: ITER }, key, 256);
  return `pbkdf2$${ITER}$${hex(salt)}$${hex(bits)}`;
}
async function passwordOk(pw, stored) {
  const salt = String(stored || '').split('$')[2];
  return !!salt && sameSecret(await hashPassword(pw, salt), stored);
}
const passwordError = pw => typeof pw !== 'string' || pw.length < 8 ? 'A senha precisa ter pelo menos 8 caracteres.' : pw.length > 200 ? 'Senha longa demais.' : null;

function readCookie(req, name) {
  const m = (req.headers.get('cookie') || '').match(new RegExp('(?:^|;\\s*)' + name + '=([^;]+)'));
  return m ? m[1] : null;
}
const sessionCookie = (url, token, maxAge) => `arca_sessao=${token}; Path=/; HttpOnly; SameSite=Lax; Max-Age=${maxAge}` + (url.protocol === 'https:' ? '; Secure' : '');

async function startSession(env, url, userId) {
  const token = b64url(crypto.getRandomValues(new Uint8Array(32)));
  await env.DB.prepare('INSERT INTO sessions (token, user_id, created_at, expires_at) VALUES (?, ?, ?, ?)')
    .bind(await sha256(token), userId, now(), new Date(Date.now() + SESSION_DAYS * 864e5).toISOString()).run();
  return sessionCookie(url, token, SESSION_DAYS * 86400);
}
async function currentUser(req, env) {
  const t = readCookie(req, 'arca_sessao');
  if (!t) return null;
  const u = await env.DB.prepare('SELECT u.*, s.token AS session FROM sessions s JOIN users u ON u.id = s.user_id WHERE s.token = ? AND s.expires_at > ?').bind(await sha256(t), now()).first();
  return u || null;
}

function profile(b) {
  const p = { name: clean(b.name), company: clean(b.company), phone: clean(b.phone, 40) };
  if (p.name.length < 2) return { error: 'Informe seu nome.' };
  if (p.company.length < 2) return { error: 'Informe a empresa.' };
  if (p.phone.replace(/\D/g, '').length < 8) return { error: 'Informe um telefone com DDD.' };
  return p;
}

/** Gera o código de uma licença nova para a conta. */
async function createLicense(env, user, ip) {
  for (let i = 0; i < 5; i++) {
    const code = newCode();
    try {
      await env.DB.prepare('INSERT INTO licenses (code, user_id, name, email, company, phone, created_at, created_ip) VALUES (?, ?, ?, ?, ?, ?, ?, ?)')
        .bind(code, user.id, user.name, user.email, user.company, user.phone, now(), ip).run();
      await log(env, code, 'codigo-gerado', null, user.email, ip);
      return code;
    } catch (e) { if (i === 4 || !String(e).includes('UNIQUE')) throw e; }
  }
}

async function signup(req, env, url, ip) {
  const b = await body(req);
  const email = clean(b.email, 160).toLowerCase(), p = profile(b);
  if (p.error) return fail(400, p.error);
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/.test(email)) return fail(400, 'Informe um e-mail válido.');
  const pe = passwordError(b.password); if (pe) return fail(400, pe);
  // Contra robôs: no máximo 5 contas por hora do mesmo endereço.
  const recent = await env.DB.prepare("SELECT COUNT(*) AS n FROM events WHERE kind = 'cadastro' AND ip = ? AND at > ?").bind(ip, since(3600_000)).first();
  if (recent && recent.n >= 5) return fail(429, 'Muitos cadastros deste endereço. Tente de novo mais tarde.');
  if (await env.DB.prepare('SELECT id FROM users WHERE email = ?').bind(email).first()) return fail(409, 'Este e-mail já tem conta. Entre com sua senha.');
  const r = await env.DB.prepare('INSERT INTO users (email, name, company, phone, password, created_at, created_ip) VALUES (?, ?, ?, ?, ?, ?, ?)')
    .bind(email, p.name, p.company, p.phone, await hashPassword(b.password), now(), ip).run();
  const user = { id: r.meta.last_row_id, email, ...p };
  // Código de licença criado junto com a conta: aparece em Minha conta › Licenças.
  const code = await createLicense(env, user, ip);
  await log(env, code, 'cadastro', null, email, ip);
  return json({ ok: true }, 200, { 'set-cookie': await startSession(env, url, user.id) });
}

async function login(req, env, url, ip) {
  const b = await body(req);
  const email = clean(b.email, 160).toLowerCase();
  // Contra tentativa de adivinhar senha: 10 erros em 15 minutos bloqueiam o endereço por um tempo.
  const tries = await env.DB.prepare("SELECT COUNT(*) AS n FROM events WHERE kind = 'login-falhou' AND ip = ? AND at > ?").bind(ip, since(900_000)).first();
  if (tries && tries.n >= 10) return fail(429, 'Muitas tentativas. Aguarde 15 minutos e tente de novo.');
  const user = await env.DB.prepare('SELECT id, password FROM users WHERE email = ?').bind(email).first();
  if (!user || !(await passwordOk(String(b.password ?? ''), user.password))) {
    await log(env, null, 'login-falhou', null, email, ip);
    return fail(401, 'E-mail ou senha incorretos.');
  }
  await log(env, null, 'login', null, email, ip);
  return json({ ok: true }, 200, { 'set-cookie': await startSession(env, url, user.id) });
}

async function account(req, env, url, ip) {
  if (req.method === 'POST' && url.pathname === '/api/account/signup') return signup(req, env, url, ip);
  if (req.method === 'POST' && url.pathname === '/api/account/login') return login(req, env, url, ip);
  const user = await currentUser(req, env);
  if (url.pathname === '/api/account/logout') {
    if (user) await env.DB.prepare('DELETE FROM sessions WHERE token = ?').bind(user.session).run();
    return json({ ok: true }, 200, { 'set-cookie': sessionCookie(url, '', 0) });
  }
  if (!user) return fail(401, 'Entre na sua conta.');

  if (req.method === 'GET' && url.pathname === '/api/account') {
    const rows = await env.DB.prepare('SELECT code, kind, status, created_at, machine_name, activated_at, last_seen, app_version, (machine IS NOT NULL) AS active FROM licenses WHERE user_id = ? ORDER BY id').bind(user.id).all();
    return json({ ok: true, user: { name: user.name, email: user.email, company: user.company, phone: user.phone, created_at: user.created_at }, licenses: rows.results });
  }
  if (req.method !== 'POST') return fail(404, 'Rota não encontrada.');
  const b = await body(req);

  // Remover a licença do computador atual: o código fica livre para ativar em outro.
  if (url.pathname === '/api/account/release') {
    const code = fmtCode(normCode(b.code) || 'XXXXXXXXXXXX');
    const lic = await env.DB.prepare('SELECT machine, machine_name, kind FROM licenses WHERE code = ? AND user_id = ?').bind(code, user.id).first();
    if (!lic) return fail(404, 'Licença não encontrada nesta conta.');
    if (lic.kind === 'ilimitada') return json({ ok: true });
    if (lic.machine) {
      await env.DB.prepare('UPDATE licenses SET machine = NULL, machine_name = NULL, activated_at = NULL WHERE code = ?').bind(code).run();
      await log(env, code, 'conta-remover', lic.machine, lic.machine_name, ip);
    }
    return json({ ok: true });
  }
  // Gerar código: uma licença gratuita por conta.
  if (url.pathname === '/api/account/license') {
    const has = await env.DB.prepare("SELECT COUNT(*) AS n FROM licenses WHERE user_id = ? AND kind <> 'ilimitada'").bind(user.id).first();
    if (has && has.n > 0) return fail(409, 'Sua conta já tem uma licença.');
    return json({ ok: true, code: await createLicense(env, user, ip) });
  }
  if (url.pathname === '/api/account/profile') {
    const p = profile(b); if (p.error) return fail(400, p.error);
    await env.DB.batch([
      env.DB.prepare('UPDATE users SET name = ?, company = ?, phone = ? WHERE id = ?').bind(p.name, p.company, p.phone, user.id),
      env.DB.prepare('UPDATE licenses SET name = ?, company = ?, phone = ? WHERE user_id = ?').bind(p.name, p.company, p.phone, user.id)
    ]);
    return json({ ok: true });
  }
  if (url.pathname === '/api/account/password') {
    if (!(await passwordOk(String(b.current ?? ''), user.password))) return fail(400, 'A senha atual não confere.');
    const pe = passwordError(b.password); if (pe) return fail(400, pe);
    // Troca a senha e encerra as outras sessões (outros navegadores precisam entrar de novo).
    await env.DB.batch([
      env.DB.prepare('UPDATE users SET password = ? WHERE id = ?').bind(await hashPassword(b.password), user.id),
      env.DB.prepare('DELETE FROM sessions WHERE user_id = ? AND token <> ?').bind(user.id, user.session)
    ]);
    await log(env, null, 'senha-trocada', null, user.email, ip);
    return json({ ok: true });
  }
  return fail(404, 'Rota não encontrada.');
}

// ——— Ativação ———
async function activate(req, env, ip) {
  const b = await body(req);
  const raw = normCode(b.code), machine = clean(b.machine, 64).toLowerCase(), machineName = clean(b.machineName, 80), version = clean(b.version, 20);
  if (raw.length !== 12) return fail(400, 'Código inválido. Ele tem o formato ARCA-XXXX-XXXX-XXXX.');
  if (!/^[0-9a-f]{64}$/.test(machine)) return fail(400, 'Identificação do computador inválida.');
  const code = fmtCode(raw);
  const lic = await env.DB.prepare('SELECT * FROM licenses WHERE code = ?').bind(code).first();
  if (!lic) { await log(env, code, 'ativacao-recusada', machine, 'código não existe', ip); return fail(404, 'Código não encontrado. Confira as letras e números.'); }
  if (lic.status !== 'ativa') { await log(env, code, 'ativacao-recusada', machine, 'bloqueada', ip); return fail(403, 'Esta licença está bloqueada. Fale com o suporte.'); }
  const t = now();
  // Licença ilimitada (do dono e dos testes): ativa em quantos computadores quiser; guarda só o último usado.
  if (lic.kind === 'ilimitada') {
    await env.DB.prepare('UPDATE licenses SET machine_name = ?, activated_at = COALESCE(activated_at, ?), last_seen = ?, app_version = ? WHERE code = ?').bind(machineName, t, t, version, code).run();
    await log(env, code, 'ativacao', machine, machineName + ' ' + version, ip);
    return json({ ok: true, license: await sign(env, { v: 1, product: 'ARCA Teleprompter', code, name: lic.name, company: lic.company, email: lic.email, kind: lic.kind, machine, issued: t }) });
  }
  if (lic.machine && lic.machine !== machine) {
    await log(env, code, 'ativacao-recusada', machine, 'outro computador: ' + (lic.machine_name || ''), ip);
    return fail(409, `Este código já está ativado em outro computador${lic.machine_name ? ` (${lic.machine_name})` : ''}. Remova-o de lá em Minha conta › Licenças, no site, e ative aqui de novo.`);
  }
  if (!lic.machine) await env.DB.prepare('UPDATE licenses SET machine = ?, machine_name = ?, activated_at = ?, last_seen = ?, app_version = ? WHERE code = ?').bind(machine, machineName, t, t, version, code).run();
  else await env.DB.prepare('UPDATE licenses SET machine_name = ?, last_seen = ?, app_version = ? WHERE code = ?').bind(machineName, t, version, code).run();
  await log(env, code, lic.machine ? 'reativacao' : 'ativacao', machine, machineName + ' ' + version, ip);
  const license = await sign(env, { v: 1, product: 'ARCA Teleprompter', code, name: lic.name, company: lic.company, email: lic.email, kind: lic.kind, machine, issued: t });
  return json({ ok: true, license });
}

// ——— Desativação (trocar de computador) ———
async function deactivate(req, env, ip) {
  const b = await body(req);
  const code = fmtCode(normCode(b.code) || 'XXXXXXXXXXXX'), machine = clean(b.machine, 64).toLowerCase();
  const lic = await env.DB.prepare('SELECT machine, kind FROM licenses WHERE code = ?').bind(code).first();
  if (!lic) return fail(404, 'Código não encontrado.');
  if (lic.kind === 'ilimitada') { await log(env, code, 'desativacao', machine, null, ip); return json({ ok: true }); }
  if (lic.machine && lic.machine !== machine) return fail(409, 'Esta licença está ativada em outro computador.');
  await env.DB.prepare('UPDATE licenses SET machine = NULL, machine_name = NULL, activated_at = NULL WHERE code = ?').bind(code).run();
  await log(env, code, 'desativacao', machine, null, ip);
  return json({ ok: true });
}

// ——— Conferência periódica ———
async function check(req, env) {
  const b = await body(req);
  const code = fmtCode(normCode(b.code) || 'XXXXXXXXXXXX'), machine = clean(b.machine, 64).toLowerCase();
  const lic = await env.DB.prepare('SELECT status, machine, kind FROM licenses WHERE code = ?').bind(code).first();
  if (!lic) return json({ ok: true, valid: false, reason: 'Licença não encontrada.' });
  if (lic.status !== 'ativa') return json({ ok: true, valid: false, reason: 'Licença bloqueada.' });
  if (lic.kind !== 'ilimitada' && !lic.machine) return json({ ok: true, valid: false, reason: 'A licença foi removida deste computador pela sua conta no site.' });
  if (lic.kind !== 'ilimitada' && lic.machine !== machine) return json({ ok: true, valid: false, reason: 'Licença ativada em outro computador.' });
  await env.DB.prepare('UPDATE licenses SET last_seen = ?, app_version = ? WHERE code = ?').bind(now(), clean(b.version, 20), code).run();
  return json({ ok: true, valid: true });
}

// ——— Administração ———
async function admin(req, env, url, ip) {
  const tok = (req.headers.get('authorization') || '').replace(/^Bearer\s+/i, '');
  if (!sameSecret(tok, env.ADMIN_TOKEN)) return fail(401, 'Token do administrador inválido.');
  if (url.pathname === '/api/admin/licenses') {
    const q = '%' + clean(url.searchParams.get('q') || '', 80) + '%';
    const rows = await env.DB.prepare('SELECT code, name, email, company, phone, kind, status, created_at, machine_name, activated_at, last_seen, app_version, (machine IS NOT NULL) AS active FROM licenses WHERE code LIKE ?1 OR name LIKE ?1 OR email LIKE ?1 OR company LIKE ?1 ORDER BY id DESC LIMIT 500').bind(q).all();
    return json({ ok: true, licenses: rows.results });
  }
  const b = await body(req); const code = fmtCode(normCode(b.code) || 'XXXXXXXXXXXX');
  if (url.pathname === '/api/admin/reset') {
    await env.DB.prepare('UPDATE licenses SET machine = NULL, machine_name = NULL, activated_at = NULL WHERE code = ?').bind(code).run();
    await log(env, code, 'admin-liberar', null, null, ip); return json({ ok: true });
  }
  // Licença ilimitada (dono, equipe, testes): ativa em qualquer computador. Fica na conta do e-mail informado.
  if (url.pathname === '/api/admin/unlimited') {
    const email = clean(b.email, 160).toLowerCase();
    const user = await env.DB.prepare('SELECT * FROM users WHERE email = ?').bind(email).first();
    if (!user) return fail(404, 'Não há conta com este e-mail.');
    const nc = await createLicense(env, user, ip);
    await env.DB.prepare("UPDATE licenses SET kind = 'ilimitada' WHERE code = ?").bind(nc).run();
    await log(env, nc, 'admin-ilimitada', null, email, ip);
    return json({ ok: true, code: nc });
  }
  // Senha esquecida (ainda sem e-mail): o administrador gera uma senha provisória e passa para a pessoa.
  if (url.pathname === '/api/admin/password') {
    const lic = await env.DB.prepare('SELECT user_id FROM licenses WHERE code = ?').bind(code).first();
    if (!lic || !lic.user_id) return fail(404, 'Esta licença não tem conta no site.');
    const temp = Array.from(crypto.getRandomValues(new Uint8Array(10)), x => ALPHABET[x % ALPHABET.length]).join('');
    await env.DB.batch([
      env.DB.prepare('UPDATE users SET password = ? WHERE id = ?').bind(await hashPassword(temp), lic.user_id),
      env.DB.prepare('DELETE FROM sessions WHERE user_id = ?').bind(lic.user_id)
    ]);
    await log(env, code, 'admin-senha', null, null, ip); return json({ ok: true, password: temp });
  }
  if (url.pathname === '/api/admin/status') {
    const st = b.status === 'bloqueada' ? 'bloqueada' : 'ativa';
    await env.DB.prepare('UPDATE licenses SET status = ? WHERE code = ?').bind(st, code).run();
    await log(env, code, 'admin-' + st, null, null, ip); return json({ ok: true });
  }
  return fail(404, 'Rota não encontrada.');
}

export default {
  async fetch(req, env) {
    const url = new URL(req.url);
    const ip = req.headers.get('cf-connecting-ip') || '';
    if (req.method === 'OPTIONS') return new Response(null, { headers: { 'access-control-allow-origin': '*', 'access-control-allow-headers': 'content-type, authorization', 'access-control-allow-methods': 'GET, POST' } });
    try {
      if (req.method === 'GET' && url.pathname === '/download') return await download(env);
      if (req.method === 'GET' && url.pathname === '/api/latest') { const l = await latest(env); return l ? json({ ok: true, version: l.version, file: l.file, size: l.size, date: l.date }) : fail(404, 'Nenhuma versão publicada ainda.'); }
      if (req.method === 'GET' && url.pathname === '/admin') return page(ADMIN);
      if (url.pathname === '/api/account' || url.pathname.startsWith('/api/account/')) {
        // Rotas da conta só aceitam chamadas da própria página (contra outro site agindo com o cookie da pessoa).
        const origin = req.headers.get('origin');
        if (req.method === 'POST' && origin && origin !== url.origin) return fail(403, 'Origem não permitida.');
        return await account(req, env, url, ip);
      }
      if (req.method === 'POST' && url.pathname === '/api/activate') return await activate(req, env, ip);
      if (req.method === 'POST' && url.pathname === '/api/deactivate') return await deactivate(req, env, ip);
      if (req.method === 'POST' && url.pathname === '/api/check') return await check(req, env);
      if (url.pathname.startsWith('/api/admin/')) return await admin(req, env, url, ip);
      return fail(404, 'Não encontrado.');
    } catch (e) {
      console.error(e);
      return fail(500, 'Erro no servidor de licenças. Tente de novo em instantes.');
    }
  }
};

// ——— Última versão (GitHub Releases) ———
const RELEASES = 'https://github.com/cassioalvees/teleprompter/releases';
async function latest(env) {
  const cache = caches.default, key = new Request('https://arca-cache/latest.json');
  const hit = await cache.match(key); if (hit) return hit.json();
  const r = await fetch(RELEASES + '/latest/download/latest.json', { headers: { 'user-agent': 'arca-licencas' }, cf: { cacheTtl: 300 } });
  if (!r.ok) return null;
  const d = await r.json().catch(() => null); if (!d || !d.version || !d.file) return null;
  await cache.put(key, new Response(JSON.stringify(d), { headers: { 'content-type': 'application/json', 'cache-control': 'max-age=300' } }));
  return d;
}
async function download(env) {
  const l = await latest(env);
  return Response.redirect(l ? `${RELEASES}/download/v${l.version}/${l.file}` : RELEASES, 302);
}

const page = html => new Response(html, { headers: { 'content-type': 'text/html; charset=utf-8', 'x-frame-options': 'DENY', 'referrer-policy': 'no-referrer' } });

const STYLE = `
:root{--bg:#0f0f11;--card:#18181b;--line:#2a2a2f;--text:#ececef;--muted:#9a9aa3;--accent:#e5484d;--ok:#30a46c}
*{box-sizing:border-box}body{margin:0;min-height:100vh;background:var(--bg);color:var(--text);font:15px/1.5 system-ui,-apple-system,"Segoe UI",sans-serif;display:flex;align-items:center;justify-content:center;padding:24px}
.card{width:min(460px,100%);background:var(--card);border:1px solid var(--line);border-radius:16px;padding:28px}
.brand{letter-spacing:.3em;font-weight:700;font-size:13px}.eyebrow{color:var(--accent);font-size:11px;font-weight:700;letter-spacing:.12em;text-transform:uppercase;margin:18px 0 4px}
h1{font-size:22px;margin:0 0 6px}p{color:var(--muted);margin:0 0 18px}label{display:block;font-size:12.5px;color:var(--muted);margin:12px 0 5px}
input{width:100%;height:40px;border-radius:10px;border:1px solid var(--line);background:#111114;color:var(--text);padding:0 12px;font:inherit}input:focus{outline:2px solid var(--accent);border-color:transparent}
button{width:100%;height:42px;margin-top:20px;border:0;border-radius:999px;background:var(--accent);color:#fff;font:600 15px system-ui,sans-serif;cursor:pointer}button:disabled{opacity:.6;cursor:default}
.err{color:#ff8a8a;font-size:13.5px;margin-top:12px;min-height:1em}.code{font:600 24px ui-monospace,Consolas,monospace;letter-spacing:.06em;background:#111114;border:1px dashed var(--accent);border-radius:12px;padding:16px;text-align:center;margin:14px 0;user-select:all}
ol{color:var(--muted);padding-left:20px;margin:0}li{margin:4px 0}.small{font-size:12.5px;color:var(--muted)}`;

const ADMIN = `<!doctype html><html lang="pt-BR"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>ARCA · Licenças</title><style>${STYLE}
body{display:block}.card{width:auto;max-width:1200px;margin:0 auto}table{width:100%;border-collapse:collapse;font-size:13px;margin-top:14px}
th,td{text-align:left;padding:8px 6px;border-bottom:1px solid var(--line);vertical-align:top}th{color:var(--muted);font-weight:600}
.row{display:flex;gap:8px;align-items:center}.row input{flex:1}.row button{width:auto;margin:0;padding:0 16px;height:40px}
.mini{width:auto;height:28px;margin:0 4px 4px 0;padding:0 10px;font-size:12px;background:#2a2a2f}.tag{font-size:11px;padding:2px 8px;border-radius:99px;background:#2a2a2f}
.on{background:rgba(48,164,108,.2);color:#7ee0ad}.off{background:rgba(229,72,77,.2);color:#ff9a9a}</style></head><body><main class="card">
<div class="brand">ARCA</div><div class="eyebrow">Administração</div><h1>Licenças</h1>
<div class="row" id="login"><input id="tok" type="password" placeholder="Token do administrador"><button id="enter">Entrar</button></div>
<div id="panel" hidden><div class="row"><input id="q" placeholder="Buscar por nome, e-mail, empresa ou código"><button id="csv">Exportar CSV</button></div>
<div class="small" id="sum"></div><table><thead><tr><th>Código</th><th>Pessoa</th><th>Empresa / telefone</th><th>Situação</th><th>Computador</th><th>Ações</th></tr></thead><tbody id="rows"></tbody></table></div>
<div class="err" id="err"></div></main><script>
const $=s=>document.querySelector(s);let tok=sessionStorage.getItem('arca-admin')||'',data=[];
const esc=s=>String(s??'').replace(/[&<>"]/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[c]));
const api=(p,b)=>fetch(p,{method:b?'POST':'GET',headers:{authorization:'Bearer '+tok,'content-type':'application/json'},body:b?JSON.stringify(b):undefined}).then(r=>r.json());
async function load(){const d=await api('/api/admin/licenses?q='+encodeURIComponent($('#q').value));if(!d.ok){$('#err').textContent=d.error;$('#login').hidden=false;$('#panel').hidden=true;return;}
 sessionStorage.setItem('arca-admin',tok);$('#login').hidden=true;$('#panel').hidden=false;$('#err').textContent='';data=d.licenses;
 $('#sum').textContent=data.length+' licença(s) · '+data.filter(x=>x.active).length+' ativada(s)';
 $('#rows').innerHTML=data.map(x=>'<tr><td><code>'+esc(x.code)+'</code><div class="small">'+esc((x.created_at||'').slice(0,10))+'</div></td><td>'+esc(x.name)+'<div class="small">'+esc(x.email)+'</div></td><td>'+esc(x.company)+'<div class="small">'+esc(x.phone)+'</div></td><td><span class="tag '+(x.status==='ativa'?'on':'off')+'">'+esc(x.status)+'</span></td><td>'+(x.active?esc(x.machine_name)+'<div class="small">'+esc((x.activated_at||'').slice(0,10))+' · v'+esc(x.app_version)+'</div>':'<span class="small">livre</span>')+'</td><td>'
  +(x.active?'<button class="mini" data-a="reset" data-c="'+esc(x.code)+'">Liberar computador</button>':'')
  +'<button class="mini" data-a="senha" data-c="'+esc(x.code)+'">Senha provisória</button>'
  +'<button class="mini" data-a="'+(x.status==='ativa'?'bloqueada':'ativa')+'" data-c="'+esc(x.code)+'">'+(x.status==='ativa'?'Bloquear':'Desbloquear')+'</button></td></tr>').join('');}
$('#enter').onclick=()=>{tok=$('#tok').value.trim();load();};$('#q').oninput=()=>load();
$('#rows').onclick=async e=>{const b=e.target.closest('button');if(!b)return;const a=b.dataset.a,c=b.dataset.c;
 if(a==='senha'){if(!confirm('Gerar uma senha provisória para a conta de '+c+'? A senha atual deixa de valer.'))return;const d=await api('/api/admin/password',{code:c});alert(d.ok?'Senha provisória: '+d.password+'\\n\\nPasse para a pessoa: ela entra em Minha conta e troca em Meus dados.':d.error);return;}
 if(a==='reset'){if(!confirm('Liberar '+c+' para ser ativado em outro computador?'))return;await api('/api/admin/reset',{code:c});}
 else{if(!confirm((a==='bloqueada'?'Bloquear ':'Desbloquear ')+c+'?'))return;await api('/api/admin/status',{code:c,status:a});}load();};
$('#csv').onclick=()=>{const h=['code','name','email','company','phone','status','machine_name','activated_at','last_seen','app_version'];
 const csv=[h.join(';')].concat(data.map(x=>h.map(k=>'"'+String(x[k]??'').replace(/"/g,'""')+'"').join(';'))).join('\\r\\n');
 const a=document.createElement('a');a.href=URL.createObjectURL(new Blob(['\\ufeff'+csv],{type:'text/csv'}));a.download='licencas-arca.csv';a.click();};
if(tok)load();
</script></body></html>`;
