// Página /monitor servida pelo app (porta 8420): espelha o prompter em qualquer navegador da rede
// e, se permitido no editor, vira controle remoto (celular/tablet).
import React, { useEffect, useRef, useState } from 'react';
import type { BusCmd, OutputSnapshot } from '../platform/types';
import { OutputView, type YFeed } from './OutputView';
import { wsTransport } from './transport';

const btn = (extra: React.CSSProperties = {}): React.CSSProperties => ({ height: 48, borderRadius: 12, border: 'none', background: 'var(--t-soft)', color: 'var(--text)', font: '600 14px var(--f-sans)', cursor: 'pointer', ...extra });
const eyebrow: React.CSSProperties = { fontFamily: 'var(--f-num)', fontSize: 10, letterSpacing: '1.5px', textTransform: 'uppercase', color: 'var(--t-faint)' };

export function MonitorPage() {
  const [snap, setSnap] = useState<OutputSnapshot | null>(null);
  const [html, setHtml] = useState('');
  const [conn, setConn] = useState(false);
  const [auth, setAuth] = useState<{ ok: boolean; needPass: boolean } | null>(null);
  const [pass, setPass] = useState(() => { try { return sessionStorage.getItem('arca-mon-pass') || ''; } catch (e) { return ''; } });
  const feed = useRef<YFeed>({ y: 0, v: 0, at: performance.now() });
  const tr = useRef<ReturnType<typeof wsTransport> | null>(null);

  useEffect(() => {
    document.title = 'ARCA Prompter · Monitor';
    // O CSS do editor trava a rolagem; no celular o monitor precisa rolar.
    document.documentElement.style.overflow = 'auto'; document.body.style.overflow = 'auto'; document.body.style.height = 'auto';
    tr.current = wsTransport(pass, {
      onState: setConn,
      onAuth: (ok, needPass) => setAuth({ ok, needPass }),
      onMsg: m => {
        if (m.t === 'y') feed.current = { y: m.y, v: m.v, at: performance.now() };
        else if (m.t === 'html') setHtml(m.html);
        else if (m.t === 'state') setSnap(m.s);
      }
    });
    return () => { tr.current?.close?.(); };
  }, []);

  const send = (c: BusCmd) => tr.current && tr.current.send(c);
  const login = (e: React.FormEvent) => { e.preventDefault(); try { sessionStorage.setItem('arca-mon-pass', pass); } catch (er) { /* */ } tr.current && tr.current.setPass(pass); };

  const shell = (body: React.ReactNode) => (
    <div data-theme="dark" style={{ minHeight: '100vh', background: 'var(--bg)', color: 'var(--text)', font: '500 13px var(--f-sans)', display: 'flex', flexDirection: 'column' }}>
      <div style={{ height: 44, flex: 'none', display: 'flex', alignItems: 'center', gap: 12, padding: '0 16px', background: 'var(--t-ui)', borderBottom: '1px solid var(--t-border)' }}>
        <span style={{ fontFamily: 'var(--f-brand)', fontSize: 15, letterSpacing: '3.5px' }}>ARCA</span>
        <div style={{ width: 1, height: 16, background: 'var(--t-border)' }} />
        <span style={{ fontWeight: 600, fontSize: 12.5 }}>Monitor web</span>
        <span style={{ flex: 1 }} />
        <span style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 12, color: 'var(--t-muted)' }}><span style={{ width: 7, height: 7, borderRadius: '50%', background: conn ? 'var(--t-ok)' : '#e5484d' }} />{conn ? 'Conectado' : 'Reconectando…'}</span>
      </div>
      {body}
    </div>
  );

  if (auth && !auth.ok) return shell(
    <form onSubmit={login} style={{ margin: '60px auto', width: 'min(340px, calc(100% - 32px))', display: 'flex', flexDirection: 'column', gap: 12, padding: 20, borderRadius: 14, background: 'var(--t-ui)', border: '1px solid var(--t-border)', boxShadow: 'var(--t-sh3)' }}>
      <span style={{ fontWeight: 600, fontSize: 15 }}>Senha do monitor</span>
      <input type="password" value={pass} onChange={e => setPass(e.target.value)} autoFocus style={{ height: 36, borderRadius: 10, border: '1px solid transparent', background: 'var(--field)', color: 'var(--text)', padding: '0 10px' }} />
      {pass && auth.needPass ? <span style={{ fontSize: 12, color: '#e5484d' }}>Senha incorreta.</span> : null}
      <button type="submit" style={btn({ height: 36, background: 'var(--t-accent)', color: 'var(--t-accent-ink)' })}>Entrar</button>
    </form>
  );
  if (!snap) return shell(<div style={{ margin: 'auto', color: 'var(--t-muted)' }}>Aguardando o prompter…</div>);

  const live = snap.live;
  return shell(
    <div style={{ flex: 1, display: 'flex', flexDirection: 'column', gap: 14, padding: 14, width: '100%', maxWidth: 1100, margin: '0 auto' }}>
      <div style={{ position: 'relative', width: '100%', aspectRatio: '16/9', overflow: 'hidden', borderRadius: 11, background: snap.bg }}>
        <OutputView snap={snap} html={html} feed={feed} contained />
        <div style={{ position: 'absolute', left: 10, top: 10, padding: '3px 9px', borderRadius: 980, background: live ? '#e5484d' : 'rgba(0,0,0,.6)', color: '#fff', font: '600 10px var(--f-num)', letterSpacing: 1 }}>{live ? 'NO AR' : 'AO VIVO · MONITOR'}</div>
      </div>
      <div style={{ display: 'flex', alignItems: 'center', gap: 20, flexWrap: 'wrap' }}>
        <div style={{ display: 'flex', flexDirection: 'column', minWidth: 0 }}><span style={eyebrow}>Bloco</span><span style={{ fontWeight: 600 }}>{snap.curName}</span></div>
        <div style={{ display: 'flex', flexDirection: 'column' }}><span style={eyebrow}>Posição</span><span style={{ font: '600 14px var(--f-num)' }}>{snap.stamp}</span></div>
        <div style={{ display: 'flex', flexDirection: 'column' }}><span style={eyebrow}>Velocidade</span><span style={{ font: '600 14px var(--f-num)' }}>{snap.speed}</span></div>
        <div style={{ display: 'flex', flexDirection: 'column' }}><span style={eyebrow}>Relógio</span><span style={{ font: '600 14px var(--f-num)' }}>{snap.clock}</span></div>
        <span style={{ flex: 1 }} />
        {!snap.webCtrl ? <span style={{ fontSize: 12, color: 'var(--t-faint)' }}>Somente visualização</span> : null}
      </div>
      {snap.webCtrl ? (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 8, maxWidth: 520, width: '100%', margin: '0 auto' }}>
          <button onClick={() => send({ t: 'togglePlay' })} style={btn({ height: 64, background: live ? '#e5484d' : 'var(--t-accent)', color: '#fff', fontSize: 16 })}>{live ? 'Pausar' : 'Play'}</button>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 8 }}>
            <button onClick={() => send({ t: 'action', name: 'slower' })} style={btn({ fontSize: 20 })}>−</button>
            <button onClick={() => send({ t: 'action', name: 'faster' })} style={btn({ fontSize: 20 })}>+</button>
            <button onClick={() => send({ t: 'action', name: 'prevBm' })} style={btn()}>Marcador anterior</button>
            <button onClick={() => send({ t: 'action', name: 'nextBm' })} style={btn()}>Próximo marcador</button>
            <button onClick={() => send({ t: 'action', name: 'prevPara' })} style={btn({ height: 40 })}>Parágrafo anterior</button>
            <button onClick={() => send({ t: 'action', name: 'nextPara' })} style={btn({ height: 40 })}>Próximo parágrafo</button>
          </div>
          <button onClick={() => send({ t: 'action', name: 'rewind' })} style={btn({ height: 40, borderRadius: 980 })}>Voltar ao início</button>
        </div>
      ) : null}
    </div>
  );
}
