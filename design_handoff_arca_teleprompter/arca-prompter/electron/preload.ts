// Ponte segura entre as páginas e o processo principal (contextIsolation ligado).
import { contextBridge, ipcRenderer } from 'electron';

const isEditor = !location.hash || location.hash === '#' || location.hash === '#/';

contextBridge.exposeInMainWorld('arca', {
  invoke: (ch: string, ...a: any[]) => ipcRenderer.invoke(ch, ...a),
  send: (ch: string, ...a: any[]) => ipcRenderer.send(ch, ...a),
  on: (ch: string, cb: (...a: any[]) => void) => { ipcRenderer.on(ch, (_e, ...a) => cb(...a)); },
  // Leitura síncrona dos dados salvos, só no editor (o protótipo lia o localStorage na montagem).
  store: isEditor ? ipcRenderer.sendSync('store:getAll') : {}
});
