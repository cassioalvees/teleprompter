// @ts-nocheck — GERADO por scripts/convert-template.mjs a partir de Instalador.dc.html e depois mantido à mão.
/* eslint-disable */
import React from 'react';

export function renderTemplate(v: any) {
  return (
    <>
    <div style={{ "minHeight": "100vh", "display": "flex", "flexDirection": "column", "alignItems": "center", "justifyContent": "center", "gap": v.windowed ? "0" : "20px", "padding": v.windowed ? "0" : "32px 16px", "fontFamily": "'Raleway',system-ui,sans-serif", "fontSize": "13px", "color": "#1a1a18" }}>
      <div style={{ "display": v.windowed ? "none" : "flex", "padding": "3px", "gap": "2px", "background": "#dcdcd8", "borderRadius": "980px" }}>
        {(v.modes || []).map((m: any, i0: number) => (
          <React.Fragment key={i0}>
            <button onClick={m.onClick} style={{ "height": "30px", "padding": "0 16px", "border": "none", "borderRadius": "980px", "background": m.bg, "boxShadow": m.shadow, "color": m.color, "fontWeight": "600", "fontSize": "12px", "cursor": "pointer" }}>
              {m.label}
            </button>
          </React.Fragment>
        ))}
      </div>
      <div data-screen-label={v.screenLabel} style={{ "width": v.windowed ? "100vw" : "760px", "maxWidth": "100%", "height": v.windowed ? "100vh" : "500px", "display": "grid", "gridTemplateColumns": "232px minmax(0,1fr)", "background": "#ffffff", "borderRadius": v.windowed ? "0" : "10px", "overflow": "hidden", "boxShadow": v.windowed ? "none" : "0 0 0 1px rgba(0,0,0,.08),0 24px 60px rgba(26,26,24,.22)" }}>
        <div style={{ "background": "#141416", "color": "#ececef", "display": "flex", "flexDirection": "column", "padding": "22px 20px 18px", "minWidth": "0" }}>
          <div style={{ "display": "flex", "alignItems": "center", "gap": "10px" }}>
            <span style={{ "fontFamily": "'GC Epic Pro','Sora',sans-serif", "fontSize": "15px", "letterSpacing": "3.5px" }}>
              ARCA
            </span>
            <span style={{ "width": "1px", "height": "14px", "background": "#3a3a40" }} />
            <span style={{ "fontWeight": "600", "fontSize": "12.5px" }}>
              Teleprompter
            </span>
          </div>
          <div style={{ "fontFamily": "'Sora',sans-serif", "fontSize": "10px", "letterSpacing": "1.5px", "textTransform": "uppercase", "color": "#91919a", "marginTop": "6px" }}>
            {v.sideCaption}
          </div>
          <div style={{ "display": "flex", "flexDirection": "column", "gap": "2px", "marginTop": "34px" }}>
            {(v.steps || []).map((s: any, i1: number) => (
              <React.Fragment key={i1}>
                <div style={{ "display": "grid", "gridTemplateColumns": "22px minmax(0,1fr)", "gap": "10px", "alignItems": "center", "height": "34px" }}>
                  <div style={{ "width": "22px", "height": "22px", "borderRadius": "50%", "display": "flex", "alignItems": "center", "justifyContent": "center", "fontFamily": "'Sora',sans-serif", "fontSize": "10.5px", "fontWeight": "600", "background": s.dotBg, "color": s.dotFg, "boxShadow": s.dotRing }}>
                    {s.mark}
                  </div>
                  <span style={{ "fontSize": "12.5px", "fontWeight": s.weight, "color": s.color, "whiteSpace": "nowrap", "overflow": "hidden", "textOverflow": "ellipsis" }}>
                    {s.label}
                  </span>
                </div>
              </React.Fragment>
            ))}
          </div>
          <span style={{ "flex": "1" }} />
          <div style={{ "fontFamily": "'Sora',sans-serif", "fontSize": "10.5px", "color": "#6e6e76", "lineHeight": "1.6" }}>
            {v.versionLine}
            <br />
            © 2026 ARCA
          </div>
        </div>
        <div style={{ "display": "flex", "flexDirection": "column", "minWidth": "0", "minHeight": "0" }}>
          <div style={{ "height": "36px", "flex": "none", "display": "flex", "alignItems": "center", "justifyContent": "space-between", "padding": "0 4px 0 20px", "borderBottom": "1px solid #ebebeb", "WebkitAppRegion": "drag" }}>
            <span style={{ "fontSize": "12px", "color": "#5a5a55" }}>
              {v.winTitle}
            </span>
            <div style={{ "display": "flex", "WebkitAppRegion": "no-drag" }}>
              <div onClick={v.minimize} title="Minimizar" style={{ "width": "40px", "height": "30px", "display": "flex", "alignItems": "center", "justifyContent": "center", "color": "#5a5a55", "borderRadius": "6px", "cursor": "pointer" }} className="sh2">
                <svg viewBox="0 0 16 16" style={{ "width": "10px", "height": "10px", "stroke": "currentColor", "strokeWidth": "1.2" }}>
                  <path d="M3 8h10" />
                </svg>
              </div>
              <button onClick={v.askCancel} title="Fechar" style={{ "width": "40px", "height": "30px", "border": "none", "background": "transparent", "borderRadius": "6px", "display": "flex", "alignItems": "center", "justifyContent": "center", "color": "#5a5a55", "cursor": "pointer" }} className="sh0">
                <svg viewBox="0 0 16 16" style={{ "width": "10px", "height": "10px", "fill": "none", "stroke": "currentColor", "strokeWidth": "1.3", "strokeLinecap": "round" }}>
                  <path d="M4 4l8 8M12 4l-8 8" />
                </svg>
              </button>
            </div>
          </div>
          <div style={{ "flex": "1", "minHeight": "0", "overflow": "auto", "padding": "28px 32px 20px", "display": "flex", "flexDirection": "column", "gap": "16px" }}>
            <div style={{ "display": "flex", "flexDirection": "column", "gap": "6px" }}>
              <div style={{ "fontFamily": "'Sora',sans-serif", "fontSize": "10px", "letterSpacing": "1.5px", "textTransform": "uppercase", "color": "#d42b1e" }}>
                {v.eyebrow}
              </div>
              <div style={{ "fontFamily": "'Sora',sans-serif", "fontSize": "21px", "fontWeight": "600", "letterSpacing": "-.2px", "lineHeight": "1.25", "textWrap": "pretty" }}>
                {v.title}
              </div>
              <div style={{ "color": "#5a5a55", "lineHeight": "1.55", "textWrap": "pretty", "maxWidth": "440px" }}>
                {v.lead}
              </div>
            </div>
            {v.isWelcome ? (
              <>
              <div style={{ "display": "flex", "flexDirection": "column", "gap": "0", "border": "1px solid #e2e2de", "borderRadius": "12px", "overflow": "hidden" }}>
                {(v.welcomeRows || []).map((w: any, i2: number) => (
                  <React.Fragment key={i2}>
                    <div style={{ "display": "grid", "gridTemplateColumns": "120px minmax(0,1fr)", "gap": "12px", "padding": "10px 14px", "borderTop": `1px solid ${w.line ?? ""}` }}>
                      <span style={{ "color": "#67675f" }}>
                        {w.k}
                      </span>
                      <span style={{ "fontWeight": "500" }}>
                        {w.v}
                      </span>
                    </div>
                  </React.Fragment>
                ))}
              </div>
              <div style={{ "fontSize": "12px", "color": "#67675f", "lineHeight": "1.55" }}>
                Feche outros programas antes de continuar. O instalador não reinicia o computador.
              </div>
              </>
            ) : null}
            {v.isLicense ? (
              <>
              <div style={{ "height": "190px", "overflow": "auto", "border": "1px solid #e2e2de", "borderRadius": "10px", "background": "#f7f7f5", "padding": "14px 16px", "fontSize": "12px", "lineHeight": "1.65", "color": "#3a3a36" }}>
                <div style={{ "fontWeight": "700", "marginBottom": "6px" }}>
                  Contrato de licença de usuário final — ARCA Teleprompter
                </div>
                <p style={{ "margin": "0 0 8px" }}>
                  1. Concessão. A ARCA concede a você uma licença não exclusiva para instalar e usar o software em computadores sob seu controle, de acordo com o plano contratado.
                </p>
                <p style={{ "margin": "0 0 8px" }}>
                  2. Restrições. Não é permitido sublicenciar, alugar, fazer engenharia reversa ou remover avisos de propriedade.
                </p>
                <p style={{ "margin": "0 0 8px" }}>
                  3. Componentes de terceiros. A saída NDI usa o NDI® Runtime, distribuído sob a licença do respectivo fabricante.
                </p>
                <p style={{ "margin": "0 0 8px" }}>
                  4. Dados. Roteiros e configurações ficam no seu computador. O monitor web funciona apenas na rede local.
                </p>
                <p style={{ "margin": "0 0 8px" }}>
                  5. Garantia. O software é fornecido "no estado em que se encontra", sem garantias além das exigidas por lei.
                </p>
                <p style={{ "margin": "0" }}>
                  6. Lei aplicável. Este contrato é regido pelas leis da República Federativa do Brasil.
                </p>
              </div>
              <label style={{ "display": "flex", "alignItems": "center", "gap": "10px", "cursor": "pointer", "fontWeight": "500" }}>
                <input type="checkbox" checked={v.accepted} onChange={v.toggleAccept} style={{ "width": "16px", "height": "16px", "margin": "0" }} />
                Li e aceito os termos do contrato de licença
              </label>
              </>
            ) : null}
            {v.isLocation ? (
              <>
              <div style={{ "display": "flex", "flexDirection": "column", "gap": "6px" }}>
                <span style={{ "fontSize": "12px", "color": "#5a5a55", "fontWeight": "600" }}>
                  Pasta de destino
                </span>
                <div style={{ "display": "flex", "gap": "8px" }}>
                  <input value={v.path} onChange={v.onPath} spellCheck={false} style={{ "flex": "1", "minWidth": "0", "height": "34px", "borderRadius": "10px", "border": "1px solid #e2e2de", "background": "#ffffff", "padding": "0 12px", "fontFamily": "'Sora',sans-serif", "fontSize": "12px", "outline": "none" }} />
                  <button onClick={v.browse} style={{ "height": "34px", "padding": "0 16px", "borderRadius": "980px", "border": "none", "background": "#eeeeec", "fontWeight": "600", "fontSize": "12px", "cursor": "pointer" }} className="sh1">
                    Procurar…
                  </button>
                </div>
                {v.pathErr ? (
                  <>
                  <span style={{ "fontSize": "12px", "color": "#d42b1e" }}>
                    Informe uma pasta válida, por exemplo C:\Program Files\ARCA Teleprompter
                  </span>
                  </>
                ) : null}
              </div>
              <div style={{ "display": "flex", "flexDirection": "column", "gap": "6px" }}>
                <span style={{ "fontSize": "12px", "color": "#5a5a55", "fontWeight": "600" }}>
                  Instalar para
                </span>
                <div style={{ "display": "flex", "padding": "2px", "gap": "2px", "background": "#eeeeec", "borderRadius": "11px", "width": "fit-content" }}>
                  {(v.scopes || []).map((sc: any, i3: number) => (
                    <React.Fragment key={i3}>
                      <button onClick={sc.onClick} style={{ "height": "28px", "padding": "0 14px", "border": "none", "borderRadius": "9px", "background": sc.bg, "boxShadow": sc.shadow, "color": sc.color, "fontSize": "12px", "fontWeight": "600", "cursor": "pointer" }}>
                        {sc.label}
                      </button>
                    </React.Fragment>
                  ))}
                </div>
              </div>
              <div style={{ "display": "grid", "gridTemplateColumns": "repeat(3,minmax(0,1fr))", "gap": "1px", "background": "#e2e2de", "border": "1px solid #e2e2de", "borderRadius": "10px", "overflow": "hidden" }}>
                {(v.diskStats || []).map((d: any, i4: number) => (
                  <React.Fragment key={i4}>
                    <div style={{ "background": "#ffffff", "padding": "10px 12px", "display": "flex", "flexDirection": "column", "gap": "2px" }}>
                      <span style={{ "fontSize": "11px", "color": "#67675f" }}>
                        {d.k}
                      </span>
                      <span style={{ "fontFamily": "'Sora',sans-serif", "fontWeight": "600" }}>
                        {d.v}
                      </span>
                    </div>
                  </React.Fragment>
                ))}
              </div>
              </>
            ) : null}
            {v.isComponents ? (
              <>
              <div style={{ "display": "flex", "flexDirection": "column", "border": "1px solid #e2e2de", "borderRadius": "12px", "overflow": "hidden" }}>
                {(v.components || []).map((c: any, i5: number) => (
                  <React.Fragment key={i5}>
                    <label style={{ "display": "grid", "gridTemplateColumns": "18px minmax(0,1fr) auto", "gap": "12px", "alignItems": "center", "padding": "11px 14px", "borderTop": `1px solid ${c.line ?? ""}`, "cursor": c.cursor, "background": c.bg }}>
                      <input type="checkbox" checked={c.on} disabled={c.disabled} onChange={c.onToggle} style={{ "width": "16px", "height": "16px", "margin": "0" }} />
                      <div style={{ "display": "flex", "flexDirection": "column", "gap": "2px", "minWidth": "0" }}>
                        <span style={{ "fontWeight": "600" }}>
                          {c.name}
                        </span>
                        <span style={{ "fontSize": "12px", "color": "#67675f", "lineHeight": "1.45" }}>
                          {c.desc}
                        </span>
                      </div>
                      <span style={{ "fontFamily": "'Sora',sans-serif", "fontSize": "11.5px", "color": "#5a5a55", "whiteSpace": "nowrap" }}>
                        {c.size}
                      </span>
                    </label>
                  </React.Fragment>
                ))}
              </div>
              <div style={{ "display": "flex", "flexDirection": "column", "gap": "8px" }}>
                {(v.extras || []).map((x: any, i6: number) => (
                  <React.Fragment key={i6}>
                    <label style={{ "display": "flex", "alignItems": "center", "gap": "10px", "cursor": "pointer" }}>
                      <input type="checkbox" checked={x.on} onChange={x.onToggle} style={{ "width": "16px", "height": "16px", "margin": "0" }} />
                      {x.label}
                    </label>
                  </React.Fragment>
                ))}
              </div>
              </>
            ) : null}
            {v.isProgress ? (
              <>
              <div style={{ "display": "flex", "flexDirection": "column", "gap": "10px", "marginTop": "8px" }}>
                <div style={{ "display": "flex", "justifyContent": "space-between", "alignItems": "baseline", "gap": "12px" }}>
                  <span style={{ "fontWeight": "600", "whiteSpace": "nowrap", "overflow": "hidden", "textOverflow": "ellipsis" }}>
                    {v.progTask}
                  </span>
                  <span style={{ "fontFamily": "'Sora',sans-serif", "fontWeight": "600", "fontSize": "15px" }}>
                    {v.progPct}
                  </span>
                </div>
                <div style={{ "height": "6px", "borderRadius": "3px", "background": "#eeeeec", "overflow": "hidden" }}>
                  <div style={{ "height": "100%", "width": v.progPct, "background": "#d42b1e", "borderRadius": "3px", "transition": "width .25s linear" }} />
                </div>
                <div style={{ "fontFamily": "'Sora',sans-serif", "fontSize": "11px", "color": "#67675f", "whiteSpace": "nowrap", "overflow": "hidden", "textOverflow": "ellipsis" }}>
                  {v.progFile}
                </div>
              </div>
              <div style={{ "height": "150px", "overflow": "auto", "border": "1px solid #e2e2de", "borderRadius": "10px", "background": "#f7f7f5", "padding": "10px 12px", "fontFamily": "'Sora',sans-serif", "fontSize": "11px", "lineHeight": "1.75", "color": "#5a5a55" }}>
                {(v.log || []).map((l: any, i7: number) => (
                  <React.Fragment key={i7}>
                    <div style={{ "display": "flex", "gap": "8px" }}>
                      <span style={{ "color": l.c }}>
                        {l.mark}
                      </span>
                      <span>
                        {l.t}
                      </span>
                    </div>
                  </React.Fragment>
                ))}
              </div>
              </>
            ) : null}
            {v.isDone ? (
              <>
              <div style={{ "display": "flex", "alignItems": "center", "gap": "14px", "padding": "14px 16px", "borderRadius": "12px", "background": "#e9f5ee" }}>
                <div style={{ "width": "34px", "height": "34px", "borderRadius": "50%", "background": "#2f8f5b", "display": "flex", "alignItems": "center", "justifyContent": "center", "flex": "none" }}>
                  <svg viewBox="0 0 16 16" style={{ "width": "16px", "height": "16px", "fill": "none", "stroke": "#fff", "strokeWidth": "2", "strokeLinecap": "round", "strokeLinejoin": "round" }}>
                    <path d="M3.5 8.5l3 3 6-6.5" />
                  </svg>
                </div>
                <div style={{ "display": "flex", "flexDirection": "column", "gap": "2px", "minWidth": "0" }}>
                  <span style={{ "fontWeight": "600" }}>
                    {v.doneHead}
                  </span>
                  <span style={{ "fontSize": "12px", "color": "#3a5a48", "whiteSpace": "nowrap", "overflow": "hidden", "textOverflow": "ellipsis" }}>
                    {v.doneSub}
                  </span>
                </div>
              </div>
              <div style={{ "display": "flex", "flexDirection": "column", "gap": "8px" }}>
                {(v.doneChecks || []).map((x: any, i8: number) => (
                  <React.Fragment key={i8}>
                    <label style={{ "display": "flex", "alignItems": "center", "gap": "10px", "cursor": "pointer" }}>
                      <input type="checkbox" checked={x.on} onChange={x.onToggle} style={{ "width": "16px", "height": "16px", "margin": "0" }} />
                      {x.label}
                    </label>
                  </React.Fragment>
                ))}
              </div>
              </>
            ) : null}
            {v.isUnConfirm ? (
              <>
              <div style={{ "display": "flex", "alignItems": "center", "gap": "12px", "padding": "12px 14px", "border": "1px solid #e2e2de", "borderRadius": "12px" }}>
                <img src="icon-64.png" alt="" style={{ "width": "36px", "height": "36px", "flex": "none" }} />
                <div style={{ "display": "flex", "flexDirection": "column", "gap": "2px", "minWidth": "0" }}>
                  <span style={{ "fontWeight": "600" }}>
                    {v.unTitle}
                  </span>
                  <span style={{ "fontFamily": "'Sora',sans-serif", "fontSize": "11px", "color": "#67675f", "whiteSpace": "nowrap", "overflow": "hidden", "textOverflow": "ellipsis" }}>
                    {v.unPathLine}
                  </span>
                </div>
              </div>
              <div style={{ "display": "flex", "flexDirection": "column", "gap": "6px" }}>
                <span style={{ "fontSize": "12px", "color": "#5a5a55", "fontWeight": "600" }}>
                  O que fazer com seus dados
                </span>
                <div style={{ "display": "flex", "flexDirection": "column", "border": "1px solid #e2e2de", "borderRadius": "12px", "overflow": "hidden" }}>
                  {(v.keepOpts || []).map((k: any, i9: number) => (
                    <React.Fragment key={i9}>
                      <label style={{ "display": "grid", "gridTemplateColumns": "18px minmax(0,1fr) auto", "gap": "12px", "alignItems": "center", "padding": "11px 14px", "borderTop": `1px solid ${k.line ?? ""}`, "cursor": "pointer" }}>
                        <input type="checkbox" checked={k.on} onChange={k.onToggle} style={{ "width": "16px", "height": "16px", "margin": "0" }} />
                        <div style={{ "display": "flex", "flexDirection": "column", "gap": "2px", "minWidth": "0" }}>
                          <span style={{ "fontWeight": "600" }}>
                            {k.name}
                          </span>
                          <span style={{ "fontSize": "12px", "color": "#67675f", "lineHeight": "1.45" }}>
                            {k.desc}
                          </span>
                        </div>
                        <span style={{ "fontFamily": "'Sora',sans-serif", "fontSize": "11.5px", "color": "#5a5a55", "whiteSpace": "nowrap" }}>
                          {k.size}
                        </span>
                      </label>
                    </React.Fragment>
                  ))}
                </div>
              </div>
              {v.appRunning ? (
                <>
                <div style={{ "display": "flex", "alignItems": "center", "gap": "12px", "padding": "10px 14px", "borderRadius": "10px", "background": "rgba(212,43,30,.08)", "color": "#8a1c13" }}>
                  <span style={{ "flex": "1", "fontSize": "12px", "lineHeight": "1.5" }}>
                    O ARCA Teleprompter está aberto. Ele será fechado antes de remover os arquivos.
                  </span>
                  <button onClick={v.closeApp} style={{ "height": "28px", "padding": "0 12px", "borderRadius": "980px", "border": "none", "background": "#ffffff", "color": "#8a1c13", "fontWeight": "600", "fontSize": "12px", "cursor": "pointer", "flex": "none" }}>
                    Fechar agora
                  </button>
                </div>
                </>
              ) : null}
              </>
            ) : null}
            {v.isUnFeedback ? (
              <>
              <div style={{ "display": "flex", "flexWrap": "wrap", "gap": "6px" }}>
                {(v.reasons || []).map((r: any, i10: number) => (
                  <React.Fragment key={i10}>
                    <button onClick={r.onClick} style={{ "height": "30px", "padding": "0 14px", "borderRadius": "980px", "border": `1px solid ${r.bd ?? ""}`, "background": r.bg, "color": r.fg, "fontSize": "12px", "fontWeight": "500", "cursor": "pointer" }}>
                      {r.label}
                    </button>
                  </React.Fragment>
                ))}
              </div>
              <textarea value={v.feedback} onChange={v.onFeedback} placeholder="Comentário opcional" style={{ "height": "84px", "resize": "none", "borderRadius": "10px", "border": "1px solid #e2e2de", "padding": "10px 12px", "font": "13px 'Raleway',sans-serif", "outline": "none" }} />
              </>
            ) : null}
          </div>
          <div style={{ "height": "60px", "flex": "none", "display": "flex", "alignItems": "center", "gap": "8px", "padding": "0 20px", "borderTop": "1px solid #ebebeb", "background": "#fafaf8" }}>
            {v.showBack ? (
              <>
              <button onClick={v.back} style={{ "height": "34px", "padding": "0 18px", "borderRadius": "980px", "border": "none", "background": "#eeeeec", "fontWeight": "600", "fontSize": "12.5px", "cursor": "pointer" }} className="sh1">
                Voltar
              </button>
              </>
            ) : null}
            <span style={{ "flex": "1" }} />
            {v.showCancel ? (
              <>
              <button onClick={v.askCancel} style={{ "height": "34px", "padding": "0 16px", "borderRadius": "980px", "border": "none", "background": "transparent", "color": "#5a5a55", "fontWeight": "600", "fontSize": "12.5px", "cursor": "pointer" }} className="sh2">
                Cancelar
              </button>
              </>
            ) : null}
            <button onClick={v.next} disabled={v.nextDisabled} style={{ "height": "34px", "minWidth": "112px", "padding": "0 20px", "borderRadius": "980px", "border": "none", "background": v.nextBg, "color": v.nextFg, "fontWeight": "600", "fontSize": "12.5px", "cursor": v.nextCursor }}>
              {v.nextLabel}
            </button>
          </div>
        </div>
      </div>
      {v.confirmCancel ? (
        <>
        <div style={{ "position": "fixed", "inset": "0", "background": "rgba(20,20,22,.35)", "display": "flex", "alignItems": "center", "justifyContent": "center", "padding": "16px", "zIndex": "10" }}>
          <div style={{ "width": "380px", "maxWidth": "100%", "background": "#ffffff", "borderRadius": "14px", "boxShadow": "0 18px 50px rgba(26,26,24,.25)", "padding": "20px", "display": "flex", "flexDirection": "column", "gap": "14px" }}>
            <div style={{ "fontFamily": "'Sora',sans-serif", "fontSize": "15px", "fontWeight": "600" }}>
              {v.cancelTitle}
            </div>
            <div style={{ "color": "#5a5a55", "lineHeight": "1.55" }}>
              {v.cancelText}
            </div>
            <div style={{ "display": "flex", "justifyContent": "flex-end", "gap": "8px" }}>
              <button onClick={v.keepGoing} style={{ "height": "32px", "padding": "0 16px", "borderRadius": "980px", "border": "none", "background": "#eeeeec", "fontWeight": "600", "fontSize": "12.5px", "cursor": "pointer" }}>
                Continuar
              </button>
              <button onClick={v.doCancel} style={{ "height": "32px", "padding": "0 16px", "borderRadius": "980px", "border": "none", "background": "#d42b1e", "color": "#fff", "fontWeight": "600", "fontSize": "12.5px", "cursor": "pointer" }}>
                {v.cancelBtn}
              </button>
            </div>
          </div>
        </div>
        </>
      ) : null}
    </div>
    </>
  );
}
