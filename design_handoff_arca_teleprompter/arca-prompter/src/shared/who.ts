// Cores e nomes dos apresentadores como CSS vars no :root (usadas pelas regras [data-who] do proto.css).
export function applyWhoVars(r: HTMLElement, pres: { name: string; color: string }[], presNames: boolean, presOnly: string) {
  for (let i = 0; i < 8; i++) {
    const p = pres[i];
    r.style.setProperty('--who' + (i + 1), p ? p.color : 'inherit');
    r.style.setProperty('--who' + (i + 1) + '-name', p ? JSON.stringify(p.name || ('Apresentador ' + (i + 1))) : "''");
  }
  r.setAttribute('data-who-names', presNames ? 'on' : 'off');
  if (presOnly !== '0') r.setAttribute('data-who-only', presOnly); else r.removeAttribute('data-who-only');
}
