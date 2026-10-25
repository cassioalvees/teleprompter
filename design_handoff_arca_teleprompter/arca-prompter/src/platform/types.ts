// Contrato entre o editor e o ambiente (navegador ou app Windows/Electron).

export interface DisplayInfo {
  id: string;
  name: string;
  res: string;
  /** Monitor do operador (onde o editor está). */
  operator: boolean;
  /** Janela comum (não ocupa um monitor inteiro). */
  windowed?: boolean;
}

export interface OpenedFile {
  file: File;
  /** Caminho no disco (apenas no app desktop). */
  path?: string;
}

/** Tudo que as saídas (janelas de prompter, monitor web, NDI) precisam para desenhar. */
export interface OutputSnapshot {
  bg: string; fg: string; fontFam: string; fontSize: number; lineH: number;
  prMargin: number; prRight: number; align: string; readPct: number;
  lineCq: string; halfLineCq: string;
  ovType: string; dimColor: string; hiColor: string; readingLine: boolean;
  indColor: string; indLeft: string; indW: string; indH: string; indClip: string; indRadius: string; indDisplay: string;
  bgImg: string; bgImgOp: number;
  pres: { name: string; color: string }[]; presNames: boolean; presOnly: string;
  displays: { id: string; flipH: boolean; flipV: boolean; align: string; find: boolean }[];
  blink: boolean; live: boolean; count: number;
  hud: boolean; hudBlock: string; blockLeft: string; blockLeftColor: string; isLastBlock: boolean; clock: string;
  flashText: string; flashSub: string; flashPct: number;
  /** Informações para o monitor web. */
  curName: string; stamp: string; speed: string; webCtrl: boolean;
}

export type BusMsg =
  | { t: 'html'; html: string }
  | { t: 'state'; s: OutputSnapshot }
  | { t: 'y'; y: number; v: number };

export type BusCmd =
  | { t: 'hello' }
  | { t: 'key'; key: string; shiftKey?: boolean; ctrlKey?: boolean; altKey?: boolean }
  | { t: 'action'; name: string }
  | { t: 'togglePlay' };

export interface MonitorOptions { port: number; pass: string; ctrl: boolean }
export interface MonitorStatus { ok: boolean; url?: string; urls?: string[]; error?: string }

export interface Platform {
  kind: 'web' | 'desktop';
  /** Armazenamento síncrono de chave/valor (localStorage na web, arquivos em %APPDATA% no desktop). */
  storage: { get(key: string): string | null; set(key: string, value: string): void };
  listDisplays(): Promise<DisplayInfo[]>;
  onDisplaysChanged(cb: (d: DisplayInfo[]) => void): void;
  openOutput(d: DisplayInfo): void;
  closeOutput(id: string): void;
  onOutputClosed(cb: (id: string) => void): void;
  /** Envia estado/HTML/posição para todas as saídas. */
  publish(msg: BusMsg): void;
  onCommand(cb: (cmd: BusCmd) => void): void;
  /** Diálogo nativo de abrir (desktop). Na web retorna null e o editor usa <input type=file>. */
  openFile(): Promise<OpenedFile | null>;
  saveFile(name: string, data: string, mime: string, path?: string, saveAs?: boolean): Promise<{ name: string; path?: string } | null>;
  printHtml(html: string, mode: 'print' | 'pdf', name: string): Promise<boolean>;
  monitor: {
    start(o: MonitorOptions): Promise<MonitorStatus>;
    stop(): Promise<void>;
    update(o: Partial<MonitorOptions>): void;
    onViewers(cb: (n: number) => void): void;
  } | null;
  mos: { connect(host: string, port: number): Promise<{ ok: boolean; error?: string }>; disconnect(): void } | null;
  win: { minimize(): void; maximize(): void; close(): void } | null;
  /** Arquivo aberto pelo sistema (duplo clique num .tps, segunda instância). */
  onOpenPath(cb: (f: OpenedFile) => void): void;
  setTitle(t: string): void;
  openExternal(url: string): void;
}
