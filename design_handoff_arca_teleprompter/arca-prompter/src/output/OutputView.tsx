// Saída do prompter desenhada a partir de um snapshot do editor: janelas em outros monitores
// (Electron), pop-ups (web) e o monitor web. Mesmo "espaço de 1000 unidades" do editor,
// então a quebra de linha é idêntica em todas as saídas.
import React, { useEffect, useLayoutEffect, useRef } from 'react';
import type { OutputSnapshot } from '../platform/types';
import { applyWhoVars } from '../shared/who';

export interface YFeed { y: number; v: number; at: number }

interface Props {
  snap: OutputSnapshot;
  html: string;
  feed: React.MutableRefObject<YFeed>;
  flipH?: boolean; flipV?: boolean; align?: string;
  /** Moldura vermelha piscando ("Localizar" na lista de telas). */
  find?: boolean;
  /** HUD do talento (bloco, tempo restante, relógio, velocidade, contagem). */
  hud?: boolean;
  /** Ocupa o pai em vez da janela inteira (monitor web). */
  contained?: boolean;
}

export function OutputView({ snap: s, html, feed, flipH, flipV, align, find, hud, contained }: Props) {
  const viewRef = useRef<HTMLDivElement>(null), textRef = useRef<HTMLDivElement>(null);
  const readPct = useRef(s.readPct); readPct.current = s.readPct;

  useLayoutEffect(() => { if (textRef.current) textRef.current.innerHTML = html; }, [html]);
  useEffect(() => { applyWhoVars(document.documentElement, s.pres, s.presNames, s.presOnly); }, [JSON.stringify([s.pres, s.presNames, s.presOnly])]);

  useEffect(() => {
    let raf = 0;
    const loop = () => {
      const v = viewRef.current, c = textRef.current;
      if (v && c) {
        const f = feed.current, k = v.clientWidth / 1000;
        // Extrapola a posição com a velocidade recebida (no máximo 0,25 s sem notícias do editor).
        const dt = Math.min(0.25, (performance.now() - f.at) / 1000);
        const last = c.lastElementChild as HTMLElement | null;
        const end = last ? last.offsetTop + last.offsetHeight : Infinity;
        const y = Math.max(0, Math.min(end, f.y + f.v * dt));
        if (k) c.style.transform = `scale(${k}) translateY(${((v.clientHeight / k) * (readPct.current / 100) - y).toFixed(2)}px)`;
      }
      raf = requestAnimationFrame(loop);
    };
    raf = requestAnimationFrame(loop);
    return () => cancelAnimationFrame(raf);
  }, [feed]);

  const rp = s.readPct, flip = `scale(${flipH ? -1 : 1},${flipV ? -1 : 1})`;
  const abs = { position: 'absolute', left: 0, right: 0, pointerEvents: 'none' } as const;
  return (
    <div style={{ position: contained ? 'absolute' : 'fixed', inset: 0, background: s.bg, overflow: 'hidden' }}>
      {s.bgImg ? <div style={{ position: 'absolute', inset: 0, backgroundImage: `url('${s.bgImg}')`, backgroundSize: 'cover', backgroundPosition: 'center', opacity: s.bgImgOp }} /> : null}
      <div ref={viewRef} style={{ position: 'absolute', inset: 0, overflow: 'hidden', containerType: 'inline-size' } as any}>
        <div style={{ position: 'absolute', inset: 0, transform: flip }}>
          {s.readingLine ? <div style={{ ...abs, top: `calc(${rp}% + ${s.halfLineCq})`, height: 2, background: s.indColor, opacity: 0.3 }} /> : null}
          <div ref={textRef} data-pr="1" data-script="1" style={{ position: 'absolute', left: 0, top: 0, width: 1000, boxSizing: 'border-box', transformOrigin: '0 0', padding: `0 ${s.prRight}px 0 ${s.prMargin}px`, fontFamily: s.fontFam, fontSize: s.fontSize, lineHeight: s.lineH, color: s.fg, textAlign: (align || s.align) as any, willChange: 'transform' }} />
          {s.ovType === 'faixa' ? <>
            <div style={{ ...abs, top: 0, height: `${rp}%`, background: s.dimColor }} />
            <div style={{ ...abs, bottom: 0, top: `calc(${rp}% + ${s.lineCq})`, background: s.dimColor }} />
          </> : null}
          {s.ovType === 'fade' ? <>
            <div style={{ ...abs, top: 0, height: `${rp}%`, background: `linear-gradient(${s.dimColor},transparent)` }} />
            <div style={{ ...abs, bottom: 0, top: `calc(${rp}% + ${s.lineCq})`, background: `linear-gradient(transparent,${s.dimColor})` }} />
          </> : null}
          {s.ovType === 'destaque' ? <div style={{ ...abs, top: `${rp}%`, height: s.lineCq, background: s.hiColor }} /> : null}
          <div style={{ position: 'absolute', left: s.indLeft, top: `calc(${rp}% + ${s.halfLineCq})`, transform: 'translateY(-50%)', width: s.indW, height: s.indH, background: s.indColor, clipPath: s.indClip, borderRadius: s.indRadius, display: s.indDisplay, pointerEvents: 'none' }} />
        </div>
      </div>
      <div style={{ position: 'absolute', inset: 0, boxShadow: 'inset 0 0 0 6px #e5484d', opacity: find && s.blink ? 1 : 0, pointerEvents: 'none' }} />
      {/* O HUD acompanha o espelho da tela, para ser lido corretamente no vidro. */}
      <div style={{ position: 'absolute', inset: 0, transform: flip, pointerEvents: 'none' }}>
      {hud && s.hud ? (
        <div style={{ position: 'absolute', left: 20, right: 20, top: 60, display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', gap: 16, pointerEvents: 'none', zIndex: 3, color: '#fff' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 10, minWidth: 0, padding: '8px 14px', borderRadius: 12, background: 'rgba(0,0,0,.62)' }}>
            {s.isLastBlock ? <span style={{ flex: 'none', padding: '3px 8px', borderRadius: 980, background: '#e5484d', font: '600 10px var(--f-num)', letterSpacing: '1.2px' }}>ÚLTIMO BLOCO</span> : null}
            <span style={{ font: '600 13px var(--f-sans)', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>{s.hudBlock}</span>
            <span style={{ flex: 'none', font: '600 15px var(--f-num)', color: s.blockLeftColor }}>{s.blockLeft}</span>
          </div>
          <div style={{ flex: 'none', padding: '8px 14px', borderRadius: 12, background: 'rgba(0,0,0,.62)', font: '600 15px var(--f-num)' }}>{s.clock}</div>
        </div>
      ) : null}
      {hud && s.flashText ? (
        <div style={{ position: 'absolute', left: '50%', bottom: 76, transform: 'translateX(-50%)', maxWidth: 'calc(100% - 32px)', display: 'flex', alignItems: 'center', gap: 10, padding: '7px 14px 7px 8px', borderRadius: 980, background: 'rgba(20,20,22,.82)', color: '#fff', pointerEvents: 'none', zIndex: 4 }}>
          {s.flashSub ? <span style={{ display: 'flex', alignItems: 'center', gap: 5, height: 20, padding: '0 9px', borderRadius: 980, background: s.flashSub === 'Ré' ? '#e5484d' : 'rgba(255,255,255,.16)', font: '600 10px var(--f-num)', letterSpacing: '1.2px', textTransform: 'uppercase' }}>
            <svg viewBox="0 0 16 16" style={{ width: 9, height: 9, fill: 'none', stroke: 'currentColor', strokeWidth: 2.2, strokeLinecap: 'round', strokeLinejoin: 'round' }}><path d={s.flashSub === 'Ré' ? 'M10 3L4 8l6 5' : 'M6 3l6 5-6 5'} /></svg>{s.flashSub}
          </span> : null}
          <span style={{ font: '600 13px var(--f-num)', whiteSpace: 'nowrap' }}>{s.flashText}</span>
          {s.flashPct >= 0 ? <div style={{ width: 72, height: 3, borderRadius: 2, background: 'rgba(255,255,255,.2)', flex: 'none' }}><div style={{ height: '100%', width: Math.round(s.flashPct * 100) + '%', borderRadius: 2, background: '#fff' }} /></div> : null}
        </div>
      ) : null}
      {hud && s.count > 0 ? (
        <div style={{ position: 'absolute', inset: 0, display: 'flex', alignItems: 'center', justifyContent: 'center', pointerEvents: 'none' }}>
          <div style={{ width: 160, height: 160, borderRadius: '50%', background: 'rgba(0,0,0,.55)', display: 'flex', alignItems: 'center', justifyContent: 'center', font: '500 84px var(--f-num)', color: '#fff' }}>{s.count}</div>
        </div>
      ) : null}
      </div>
    </div>
  );
}
