import React from 'react';
import { createRoot } from 'react-dom/client';
import './fonts';
import './styles/proto.css';
import './styles/hover.css';
import './styles/app.css';

// Uma única build serve três páginas: editor, janela de prompter (#/output?d=…) e monitor web (/monitor).
const root = createRoot(document.getElementById('root')!);
const hash = location.hash;

if (location.pathname.startsWith('/monitor') || hash.startsWith('#/monitor')) {
  import('./output/MonitorPage').then(({ MonitorPage }) => root.render(<MonitorPage />));
} else if (hash.startsWith('#/setup') || hash.startsWith('#/uninstall')) {
  // Assistente de instalação / desinstalação (Setup.exe). No navegador roda em modo demonstração.
  import('./setup/Setup').then(({ default: Setup }) => root.render(<Setup startMode={hash.startsWith('#/uninstall') ? 'uninstall' : 'install'} />));
} else if (hash.startsWith('#/output')) {
  const id = new URLSearchParams(hash.split('?')[1] || '').get('d') || '';
  import('./output/OutputPage').then(({ OutputPage }) => root.render(<OutputPage id={id} />));
} else {
  import('./editor/App').then(({ default: App }) => root.render(<App />));
}
