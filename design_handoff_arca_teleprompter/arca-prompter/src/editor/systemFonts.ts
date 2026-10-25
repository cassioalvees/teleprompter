// Fontes instaladas no computador (Local Font Access API: Electron e Chrome/Edge).
// Na web o navegador pede permissão; precisa partir de um gesto do usuário.

export async function listSystemFonts(): Promise<string[]> {
  const q = (window as any).queryLocalFonts;
  if (!q) throw new Error('unsupported');
  const fonts: any[] = await q.call(window);
  return [...new Set(fonts.map(f => String(f.family)))].sort((a, b) => a.localeCompare(b, 'pt-BR'));
}

const cache = new Map<string, number>();
/**
 * Fator de largura relativo à Atkinson Hyperlegible (fator 1), usado em fs() para manter
 * as palavras por linha ao trocar de fonte.
 */
export function widthFactor(family: string): number {
  if (cache.has(family)) return cache.get(family)!;
  const c = document.createElement('canvas').getContext('2d');
  if (!c) return 1;
  const sample = 'Boa noite. Começa agora o Jornal da Noite, com as notícias do dia.';
  c.font = "100px 'Atkinson Hyperlegible'"; const base = c.measureText(sample).width;
  c.font = `100px ${JSON.stringify(family)}, sans-serif`; const w = c.measureText(sample).width;
  const f = base && w ? Math.max(0.6, Math.min(1.6, w / base)) : 1;
  cache.set(family, f);
  return f;
}
