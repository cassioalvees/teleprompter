// Janela de prompter num monitor (Electron) ou pop-up (web). Só desenha; o editor manda.
import React, { useEffect, useRef, useState } from 'react';
import type { BusMsg, OutputSnapshot } from '../platform/types';
import { OutputView, type YFeed } from './OutputView';
import { localTransport } from './transport';

export function OutputPage({ id }: { id: string }) {
  const [snap, setSnap] = useState<OutputSnapshot | null>(null);
  const [html, setHtml] = useState('');
  const [idle, setIdle] = useState(false);
  const feed = useRef<YFeed>({ y: 0, v: 0, at: performance.now() });

  useEffect(() => {
    const tr = localTransport();
    tr.onMsg((m: BusMsg) => {
      if (m.t === 'y') feed.current = { y: m.y, v: m.v, at: performance.now() };
      else if (m.t === 'html') setHtml(m.html);
      else if (m.t === 'state') setSnap(m.s);
    });
    tr.send({ t: 'hello' });
    // Teclas nesta janela (ex.: clicker) controlam o editor.
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'F11' || (e.key === 'Escape' && document.fullscreenElement)) return;
      e.preventDefault();
      tr.send({ t: 'key', key: e.key, shiftKey: e.shiftKey, ctrlKey: e.ctrlKey || e.metaKey, altKey: e.altKey });
    };
    window.addEventListener('keydown', onKey);
    let t: any = 0;
    const onMove = () => { setIdle(false); clearTimeout(t); t = setTimeout(() => setIdle(true), 2000); };
    window.addEventListener('mousemove', onMove); onMove();
    return () => { window.removeEventListener('keydown', onKey); window.removeEventListener('mousemove', onMove); tr.close && tr.close(); };
  }, []);

  useEffect(() => { document.title = 'ARCA Teleprompter · Saída'; }, []);
  const toggleFs = () => { if (document.fullscreenElement) document.exitFullscreen().catch(() => {}); else document.documentElement.requestFullscreen().catch(() => {}); };

  if (!snap) return <div style={{ position: 'fixed', inset: 0, background: '#000', color: '#777', display: 'flex', alignItems: 'center', justifyContent: 'center', font: '13px var(--f-sans)' }}>Aguardando o editor…</div>;
  const d = snap.displays.find(x => x.id === id) || { flipH: false, flipV: false, align: snap.align, find: false };
  return (
    <div data-theme="dark" onDoubleClick={toggleFs} style={{ position: 'fixed', inset: 0, cursor: idle ? 'none' : 'default' }} title="Duplo clique: tela cheia">
      <OutputView snap={snap} html={html} feed={feed} flipH={d.flipH} flipV={d.flipV} align={d.align} find={d.find} hud />
    </div>
  );
}
