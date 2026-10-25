// @ts-nocheck — JSX gerado do template do protótipo (estilos inline com CSS vars e chaves repetidas como no original).
// GERADO por scripts/convert-template.mjs a partir de Teleprompter.dc.html e depois mantido à mão.
/* eslint-disable */
import React from 'react';

export function renderTemplate(v: any) {
  return (
    <>
    <div data-theme={v.theme} style={{ "height": "100vh", "display": "flex", "flexDirection": "column", "font": "500 13px var(--f-sans)", "color": "var(--text)", "background": "var(--bg)", "userSelect": "none" }}>
      <div style={{ "height": "36px", "flex": "none", "display": "flex", "alignItems": "center", "background": "var(--t-ui)", "borderBottom": "1px solid var(--t-hair)", "WebkitAppRegion": "drag" } as any}>
        <div style={{ "flex": "1", "minWidth": "0", "display": "flex", "alignItems": "center", "gap": "12px", "padding": "0 14px" }}>
          <span style={{ "fontFamily": "var(--f-brand)", "fontSize": "15px", "letterSpacing": "3.5px", "lineHeight": "1" }}>
            ARCA
          </span>
          <div style={{ "width": "1px", "height": "16px", "background": "var(--t-border)" }} />
          <span style={{ "fontWeight": "600", "fontSize": "12.5px" }}>
            Teleprompter
          </span>
          <span style={{ "fontSize": "12px", "color": "var(--t-muted)", "whiteSpace": "nowrap", "overflow": "hidden", "textOverflow": "ellipsis" }}>
            {v.docTitle}
          </span>
        </div>
        <div style={{ "display": v.winCtl, "height": "100%", "WebkitAppRegion": "no-drag" } as any}>
          <div onClick={v.winMin} title="Minimizar" style={{ "width": "46px", "display": "flex", "alignItems": "center", "justifyContent": "center" }} className="h0">
            <div style={{ "width": "10px", "height": "1px", "background": "var(--text)" }} />
          </div>
          <div onClick={v.winMax} title="Maximizar" style={{ "width": "46px", "display": "flex", "alignItems": "center", "justifyContent": "center" }} className="h0">
            <div style={{ "width": "9px", "height": "9px", "border": "1px solid var(--text)", "borderRadius": "2px" }} />
          </div>
          <div onClick={v.winClose} title="Fechar" style={{ "width": "46px", "display": "flex", "alignItems": "center", "justifyContent": "center", "color": "var(--text)" }} className="h1">
            <svg viewBox="0 0 16 16" style={{ "width": "10px", "height": "10px", "fill": "none", "stroke": "currentColor", "strokeWidth": "1.4" }}>
              <path d="M2 2l12 12M14 2L2 14" />
            </svg>
          </div>
        </div>
      </div>
      <div style={{ "height": "32px", "flex": "none", "display": "flex", "alignItems": "center", "gap": "2px", "padding": "0 8px", "background": "var(--t-ui)", "position": "relative", "zIndex": "75" }}>
        {(v.menus || []).map((m: any, i0: number) => (
          <React.Fragment key={i0}>
            <div style={{ "position": "relative" }}>
              <div onClick={m.onClick} onMouseEnter={m.onEnter} style={{ "height": "26px", "padding": "0 10px", "display": "flex", "alignItems": "center", "borderRadius": "8px", "background": m.bg, "cursor": "default" }} className="h0">
                {m.label}
              </div>
              {m.open ? (
                <>
                <div style={{ "position": "absolute", "top": "30px", "left": "0", "minWidth": "270px", "width": "max-content", "maxWidth": "min(440px,calc(100vw - 16px))", "padding": "6px", "zIndex": "50", "background": "var(--t-ui)", "border": "1px solid var(--t-border)", "borderRadius": "12px", "boxShadow": "var(--t-sh2)" }}>
                  {(m.items || []).map((it: any, i1: number) => (
                    <React.Fragment key={i1}>
                      {it.sep ? (
                        <>
                        <div style={{ "height": "1px", "background": "var(--line)", "margin": "4px 6px" }} />
                        </>
                      ) : null}
                      {it.isItem ? (
                        <>
                        <div onClick={it.onClick} style={{ "display": "grid", "gridTemplateColumns": "18px minmax(0,1fr) auto", "gap": "6px", "alignItems": "center", "height": "30px", "padding": "0 10px 0 6px", "borderRadius": "8px", "cursor": "default" }} className="h0">
                          <span style={{ "fontSize": "12px", "color": "var(--muted)" }}>
                            {it.check}
                          </span>
                          <span style={{ "whiteSpace": "nowrap" }}>
                            {it.label}
                          </span>
                          <span style={{ "color": "var(--faint)", "fontSize": "12px", "paddingLeft": "24px" }}>
                            {it.key}
                          </span>
                        </div>
                        </>
                      ) : null}
                    </React.Fragment>
                  ))}
                </div>
                </>
              ) : null}
            </div>
          </React.Fragment>
        ))}
      </div>
      {v.presMenu ? (
        <>
        <div onMouseDown={v.closePresMenu} style={{ "position": "fixed", "inset": "0", "zIndex": "4" }} />
        </>
      ) : null}
      {v.anyMenu ? (
        <>
        <div onClick={v.closeMenus} style={{ "position": "fixed", "inset": "0", "zIndex": "70" }} />
        </>
      ) : null}
      <div style={{ "height": "50px", "flex": "none", "display": "flex", "alignItems": "center", "gap": "8px", "padding": "0 12px", "background": "var(--t-ui)", "borderBottom": "1px solid var(--t-border)", "boxShadow": "var(--t-sh1)", "position": "relative", "zIndex": "5" }}>
        <div style={{ "flex": "1", "minWidth": "0", "height": "30px", "display": "flex", "flexWrap": "nowrap", "alignItems": "center", "gap": "0 2px", "overflowX": "auto", "overflowY": "hidden", "scrollbarWidth": "none" }}>
          {(v.tools || []).map((b: any, i2: number) => (
            <React.Fragment key={i2}>
              {b.sep ? (
                <>
                <div style={{ "width": "1px", "height": "18px", "background": "var(--line)", "margin": "6px 6px" }} />
                </>
              ) : null}
              {b.icon ? (
                <>
                <button onMouseDown={b.onDown} title={b.title} style={{ "width": "32px", "height": "30px", "display": "flex", "alignItems": "center", "justifyContent": "center", "background": "transparent", "border": "none", "borderRadius": "980px", "color": "var(--text)", "cursor": "pointer" }} className="h2">
                  <svg viewBox="0 0 16 16" style={{ "width": "16px", "height": "16px", "fill": "none", "stroke": "currentColor", "strokeWidth": "1.5", "strokeLinecap": "round", "strokeLinejoin": "round" }}>
                    <path d={b.d} />
                  </svg>
                </button>
                </>
              ) : null}
              {b.text ? (
                <>
                <button onMouseDown={b.onDown} title={b.title} style={{ "width": "32px", "height": "30px", "background": "transparent", "border": "none", "borderRadius": "980px", "color": "var(--text)", "cursor": "pointer", "font": b.font, "textDecoration": b.deco }} className="h2">
                  {b.label}
                </button>
                </>
              ) : null}
              {b.swatch ? (
                <>
                <button onMouseDown={b.onDown} title={b.title} style={{ "width": "24px", "height": "30px", "padding": "0", "background": "transparent", "border": "none", "borderRadius": "980px", "cursor": "pointer", "display": "flex", "alignItems": "center", "justifyContent": "center" }} className="h2">
                  <div style={{ "width": "12px", "height": "12px", "borderRadius": "50%", "background": b.color, "boxShadow": "inset 0 0 0 1px rgba(128,128,128,.6)" }} />
                </button>
                </>
              ) : null}
            </React.Fragment>
          ))}
        </div>
        <div style={{ "position": "relative", "flex": "none" }}>
          <button onMouseDown={v.togglePresMenu} title="Apresentador do trecho (Alt+1…8)" style={{ "height": "30px", "display": "flex", "alignItems": "center", "gap": "7px", "padding": "0 10px 0 12px", "borderRadius": "980px", "border": "none", "background": "var(--t-soft)", "color": "var(--text)", "font": "inherit", "fontSize": "12px", "cursor": "pointer", "whiteSpace": "nowrap" }}>
            <span style={{ "width": "10px", "height": "10px", "borderRadius": "50%", "background": v.curPresColor, "boxShadow": "inset 0 0 0 1px rgba(128,128,128,.6)" }} />
            {v.curPresName}
            <svg viewBox="0 0 16 16" style={{ "width": "10px", "height": "10px", "fill": "none", "stroke": "currentColor", "strokeWidth": "1.6", "strokeLinecap": "round", "strokeLinejoin": "round" }}>
              <path d="M4 6l4 4 4-4" />
            </svg>
          </button>
          {v.presMenu ? (
            <>
            <div style={{ "position": "absolute", "top": "36px", "right": "0", "minWidth": "220px", "width": "max-content", "padding": "6px", "background": "var(--t-ui)", "border": "1px solid var(--t-border)", "borderRadius": "12px", "boxShadow": "var(--t-sh2)", "zIndex": "80" }}>
              {(v.presMenuItems || []).map((pm: any, i3: number) => (
                <React.Fragment key={i3}>
                  <div onMouseDown={pm.onDown} style={{ "display": "grid", "gridTemplateColumns": "12px minmax(0,1fr) auto", "gap": "10px", "alignItems": "center", "height": "30px", "padding": "0 10px", "borderRadius": "8px", "cursor": "default", "fontSize": "13px" }} className="h0">
                    <span style={{ "width": "10px", "height": "10px", "borderRadius": "50%", "background": pm.color, "boxShadow": "inset 0 0 0 1px rgba(128,128,128,.6)" }} />
                    <span style={{ "whiteSpace": "nowrap" }}>
                      {pm.name}
                    </span>
                    <span style={{ "color": "var(--faint)", "fontSize": "12px", "paddingLeft": "16px" }}>
                      {pm.key}
                    </span>
                  </div>
                </React.Fragment>
              ))}
            </div>
            </>
          ) : null}
        </div>
        <button onClick={v.toggleLock} title="Bloquear / editar ao vivo (Ctrl+E)" style={{ "height": "30px", "flex": "none", "padding": "0 12px", "display": "flex", "alignItems": "center", "gap": "7px", "borderRadius": "980px", "border": "none", "background": v.lockBg, "color": "var(--text)", "font": "inherit", "cursor": "pointer", "whiteSpace": "nowrap" }}>
          <svg viewBox="0 0 16 16" style={{ "width": "14px", "height": "14px", "fill": "none", "stroke": "currentColor", "strokeWidth": "1.6", "strokeLinejoin": "round" }}>
            <path d={v.lockIcon} />
          </svg>
          {v.lockLabel}
        </button>
        <button onClick={v.toggleTalent} title="Janela do talento (F5)" style={{ "height": "30px", "flex": "none", "padding": "0 12px", "display": "flex", "alignItems": "center", "gap": "7px", "borderRadius": "980px", "border": "none", "background": "var(--t-soft)", "color": "var(--text)", "font": "inherit", "cursor": "pointer", "whiteSpace": "nowrap" }} className="h2">
          <svg viewBox="0 0 16 16" style={{ "width": "14px", "height": "14px", "fill": "none", "stroke": "currentColor", "strokeWidth": "1.6", "strokeLinejoin": "round" }}>
            <path d="M1.5 2.5h13v9h-13z M5.5 14.5h5 M8 11.5v3" />
          </svg>
          Talento
        </button>
        <button onClick={v.togglePlay} title="Iniciar / parar prompt (Ctrl+Enter)" style={{ "height": "30px", "flex": "none", "minWidth": "100px", "padding": "0 14px", "display": "flex", "alignItems": "center", "justifyContent": "center", "gap": "8px", "borderRadius": "980px", "border": "none", "background": v.promptBg, "color": v.promptFg, "font": "600 13px var(--f-sans)", "cursor": "pointer", "whiteSpace": "nowrap" }}>
          <svg viewBox="0 0 16 16" style={{ "width": "11px", "height": "11px", "fill": "currentColor" }}>
            <path d={v.promptIcon} />
          </svg>
          {v.promptLabel}
        </button>
      </div>
      <div style={{ "flex": "1", "minHeight": "0", "display": "grid", "gridTemplateColumns": v.gridCols }}>
        {v.showBm ? (
          <>
          <div style={{ "display": "flex", "flexDirection": "column", "minHeight": "0", "minWidth": "0", "background": "var(--panel)", "borderRight": "1px solid var(--line)" }}>
            <div style={{ "height": "40px", "flex": "none", "display": "flex", "alignItems": "center", "justifyContent": "space-between", "padding": "0 8px 0 14px" }}>
              <span style={{ "fontFamily": "var(--f-num)", "fontSize": "10px", "letterSpacing": "1.5px", "textTransform": "uppercase", "color": "var(--t-faint)" }}>
                Marcadores
              </span>
              <button onMouseDown={v.insertBm} title="Inserir marcador (Ctrl+K)" style={{ "width": "26px", "height": "26px", "borderRadius": "980px", "border": "none", "background": "transparent", "color": "var(--muted)", "cursor": "pointer", "display": "flex", "alignItems": "center", "justifyContent": "center" }} className="h3">
                <svg viewBox="0 0 16 16" style={{ "width": "12px", "height": "12px", "fill": "none", "stroke": "currentColor", "strokeWidth": "1.5", "strokeLinecap": "round" }}>
                  <path d="M8 2.5v11M2.5 8h11" />
                </svg>
              </button>
            </div>
            <div style={{ "flex": "1", "minHeight": "0", "overflowY": "auto", "overflowX": "hidden", "padding": "0 6px", "display": "flex", "flexDirection": "column", "gap": "1px" }}>
              {(v.bookmarks || []).map((b: any, i4: number) => (
                <React.Fragment key={i4}>
                  <div draggable="true" onDragStart={b.onDragStart} onDragOver={b.onDragOver} onDragLeave={b.onDragLeave} onDrop={b.onDrop} onDragEnd={v.onBmDragEnd} onDoubleClick={b.onJump} onContextMenu={b.onDelete} title="Arraste para reordenar · Duplo clique: ir · Botão direito: excluir" style={{ "borderTop": `2px solid ${b.dropLine ?? ""}`, "display": "grid", "gridTemplateColumns": "14px minmax(0,1fr) 22px 22px", "gap": "2px", "alignItems": "center", "minHeight": "38px", "padding": "3px 2px 3px 8px", "borderRadius": "9px", "background": b.bg, "fontWeight": b.weight, "opacity": b.op }} className="h0">
                    <span style={{ "font": "600 11px var(--f-num)", "color": b.keyColor }}>
                      {b.key}
                    </span>
                    <div style={{ "display": "flex", "flexDirection": "column", "minWidth": "0" }}>
                      <span style={{ "whiteSpace": "nowrap", "overflow": "hidden", "textOverflow": "ellipsis", "textDecoration": b.deco }}>
                        {b.name}
                      </span>
                      <span style={{ "font": "10px var(--f-num)", "color": "var(--t-faint)", "whiteSpace": "nowrap", "overflow": "hidden", "textOverflow": "ellipsis" }}>
                        {b.sub}
                      </span>
                    </div>
                    <button onClick={b.onSkip} title={b.skipTitle} style={{ "width": "22px", "height": "22px", "borderRadius": "980px", "border": "none", "background": "transparent", "color": b.skipColor, "cursor": "pointer", "display": "flex", "alignItems": "center", "justifyContent": "center", "flex": "none" }} className="h3">
                      <svg viewBox="0 0 16 16" style={{ "width": "12px", "height": "12px", "fill": "none", "stroke": "currentColor", "strokeWidth": "1.5", "strokeLinecap": "round", "strokeLinejoin": "round" }}>
                        <path d={b.skipIcon} />
                      </svg>
                    </button>
                    <button onClick={b.onRemove} title="Excluir marcador" style={{ "width": "22px", "height": "22px", "borderRadius": "980px", "border": "none", "background": "transparent", "color": "var(--t-faint)", "cursor": "pointer", "display": "flex", "alignItems": "center", "justifyContent": "center", "flex": "none" }} className="h3">
                      <svg viewBox="0 0 16 16" style={{ "width": "10px", "height": "10px", "fill": "none", "stroke": "currentColor", "strokeWidth": "1.7", "strokeLinecap": "round" }}>
                        <path d="M4 4l8 8M12 4l-8 8" />
                      </svg>
                    </button>
                  </div>
                </React.Fragment>
              ))}
              {v.hasStops ? (
                <>
                <div style={{ "height": "32px", "flex": "none", "display": "flex", "alignItems": "center", "padding": "8px 8px 0", "fontFamily": "var(--f-num)", "fontSize": "10px", "letterSpacing": "1.5px", "textTransform": "uppercase", "color": "var(--t-faint)" }}>
                  Pausas
                </div>
                {(v.stopsList || []).map((p: any, i5: number) => (
                  <React.Fragment key={i5}>
                    <div onDoubleClick={p.onJump} title="Duplo clique: ir" style={{ "display": "grid", "gridTemplateColumns": "14px minmax(0,1fr) auto 22px", "gap": "6px", "alignItems": "center", "height": "32px", "padding": "0 2px 0 8px", "borderRadius": "9px" }} className="h0">
                      <svg viewBox="0 0 16 16" style={{ "width": "10px", "height": "10px", "fill": "var(--t-faint)" }}>
                        <path d="M4 3h3v10H4z M9 3h3v10H9z" />
                      </svg>
                      <span style={{ "whiteSpace": "nowrap", "overflow": "hidden", "textOverflow": "ellipsis" }}>
                        {p.name}
                      </span>
                      <span style={{ "font": "11px var(--f-num)", "color": "var(--muted)" }}>
                        {p.time}
                      </span>
                      <button onClick={p.onRemove} title="Excluir pausa" style={{ "width": "22px", "height": "22px", "borderRadius": "980px", "border": "none", "background": "transparent", "color": "var(--t-faint)", "cursor": "pointer", "display": "flex", "alignItems": "center", "justifyContent": "center", "flex": "none" }} className="h3">
                        <svg viewBox="0 0 16 16" style={{ "width": "10px", "height": "10px", "fill": "none", "stroke": "currentColor", "strokeWidth": "1.7", "strokeLinecap": "round" }}>
                          <path d="M4 4l8 8M12 4l-8 8" />
                        </svg>
                      </button>
                    </div>
                  </React.Fragment>
                ))}
                </>
              ) : null}
            </div>
            <div style={{ "flex": "none", "display": "flex", "flexDirection": "column", "gap": "6px", "padding": "8px 10px 10px", "borderTop": "1px solid var(--line)", "fontSize": "11px" }}>
              <div style={{ "display": "flex", "alignItems": "center", "gap": "6px" }}>
                <span style={{ "color": "var(--muted)", "flex": "none" }}>
                  Programa
                </span>
                <input value={v.progInput} onChange={v.onProgInput} onBlur={v.onProgBlur} title="Duração do programa (mm:ss)" style={{ "height": "24px", "width": "52px", "flex": "none", "borderRadius": "7px", "border": "1px solid transparent", "background": "var(--field)", "color": "var(--text)", "font": "11px var(--f-num)", "padding": "0 6px", "textAlign": "right", "userSelect": "text" }} />
                <span style={{ "flex": "1", "minWidth": "0", "textAlign": "right", "fontFamily": "var(--f-num)", "fontWeight": "600", "color": v.overColor, "whiteSpace": "nowrap", "overflow": "hidden", "textOverflow": "ellipsis" }} title={`${v.overTitle ?? ""} · estimado ${v.rundownTotal ?? ""}`}>
                  {v.overLabel}
                </span>
              </div>
              <div onClick={v.toggleRehearse} style={{ "display": "flex", "alignItems": "center", "gap": "6px", "cursor": "default" }}>
                <div style={{ "width": "13px", "height": "13px", "borderRadius": "4px", "border": `1px solid ${v.rehCk.bd ?? ""}`, "background": v.rehCk.bg, "display": "flex", "alignItems": "center", "justifyContent": "center", "flex": "none" }}>
                  <svg viewBox="0 0 16 16" style={{ "width": "10px", "height": "10px", "fill": "none", "stroke": "var(--invtext)", "strokeWidth": "2.2", "opacity": v.rehCk.op }}>
                    <path d="M3 8.5l3 3 7-7" />
                  </svg>
                </div>
                <span style={{ "flex": "1", "minWidth": "0", "whiteSpace": "nowrap", "overflow": "hidden", "textOverflow": "ellipsis" }}>
                  Modo ensaio
                </span>
                {v.hasReh ? (
                  <>
                  <button onClick={v.clearReh} style={{ "border": "none", "background": "transparent", "color": "var(--t-accent)", "font": "inherit", "cursor": "pointer", "padding": "0" }}>
                    Zerar
                  </button>
                  </>
                ) : null}
              </div>
              <div style={{ "display": "flex", "justifyContent": "space-between", "gap": "6px", "color": "var(--t-faint)", "whiteSpace": "nowrap" }}>
                <span>
                  {v.wordCount} palavras
                </span>
                <span style={{ "fontFamily": "var(--f-num)" }}>
                  est. {v.rundownTotal}
                </span>
              </div>
            </div>
          </div>
          </>
        ) : null}
        <div style={{ "display": "flex", "flexDirection": "column", "minHeight": "0", "minWidth": "0" }}>
          <div style={{ "height": "38px", "flex": "none", "display": "flex", "alignItems": "center", "gap": "2px", "padding": "0 6px", "background": "var(--panel)", "borderBottom": "1px solid var(--line)", "overflowX": "auto", "overflowY": "hidden", "scrollbarWidth": "none" }}>
            {(v.docTabs || []).map((dt: any, i6: number) => (
              <React.Fragment key={i6}>
                <div onClick={dt.onClick} title={dt.name} style={{ "height": "28px", "flex": "none", "maxWidth": "220px", "display": "flex", "alignItems": "center", "gap": "6px", "padding": "0 4px 0 12px", "borderRadius": "8px", "background": dt.bg, "boxShadow": dt.shadow, "color": dt.color, "fontSize": "12px", "fontWeight": dt.weight, "cursor": "default" }} className="h4">
                  <span style={{ "whiteSpace": "nowrap", "overflow": "hidden", "textOverflow": "ellipsis" }}>
                    {dt.name}
                  </span>
                  <span style={{ "width": "6px", "height": "6px", "borderRadius": "50%", "background": "var(--t-accent)", "flex": "none", "opacity": dt.dirtyOp }} />
                  <button onClick={dt.onClose} title="Fechar" style={{ "width": "20px", "height": "20px", "borderRadius": "980px", "border": "none", "background": "transparent", "color": "var(--t-faint)", "cursor": "pointer", "display": "flex", "alignItems": "center", "justifyContent": "center", "flex": "none" }} className="h3">
                    <svg viewBox="0 0 16 16" style={{ "width": "9px", "height": "9px", "fill": "none", "stroke": "currentColor", "strokeWidth": "1.8", "strokeLinecap": "round" }}>
                      <path d="M4 4l8 8M12 4l-8 8" />
                    </svg>
                  </button>
                </div>
              </React.Fragment>
            ))}
            <button onClick={v.newTab} title="Novo roteiro (Ctrl+N)" style={{ "width": "28px", "height": "28px", "flex": "none", "borderRadius": "8px", "border": "none", "background": "transparent", "color": "var(--muted)", "cursor": "pointer", "display": "flex", "alignItems": "center", "justifyContent": "center" }} className="h3">
              <svg viewBox="0 0 16 16" style={{ "width": "12px", "height": "12px", "fill": "none", "stroke": "currentColor", "strokeWidth": "1.5", "strokeLinecap": "round" }}>
                <path d="M8 2.5v11M2.5 8h11" />
              </svg>
            </button>
          </div>
          <div style={{ "flex": "1", "minHeight": "0", "position": "relative", "containerType": "inline-size" }}>
            <div style={{ "position": "absolute", "inset": "0", "background": v.bg }} />
            {v.hasBgImg ? (
              <>
              <div style={{ "position": "absolute", "inset": "0", "backgroundImage": `url('${v.bgImg ?? ""}')`, "backgroundSize": "cover", "backgroundPosition": "center", "opacity": v.bgImgOp, "pointerEvents": "none" }} />
              </>
            ) : null}
            <div ref={v.edScroll} onScroll={v.onEdScroll} onWheel={v.onEdWheel} style={{ "position": "absolute", "inset": "0", "overflowY": "scroll", "overflowX": "hidden" }}>
              <div ref={v.edSizer} style={{ "position": "relative", "width": "100%", "overflow": "hidden" }}>
                <div ref={v.edZoom} style={{ "position": "absolute", "left": "0", "top": "0", "width": "1000px", "transformOrigin": "0 0" }}>
                  <div ref={v.edRef} data-ed="1" data-script="1" onInput={v.onEdInput} spellCheck={false} style={{ "--fs": `${v.fontSize ?? ""}px`, "boxSizing": "border-box", "padding": `0 ${v.prRight ?? ""}px 0 ${v.prMargin ?? ""}px`, "fontFamily": v.fontFam, "fontSize": `${v.fontSize ?? ""}px`, "lineHeight": v.lineH, "color": v.fg, "textAlign": v.align, "caretColor": v.fg, "userSelect": "text", "transform": v.edFlip, "cursor": v.edCursor }} />
                </div>
              </div>
            </div>
            <div style={{ "position": "absolute", "left": "0", "right": "10px", "top": "0", "bottom": "0", "pointerEvents": "none", "overflow": "hidden" }}>
              {v.readingLine ? (
                <>
                <div style={{ "position": "absolute", "left": "0", "right": "0", "top": `calc(${v.readPct ?? ""}% + ${v.halfLineCq ?? ""})`, "height": "2px", "background": v.indColorV, "opacity": ".35", "pointerEvents": "none" }} />
                </>
              ) : null}
              {v.ovFaixa ? (
                <>
                <div style={{ "position": "absolute", "left": "0", "right": "0", "top": "0", "height": `${v.readPct ?? ""}%`, "background": v.dimColor, "pointerEvents": "none" }} />
                <div style={{ "position": "absolute", "left": "0", "right": "0", "bottom": "0", "top": `calc(${v.readPct ?? ""}% + ${v.lineCq ?? ""})`, "background": v.dimColor, "pointerEvents": "none" }} />
                </>
              ) : null}
              {v.ovFade ? (
                <>
                <div style={{ "position": "absolute", "left": "0", "right": "0", "top": "0", "height": `${v.readPct ?? ""}%`, "background": `linear-gradient(${v.dimColor ?? ""},transparent)`, "pointerEvents": "none" }} />
                <div style={{ "position": "absolute", "left": "0", "right": "0", "bottom": "0", "top": `calc(${v.readPct ?? ""}% + ${v.lineCq ?? ""})`, "background": `linear-gradient(transparent,${v.dimColor ?? ""})`, "pointerEvents": "none" }} />
                </>
              ) : null}
              {v.ovDestaque ? (
                <>
                <div style={{ "position": "absolute", "left": "0", "right": "0", "top": `${v.readPct ?? ""}%`, "height": v.lineCq, "background": v.hiColor, "pointerEvents": "none" }} />
                </>
              ) : null}
              <div style={{ "position": "absolute", "left": v.indLeft, "top": `calc(${v.readPct ?? ""}% + ${v.halfLineCq ?? ""})`, "transform": "translateY(-50%)", "width": v.indW, "height": v.indH, "background": v.indColorV, "clipPath": v.indClip, "borderRadius": v.indRadius, "display": v.indDisplay, "pointerEvents": "none" }} />
            </div>
            <div style={{ "position": "absolute", "top": "10px", "left": "50%", "transform": "translateX(-50%)", "display": v.edBarDisplay, "alignItems": "center", "gap": "8px", "padding": "4px 4px 4px 12px", "background": "var(--raised)", "border": "1px solid var(--line)", "borderRadius": "8px", "boxShadow": "0 6px 20px rgba(0,0,0,.25)", "whiteSpace": "nowrap" }}>
              <span style={{ "color": "var(--muted)" }}>
                {v.edBarText}
              </span>
              {v.notFollowing ? (
                <>
                <button onMouseDown={v.cursorToPrompter} style={{ "height": "26px", "padding": "0 10px", "borderRadius": "980px", "border": "none", "background": "var(--t-soft)", "color": "var(--text)", "font": "inherit", "cursor": "pointer" }} className="h2">
                  Prompter no cursor
                </button>
                </>
              ) : null}
              <button onClick={v.edBarAction} style={{ "height": "26px", "padding": "0 10px", "borderRadius": "980px", "border": "none", "background": "var(--inv)", "color": "var(--invtext)", "font": "600 12px var(--f-sans)", "cursor": "pointer" }}>
                {v.edBarBtn}
              </button>
            </div>
            {v.hasFlash ? (
              <>
              <div style={{ "position": "absolute", "left": "50%", "bottom": "76px", "transform": "translateX(-50%)", "maxWidth": "calc(100% - 32px)", "boxSizing": "border-box", "display": "flex", "flexDirection": "row", "alignItems": "center", "gap": "10px", "padding": "7px 14px 7px 8px", "borderRadius": "980px", "background": "rgba(20,20,22,.82)", "color": "#fff", "pointerEvents": "none", "zIndex": "4" }}>
                {v.hasFlashSub ? (
                  <>
                  <span style={{ "display": "flex", "alignItems": "center", "gap": "5px", "height": "20px", "padding": "0 9px", "borderRadius": "980px", "background": v.flashSubBg, "font": "600 10px var(--f-num)", "letterSpacing": "1.2px", "textTransform": "uppercase" }}>
                    <svg viewBox="0 0 16 16" style={{ "width": "9px", "height": "9px", "fill": "none", "stroke": "currentColor", "strokeWidth": "2.2", "strokeLinecap": "round", "strokeLinejoin": "round" }}>
                      <path d={v.flashArrow} />
                    </svg>
                    {v.flashSub}
                  </span>
                  </>
                ) : null}
                <span style={{ "font": "600 13px var(--f-num)", "whiteSpace": "nowrap" }}>
                  {v.flashText}
                </span>
                {v.flashBar ? (
                  <>
                  <div style={{ "width": "72px", "height": "3px", "borderRadius": "2px", "background": "rgba(255,255,255,.2)", "flex": "none" }}>
                    <div style={{ "height": "100%", "width": v.flashW, "borderRadius": "2px", "background": "#fff" }} />
                  </div>
                  </>
                ) : null}
              </div>
              </>
            ) : null}
            {v.counting ? (
              <>
              <div style={{ "position": "absolute", "right": "20px", "top": "14px", "width": "56px", "height": "56px", "borderRadius": "50%", "background": "var(--raised)", "border": "1px solid var(--line)", "display": "flex", "alignItems": "center", "justifyContent": "center", "font": "500 26px var(--f-num)" }}>
                {v.count}
              </div>
              </>
            ) : null}
          </div>
        </div>
        {v.showInsp ? (
          <>
          <div style={{ "display": "flex", "flexDirection": "column", "minHeight": "0", "minWidth": "0", "background": "var(--panel)", "borderLeft": "1px solid var(--line)" }}>
            <div style={{ "padding": "10px 12px", "flex": "none" }}>
              <div style={{ "display": "flex", "padding": "2px", "gap": "2px", "background": "var(--t-sunk)", "borderRadius": "11px" }}>
                {(v.tabs || []).map((t: any, i7: number) => (
                  <React.Fragment key={i7}>
                    <div onClick={t.onClick} style={{ "flex": "1", "height": "26px", "display": "flex", "alignItems": "center", "justifyContent": "center", "borderRadius": "8px", "background": t.bg, "boxShadow": t.shadow, "color": t.color, "cursor": "default", "fontSize": "12px" }}>
                      {t.label}
                    </div>
                  </React.Fragment>
                ))}
              </div>
            </div>
            <div style={{ "flex": "1", "minHeight": "0", "overflowY": "auto", "overflowX": "hidden", "paddingBottom": "12px" }}>
              {v.tabRoteiro ? (
                <>
                <div style={{ "display": "flex", "justifyContent": "space-between", "padding": "8px 16px 10px" }}>
                  <span style={{ "fontFamily": "var(--f-num)", "fontSize": "10px", "letterSpacing": "1.5px", "textTransform": "uppercase", "color": "var(--t-faint)" }}>
                    Aparência
                  </span>
                  <span onClick={v.resetLook} style={{ "color": "var(--muted)", "cursor": "pointer", "fontSize": "12px" }}>
                    Redefinir
                  </span>
                </div>
                <div style={{ "display": "grid", "gridTemplateColumns": "84px minmax(0,1fr)", "gap": "10px", "alignItems": "center", "padding": "0 16px 16px" }}>
                  <span style={{ "color": "var(--muted)" }}>
                    Fundo
                  </span>
                  <div style={{ "display": "flex", "flexWrap": "wrap", "alignItems": "center", "gap": "8px" }}>
                    {(v.bgSwatches || []).map((w: any, i8: number) => (
                      <React.Fragment key={i8}>
                        <div onClick={w.onClick} style={{ "width": "24px", "height": "24px", "flex": "none", "borderRadius": "6px", "background": w.value, "boxShadow": `inset 0 0 0 1px rgba(128,128,128,.45),0 0 0 2px var(--panel),0 0 0 3.5px ${w.ring ?? ""}`, "cursor": "pointer" }} />
                      </React.Fragment>
                    ))}
                    <label title="Outra cor…" style={{ "width": "24px", "height": "24px", "flex": "none", "borderRadius": "6px", "position": "relative", "cursor": "pointer", "border": "1px dashed var(--t-border)", "boxSizing": "border-box", "display": "flex", "alignItems": "center", "justifyContent": "center", "color": "var(--muted)", "boxShadow": `0 0 0 2px var(--panel),0 0 0 3.5px ${v.bgPickRing ?? ""}` }}>
                      <svg viewBox="0 0 16 16" style={{ "width": "11px", "height": "11px", "fill": "none", "stroke": "currentColor", "strokeWidth": "1.6", "strokeLinecap": "round" }}>
                        <path d="M8 3v10M3 8h10" />
                      </svg>
                      <input type="color" value={v.bg} onInput={v.onBgPick} onChange={v.onBgPick} style={{ "position": "absolute", "inset": "0", "width": "100%", "height": "100%", "opacity": "0", "cursor": "pointer", "border": "none", "padding": "0" }} />
                    </label>
                    <input value={v.bgHex} onChange={v.onBgHex} onBlur={v.onBgHexBlur} spellCheck={false} maxLength={7} style={{ "width": "84px", "height": "24px", "flex": "none", "borderRadius": "6px", "border": "1px solid transparent", "background": "var(--field)", "color": "var(--text)", "font": "12px var(--f-num)", "padding": "0 6px", "textTransform": "uppercase", "userSelect": "text" }} />
                  </div>
                  <span style={{ "color": "var(--muted)" }}>
                    Texto
                  </span>
                  <div style={{ "display": "flex", "flexWrap": "wrap", "alignItems": "center", "gap": "8px" }}>
                    {(v.fgSwatches || []).map((w: any, i9: number) => (
                      <React.Fragment key={i9}>
                        <div onClick={w.onClick} style={{ "width": "24px", "height": "24px", "flex": "none", "borderRadius": "6px", "background": w.value, "boxShadow": `inset 0 0 0 1px rgba(128,128,128,.45),0 0 0 2px var(--panel),0 0 0 3.5px ${w.ring ?? ""}`, "cursor": "pointer" }} />
                      </React.Fragment>
                    ))}
                    <label title="Outra cor…" style={{ "width": "24px", "height": "24px", "flex": "none", "borderRadius": "6px", "position": "relative", "cursor": "pointer", "border": "1px dashed var(--t-border)", "boxSizing": "border-box", "display": "flex", "alignItems": "center", "justifyContent": "center", "color": "var(--muted)", "boxShadow": `0 0 0 2px var(--panel),0 0 0 3.5px ${v.fgPickRing ?? ""}` }}>
                      <svg viewBox="0 0 16 16" style={{ "width": "11px", "height": "11px", "fill": "none", "stroke": "currentColor", "strokeWidth": "1.6", "strokeLinecap": "round" }}>
                        <path d="M8 3v10M3 8h10" />
                      </svg>
                      <input type="color" value={v.fg} onInput={v.onFgPick} onChange={v.onFgPick} style={{ "position": "absolute", "inset": "0", "width": "100%", "height": "100%", "opacity": "0", "cursor": "pointer", "border": "none", "padding": "0" }} />
                    </label>
                    <input value={v.fgHex} onChange={v.onFgHex} onBlur={v.onFgHexBlur} spellCheck={false} maxLength={7} style={{ "width": "84px", "height": "24px", "flex": "none", "borderRadius": "6px", "border": "1px solid transparent", "background": "var(--field)", "color": "var(--text)", "font": "12px var(--f-num)", "padding": "0 6px", "textTransform": "uppercase", "userSelect": "text" }} />
                  </div>
                  <span style={{ "color": "var(--muted)" }}>
                    Fonte
                  </span>
                  <select value={v.fontKey} onChange={v.onFont} onMouseDown={v.onFontDown} style={{ "height": "32px", "borderRadius": "10px", "border": "1px solid transparent", "background": "var(--field)", "color": "var(--text)", "font": "inherit", "padding": "0 6px", "minWidth": "0" }}>
                    <option value="atk">
                      Atkinson Hyperlegible
                    </option>
                    <option value="sans">
                      Source Sans 3
                    </option>
                    <option value="serif">
                      Source Serif 4
                    </option>
                    <option value="mono">
                      IBM Plex Mono
                    </option>
                    {v.sysFonts.length ? (
                      <optgroup label="Fontes do computador">
                        {v.sysFonts.map((f: string) => <option key={f} value={'sys:' + f}>{f}</option>)}
                      </optgroup>
                    ) : (
                      <option value="__sys">{v.sysFontsLabel}</option>
                    )}
                  </select>
                  <span style={{ "color": "var(--muted)" }}>
                    Alinhamento
                  </span>
                  <div style={{ "display": "flex", "padding": "2px", "gap": "2px", "background": "var(--t-sunk)", "borderRadius": "11px", "width": "fit-content" }}>
                    {(v.aligns || []).map((a: any, i10: number) => (
                      <React.Fragment key={i10}>
                        <div onClick={a.onClick} title={a.title} style={{ "width": "36px", "height": "24px", "borderRadius": "8px", "display": "flex", "alignItems": "center", "justifyContent": "center", "background": a.bg, "boxShadow": a.shadow, "cursor": "pointer" }}>
                          <svg viewBox="0 0 16 16" style={{ "width": "13px", "height": "13px", "fill": "none", "stroke": "var(--text)", "strokeWidth": "1.6", "strokeLinecap": "round" }}>
                            <path d={a.d} />
                          </svg>
                        </div>
                      </React.Fragment>
                    ))}
                  </div>
                  <span style={{ "color": "var(--muted)" }}>
                    Espelhar
                  </span>
                  <div style={{ "display": "flex", "flexWrap": "wrap", "gap": "6px 14px" }}>
                    {(v.edFlipChecks || []).map((c: any, i11: number) => (
                      <React.Fragment key={i11}>
                        <div onClick={c.onClick} style={{ "display": "flex", "alignItems": "center", "gap": "7px", "cursor": "default" }}>
                          <div style={{ "width": "15px", "height": "15px", "borderRadius": "4px", "border": `1px solid ${c.bd ?? ""}`, "background": c.bg, "display": "flex", "alignItems": "center", "justifyContent": "center" }}>
                            <svg viewBox="0 0 16 16" style={{ "width": "11px", "height": "11px", "fill": "none", "stroke": "var(--invtext)", "strokeWidth": "2.2", "opacity": c.op }}>
                              <path d="M3 8.5l3 3 7-7" />
                            </svg>
                          </div>
                          {c.label}
                        </div>
                      </React.Fragment>
                    ))}
                  </div>
                </div>
                <div style={{ "height": "1px", "background": "var(--line)", "margin": "0 16px" }} />
                <div style={{ "display": "flex", "justifyContent": "space-between", "alignItems": "center", "padding": "14px 16px 10px" }}>
                  <span style={{ "fontFamily": "var(--f-num)", "fontSize": "10px", "letterSpacing": "1.5px", "textTransform": "uppercase", "color": "var(--t-faint)" }}>
                    Apresentadores
                  </span>
                  <button onClick={v.addPres} title="Adicionar apresentador" style={{ "height": "24px", "padding": "0 10px", "borderRadius": "980px", "border": "none", "background": "var(--t-soft)", "color": "var(--text)", "font": "inherit", "fontSize": "12px", "cursor": "pointer" }}>
                    Adicionar
                  </button>
                </div>
                <div style={{ "display": "flex", "flexDirection": "column", "gap": "6px", "padding": "0 16px 12px" }}>
                  {(v.presList || []).map((pr: any, i12: number) => (
                    <React.Fragment key={i12}>
                      <div style={{ "display": "grid", "gridTemplateColumns": "24px minmax(0,1fr) 44px 22px", "gap": "8px", "alignItems": "center" }}>
                        <label title="Cor" style={{ "width": "24px", "height": "24px", "borderRadius": "6px", "position": "relative", "cursor": "pointer", "background": pr.color, "boxShadow": "inset 0 0 0 1px rgba(128,128,128,.45)" }}>
                          <input type="color" value={pr.color} onInput={pr.onColor} onChange={pr.onColor} style={{ "position": "absolute", "inset": "0", "width": "100%", "height": "100%", "opacity": "0", "cursor": "pointer", "border": "none", "padding": "0" }} />
                        </label>
                        <input value={pr.name} onChange={pr.onName} placeholder="Nome" style={{ "height": "28px", "borderRadius": "8px", "border": "1px solid transparent", "background": "var(--field)", "color": "var(--text)", "font": "inherit", "padding": "0 8px", "minWidth": "0", "userSelect": "text" }} />
                        <span style={{ "font": "11px var(--f-num)", "color": "var(--t-faint)", "textAlign": "right" }}>
                          {pr.key}
                        </span>
                        <button onClick={pr.onRemove} title="Remover apresentador" style={{ "width": "22px", "height": "22px", "borderRadius": "980px", "border": "none", "background": "transparent", "color": "var(--t-faint)", "cursor": "pointer", "display": "flex", "alignItems": "center", "justifyContent": "center" }} className="h3">
                          <svg viewBox="0 0 16 16" style={{ "width": "10px", "height": "10px", "fill": "none", "stroke": "currentColor", "strokeWidth": "1.7", "strokeLinecap": "round" }}>
                            <path d="M4 4l8 8M12 4l-8 8" />
                          </svg>
                        </button>
                      </div>
                    </React.Fragment>
                  ))}
                  <div onClick={v.togglePresNames} style={{ "display": "flex", "alignItems": "center", "gap": "8px", "cursor": "default", "minHeight": "26px", "paddingTop": "4px" }}>
                    <div style={{ "width": "15px", "height": "15px", "borderRadius": "4px", "border": `1px solid ${v.presNamesCk.bd ?? ""}`, "background": v.presNamesCk.bg, "display": "flex", "alignItems": "center", "justifyContent": "center", "flex": "none" }}>
                      <svg viewBox="0 0 16 16" style={{ "width": "11px", "height": "11px", "fill": "none", "stroke": "var(--invtext)", "strokeWidth": "2.2", "opacity": v.presNamesCk.op }}>
                        <path d="M3 8.5l3 3 7-7" />
                      </svg>
                    </div>
                    Mostrar o nome no início de cada fala
                  </div>
                  <div style={{ "display": "grid", "gridTemplateColumns": "84px minmax(0,1fr)", "gap": "10px", "alignItems": "center" }}>
                    <span style={{ "color": "var(--muted)" }}>
                      Destacar
                    </span>
                    <div style={{ "display": "flex", "flexWrap": "wrap", "gap": "4px" }}>
                      {(v.presOnlyOpts || []).map((po: any, i13: number) => (
                        <React.Fragment key={i13}>
                          <div onClick={po.onClick} style={{ "height": "26px", "display": "flex", "alignItems": "center", "gap": "6px", "padding": "0 10px", "borderRadius": "980px", "background": po.bg, "color": po.fg, "fontSize": "12px", "cursor": "pointer", "whiteSpace": "nowrap" }}>
                            <span style={{ "width": "8px", "height": "8px", "borderRadius": "50%", "background": po.color, "display": po.dot }} />
                            {po.name}
                          </div>
                        </React.Fragment>
                      ))}
                    </div>
                  </div>
                </div>
                <div style={{ "height": "1px", "background": "var(--line)", "margin": "0 16px" }} />
                <div style={{ "display": "flex", "justifyContent": "space-between", "padding": "14px 16px 10px" }}>
                  <span style={{ "fontFamily": "var(--f-num)", "fontSize": "10px", "letterSpacing": "1.5px", "textTransform": "uppercase", "color": "var(--t-faint)" }}>
                    Palavras por linha e margem
                  </span>
                  <span onClick={v.resetLayout} style={{ "color": "var(--muted)", "cursor": "pointer", "fontSize": "12px" }}>
                    Redefinir
                  </span>
                </div>
                <div style={{ "display": "grid", "gridTemplateColumns": "84px minmax(0,1fr) 40px", "gap": "12px 10px", "alignItems": "center", "padding": "0 16px 16px" }}>
                  <span style={{ "color": "var(--muted)" }}>
                    Palavras/linha
                  </span>
                  <input type="range" min="2" max="12" step="0.25" value={v.wpl} onChange={v.onWpl} />
                  <span style={{ "font": "12px var(--f-num)", "textAlign": "right" }}>
                    {v.wplLabel}
                  </span>
                  <span style={{ "color": "var(--muted)" }}>
                    Margem
                  </span>
                  <input type="range" min="0" max="400" step="10" value={v.margin} onChange={v.onMargin} />
                  <span style={{ "font": "12px var(--f-num)", "textAlign": "right" }}>
                    {v.margin}
                  </span>
                  <span style={{ "color": "var(--muted)" }}>
                    Posição
                  </span>
                  <div style={{ "display": "flex", "flexDirection": "column", "gap": "2px", "minWidth": "0" }}>
                    <input type="range" min="-1" max="1" step="0.05" value={v.mPos} onChange={v.onMPos} onDoubleClick={v.resetMPos} title="Duplo clique: esquerda" />
                    <div style={{ "display": "flex", "justifyContent": "space-between", "fontSize": "10px", "color": "var(--t-faint)" }}>
                      <span>
                        Esq.
                      </span>
                      <span>
                        Centro
                      </span>
                      <span>
                        Dir.
                      </span>
                    </div>
                  </div>
                  <span style={{ "font": "12px var(--f-num)", "textAlign": "right" }}>
                    {v.mPosLabel}
                  </span>
                  <span style={{ "color": "var(--muted)" }}>
                    Entrelinha
                  </span>
                  <input type="range" min="1" max="2" step="0.05" value={v.lineH} onChange={v.onLineH} />
                  <span style={{ "font": "12px var(--f-num)", "textAlign": "right" }}>
                    {v.lineHLabel}
                  </span>
                </div>
                <div style={{ "height": "1px", "background": "var(--line)", "margin": "0 16px" }} />
                <div style={{ "display": "flex", "justifyContent": "space-between", "padding": "14px 16px 10px" }}>
                  <span style={{ "fontFamily": "var(--f-num)", "fontSize": "10px", "letterSpacing": "1.5px", "textTransform": "uppercase", "color": "var(--t-faint)" }}>
                    Velocidade
                  </span>
                  <span style={{ "color": "var(--muted)", "fontSize": "12px" }}>
                    {v.unitName}
                  </span>
                </div>
                <div style={{ "display": "grid", "gridTemplateColumns": "84px minmax(0,1fr) 40px", "gap": "12px 10px", "alignItems": "center", "padding": "0 16px 16px" }}>
                  <span style={{ "color": "var(--muted)" }}>
                    Mínima
                  </span>
                  <input type="range" min="0.1" max="2" step="0.1" value={v.minS} onChange={v.onMinS} />
                  <span style={{ "font": "12px var(--f-num)", "textAlign": "right" }}>
                    {v.minLabel}
                  </span>
                  <span style={{ "color": "var(--muted)" }}>
                    Máxima
                  </span>
                  <input type="range" min="1" max="6" step="0.1" value={v.maxS} onChange={v.onMaxS} />
                  <span style={{ "font": "12px var(--f-num)", "textAlign": "right" }}>
                    {v.maxLabel}
                  </span>
                  <span style={{ "color": "var(--muted)" }}>
                    Atual
                  </span>
                  <input type="range" min="0" max="1" step="0.01" value={v.speedT} onChange={v.onSpeed} />
                  <span style={{ "font": "12px var(--f-num)", "textAlign": "right" }}>
                    {v.speedValue}
                  </span>
                </div>
                <div style={{ "height": "1px", "background": "var(--line)", "margin": "0 16px" }} />
                <div style={{ "display": "flex", "justifyContent": "space-between", "alignItems": "center", "padding": "14px 16px 10px" }}>
                  <span style={{ "fontFamily": "var(--f-num)", "fontSize": "10px", "letterSpacing": "1.5px", "textTransform": "uppercase", "color": "var(--t-faint)" }}>
                    Duração automática
                  </span>
                  <div onClick={v.toggleAutoRun} style={{ "width": "34px", "height": "20px", "borderRadius": "10px", "background": v.autoRunT.track, "position": "relative", "cursor": "pointer", "flex": "none" }}>
                    <div style={{ "position": "absolute", "top": "2px", "left": v.autoRunT.left, "width": "16px", "height": "16px", "borderRadius": "50%", "background": v.autoRunT.knob, "boxShadow": "var(--shadow)" }} />
                  </div>
                </div>
                <div style={{ "display": "flex", "alignItems": "center", "gap": "10px", "padding": "0 16px" }}>
                  <span style={{ "color": "var(--muted)", "width": "84px", "flex": "none" }}>
                    Tempo total
                  </span>
                  <input value={v.runTarget} onChange={v.onRunTarget} placeholder="hh:mm:ss" style={{ "height": "32px", "width": "92px", "borderRadius": "10px", "border": "1px solid transparent", "background": "var(--field)", "color": "var(--text)", "font": "12px var(--f-num)", "padding": "0 8px", "userSelect": "text" }} />
                  <span style={{ "color": "var(--faint)", "fontSize": "12px" }}>
                    atual {v.totalLabel}
                  </span>
                </div>
                </>
              ) : null}
              {v.tabOverlay ? (
                <>
                <div style={{ "padding": "8px 16px 10px", "fontFamily": "var(--f-num)", "fontSize": "10px", "letterSpacing": "1.5px", "textTransform": "uppercase", "color": "var(--t-faint)" }}>
                  Overlay
                </div>
                <div style={{ "display": "grid", "gridTemplateColumns": "84px minmax(0,1fr) 40px", "gap": "12px 10px", "alignItems": "center", "padding": "0 16px 16px" }}>
                  <span style={{ "color": "var(--muted)" }}>
                    Tipo
                  </span>
                  <select value={v.ovType} onChange={v.onOvType} style={{ "gridColumn": "span 2", "height": "32px", "borderRadius": "10px", "border": "1px solid transparent", "background": "var(--field)", "color": "var(--text)", "font": "inherit", "padding": "0 6px" }}>
                    <option value="nenhum">
                      Nenhum
                    </option>
                    <option value="faixa">
                      Faixa de leitura
                    </option>
                    <option value="fade">
                      Degradê nas bordas
                    </option>
                    <option value="destaque">
                      Destaque da linha
                    </option>
                  </select>
                  <span style={{ "color": "var(--muted)" }}>
                    Opacidade
                  </span>
                  <input type="range" min="0" max="1" step="0.05" value={v.ovOp} onChange={v.onOvOp} />
                  <span style={{ "font": "12px var(--f-num)", "textAlign": "right" }}>
                    {v.ovOpLabel}
                  </span>
                </div>
                <div style={{ "height": "1px", "background": "var(--line)", "margin": "0 16px" }} />
                <div style={{ "padding": "14px 16px 10px", "fontFamily": "var(--f-num)", "fontSize": "10px", "letterSpacing": "1.5px", "textTransform": "uppercase", "color": "var(--t-faint)" }}>
                  Indicador
                </div>
                <div style={{ "display": "grid", "gridTemplateColumns": "84px minmax(0,1fr) 40px", "gap": "12px 10px", "alignItems": "center", "padding": "0 16px 16px" }}>
                  <span style={{ "color": "var(--muted)" }}>
                    Tipo
                  </span>
                  <select value={v.indType} onChange={v.onIndType} style={{ "gridColumn": "span 2", "height": "32px", "borderRadius": "10px", "border": "1px solid transparent", "background": "var(--field)", "color": "var(--text)", "font": "inherit", "padding": "0 6px" }}>
                    <option value="nenhum">
                      Nenhum
                    </option>
                    <option value="seta">
                      Seta
                    </option>
                    <option value="quadrado">
                      Quadrado
                    </option>
                    <option value="ponto">
                      Ponto
                    </option>
                    <option value="linha">
                      Linha
                    </option>
                  </select>
                  <span style={{ "color": "var(--muted)" }}>
                    Tamanho
                  </span>
                  <div style={{ "gridColumn": "span 2", "display": "flex", "padding": "2px", "gap": "2px", "background": "var(--t-sunk)", "borderRadius": "11px", "width": "fit-content" }}>
                    {(v.indSizes || []).map((z: any, i14: number) => (
                      <React.Fragment key={i14}>
                        <div onClick={z.onClick} style={{ "width": "36px", "height": "24px", "borderRadius": "8px", "display": "flex", "alignItems": "center", "justifyContent": "center", "background": z.bg, "boxShadow": z.shadow, "color": z.color, "cursor": "pointer", "fontSize": "12px" }}>
                          {z.label}
                        </div>
                      </React.Fragment>
                    ))}
                  </div>
                  <span style={{ "color": "var(--muted)" }}>
                    Cor
                  </span>
                  <div style={{ "gridColumn": "span 2", "display": "flex", "gap": "6px" }}>
                    {(v.indColors || []).map((w: any, i15: number) => (
                      <React.Fragment key={i15}>
                        <div onClick={w.onClick} style={{ "width": "24px", "height": "24px", "flex": "none", "borderRadius": "6px", "background": w.value, "boxShadow": `inset 0 0 0 1px rgba(128,128,128,.45),0 0 0 2px var(--panel),0 0 0 3.5px ${w.ring ?? ""}`, "cursor": "pointer" }} />
                      </React.Fragment>
                    ))}
                    <label title="Outra cor…" style={{ "width": "24px", "height": "24px", "flex": "none", "borderRadius": "6px", "position": "relative", "cursor": "pointer", "border": "1px dashed var(--t-border)", "boxSizing": "border-box", "display": "flex", "alignItems": "center", "justifyContent": "center", "color": "var(--muted)", "boxShadow": `0 0 0 2px var(--panel),0 0 0 3.5px ${v.indPickRing ?? ""}` }}>
                      <svg viewBox="0 0 16 16" style={{ "width": "11px", "height": "11px", "fill": "none", "stroke": "currentColor", "strokeWidth": "1.6", "strokeLinecap": "round" }}>
                        <path d="M8 3v10M3 8h10" />
                      </svg>
                      <input type="color" value={v.indColor} onInput={v.onIndPick} onChange={v.onIndPick} style={{ "position": "absolute", "inset": "0", "width": "100%", "height": "100%", "opacity": "0", "cursor": "pointer", "border": "none", "padding": "0" }} />
                    </label>
                    <input value={v.indColorHex} onChange={v.onIndHex} onBlur={v.onIndHexBlur} spellCheck={false} maxLength={7} style={{ "width": "84px", "height": "24px", "flex": "none", "borderRadius": "6px", "border": "1px solid transparent", "background": "var(--field)", "color": "var(--text)", "font": "12px var(--f-num)", "padding": "0 6px", "textTransform": "uppercase", "userSelect": "text" }} />
                  </div>
                  <span style={{ "color": "var(--muted)" }}>
                    Posição
                  </span>
                  <input type="range" min="10" max="70" step="1" value={v.readPct} onChange={v.onRead} />
                  <span style={{ "font": "12px var(--f-num)", "textAlign": "right" }}>
                    {v.readPct}%
                  </span>
                  <span />
                  <div onClick={v.toggleReadingLine} style={{ "gridColumn": "span 2", "display": "flex", "alignItems": "center", "gap": "7px", "cursor": "default" }}>
                    <div style={{ "width": "15px", "height": "15px", "borderRadius": "4px", "border": `1px solid ${v.readingLineCk.bd ?? ""}`, "background": v.readingLineCk.bg, "display": "flex", "alignItems": "center", "justifyContent": "center" }}>
                      <svg viewBox="0 0 16 16" style={{ "width": "11px", "height": "11px", "fill": "none", "stroke": "var(--invtext)", "strokeWidth": "2.2", "opacity": v.readingLineCk.op }}>
                        <path d="M3 8.5l3 3 7-7" />
                      </svg>
                    </div>
                    Linha de leitura atrás do texto
                  </div>
                </div>
                <div style={{ "height": "1px", "background": "var(--line)", "margin": "0 16px" }} />
                <div style={{ "padding": "14px 16px 10px", "fontFamily": "var(--f-num)", "fontSize": "10px", "letterSpacing": "1.5px", "textTransform": "uppercase", "color": "var(--t-faint)" }}>
                  Imagem de fundo
                </div>
                <div style={{ "display": "grid", "gridTemplateColumns": "84px minmax(0,1fr) 40px", "gap": "12px 10px", "alignItems": "center", "padding": "0 16px" }}>
                  <span style={{ "color": "var(--muted)" }}>
                    Arquivo
                  </span>
                  <div style={{ "gridColumn": "span 2", "display": "flex", "gap": "6px" }}>
                    <button onClick={v.pickBg} style={{ "height": "28px", "padding": "0 12px", "borderRadius": "980px", "border": "none", "background": "var(--t-soft)", "color": "var(--text)", "font": "inherit", "cursor": "pointer" }} className="h2">
                      Escolher…
                    </button>
                    {v.hasBgImg ? (
                      <>
                      <button onClick={v.clearBg} style={{ "height": "28px", "padding": "0 12px", "borderRadius": "980px", "border": "none", "background": "var(--t-soft)", "color": "var(--text)", "font": "inherit", "cursor": "pointer" }}>
                        Remover
                      </button>
                      </>
                    ) : null}
                  </div>
                  <span style={{ "color": "var(--muted)" }}>
                    Opacidade
                  </span>
                  <input type="range" min="0.05" max="0.6" step="0.05" value={v.bgImgOp} onChange={v.onBgOp} />
                  <span style={{ "font": "12px var(--f-num)", "textAlign": "right" }}>
                    {v.bgImgOpLabel}
                  </span>
                </div>
                </>
              ) : null}
              {v.tabTelas ? (
                <>
                <div style={{ "padding": "8px 16px 10px", "fontFamily": "var(--f-num)", "fontSize": "10px", "letterSpacing": "1.5px", "textTransform": "uppercase", "color": "var(--t-faint)" }}>
                  Saída · {v.pvLabel}
                </div>
                <div style={{ "padding": "0 16px 16px" }}>
                  <div ref={v.pvRef} onWheel={v.onTvWheel} onDoubleClick={v.toggleTalent} title="Duplo clique: abrir a janela do talento" style={{ "position": "relative", "width": "100%", "aspectRatio": "16/9", "overflow": "hidden", "borderRadius": "11px", "boxShadow": `0 0 0 1px ${v.pvBorder ?? ""}`, "background": v.bg, "containerType": "inline-size" }}>
                    {v.hasBgImg ? (
                      <>
                      <div style={{ "position": "absolute", "inset": "0", "backgroundImage": `url('${v.bgImg ?? ""}')`, "backgroundSize": "cover", "opacity": v.bgImgOp }} />
                      </>
                    ) : null}
                    <div style={{ "position": "absolute", "inset": "0", "transform": v.pvFlip }}>
                      <div ref={v.pcRef} data-pr="1" data-script="1" style={{ "position": "absolute", "left": "0", "top": "0", "width": "1000px", "boxSizing": "border-box", "transformOrigin": "0 0", "padding": `0 ${v.prRight ?? ""}px 0 ${v.prMargin ?? ""}px`, "fontFamily": v.fontFam, "fontSize": `${v.fontSize ?? ""}px`, "lineHeight": v.lineH, "color": v.fg, "textAlign": v.pvAlign }} />
                      {v.readingLine ? (
                        <>
                        <div style={{ "position": "absolute", "left": "0", "right": "0", "top": `calc(${v.readPct ?? ""}% + ${v.halfLineCq ?? ""})`, "height": "2px", "background": v.indColorV, "opacity": ".35", "pointerEvents": "none" }} />
                        </>
                      ) : null}
                      {v.ovFaixa ? (
                        <>
                        <div style={{ "position": "absolute", "left": "0", "right": "0", "top": "0", "height": `${v.readPct ?? ""}%`, "background": v.dimColor, "pointerEvents": "none" }} />
                        <div style={{ "position": "absolute", "left": "0", "right": "0", "bottom": "0", "top": `calc(${v.readPct ?? ""}% + ${v.lineCq ?? ""})`, "background": v.dimColor, "pointerEvents": "none" }} />
                        </>
                      ) : null}
                      {v.ovFade ? (
                        <>
                        <div style={{ "position": "absolute", "left": "0", "right": "0", "top": "0", "height": `${v.readPct ?? ""}%`, "background": `linear-gradient(${v.dimColor ?? ""},transparent)`, "pointerEvents": "none" }} />
                        <div style={{ "position": "absolute", "left": "0", "right": "0", "bottom": "0", "top": `calc(${v.readPct ?? ""}% + ${v.lineCq ?? ""})`, "background": `linear-gradient(transparent,${v.dimColor ?? ""})`, "pointerEvents": "none" }} />
                        </>
                      ) : null}
                      {v.ovDestaque ? (
                        <>
                        <div style={{ "position": "absolute", "left": "0", "right": "0", "top": `${v.readPct ?? ""}%`, "height": v.lineCq, "background": v.hiColor, "pointerEvents": "none" }} />
                        </>
                      ) : null}
                      <div style={{ "position": "absolute", "left": v.indLeft, "top": `calc(${v.readPct ?? ""}% + ${v.halfLineCq ?? ""})`, "transform": "translateY(-50%)", "width": v.indW, "height": v.indH, "background": v.indColorV, "clipPath": v.indClip, "borderRadius": v.indRadius, "display": v.indDisplay, "pointerEvents": "none" }} />
                    </div>
                  </div>
                </div>
                <div style={{ "height": "1px", "background": "var(--line)", "margin": "0 16px" }} />
                <div style={{ "padding": "14px 16px 6px", "fontFamily": "var(--f-num)", "fontSize": "10px", "letterSpacing": "1.5px", "textTransform": "uppercase", "color": "var(--t-faint)" }}>
                  Telas conectadas
                </div>
                <div style={{ "display": "flex", "flexDirection": "column", "padding": "0 8px" }}>
                  {(v.displays || []).map((d: any, i16: number) => (
                    <React.Fragment key={i16}>
                      <div style={{ "display": "flex", "flexDirection": "column", "gap": "8px", "padding": "10px 8px", "borderRadius": "8px", "background": d.rowBg }}>
                        <div style={{ "display": "flex", "alignItems": "center", "gap": "10px" }}>
                          <div onClick={d.onToggle} title="Abrir prompter nesta tela" style={{ "width": "15px", "height": "15px", "flex": "none", "borderRadius": "4px", "border": `1px solid ${d.ck.bd ?? ""}`, "background": d.ck.bg, "display": "flex", "alignItems": "center", "justifyContent": "center", "cursor": "pointer" }}>
                            <svg viewBox="0 0 16 16" style={{ "width": "11px", "height": "11px", "fill": "none", "stroke": "var(--invtext)", "strokeWidth": "2.2", "opacity": d.ck.op }}>
                              <path d="M3 8.5l3 3 7-7" />
                            </svg>
                          </div>
                          <div style={{ "flex": "1", "minWidth": "0", "display": "flex", "flexDirection": "column" }}>
                            <input value={d.nick} onChange={d.onNick} title="Clique para renomear" style={{ "height": "22px", "background": "transparent", "border": "1px solid transparent", "borderRadius": "4px", "color": "var(--text)", "font": "inherit", "fontWeight": "600", "padding": "0 4px", "marginLeft": "-5px", "minWidth": "0", "userSelect": "text" }} className="h5" />
                            <span style={{ "fontSize": "11px", "color": "var(--muted)", "whiteSpace": "nowrap", "overflow": "hidden", "textOverflow": "ellipsis" }}>
                              {d.desc}
                            </span>
                          </div>
                        </div>
                        <div style={{ "display": "flex", "gap": "4px", "paddingLeft": "25px" }}>
                          {(d.tools || []).map((b: any, i17: number) => (
                            <React.Fragment key={i17}>
                              <div onClick={b.onClick} title={b.title} style={{ "width": "30px", "height": "26px", "borderRadius": "6px", "display": "flex", "alignItems": "center", "justifyContent": "center", "background": b.bg, "border": "none", "color": b.fg, "cursor": "pointer" }}>
                                <svg viewBox="0 0 16 16" style={{ "width": "14px", "height": "14px", "fill": "none", "stroke": "currentColor", "strokeWidth": "1.5", "strokeLinecap": "round", "strokeLinejoin": "round" }}>
                                  <path d={b.d} />
                                </svg>
                              </div>
                            </React.Fragment>
                          ))}
                        </div>
                      </div>
                    </React.Fragment>
                  ))}
                </div>
                <div style={{ "padding": "8px 16px 0" }}>
                  <button onClick={v.addWindowOutput} style={{ "height": "28px", "width": "100%", "borderRadius": "980px", "border": "none", "background": "var(--t-soft)", "color": "var(--text)", "font": "inherit", "cursor": "pointer" }} className="h2">
                    Nova janela de prompter
                  </button>
                </div>
                <div style={{ "height": "1px", "background": "var(--line)", "margin": "16px 16px 0" }} />
                <div style={{ "display": "flex", "justifyContent": "space-between", "alignItems": "center", "padding": "14px 16px 10px" }}>
                  <span style={{ "fontFamily": "var(--f-num)", "fontSize": "10px", "letterSpacing": "1.5px", "textTransform": "uppercase", "color": "var(--t-faint)" }}>
                    Saída NDI
                  </span>
                  <div onClick={v.toggleNdi} style={{ "width": "34px", "height": "20px", "borderRadius": "10px", "background": v.ndiT.track, "position": "relative", "cursor": "pointer", "flex": "none" }}>
                    <div style={{ "position": "absolute", "top": "2px", "left": v.ndiT.left, "width": "16px", "height": "16px", "borderRadius": "50%", "background": v.ndiT.knob, "boxShadow": "var(--shadow)" }} />
                  </div>
                </div>
                <div style={{ "display": "flex", "flexDirection": "column", "gap": "10px", "padding": "0 16px" }}>
                  <div style={{ "display": "flex", "alignItems": "center", "gap": "8px", "fontSize": "12px", "color": "var(--t-muted)" }}>
                    <span style={{ "width": "7px", "height": "7px", "borderRadius": "50%", "background": v.ndiDot, "flex": "none" }} />
                    <span style={{ "whiteSpace": "nowrap", "overflow": "hidden", "textOverflow": "ellipsis" }}>
                      {v.ndiStatus}
                    </span>
                  </div>
                  <div style={{ "display": "grid", "gridTemplateColumns": "84px minmax(0,1fr)", "gap": "10px", "alignItems": "center" }}>
                    <span style={{ "color": "var(--muted)" }}>
                      Nome da fonte
                    </span>
                    <input value={v.ndiName} onChange={v.onNdiName} style={{ "height": "32px", "borderRadius": "10px", "border": "1px solid transparent", "background": "var(--field)", "color": "var(--text)", "font": "inherit", "padding": "0 10px", "minWidth": "0", "userSelect": "text" }} />
                    <span style={{ "color": "var(--muted)" }}>
                      Formato
                    </span>
                    <select value={v.ndiFmt} onChange={v.onNdiFmt} style={{ "height": "32px", "borderRadius": "10px", "border": "1px solid transparent", "background": "var(--field)", "color": "var(--text)", "font": "inherit", "padding": "0 10px", "minWidth": "0", "userSelect": "text" }}>
                      <option value="1080p30">
                        1920 × 1080 · 30p
                      </option>
                      <option value="1080p60">
                        1920 × 1080 · 60p
                      </option>
                      <option value="1080p25">
                        1920 × 1080 · 25p
                      </option>
                      <option value="720p30">
                        1280 × 720 · 30p
                      </option>
                    </select>
                  </div>
                  {(v.ndiChecks || []).map((c: any, i18: number) => (
                    <React.Fragment key={i18}>
                      <div onClick={c.onClick} style={{ "display": "flex", "alignItems": "center", "gap": "8px", "cursor": "default", "minHeight": "24px" }}>
                        <div style={{ "width": "15px", "height": "15px", "borderRadius": "4px", "border": `1px solid ${c.bd ?? ""}`, "background": c.bg, "display": "flex", "alignItems": "center", "justifyContent": "center", "flex": "none" }}>
                          <svg viewBox="0 0 16 16" style={{ "width": "11px", "height": "11px", "fill": "none", "stroke": "var(--invtext)", "strokeWidth": "2.2", "opacity": c.op }}>
                            <path d="M3 8.5l3 3 7-7" />
                          </svg>
                        </div>
                        {c.label}
                      </div>
                    </React.Fragment>
                  ))}
                </div>
                <div style={{ "height": "1px", "background": "var(--line)", "margin": "16px 16px 0" }} />
                <div style={{ "display": "flex", "justifyContent": "space-between", "alignItems": "center", "padding": "14px 16px 10px" }}>
                  <span style={{ "fontFamily": "var(--f-num)", "fontSize": "10px", "letterSpacing": "1.5px", "textTransform": "uppercase", "color": "var(--t-faint)" }}>
                    Monitor web
                  </span>
                  <div onClick={v.toggleWeb} style={{ "width": "34px", "height": "20px", "borderRadius": "10px", "background": v.webT.track, "position": "relative", "cursor": "pointer", "flex": "none" }}>
                    <div style={{ "position": "absolute", "top": "2px", "left": v.webT.left, "width": "16px", "height": "16px", "borderRadius": "50%", "background": v.webT.knob, "boxShadow": "var(--shadow)" }} />
                  </div>
                </div>
                <div style={{ "display": "flex", "flexDirection": "column", "gap": "10px", "padding": "0 16px" }}>
                  <div style={{ "display": "flex", "alignItems": "center", "gap": "8px", "fontSize": "12px", "color": "var(--t-muted)" }}>
                    <span style={{ "width": "7px", "height": "7px", "borderRadius": "50%", "background": v.webDot, "flex": "none" }} />
                    <span style={{ "whiteSpace": "nowrap", "overflow": "hidden", "textOverflow": "ellipsis" }}>
                      {v.webStatus}
                    </span>
                  </div>
                  <div style={{ "display": "flex", "gap": "6px" }}>
                    <input value={v.webUrl} readOnly style={{ "height": "32px", "borderRadius": "10px", "border": "1px solid transparent", "background": "var(--field)", "color": "var(--text)", "font": "inherit", "padding": "0 10px", "minWidth": "0", "userSelect": "text", "flex": "1", "fontFamily": "var(--f-num)", "fontSize": "12px", "color": "var(--t-muted)" }} />
                    <button onClick={v.copyUrl} style={{ "height": "30px", "padding": "0 14px", "borderRadius": "980px", "border": "none", "background": "var(--t-soft)", "color": "var(--text)", "font": "inherit", "cursor": "pointer", "whiteSpace": "nowrap" }}>
                      {v.copyLabel}
                    </button>
                  </div>
                  <div style={{ "display": "grid", "gridTemplateColumns": "84px minmax(0,1fr)", "gap": "10px", "alignItems": "center" }}>
                    <span style={{ "color": "var(--muted)" }}>
                      Senha
                    </span>
                    <input value={v.webPass} onChange={v.onWebPass} placeholder="opcional" style={{ "height": "32px", "borderRadius": "10px", "border": "1px solid transparent", "background": "var(--field)", "color": "var(--text)", "font": "inherit", "padding": "0 10px", "minWidth": "0", "userSelect": "text" }} />
                  </div>
                  {(v.webChecks || []).map((c: any, i19: number) => (
                    <React.Fragment key={i19}>
                      <div onClick={c.onClick} style={{ "display": "flex", "alignItems": "center", "gap": "8px", "cursor": "default", "minHeight": "24px" }}>
                        <div style={{ "width": "15px", "height": "15px", "borderRadius": "4px", "border": `1px solid ${c.bd ?? ""}`, "background": c.bg, "display": "flex", "alignItems": "center", "justifyContent": "center", "flex": "none" }}>
                          <svg viewBox="0 0 16 16" style={{ "width": "11px", "height": "11px", "fill": "none", "stroke": "var(--invtext)", "strokeWidth": "2.2", "opacity": c.op }}>
                            <path d="M3 8.5l3 3 7-7" />
                          </svg>
                        </div>
                        {c.label}
                      </div>
                    </React.Fragment>
                  ))}
                  <button onClick={v.openMonitor} style={{ "height": "30px", "padding": "0 14px", "borderRadius": "980px", "border": "none", "background": "var(--t-soft)", "color": "var(--text)", "font": "inherit", "cursor": "pointer", "whiteSpace": "nowrap" }}>
                    Abrir monitor web
                  </button>
                </div>
                </>
              ) : null}
              {v.tabVoz ? (
                <>
                <div style={{ "padding": "8px 16px 10px", "fontFamily": "var(--f-num)", "fontSize": "10px", "letterSpacing": "1.5px", "textTransform": "uppercase", "color": "var(--t-faint)" }}>
                  Acompanhamento por voz
                </div>
                <div style={{ "display": "flex", "flexDirection": "column", "gap": "10px", "padding": "0 16px 16px" }}>
                  <div style={{ "display": "flex", "alignItems": "center", "gap": "8px" }}>
                    <span style={{ "width": "8px", "height": "8px", "borderRadius": "50%", "background": v.vDot, "flex": "none" }} />
                    <span style={{ "font": "600 11px var(--f-num)", "letterSpacing": ".08em" }}>
                      {v.vCode}
                    </span>
                    <span style={{ "color": "var(--muted)", "whiteSpace": "nowrap", "overflow": "hidden", "textOverflow": "ellipsis" }}>
                      {v.vTitle}
                    </span>
                  </div>
                  <div style={{ "height": "72px", "overflow": "hidden", "display": "flex", "flexWrap": "wrap", "gap": "2px 6px", "alignContent": "flex-start", "padding": "8px 10px", "borderRadius": "6px", "background": "var(--field)", "border": "1px solid var(--line)", "font": "12px var(--f-num)" }}>
                    {v.noHeard ? (
                      <>
                      <span style={{ "color": "var(--faint)" }}>
                        {v.vErrOrIdle}
                      </span>
                      </>
                    ) : null}
                    {(v.heard || []).map((h: any, i20: number) => (
                      <React.Fragment key={i20}>
                        <span style={{ "color": h.color, "textDecoration": h.deco }}>
                          {h.w}
                        </span>
                      </React.Fragment>
                    ))}
                  </div>
                  <div style={{ "display": "flex", "gap": "6px" }}>
                    <button onClick={v.startMic} style={{ "flex": "1", "height": "30px", "borderRadius": "980px", "border": "none", "background": v.micPrimary, "color": v.micFg, "font": "600 12px var(--f-sans)", "cursor": "pointer" }}>
                      {v.micLabel}
                    </button>
                    <button onClick={v.startSim} style={{ "flex": "1", "height": "30px", "borderRadius": "980px", "border": "none", "background": v.simBg, "color": "var(--text)", "font": "inherit", "cursor": "pointer" }}>
                      {v.simLabel}
                    </button>
                  </div>
                </div>
                <div style={{ "height": "1px", "background": "var(--line)", "margin": "0 16px" }} />
                <div style={{ "padding": "14px 16px 10px", "fontFamily": "var(--f-num)", "fontSize": "10px", "letterSpacing": "1.5px", "textTransform": "uppercase", "color": "var(--t-faint)" }}>
                  Entrada de áudio
                </div>
                <div style={{ "display": "grid", "gridTemplateColumns": "84px minmax(0,1fr)", "gap": "12px 10px", "alignItems": "center", "padding": "0 16px 16px" }}>
                  <span style={{ "color": "var(--muted)" }}>
                    Driver
                  </span>
                  <div style={{ "display": "flex", "padding": "2px", "gap": "2px", "background": "var(--t-sunk)", "borderRadius": "11px", "width": "fit-content" }}>
                    {(v.drivers || []).map((z: any, i21: number) => (
                      <React.Fragment key={i21}>
                        <div onClick={z.onClick} style={{ "padding": "0 14px", "height": "24px", "borderRadius": "8px", "display": "flex", "alignItems": "center", "background": z.bg, "boxShadow": z.shadow, "color": z.color, "cursor": "pointer", "fontSize": "12px" }}>
                          {z.label}
                        </div>
                      </React.Fragment>
                    ))}
                  </div>
                  <span style={{ "color": "var(--muted)" }}>
                    Dispositivo
                  </span>
                  <select style={{ "height": "32px", "borderRadius": "10px", "border": "1px solid transparent", "background": "var(--field)", "color": "var(--text)", "font": "inherit", "padding": "0 6px", "minWidth": "0" }}>
                    {(v.devices || []).map((o: any, i22: number) => (
                      <React.Fragment key={i22}>
                        <option>
                          {o}
                        </option>
                      </React.Fragment>
                    ))}
                  </select>
                  {v.isAsio ? (
                    <>
                    <span style={{ "color": "var(--muted)" }}>
                      Canal
                    </span>
                    <select value={v.asioCh} onChange={v.onAsioCh} style={{ "height": "32px", "width": "120px", "borderRadius": "10px", "border": "1px solid transparent", "background": "var(--field)", "color": "var(--text)", "font": "inherit", "padding": "0 6px" }}>
                      {(v.channels || []).map((c: any, i23: number) => (
                        <React.Fragment key={i23}>
                          <option value={c.v}>
                            {c.l}
                          </option>
                        </React.Fragment>
                      ))}
                    </select>
                    </>
                  ) : null}
                  <span style={{ "color": "var(--muted)" }}>
                    Nível
                  </span>
                  <div style={{ "position": "relative", "height": "6px", "borderRadius": "3px", "background": "var(--sel)", "overflow": "visible" }}>
                    <div style={{ "position": "absolute", "left": "0", "top": "0", "bottom": "0", "borderRadius": "3px", "width": `${v.levelPct ?? ""}%`, "background": v.levelColor }} />
                    <div style={{ "position": "absolute", "top": "-3px", "bottom": "-3px", "left": `${v.gatePct ?? ""}%`, "width": "2px", "borderRadius": "1px", "background": "var(--muted)" }} />
                  </div>
                  <span style={{ "color": "var(--muted)" }}>
                    Noise gate
                  </span>
                  <div style={{ "display": "flex", "alignItems": "center", "gap": "10px" }}>
                    <input type="range" min="-70" max="-20" step="1" value={v.gate} onChange={v.onGate} style={{ "flex": "1", "minWidth": "0" }} />
                    <span style={{ "font": "12px var(--f-num)", "width": "50px", "textAlign": "right" }}>
                      {v.gate} dB
                    </span>
                  </div>
                  <span style={{ "color": "var(--muted)" }}>
                    Idioma
                  </span>
                  <select value={v.vLang} onChange={v.onVLang} style={{ "height": "32px", "borderRadius": "10px", "border": "1px solid transparent", "background": "var(--field)", "color": "var(--text)", "font": "inherit", "padding": "0 6px", "minWidth": "0" }}>
                    <option value="pt-BR">
                      Português (Brasil)
                    </option>
                    <option value="pt-PT">
                      Português (Portugal)
                    </option>
                    <option value="es-ES">
                      Español
                    </option>
                    <option value="en-US">
                      English (US)
                    </option>
                  </select>
                </div>
                <div style={{ "height": "1px", "background": "var(--line)", "margin": "0 16px" }} />
                <div style={{ "padding": "14px 16px 10px", "fontFamily": "var(--f-num)", "fontSize": "10px", "letterSpacing": "1.5px", "textTransform": "uppercase", "color": "var(--t-faint)" }}>
                  Sensibilidade
                </div>
                <div style={{ "display": "grid", "gridTemplateColumns": "84px minmax(0,1fr) 40px", "gap": "12px 10px", "alignItems": "center", "padding": "0 16px" }}>
                  <span style={{ "color": "var(--muted)" }}>
                    Busca à frente
                  </span>
                  <input type="range" min="8" max="40" step="1" value={v.win} onChange={v.onWin} />
                  <span style={{ "font": "12px var(--f-num)", "textAlign": "right" }}>
                    {v.win}
                  </span>
                  <span style={{ "color": "var(--muted)" }}>
                    Improviso após
                  </span>
                  <input type="range" min="2" max="6" step="1" value={v.tol} onChange={v.onTol} />
                  <span style={{ "font": "12px var(--f-num)", "textAlign": "right" }}>
                    {v.tol}
                  </span>
                </div>
                </>
              ) : null}
              {v.tabVideo ? (
                <>
                <div style={{ "padding": "8px 16px 10px", "fontFamily": "var(--f-num)", "fontSize": "10px", "letterSpacing": "1.5px", "textTransform": "uppercase", "color": "var(--t-faint)" }}>
                  Gravação
                </div>
                <div style={{ "display": "flex", "flexDirection": "column", "gap": "10px", "padding": "0 16px" }}>
                  <div style={{ "position": "relative", "width": "100%", "aspectRatio": "16/9", "borderRadius": "8px", "background": "#0e0e0e", "display": "flex", "alignItems": "center", "justifyContent": "center", "overflow": "hidden" }}>
                    <video ref={v.videoRef} autoPlay muted playsInline style={{ "position": "absolute", "inset": "0", "width": "100%", "height": "100%", "objectFit": "cover", "transform": "scaleX(-1)", "visibility": v.camVis }} />
                    {v.camOff ? (
                      <>
                      <span style={{ "color": "#777", "fontSize": "12px" }}>
                        {v.camMsg}
                      </span>
                      </>
                    ) : null}
                    {v.recording ? (
                      <>
                      <div style={{ "position": "absolute", "left": "8px", "top": "8px", "display": "flex", "alignItems": "center", "gap": "6px", "padding": "3px 8px", "borderRadius": "5px", "background": "rgba(0,0,0,.6)", "color": "#fff", "font": "500 11px var(--f-num)" }}>
                        <span style={{ "width": "7px", "height": "7px", "borderRadius": "50%", "background": "var(--live)" }} />
                        {v.recLabel}
                      </div>
                      </>
                    ) : null}
                  </div>
                  <div style={{ "display": "flex", "gap": "6px" }}>
                    <button onClick={v.toggleCam} style={{ "flex": "1", "height": "30px", "borderRadius": "980px", "border": "none", "background": "var(--t-soft)", "color": "var(--text)", "font": "inherit", "cursor": "pointer" }} className="h2">
                      {v.camBtn}
                    </button>
                    <button onClick={v.toggleRec} style={{ "flex": "1", "height": "30px", "borderRadius": "980px", "border": "none", "background": v.recBg, "color": v.recFg, "font": "600 12px var(--f-sans)", "cursor": "pointer" }}>
                      {v.recBtn}
                    </button>
                  </div>
                  <div onClick={v.toggleRecWithPrompt} style={{ "display": "flex", "alignItems": "center", "gap": "7px", "cursor": "default" }}>
                    <div style={{ "width": "15px", "height": "15px", "borderRadius": "4px", "border": `1px solid ${v.recWithCk.bd ?? ""}`, "background": v.recWithCk.bg, "display": "flex", "alignItems": "center", "justifyContent": "center" }}>
                      <svg viewBox="0 0 16 16" style={{ "width": "11px", "height": "11px", "fill": "none", "stroke": "var(--invtext)", "strokeWidth": "2.2", "opacity": v.recWithCk.op }}>
                        <path d="M3 8.5l3 3 7-7" />
                      </svg>
                    </div>
                    Gravar junto com o prompt
                  </div>
                  {v.hasRecUrl ? (
                    <>
                    <a href={v.recUrl} download={v.recFile} style={{ "height": "30px", "borderRadius": "6px", "border": "1px solid var(--line)", "display": "flex", "alignItems": "center", "justifyContent": "center", "textDecoration": "none" }}>
                      Baixar gravação · {v.lastRecLabel}
                    </a>
                    </>
                  ) : null}
                </div>
                </>
              ) : null}
            </div>
          </div>
          </>
        ) : null}
      </div>
      <div style={{ "flex": "none", "minHeight": "60px", "display": "flex", "flexWrap": "wrap", "alignItems": "center", "gap": "8px 24px", "padding": "8px 18px", "background": "var(--t-ui)", "borderTop": "1px solid var(--t-border)" }}>
        <div style={{ "display": "flex", "gap": "22px", "flex": "none" }}>
          {(v.readouts || []).map((r: any, i24: number) => (
            <React.Fragment key={i24}>
              <div onClick={r.onClick} style={{ "display": "flex", "flexDirection": "column", "gap": "1px", "cursor": "default" }}>
                <span style={{ "fontFamily": "var(--f-num)", "fontSize": "9.5px", "letterSpacing": "1.5px", "textTransform": "uppercase", "color": "var(--t-faint)" }}>
                  {r.label}
                </span>
                <span style={{ "font": "600 17px var(--f-num)", "letterSpacing": "-.3px", "fontVariantNumeric": "tabular-nums", "whiteSpace": "nowrap" }}>
                  {r.value}
                </span>
              </div>
            </React.Fragment>
          ))}
        </div>
        <div style={{ "flex": "1 1 auto", "display": "flex", "justifyContent": "center", "alignItems": "center", "gap": "4px" }}>
          {(v.transport || []).map((b: any, i25: number) => (
            <React.Fragment key={i25}>
              <button onClick={b.onClick} onMouseDown={b.onDown} onMouseUp={b.onUp} onMouseLeave={b.onUp} title={b.title} style={{ "width": b.w, "height": "34px", "display": "flex", "alignItems": "center", "justifyContent": "center", "borderRadius": "980px", "border": b.border, "background": b.bg, "color": b.fg, "cursor": "pointer" }} className="h6 a7">
                <svg viewBox="0 0 16 16" style={{ "width": "14px", "height": "14px", "fill": b.fill, "stroke": b.stroke, "strokeWidth": "1.6", "strokeLinecap": "round", "strokeLinejoin": "round" }}>
                  <path d={b.d} />
                </svg>
              </button>
            </React.Fragment>
          ))}
        </div>
        <div style={{ "display": "flex", "alignItems": "center", "gap": "10px", "flex": "1 1 240px", "justifyContent": "flex-end", "minWidth": "0" }}>
          <input type="range" min="0" max="1" step="0.01" value={v.speedT} onChange={v.onSpeed} style={{ "flex": "1", "minWidth": "70px", "maxWidth": "200px" }} />
          <span style={{ "font": "500 14px var(--f-num)", "minWidth": "44px", "textAlign": "right", "color": v.speedColor }}>
            {v.speedValue}
          </span>
          <select value={v.unit} onChange={v.onUnit} style={{ "height": "32px", "borderRadius": "10px", "border": "1px solid transparent", "background": "var(--field)", "color": "var(--text)", "font": "inherit", "padding": "0 6px" }}>
            <option value="0">
              linhas/s
            </option>
            <option value="1">
              linhas/min
            </option>
            <option value="2">
              palavras/s
            </option>
            <option value="3">
              palavras/min
            </option>
          </select>
        </div>
      </div>
      <div style={{ "height": "24px", "flex": "none", "display": "flex", "alignItems": "center", "gap": "16px", "padding": "0 14px", "borderTop": "1px solid var(--line)", "fontSize": "12px", "color": "var(--muted)", "whiteSpace": "nowrap", "overflow": "hidden" }}>
        <span style={{ "display": "flex", "alignItems": "center", "gap": "6px" }}>
          <span style={{ "width": "7px", "height": "7px", "borderRadius": "50%", "background": v.statusDot }} />
          {v.statusLabel}
        </span>
        <span>
          {v.screensLabel}
        </span>
        <span>
          Voz: {v.vShort}
        </span>
        <span>
          {v.netLabel}
        </span>
        <span>
          {v.saveLabel}
        </span>
        <span style={{ "flex": "1" }} />
        <span>
          {v.lastKeyLabel}
        </span>
      </div>
      <input ref={v.openFile} type="file" accept={v.openAccept} onChange={v.onOpenFile} style={{ "display": "none" }} />
      <input ref={v.bgFile} type="file" accept="image/*,.svg,image/svg+xml" onChange={v.onBgFile} style={{ "display": "none" }} />
      <div onMouseMove={v.onTvMove} style={{ "position": "fixed", "inset": "0", "zIndex": "80", "visibility": v.talentVis, "background": v.bg, "overflow": "hidden", "cursor": v.tvCursor }}>
        {v.hasBgImg ? (
          <>
          <div style={{ "position": "absolute", "inset": "0", "backgroundImage": `url('${v.bgImg ?? ""}')`, "backgroundSize": "cover", "backgroundPosition": "center", "opacity": v.bgImgOp }} />
          </>
        ) : null}
        <div ref={v.tvRef} onWheel={v.onTvWheel} onDoubleClick={v.startInline} onPointerDown={v.onTvDown} onPointerMove={v.onTvPMove} onPointerUp={v.onTvUp} onPointerLeave={v.onTvUp} style={{ "position": "absolute", "inset": "0", "overflow": "hidden", "containerType": "inline-size", "touchAction": "none" }}>
          <div style={{ "position": "absolute", "inset": "0", "transform": v.tvFlip }}>
            {v.readingLine ? (
              <>
              <div style={{ "position": "absolute", "left": "0", "right": "0", "top": `calc(${v.readPct ?? ""}% + ${v.halfLineCq ?? ""})`, "height": "2px", "background": v.indColorV, "opacity": ".3" }} />
              </>
            ) : null}
            <div ref={v.tcRef} data-pr="1" data-script="1" style={{ "position": "absolute", "left": "0", "top": "0", "width": "1000px", "boxSizing": "border-box", "transformOrigin": "0 0", "padding": `0 ${v.prRight ?? ""}px 0 ${v.prMargin ?? ""}px`, "fontFamily": v.fontFam, "fontSize": `${v.fontSize ?? ""}px`, "lineHeight": v.lineH, "color": v.fg, "textAlign": v.tvAlign, "caretColor": v.fg, "willChange": "transform" }} />
            {v.ovFaixa ? (
              <>
              <div style={{ "position": "absolute", "left": "0", "right": "0", "top": "0", "height": `${v.readPct ?? ""}%`, "background": v.dimColor, "pointerEvents": "none" }} />
              <div style={{ "position": "absolute", "left": "0", "right": "0", "bottom": "0", "top": `calc(${v.readPct ?? ""}% + ${v.lineCq ?? ""})`, "background": v.dimColor, "pointerEvents": "none" }} />
              </>
            ) : null}
            {v.ovFade ? (
              <>
              <div style={{ "position": "absolute", "left": "0", "right": "0", "top": "0", "height": `${v.readPct ?? ""}%`, "background": `linear-gradient(${v.dimColor ?? ""},transparent)`, "pointerEvents": "none" }} />
              <div style={{ "position": "absolute", "left": "0", "right": "0", "bottom": "0", "top": `calc(${v.readPct ?? ""}% + ${v.lineCq ?? ""})`, "background": `linear-gradient(transparent,${v.dimColor ?? ""})`, "pointerEvents": "none" }} />
              </>
            ) : null}
            {v.ovDestaque ? (
              <>
              <div style={{ "position": "absolute", "left": "0", "right": "0", "top": `${v.readPct ?? ""}%`, "height": v.lineCq, "background": v.hiColor, "pointerEvents": "none" }} />
              </>
            ) : null}
            <div style={{ "position": "absolute", "left": v.indLeft, "top": `calc(${v.readPct ?? ""}% + ${v.halfLineCq ?? ""})`, "transform": "translateY(-50%)", "width": v.indW, "height": v.indH, "background": v.indColorV, "clipPath": v.indClip, "borderRadius": v.indRadius, "display": v.indDisplay, "pointerEvents": "none" }} />
          </div>
        </div>
        <div style={{ "position": "absolute", "inset": "0", "boxShadow": "inset 0 0 0 6px var(--live)", "opacity": v.blinkOp, "pointerEvents": "none" }} />
        <div style={{ "position": "absolute", "left": "0", "right": "0", "top": "0", "height": "46px", "display": "flex", "alignItems": "center", "gap": "4px", "padding": "0 10px", "background": "var(--panel)", "borderBottom": "1px solid var(--line)", "color": "var(--text)", "opacity": v.tbOp, "pointerEvents": v.tbPe, "transition": "opacity .2s" }}>
          <span style={{ "fontSize": "12px", "color": "var(--muted)", "padding": "0 8px", "whiteSpace": "nowrap" }}>
            {v.tvTitle}
          </span>
          <div style={{ "width": "1px", "height": "18px", "background": "var(--line)", "margin": "0 6px" }} />
          {(v.tvTools || []).map((b: any, i26: number) => (
            <React.Fragment key={i26}>
              <button onClick={b.onClick} title={b.title} style={{ "width": "32px", "height": "30px", "borderRadius": "980px", "background": b.bg, "border": "none", "color": b.fg, "cursor": "pointer", "display": "flex", "alignItems": "center", "justifyContent": "center" }}>
                <svg viewBox="0 0 16 16" style={{ "width": "15px", "height": "15px", "fill": "none", "stroke": "currentColor", "strokeWidth": "1.5", "strokeLinecap": "round", "strokeLinejoin": "round" }}>
                  <path d={b.d} />
                </svg>
              </button>
            </React.Fragment>
          ))}
          <div style={{ "display": "flex", "alignItems": "center", "gap": "8px", "padding": "0 10px", "color": "var(--muted)", "fontSize": "12px" }}>
            <span>
              Opacidade
            </span>
            <input type="range" min="0.2" max="1" step="0.05" value={v.ctrlOp} onChange={v.onCtrlOp} style={{ "width": "90px" }} />
          </div>
          <div style={{ "flex": "1" }} />
          <button onClick={v.editHere} title="Abrir o editor neste trecho (E)" style={{ "height": "30px", "padding": "0 12px", "display": "flex", "alignItems": "center", "gap": "7px", "borderRadius": "980px", "border": "none", "background": "var(--inv)", "color": "var(--invtext)", "font": "600 12px var(--f-sans)", "cursor": "pointer" }}>
            <svg viewBox="0 0 16 16" style={{ "width": "13px", "height": "13px", "fill": "none", "stroke": "currentColor", "strokeWidth": "1.4", "strokeLinejoin": "round" }}>
              <path d="M10.5 2.5l3 3-8 8h-3v-3z" />
            </svg>
            Editar trecho
          </button>
          <button onClick={v.toggleTalent} title="Fechar (Esc)" style={{ "width": "32px", "height": "30px", "borderRadius": "980px", "background": "transparent", "border": "none", "color": "var(--text)", "cursor": "pointer", "display": "flex", "alignItems": "center", "justifyContent": "center" }} className="h2">
            <svg viewBox="0 0 16 16" style={{ "width": "11px", "height": "11px", "fill": "none", "stroke": "currentColor", "strokeWidth": "1.4" }}>
              <path d="M3 3l10 10M13 3L3 13" />
            </svg>
          </button>
        </div>
        {v.inline ? (
          <>
          <div style={{ "position": "absolute", "left": "50%", "top": "58px", "transform": "translateX(-50%)", "display": "flex", "alignItems": "center", "gap": "10px", "padding": "4px 4px 4px 12px", "borderRadius": "8px", "background": "var(--raised)", "border": "1px solid var(--line)", "color": "var(--text)", "boxShadow": "0 8px 24px rgba(0,0,0,.4)", "whiteSpace": "nowrap" }}>
            <span style={{ "color": "var(--muted)" }}>
              Editando no prompter — posição mantida
            </span>
            <button onClick={v.finishInline} style={{ "height": "26px", "padding": "0 10px", "borderRadius": "980px", "border": "none", "background": "var(--inv)", "color": "var(--invtext)", "font": "600 12px var(--f-sans)", "cursor": "pointer" }}>
              Concluir · Esc
            </button>
          </div>
          </>
        ) : null}
        {v.talentHud ? (
          <>
          <div style={{ "position": "absolute", "left": "20px", "right": "20px", "top": "60px", "display": "flex", "alignItems": "flex-start", "justifyContent": "space-between", "gap": "16px", "pointerEvents": "none", "zIndex": "3", "color": "#fff" }}>
            <div style={{ "display": "flex", "alignItems": "center", "gap": "10px", "minWidth": "0", "padding": "8px 14px", "borderRadius": "12px", "background": "rgba(0,0,0,.62)" }}>
              {v.isLastBlock ? (
                <>
                <span style={{ "flex": "none", "padding": "3px 8px", "borderRadius": "980px", "background": "#e5484d", "font": "600 10px var(--f-num)", "letterSpacing": "1.2px" }}>
                  ÚLTIMO BLOCO
                </span>
                </>
              ) : null}
              <span style={{ "font": "600 13px var(--f-sans)", "whiteSpace": "nowrap", "overflow": "hidden", "textOverflow": "ellipsis" }}>
                {v.hudBlock}
              </span>
              <span style={{ "flex": "none", "font": "600 15px var(--f-num)", "color": v.blockLeftColor }}>
                {v.blockLeft}
              </span>
            </div>
            <div style={{ "flex": "none", "padding": "8px 14px", "borderRadius": "12px", "background": "rgba(0,0,0,.62)", "font": "600 15px var(--f-num)" }}>
              {v.clockV}
            </div>
          </div>
          </>
        ) : null}
        {v.hasFlash ? (
          <>
          <div style={{ "position": "absolute", "left": "50%", "bottom": "76px", "transform": "translateX(-50%)", "maxWidth": "calc(100% - 32px)", "boxSizing": "border-box", "display": "flex", "flexDirection": "row", "alignItems": "center", "gap": "10px", "padding": "7px 14px 7px 8px", "borderRadius": "980px", "background": "rgba(20,20,22,.82)", "color": "#fff", "pointerEvents": "none", "zIndex": "4" }}>
            {v.hasFlashSub ? (
              <>
              <span style={{ "display": "flex", "alignItems": "center", "gap": "5px", "height": "20px", "padding": "0 9px", "borderRadius": "980px", "background": v.flashSubBg, "font": "600 10px var(--f-num)", "letterSpacing": "1.2px", "textTransform": "uppercase" }}>
                <svg viewBox="0 0 16 16" style={{ "width": "9px", "height": "9px", "fill": "none", "stroke": "currentColor", "strokeWidth": "2.2", "strokeLinecap": "round", "strokeLinejoin": "round" }}>
                  <path d={v.flashArrow} />
                </svg>
                {v.flashSub}
              </span>
              </>
            ) : null}
            <span style={{ "font": "600 13px var(--f-num)", "whiteSpace": "nowrap" }}>
              {v.flashText}
            </span>
            {v.flashBar ? (
              <>
              <div style={{ "width": "72px", "height": "3px", "borderRadius": "2px", "background": "rgba(255,255,255,.2)", "flex": "none" }}>
                <div style={{ "height": "100%", "width": v.flashW, "borderRadius": "2px", "background": "#fff" }} />
              </div>
              </>
            ) : null}
          </div>
          </>
        ) : null}
        {v.counting ? (
          <>
          <div style={{ "position": "absolute", "inset": "0", "display": "flex", "alignItems": "center", "justifyContent": "center", "pointerEvents": "none" }}>
            <div style={{ "width": "160px", "height": "160px", "borderRadius": "50%", "background": "rgba(0,0,0,.55)", "display": "flex", "alignItems": "center", "justifyContent": "center", "font": "500 84px var(--f-num)", "color": "#fff" }}>
              {v.count}
            </div>
          </div>
          </>
        ) : null}
        {v.ctrlOn ? (
          <>
          <div style={{ "position": "absolute", "left": "50%", "bottom": "20px", "transform": "translateX(-50%)", "display": "flex", "alignItems": "center", "gap": "4px", "padding": "6px", "borderRadius": "12px", "background": "rgba(30,30,30,.92)", "border": "1px solid rgba(255,255,255,.1)", "color": "#e6e6e6", "opacity": v.ctrlOp }}>
            <button onClick={v.paraPrev} title="Parágrafo anterior ([)" style={{ "width": "36px", "height": "36px", "borderRadius": "980px", "border": "none", "background": "transparent", "color": "inherit", "cursor": "pointer", "display": "flex", "alignItems": "center", "justifyContent": "center" }} className="h8">
              <svg viewBox="0 0 16 16" style={{ "width": "15px", "height": "15px", "fill": "none", "stroke": "currentColor", "strokeWidth": "1.4", "strokeLinecap": "round", "strokeLinejoin": "round" }}>
                <path d="M3.5 8L8 3.5 12.5 8 M3.5 12.5L8 8 12.5 12.5" />
              </svg>
            </button>
            <button onClick={v.lineUp} title="Linha anterior" style={{ "width": "36px", "height": "36px", "borderRadius": "980px", "border": "none", "background": "transparent", "color": "inherit", "cursor": "pointer", "display": "flex", "alignItems": "center", "justifyContent": "center" }} className="h8">
              <svg viewBox="0 0 16 16" style={{ "width": "15px", "height": "15px", "fill": "none", "stroke": "currentColor", "strokeWidth": "1.4", "strokeLinecap": "round", "strokeLinejoin": "round" }}>
                <path d="M3.5 10L8 5.5l4.5 4.5" />
              </svg>
            </button>
            <button onClick={v.lineDown} title="Próxima linha" style={{ "width": "36px", "height": "36px", "borderRadius": "980px", "border": "none", "background": "transparent", "color": "inherit", "cursor": "pointer", "display": "flex", "alignItems": "center", "justifyContent": "center" }} className="h8">
              <svg viewBox="0 0 16 16" style={{ "width": "15px", "height": "15px", "fill": "none", "stroke": "currentColor", "strokeWidth": "1.4", "strokeLinecap": "round", "strokeLinejoin": "round" }}>
                <path d="M3.5 6L8 10.5 12.5 6" />
              </svg>
            </button>
            <button onClick={v.paraNext} title="Próximo parágrafo (])" style={{ "width": "36px", "height": "36px", "borderRadius": "980px", "border": "none", "background": "transparent", "color": "inherit", "cursor": "pointer", "display": "flex", "alignItems": "center", "justifyContent": "center" }} className="h8">
              <svg viewBox="0 0 16 16" style={{ "width": "15px", "height": "15px", "fill": "none", "stroke": "currentColor", "strokeWidth": "1.4", "strokeLinecap": "round", "strokeLinejoin": "round" }}>
                <path d="M3.5 3.5L8 8 12.5 3.5 M3.5 8L8 12.5 12.5 8" />
              </svg>
            </button>
            <button onClick={v.togglePlay} title="Play / pausa" style={{ "width": "52px", "height": "36px", "borderRadius": "980px", "border": "none", "background": v.playBg, "color": v.playFg, "cursor": "pointer", "display": "flex", "alignItems": "center", "justifyContent": "center" }}>
              <svg viewBox="0 0 16 16" style={{ "width": "14px", "height": "14px", "fill": "currentColor" }}>
                <path d={v.playIcon} />
              </svg>
            </button>
            <button onClick={v.rewind} title="Voltar ao início" style={{ "width": "36px", "height": "36px", "borderRadius": "980px", "border": "none", "background": "transparent", "color": "inherit", "cursor": "pointer", "display": "flex", "alignItems": "center", "justifyContent": "center" }} className="h8">
              <svg viewBox="0 0 16 16" style={{ "width": "14px", "height": "14px", "fill": "none", "stroke": "currentColor", "strokeWidth": "1.4", "strokeLinejoin": "round" }}>
                <path d="M3 3v10 M13 3L5.5 8 13 13z" />
              </svg>
            </button>
            <div style={{ "width": "1px", "height": "20px", "background": "rgba(255,255,255,.15)", "margin": "0 6px" }} />
            <input type="range" min="0" max="1" step="0.01" value={v.speedT} onChange={v.onSpeedTv} disabled={v.speedLock} style={{ "width": "150px" }} />
            <span style={{ "font": "500 13px var(--f-num)", "minWidth": "84px", "textAlign": "center" }}>
              {v.speedValue} {v.unitName}
            </span>
          </div>
          </>
        ) : null}
        {v.stampOn ? (
          <>
          <div onClick={v.toggleStamp} style={{ "position": "absolute", "right": "20px", "bottom": "28px", "padding": "4px 10px", "borderRadius": "6px", "background": "rgba(0,0,0,.55)", "color": "#fff", "font": "500 22px var(--f-num)", "cursor": "pointer" }}>
            {v.stampValue}
          </div>
          </>
        ) : null}
        {v.vActive ? (
          <>
          <div style={{ "position": "absolute", "left": "20px", "bottom": "28px", "display": "flex", "alignItems": "center", "gap": "8px", "padding": "6px 12px", "borderRadius": "6px", "background": "rgba(0,0,0,.6)", "color": "#fff", "font": "600 11px var(--f-num)", "letterSpacing": ".08em" }}>
            <span style={{ "width": "7px", "height": "7px", "borderRadius": "50%", "background": v.vDot }} />
            {v.vCode}
          </div>
          </>
        ) : null}
      </div>
      {v.dlgOpen ? (
        <>
        <div style={{ "position": "fixed", "inset": "0", "zIndex": "90", "background": "rgba(0,0,0,.35)", "display": "flex", "alignItems": "center", "justifyContent": "center", "padding": "20px" }}>
          <div style={{ "width": v.dlgW, "maxWidth": "100%", "maxHeight": "100%", "display": "flex", "flexDirection": "column", "borderRadius": "14px", "background": "var(--t-ui)", "border": "1px solid var(--t-border)", "boxShadow": "var(--t-sh3)", "overflow": "hidden" }}>
            <div style={{ "height": "40px", "flex": "none", "display": "flex", "alignItems": "center", "padding": "0 6px 0 16px", "borderBottom": "1px solid var(--line)" }}>
              <span style={{ "flex": "1", "fontWeight": "600" }}>
                {v.dlgTitle}
              </span>
              <button onClick={v.closeDlg} title="Fechar (Esc)" style={{ "width": "30px", "height": "28px", "borderRadius": "980px", "border": "none", "background": "transparent", "color": "var(--text)", "cursor": "pointer", "display": "flex", "alignItems": "center", "justifyContent": "center" }} className="h2">
                <svg viewBox="0 0 16 16" style={{ "width": "11px", "height": "11px", "fill": "none", "stroke": "currentColor", "strokeWidth": "1.4" }}>
                  <path d="M3 3l10 10M13 3L3 13" />
                </svg>
              </button>
            </div>
            {v.dlgPrefs ? (
              <>
              <div style={{ "display": "grid", "gridTemplateColumns": "150px minmax(0,1fr)", "minHeight": "380px" }}>
                <div style={{ "display": "flex", "flexDirection": "column", "gap": "2px", "padding": "10px", "borderRight": "1px solid var(--line)" }}>
                  {(v.prefTabs || []).map((t: any, i27: number) => (
                    <React.Fragment key={i27}>
                      <div onClick={t.onClick} style={{ "height": "34px", "display": "flex", "alignItems": "center", "padding": "0 12px", "borderRadius": "9px", "background": t.bg, "fontWeight": t.weight, "cursor": "default" }} className="h0">
                        {t.label}
                      </div>
                    </React.Fragment>
                  ))}
                </div>
                <div style={{ "padding": "16px 20px", "overflow": "auto", "maxHeight": "70vh" }}>
                  {v.prefGeral ? (
                    <>
                    <div style={{ "display": "flex", "flexDirection": "column", "gap": "16px" }}>
                      <div style={{ "display": "flex", "alignItems": "center", "gap": "16px" }}>
                        <span style={{ "width": "110px", "color": "var(--muted)" }}>
                          Aparência
                        </span>
                        <div style={{ "display": "flex", "padding": "2px", "gap": "2px", "background": "var(--t-sunk)", "borderRadius": "11px" }}>
                          {(v.themes || []).map((z: any, i28: number) => (
                            <React.Fragment key={i28}>
                              <div onClick={z.onClick} style={{ "padding": "0 16px", "height": "26px", "borderRadius": "8px", "display": "flex", "alignItems": "center", "background": z.bg, "boxShadow": z.shadow, "color": z.color, "cursor": "pointer" }}>
                                {z.label}
                              </div>
                            </React.Fragment>
                          ))}
                        </div>
                      </div>
                      <div style={{ "height": "1px", "background": "var(--line)" }} />
                      {(v.prefChecks || []).map((c: any, i29: number) => (
                        <React.Fragment key={i29}>
                          <div onClick={c.onClick} style={{ "display": "flex", "alignItems": "center", "gap": "10px", "cursor": "default" }}>
                            <div style={{ "width": "15px", "height": "15px", "borderRadius": "4px", "border": `1px solid ${c.bd ?? ""}`, "background": c.bg, "display": "flex", "alignItems": "center", "justifyContent": "center", "flex": "none" }}>
                              <svg viewBox="0 0 16 16" style={{ "width": "11px", "height": "11px", "fill": "none", "stroke": "var(--invtext)", "strokeWidth": "2.2", "opacity": c.op }}>
                                <path d="M3 8.5l3 3 7-7" />
                              </svg>
                            </div>
                            {c.label}
                          </div>
                        </React.Fragment>
                      ))}
                    </div>
                    </>
                  ) : null}
                  {v.prefAtalhos ? (
                    <>
                    <div style={{ "display": "flex", "flexDirection": "column" }}>
                      {(v.shortcuts || []).map((k: any, i30: number) => (
                        <React.Fragment key={i30}>
                          <div style={{ "display": "flex", "alignItems": "center", "justifyContent": "space-between", "gap": "12px", "height": "36px", "borderBottom": "1px solid var(--line)" }}>
                            <span>
                              {k.label}
                            </span>
                            <button onClick={k.onClick} title="Clique e pressione a nova tecla" style={{ "minWidth": "120px", "height": "26px", "padding": "0 10px", "borderRadius": "980px", "border": "none", "background": k.bg, "color": "var(--text)", "font": "12px var(--f-num)", "cursor": "pointer" }}>
                              {k.key}
                            </button>
                          </div>
                        </React.Fragment>
                      ))}
                      <div style={{ "paddingTop": "12px", "fontSize": "12px", "lineHeight": "1.6", "color": "var(--t-muted)", "textWrap": "pretty" }}>
                        {v.shortcutHint}
                      </div>
                      <div style={{ "display": "flex", "justifyContent": "flex-end", "paddingTop": "12px" }}>
                        <button onClick={v.resetKeys} style={{ "height": "28px", "padding": "0 12px", "borderRadius": "980px", "border": "none", "background": "var(--t-soft)", "color": "var(--text)", "font": "inherit", "cursor": "pointer" }} className="h2">
                          Restaurar padrão
                        </button>
                      </div>
                    </div>
                    </>
                  ) : null}
                  {v.prefInteg ? (
                    <>
                    <div style={{ "display": "flex", "flexDirection": "column", "gap": "14px" }}>
                      <span style={{ "fontFamily": "var(--f-num)", "fontSize": "10px", "letterSpacing": "1.5px", "textTransform": "uppercase", "color": "var(--t-faint)" }}>
                        Newsroom (MOS)
                      </span>
                      <div style={{ "display": "grid", "gridTemplateColumns": "110px minmax(0,1fr)", "gap": "10px", "alignItems": "center" }}>
                        <span style={{ "color": "var(--muted)" }}>
                          Sistema
                        </span>
                        <select value={v.mosSys} onChange={v.onMosSys} style={{ "height": "32px", "borderRadius": "10px", "border": "1px solid transparent", "background": "var(--field)", "color": "var(--text)", "font": "inherit", "padding": "0 10px", "minWidth": "0", "userSelect": "text" }}>
                          <option>
                            ENPS
                          </option>
                          <option>
                            Avid iNEWS
                          </option>
                          <option>
                            Octopus
                          </option>
                          <option>
                            Dalet
                          </option>
                          <option>
                            Ross Inception
                          </option>
                        </select>
                        <span style={{ "color": "var(--muted)" }}>
                          Servidor
                        </span>
                        <input value={v.mosHost} onChange={v.onMosHost} style={{ "height": "32px", "borderRadius": "10px", "border": "1px solid transparent", "background": "var(--field)", "color": "var(--text)", "font": "inherit", "padding": "0 10px", "minWidth": "0", "userSelect": "text" }} />
                        <span style={{ "color": "var(--muted)" }}>
                          ID MOS
                        </span>
                        <input value={v.mosId} onChange={v.onMosId} style={{ "height": "32px", "borderRadius": "10px", "border": "1px solid transparent", "background": "var(--field)", "color": "var(--text)", "font": "inherit", "padding": "0 10px", "minWidth": "0", "userSelect": "text" }} />
                      </div>
                      <div style={{ "display": "flex", "alignItems": "center", "gap": "10px" }}>
                        <div style={{ "display": "flex", "alignItems": "center", "gap": "8px", "fontSize": "12px", "color": "var(--t-muted)" }}>
                          <span style={{ "width": "7px", "height": "7px", "borderRadius": "50%", "background": v.mosDot, "flex": "none" }} />
                          <span style={{ "whiteSpace": "nowrap", "overflow": "hidden", "textOverflow": "ellipsis" }}>
                            {v.mosStatus}
                          </span>
                        </div>
                        <span style={{ "flex": "1" }} />
                        <button onClick={v.toggleMos} style={{ "height": "30px", "padding": "0 14px", "borderRadius": "980px", "border": "none", "background": "var(--t-soft)", "color": "var(--text)", "font": "inherit", "cursor": "pointer", "whiteSpace": "nowrap" }}>
                          {v.mosBtn}
                        </button>
                      </div>
                      <div style={{ "height": "1px", "background": "var(--line)" }} />
                      <span style={{ "fontFamily": "var(--f-num)", "fontSize": "10px", "letterSpacing": "1.5px", "textTransform": "uppercase", "color": "var(--t-faint)" }}>
                        Estúdio
                      </span>
                      {(v.integChecks || []).map((c: any, i31: number) => (
                        <React.Fragment key={i31}>
                          <div onClick={c.onClick} style={{ "display": "flex", "alignItems": "center", "gap": "8px", "cursor": "default", "minHeight": "24px" }}>
                            <div style={{ "width": "15px", "height": "15px", "borderRadius": "4px", "border": `1px solid ${c.bd ?? ""}`, "background": c.bg, "display": "flex", "alignItems": "center", "justifyContent": "center", "flex": "none" }}>
                              <svg viewBox="0 0 16 16" style={{ "width": "11px", "height": "11px", "fill": "none", "stroke": "var(--invtext)", "strokeWidth": "2.2", "opacity": c.op }}>
                                <path d="M3 8.5l3 3 7-7" />
                              </svg>
                            </div>
                            {c.label}
                          </div>
                        </React.Fragment>
                      ))}
                    </div>
                    </>
                  ) : null}
                  {v.prefDisp ? (
                    <>
                    <div style={{ "display": "flex", "flexDirection": "column" }}>
                      {(v.devicesList || []).map((d: any, i32: number) => (
                        <React.Fragment key={i32}>
                          <div style={{ "display": "flex", "alignItems": "center", "justifyContent": "space-between", "gap": "12px", "height": "36px", "borderBottom": "1px solid var(--line)" }}>
                            <span>
                              {d.name}
                            </span>
                            <span style={{ "color": "var(--muted)", "fontSize": "12px" }}>
                              {d.state}
                            </span>
                          </div>
                        </React.Fragment>
                      ))}
                      <div style={{ "marginTop": "16px", "padding": "14px", "borderRadius": "8px", "background": "var(--field)", "border": "1px solid var(--line)", "display": "flex", "alignItems": "center", "justifyContent": "space-between" }}>
                        <span style={{ "color": "var(--muted)" }}>
                          Teste de entrada — pressione qualquer botão do controle
                        </span>
                        <span style={{ "font": "500 16px var(--f-num)" }}>
                          {v.lastKeyBig}
                        </span>
                      </div>
                    </div>
                    </>
                  ) : null}
                </div>
              </div>
              </>
            ) : null}
            {v.dlgFind ? (
              <>
              <div style={{ "display": "grid", "gridTemplateColumns": "90px minmax(0,1fr)", "gap": "10px", "alignItems": "center", "padding": "16px" }}>
                <span style={{ "color": "var(--muted)" }}>
                  Localizar
                </span>
                <input value={v.findQ} onChange={v.onFindQ} onKeyDown={v.onFindKey} autoFocus style={{ "height": "30px", "borderRadius": "10px", "border": "1px solid transparent", "background": "var(--field)", "color": "var(--text)", "font": "inherit", "padding": "0 10px", "userSelect": "text" }} />
                <span style={{ "color": "var(--muted)" }}>
                  Substituir por
                </span>
                <input value={v.replQ} onChange={v.onReplQ} style={{ "height": "30px", "borderRadius": "10px", "border": "1px solid transparent", "background": "var(--field)", "color": "var(--text)", "font": "inherit", "padding": "0 10px", "userSelect": "text" }} />
              </div>
              <div style={{ "display": "flex", "alignItems": "center", "gap": "8px", "padding": "12px 16px", "borderTop": "1px solid var(--line)" }}>
                <span style={{ "flex": "1", "color": "var(--muted)", "fontSize": "12px" }}>
                  {v.findInfo}
                </span>
                <button onClick={v.replaceAll} style={{ "height": "30px", "padding": "0 14px", "borderRadius": "980px", "border": "none", "background": "var(--t-soft)", "color": "var(--text)", "font": "inherit", "cursor": "pointer" }} className="h2">
                  Substituir tudo
                </button>
                <button onClick={v.findNext} style={{ "height": "30px", "padding": "0 14px", "borderRadius": "980px", "border": "none", "background": "var(--inv)", "color": "var(--invtext)", "font": "600 13px var(--f-sans)", "cursor": "pointer" }}>
                  Próximo
                </button>
              </div>
              </>
            ) : null}
            {v.dlgRemote ? (
              <>
              <div style={{ "display": "flex", "flexDirection": "column", "gap": "14px", "padding": "18px" }}>
                <div style={{ "display": "flex", "justifyContent": "space-between", "fontSize": "12px", "color": "var(--muted)" }}>
                  <span style={{ "userSelect": "text" }}>
                    {v.remoteInfo}
                  </span>
                </div>
                <div style={{ "padding": "14px", "borderRadius": "10px", "background": "var(--field)", "border": "1px solid var(--line)", "display": "flex", "flexDirection": "column", "gap": "4px" }}>
                  <span style={{ "fontSize": "12px", "color": "var(--muted)" }}>
                    No prompter
                  </span>
                  <span style={{ "fontWeight": "600", "fontSize": "15px" }}>
                    {v.remoteCur}
                  </span>
                  <span style={{ "font": "12px var(--f-num)", "color": "var(--muted)" }}>
                    {v.stampValue} · {v.remoteSpeed}
                  </span>
                </div>
                <button onClick={v.togglePlay} style={{ "height": "64px", "borderRadius": "12px", "border": "none", "background": v.playBg, "color": v.playFg, "cursor": "pointer", "display": "flex", "alignItems": "center", "justifyContent": "center" }}>
                  <svg viewBox="0 0 16 16" style={{ "width": "22px", "height": "22px", "fill": "currentColor" }}>
                    <path d={v.playIcon} />
                  </svg>
                </button>
                <div style={{ "display": "grid", "gridTemplateColumns": "1fr 1fr", "gap": "8px" }}>
                  <button onClick={v.rSlower} style={{ "height": "48px", "borderRadius": "10px", "border": "none", "background": "var(--t-soft)", "color": "var(--text)", "font": "600 18px var(--f-sans)", "cursor": "pointer" }}>
                    −
                  </button>
                  <button onClick={v.rFaster} style={{ "height": "48px", "borderRadius": "10px", "border": "none", "background": "var(--t-soft)", "color": "var(--text)", "font": "600 18px var(--f-sans)", "cursor": "pointer" }}>
                    +
                  </button>
                  <button onClick={v.rPrev} style={{ "height": "44px", "borderRadius": "10px", "border": "none", "background": "var(--t-soft)", "color": "var(--text)", "font": "inherit", "cursor": "pointer" }}>
                    Marcador anterior
                  </button>
                  <button onClick={v.rNext} style={{ "height": "44px", "borderRadius": "10px", "border": "none", "background": "var(--t-soft)", "color": "var(--text)", "font": "inherit", "cursor": "pointer" }}>
                    Próximo marcador
                  </button>
                </div>
                <button onClick={v.rewind} style={{ "height": "36px", "borderRadius": "980px", "border": "none", "background": "var(--t-soft)", "color": "var(--text)", "font": "inherit", "cursor": "pointer" }}>
                  Voltar ao início
                </button>
              </div>
              </>
            ) : null}
            {v.dlgAbout ? (
              <>
              <div style={{ "display": "flex", "flexDirection": "column", "gap": "6px", "padding": "22px 20px" }}>
                <span style={{ "fontFamily": "var(--f-disp)", "fontWeight": "600", "fontSize": "18px", "letterSpacing": "-.3px" }}>
                  ARCA Prompter
                </span>
                <span style={{ "color": "var(--muted)" }}>
                  {v.aboutVer}
                </span>
                <span style={{ "color": "var(--muted)", "fontSize": "12px", "paddingTop": "10px", "textWrap": "pretty" }}>
                  Editor e prompter sincronizados, marcadores, várias telas e acompanhamento por voz.
                </span>
              </div>
              </>
            ) : null}
            {v.dlgPerfis ? (
              <>
              <div style={{ "display": "flex", "flexDirection": "column", "gap": "10px", "padding": "14px 16px 16px" }}>
                <div style={{ "fontSize": "12px", "color": "var(--muted)", "textWrap": "pretty" }}>
                  Um perfil guarda fonte, tamanho (palavras por linha), margem, entrelinha, cores, alinhamento, seta e velocidade de cada apresentador.
                </div>
                <div style={{ "display": "flex", "gap": "6px" }}>
                  <input value={v.profName} onChange={v.onProfName} placeholder="Nome do perfil (ex.: Ana — telejornal)" style={{ "flex": "1", "height": "30px", "borderRadius": "9px", "border": "1px solid transparent", "background": "var(--field)", "color": "var(--text)", "font": "inherit", "padding": "0 10px", "minWidth": "0", "userSelect": "text" }} />
                  <button onClick={v.saveProfile} style={{ "height": "28px", "padding": "0 12px", "borderRadius": "980px", "border": "none", "background": "var(--t-soft)", "color": "var(--text)", "font": "inherit", "fontSize": "12px", "cursor": "pointer", "whiteSpace": "nowrap", "background": "var(--t-accent)", "color": "var(--t-accent-ink)" }}>
                    Salvar atual
                  </button>
                </div>
                <div style={{ "height": "1px", "background": "var(--line)" }} />
                {v.noProfiles ? (
                  <>
                  <div style={{ "padding": "14px 0", "textAlign": "center", "color": "var(--muted)", "fontSize": "13px" }}>
                    Nenhum perfil salvo.
                  </div>
                  </>
                ) : null}
                <div style={{ "display": "flex", "flexDirection": "column", "maxHeight": "300px", "overflow": "auto" }}>
                  {(v.profiles || []).map((pf: any, i33: number) => (
                    <React.Fragment key={i33}>
                      <div style={{ "display": "grid", "gridTemplateColumns": "minmax(0,1fr) auto auto", "gap": "6px", "alignItems": "center", "padding": "7px 8px", "borderRadius": "9px" }} className="h0">
                        <div style={{ "display": "flex", "flexDirection": "column", "minWidth": "0" }}>
                          <span style={{ "fontWeight": "600", "whiteSpace": "nowrap", "overflow": "hidden", "textOverflow": "ellipsis" }}>
                            {pf.name}
                          </span>
                          <span style={{ "font": "11px var(--f-num)", "color": "var(--muted)", "whiteSpace": "nowrap", "overflow": "hidden", "textOverflow": "ellipsis" }}>
                            {pf.meta}
                          </span>
                        </div>
                        <button onClick={pf.onApply} style={{ "height": "28px", "padding": "0 12px", "borderRadius": "980px", "border": "none", "background": "var(--t-soft)", "color": "var(--text)", "font": "inherit", "fontSize": "12px", "cursor": "pointer", "whiteSpace": "nowrap" }}>
                          Aplicar
                        </button>
                        <button onClick={pf.onDelete} title="Excluir perfil" style={{ "width": "26px", "height": "26px", "borderRadius": "980px", "border": "none", "background": "transparent", "color": "var(--t-faint)", "cursor": "pointer", "display": "flex", "alignItems": "center", "justifyContent": "center" }} className="h3">
                          <svg viewBox="0 0 16 16" style={{ "width": "10px", "height": "10px", "fill": "none", "stroke": "currentColor", "strokeWidth": "1.7", "strokeLinecap": "round" }}>
                            <path d="M4 4l8 8M12 4l-8 8" />
                          </svg>
                        </button>
                      </div>
                    </React.Fragment>
                  ))}
                </div>
              </div>
              </>
            ) : null}
            {v.dlgVersoes ? (
              <>
              <div style={{ "display": "flex", "flexDirection": "column", "maxHeight": "420px", "overflow": "auto", "padding": "6px" }}>
                {v.noVersions ? (
                  <>
                  <div style={{ "padding": "24px 12px", "textAlign": "center", "color": "var(--muted)", "fontSize": "13px" }}>
                    Nenhuma versão ainda. Versões são criadas ao salvar e a cada 2 minutos de edição.
                  </div>
                  </>
                ) : null}
                {(v.versions || []).map((v: any, i34: number) => (
                  <React.Fragment key={i34}>
                    <div style={{ "display": "grid", "gridTemplateColumns": "minmax(0,1fr) auto", "gap": "10px", "alignItems": "center", "padding": "8px 10px", "borderRadius": "9px" }} className="h0">
                      <div style={{ "display": "flex", "flexDirection": "column", "minWidth": "0" }}>
                        <span style={{ "fontWeight": "600", "whiteSpace": "nowrap", "overflow": "hidden", "textOverflow": "ellipsis" }}>
                          {v.name}
                        </span>
                        <span style={{ "font": "11px var(--f-num)", "color": "var(--muted)" }}>
                          {v.meta}
                        </span>
                      </div>
                      <button onClick={v.onRestore} style={{ "height": "28px", "padding": "0 12px", "borderRadius": "980px", "border": "none", "background": "var(--t-soft)", "color": "var(--text)", "font": "inherit", "fontSize": "12px", "cursor": "pointer", "whiteSpace": "nowrap" }}>
                        Restaurar
                      </button>
                    </div>
                  </React.Fragment>
                ))}
              </div>
              </>
            ) : null}
            {v.dlgMonitor ? (
              <>
              <div style={{ "display": "flex", "flexDirection": "column", "gap": "12px", "padding": "14px" }}>
                <div onClick={v.openMonitorExt} title="Abrir no navegador" style={{ "height": "34px", "display": "flex", "alignItems": "center", "gap": "8px", "padding": "0 14px", "borderRadius": "980px", "background": "var(--t-soft)", "font": "12px var(--f-num)", "color": "var(--t-muted)", "cursor": "pointer" }}>
                  <svg viewBox="0 0 16 16" style={{ "width": "12px", "height": "12px", "fill": "none", "stroke": "currentColor", "strokeWidth": "1.5", "strokeLinejoin": "round" }}>
                    <path d="M3.5 7.5h9v7h-9z M5.5 7.5V5a2.5 2.5 0 0 1 5 0v2.5" />
                  </svg>
                  {v.webUrl}
                </div>
                <div ref={v.mvRef} style={{ "position": "relative", "width": "100%", "aspectRatio": "16/9", "overflow": "hidden", "borderRadius": "11px", "background": v.bg, "containerType": "inline-size" }}>
                  <div style={{ "position": "absolute", "inset": "0" }}>
                    <div ref={v.mcRef} data-pr="1" data-script="1" style={{ "position": "absolute", "left": "0", "top": "0", "width": "1000px", "boxSizing": "border-box", "transformOrigin": "0 0", "padding": `0 ${v.prRight ?? ""}px 0 ${v.prMargin ?? ""}px`, "fontFamily": v.fontFam, "fontSize": `${v.fontSize ?? ""}px`, "lineHeight": v.lineH, "color": v.fg, "textAlign": v.align }} />
                    <div style={{ "position": "absolute", "left": v.indLeft, "top": `calc(${v.readPct ?? ""}% + ${v.halfLineCq ?? ""})`, "transform": "translateY(-50%)", "width": v.indW, "height": v.indH, "background": v.indColorV, "clipPath": v.indClip, "borderRadius": v.indRadius, "display": v.indDisplay }} />
                  </div>
                  <div style={{ "position": "absolute", "left": "10px", "top": "10px", "padding": "3px 9px", "borderRadius": "980px", "background": "rgba(0,0,0,.6)", "color": "#fff", "font": "600 10px var(--f-num)", "letterSpacing": "1px" }}>
                    {v.monTag}
                  </div>
                </div>
                <div style={{ "display": "flex", "alignItems": "center", "gap": "20px", "flexWrap": "wrap" }}>
                  <div style={{ "display": "flex", "flexDirection": "column" }}>
                    <span style={{ "fontFamily": "var(--f-num)", "fontSize": "10px", "letterSpacing": "1.5px", "textTransform": "uppercase", "color": "var(--t-faint)" }}>
                      Bloco
                    </span>
                    <span style={{ "fontWeight": "600" }}>
                      {v.remoteCur}
                    </span>
                  </div>
                  <div style={{ "display": "flex", "flexDirection": "column" }}>
                    <span style={{ "fontFamily": "var(--f-num)", "fontSize": "10px", "letterSpacing": "1.5px", "textTransform": "uppercase", "color": "var(--t-faint)" }}>
                      Posição
                    </span>
                    <span style={{ "font": "600 14px var(--f-num)" }}>
                      {v.stampValue}
                    </span>
                  </div>
                  <div style={{ "display": "flex", "flexDirection": "column" }}>
                    <span style={{ "fontFamily": "var(--f-num)", "fontSize": "10px", "letterSpacing": "1.5px", "textTransform": "uppercase", "color": "var(--t-faint)" }}>
                      Velocidade
                    </span>
                    <span style={{ "font": "600 14px var(--f-num)" }}>
                      {v.remoteSpeed}
                    </span>
                  </div>
                  <span style={{ "flex": "1" }} />
                  {v.webCtrl ? (
                    <>
                    <button onClick={v.togglePlay} style={{ "height": "34px", "padding": "0 18px", "borderRadius": "980px", "border": "none", "background": v.playBg, "color": v.playFg, "font": "600 13px var(--f-sans)", "cursor": "pointer" }}>
                      {v.monPlayLabel}
                    </button>
                    </>
                  ) : null}
                  {v.webViewOnly ? (
                    <>
                    <span style={{ "fontSize": "12px", "color": "var(--t-faint)" }}>
                      Somente visualização
                    </span>
                    </>
                  ) : null}
                </div>
              </div>
              </>
            ) : null}
            {v.dlgNovo ? (
              <>
              <div style={{ "padding": "20px", "color": "var(--muted)" }}>
                Descartar o roteiro atual e começar um novo?
              </div>
              <div style={{ "display": "flex", "justifyContent": "flex-end", "gap": "8px", "padding": "12px 16px", "borderTop": "1px solid var(--line)" }}>
                <button onClick={v.closeDlg} style={{ "height": "30px", "padding": "0 14px", "borderRadius": "980px", "border": "none", "background": "var(--t-soft)", "color": "var(--text)", "font": "inherit", "cursor": "pointer" }}>
                  Cancelar
                </button>
                <button onClick={v.confirmNew} style={{ "height": "30px", "padding": "0 14px", "borderRadius": "980px", "border": "none", "background": "var(--inv)", "color": "var(--invtext)", "font": "600 13px var(--f-sans)", "cursor": "pointer" }}>
                  Novo roteiro
                </button>
              </div>
              </>
            ) : null}
          </div>
        </div>
        </>
      ) : null}
    </div>
    </>
  );
}
