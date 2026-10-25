// Converte o template <x-dc> de um protótipo (.dc.html) em JSX.
// Uso único por protótipo: o resultado passa a ser o código-fonte mantido (e já foi editado à mão).
// Rodar de novo SOBRESCREVE os arquivos de saída.
//   node scripts/convert-template.mjs                       → Teleprompter.dc.html → src/editor/Template.tsx
//   node scripts/convert-template.mjs setup                 → Instalador.dc.html   → src/setup/SetupTemplate.tsx
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { parseDocument } from 'htmlparser2';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const TARGETS = {
  editor: { src: '../Teleprompter.dc.html', tsx: 'src/editor/Template.tsx', css: 'src/styles/proto.css', hover: 'src/styles/hover.css', prefix: '' },
  setup: { src: '../Instalador.dc.html', tsx: 'src/setup/SetupTemplate.tsx', css: 'src/setup/setup.css', hover: 'src/setup/setup-hover.css', prefix: 's' }
};
const T = TARGETS[process.argv[2] || 'editor'];
const src = readFileSync(resolve(root, T.src), 'utf8');

let tpl = src.slice(src.indexOf('<x-dc>') + 6, src.indexOf('</x-dc>'));
// O protótipo usa aspas duplas dentro de style="" (url("{{ bgImg }}")), o que quebra o atributo.
tpl = tpl.replace(/url\("\{\{ bgImg \}\}"\)/g, "url('{{ bgImg }}')");

const helmet = tpl.slice(tpl.indexOf('<helmet>') + 8, tpl.indexOf('</helmet>'));
const css = helmet.slice(helmet.indexOf('<style>') + 7, helmet.indexOf('</style>')).trim();
tpl = tpl.slice(tpl.indexOf('</helmet>') + 9);

const dom = parseDocument(tpl, { lowerCaseAttributeNames: false, lowerCaseTags: false, recognizeSelfClosing: true, decodeEntities: true });

const ATTR = { readonly: 'readOnly', autofocus: 'autoFocus', autoplay: 'autoPlay', playsinline: 'playsInline', spellcheck: 'spellCheck', maxlength: 'maxLength', class: 'className', for: 'htmlFor', contenteditable: 'contentEditable' };
const BOOL = new Set(['readOnly', 'autoFocus', 'autoPlay', 'muted', 'playsInline']);
const VOID = new Set(['input', 'br', 'img', 'hr', 'meta', 'link']);
const hoverRules = [];
const hoverIdx = new Map();

function expr(path, scope) {
  path = path.trim();
  if (path === 'true' || path === 'false') return path;
  const head = path.split(/[.[]/)[0];
  return scope.includes(head) ? path : 'v.' + path;
}
// "abc {{ x }} def" -> `abc ${v.x} def`
function interp(raw, scope) {
  const whole = raw.match(/^\s*\{\{([^}]+)\}\}\s*$/);
  if (whole) return { code: expr(whole[1], scope), dynamic: true, whole: true };
  if (!raw.includes('{{')) return { code: JSON.stringify(raw), dynamic: false };
  const parts = raw.split(/\{\{([\s\S]+?)\}\}/g);
  const code = '`' + parts.map((s, i) => (i & 1) ? '${' + expr(s, scope) + ' ?? ""}' : s.replace(/[`\\$]/g, c => '\\' + c)).join('') + '`';
  return { code, dynamic: true };
}
function splitDecls(css) {
  const out = []; let depth = 0, cur = '', inT = 0;
  for (let i = 0; i < css.length; i++) {
    const c = css[i];
    if (css.startsWith('{{', i)) inT++;
    if (css.startsWith('}}', i)) inT--;
    if (c === '(') depth++;
    if (c === ')') depth--;
    if (c === ';' && !depth && !inT) { out.push(cur); cur = ''; continue; }
    cur += c;
  }
  if (cur.trim()) out.push(cur);
  return out;
}
const camel = p => p.startsWith('--') ? p : p.replace(/-([a-z])/g, (_, c) => c.toUpperCase());
function styleObj(raw, scope) {
  const props = [];
  for (const d of splitDecls(raw)) {
    const i = d.indexOf(':'); if (i < 0) continue;
    const k = d.slice(0, i).trim(), val = d.slice(i + 1).trim();
    const { code } = interp(val, scope);
    props.push(`${JSON.stringify(camel(k))}: ${code}`);
  }
  return '{ ' + props.join(', ') + ' }';
}
function pseudoClass(kind, raw) {
  const key = kind + '|' + raw;
  if (!hoverIdx.has(key)) {
    const cls = T.prefix + (kind === 'hover' ? 'h' : 'a') + hoverIdx.size;
    hoverIdx.set(key, cls);
    const decls = splitDecls(raw).map(d => d.trim()).filter(Boolean).map(d => d + ' !important').join(';');
    hoverRules.push(`.${cls}:${kind}{${decls}}`);
  }
  return hoverIdx.get(key);
}
function textJsx(t, scope) {
  if (!t.trim()) return t.includes('\n') ? '' : (t ? '{" "}' : '');
  const parts = t.split(/\{\{([\s\S]+?)\}\}/g);
  return parts.map((s, i) => (i & 1) ? '{' + expr(s, scope) + '}' : (s ? (/[{}<>]/.test(s) ? '{' + JSON.stringify(s) + '}' : s.replace(/\s+/g, ' ')) : '')).join('');
}
function attrsJsx(el, scope) {
  const out = []; const cls = [];
  for (const [name, value] of Object.entries(el.attribs)) {
    if (name.startsWith('hint-')) continue;
    if (name === 'style') { out.push(`style={${styleObj(value, scope)}}`); continue; }
    if (name === 'style-hover') { cls.push(pseudoClass('hover', value)); continue; }
    if (name === 'style-active') { cls.push(pseudoClass('active', value)); continue; }
    const jn = ATTR[name] || name;
    if (BOOL.has(jn) && !value.includes('{{')) { out.push(jn); continue; }
    if (jn === 'spellCheck') { out.push(`spellCheck={${value === 'true'}}`); continue; }
    if (jn === 'maxLength') { out.push(`maxLength={${+value}}`); continue; }
    const r = interp(value, scope);
    out.push(r.dynamic ? `${jn}={${r.code}}` : `${jn}=${r.code}`);
  }
  if (cls.length) out.push(`className="${cls.join(' ')}"`);
  return out.length ? ' ' + out.join(' ') : '';
}
let loopN = 0;
function node(n, scope, ind) {
  const pad = '  '.repeat(ind);
  if (n.type === 'text') { const t = textJsx(n.data, scope); return t ? pad + t : ''; }
  if (n.type !== 'tag') return '';
  const kids = c => c.children.map(k => node(k, scope, ind + 1)).filter(Boolean).join('\n');
  if (n.name === 'sc-for') {
    const list = n.attribs.list.match(/\{\{([\s\S]+?)\}\}/)[1].trim(), as = n.attribs.as, ix = 'i' + (loopN++);
    const inner = n.children.map(k => node(k, [...scope, as], ind + 2)).filter(Boolean).join('\n');
    return `${pad}{(${expr(list, scope)} || []).map((${as}: any, ${ix}: number) => (\n${pad}  <React.Fragment key={${ix}}>\n${inner}\n${pad}  </React.Fragment>\n${pad}))}`;
  }
  if (n.name === 'sc-if') {
    const cond = n.attribs.value.match(/\{\{([\s\S]+?)\}\}/)[1].trim();
    return `${pad}{${expr(cond, scope)} ? (\n${pad}  <>\n${kids(n)}\n${pad}  </>\n${pad}) : null}`;
  }
  const a = attrsJsx(n, scope);
  if (VOID.has(n.name) || !n.children.length) return `${pad}<${n.name}${a} />`;
  return `${pad}<${n.name}${a}>\n${kids(n)}\n${pad}</${n.name}>`;
}

const body = dom.children.map(k => node(k, [], 2)).filter(Boolean).join('\n');
const tsx = `// @ts-nocheck — GERADO por scripts/convert-template.mjs a partir de ${T.src.slice(3)} e depois mantido à mão.
/* eslint-disable */
import React from 'react';

export function renderTemplate(v: any) {
  return (
    <>
${body}
    </>
  );
}
`;
for (const p of [T.tsx, T.css, T.hover]) mkdirSync(dirname(resolve(root, p)), { recursive: true });
writeFileSync(resolve(root, T.tsx), tsx);
writeFileSync(resolve(root, T.css), '/* Estilos globais do protótipo (bloco <helmet>). */\n' + css.replace(/@font-face\{[^}]*\}\n?/, '') + '\n');
writeFileSync(resolve(root, T.hover), '/* Estados :hover / :active do protótipo (atributos style-hover / style-active). */\n' + hoverRules.join('\n') + '\n');
console.log(T.tsx + ':', tsx.length, 'bytes ·', hoverRules.length, 'regras de hover');
