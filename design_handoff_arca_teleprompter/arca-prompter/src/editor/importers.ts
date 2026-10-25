// Leitura de .docx (mammoth) e .pdf (pdf.js) como texto simples, uma linha por parágrafo.
// As bibliotecas são carregadas sob demanda para não pesar a abertura do app.

export async function docxToText(file: File): Promise<string> {
  const mammoth: any = await import('mammoth');
  const { value } = await (mammoth.default || mammoth).extractRawText({ arrayBuffer: await file.arrayBuffer() });
  return String(value).split(/\r?\n/).map(l => l.trim()).filter(Boolean).join('\n');
}

export async function pdfToText(file: File): Promise<string> {
  const pdfjs: any = await import('pdfjs-dist');
  const worker = (await import('pdfjs-dist/build/pdf.worker.min.mjs?url')).default;
  pdfjs.GlobalWorkerOptions.workerSrc = worker;
  const doc = await pdfjs.getDocument({ data: new Uint8Array(await file.arrayBuffer()) }).promise;
  const paras: string[] = [];
  for (let p = 1; p <= doc.numPages; p++) {
    const page = await doc.getPage(p), tc = await page.getTextContent();
    let line = '', lastY: number | null = null, lastH = 0;
    for (const it of tc.items as any[]) {
      if (!('str' in it)) continue;
      const y = it.transform[5], h = it.height || lastH || 10;
      // Um salto vertical maior que ~1,6 linha separa parágrafos.
      if (lastY != null && Math.abs(lastY - y) > h * 1.6 && line.trim()) { paras.push(line.trim()); line = ''; }
      else if (lastY != null && Math.abs(lastY - y) > 1 && line && !line.endsWith(' ')) line += ' ';
      line += it.str; if (it.hasEOL) line += ' ';
      lastY = y; lastH = h;
    }
    if (line.trim()) paras.push(line.trim());
  }
  return paras.map(l => l.replace(/\s+/g, ' ')).join('\n');
}
