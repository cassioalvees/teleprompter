// Service worker: guarda a "casca" do app para abrir instantaneamente (e mostrar "Reconectando…"
// sem rede). O conteúdo do roteiro sempre vem ao vivo pelo WebSocket.
const CACHE = 'arca-web-v11';
const SHELL = ['./', 'index.html', 'app.css', 'app.js', 'manifest.webmanifest', 'icons/apple-touch-icon.png', 'icons/icon-512.png', 'icons/icon.svg',
  'fonts/AtkinsonHyperlegible-400.ttf', 'fonts/AtkinsonHyperlegible-700.ttf', 'fonts/Sora-400.ttf', 'fonts/Sora-600.ttf', 'fonts/Raleway-500.ttf', 'fonts/Raleway-600.ttf'];

self.addEventListener('install', e => e.waitUntil(caches.open(CACHE).then(c => c.addAll(SHELL)).then(() => self.skipWaiting())));
self.addEventListener('activate', e => e.waitUntil(caches.keys().then(ks => Promise.all(ks.filter(k => k !== CACHE).map(k => caches.delete(k)))).then(() => self.clients.claim())));
self.addEventListener('fetch', e => {
  const u = new URL(e.request.url);
  if (e.request.method !== 'GET' || u.pathname.startsWith('/ws') || u.pathname.startsWith('/api')) return;
  // Rede primeiro (o app pode ter sido atualizado); cache se estiver offline.
  e.respondWith(fetch(e.request).then(r => { const c = r.clone(); caches.open(CACHE).then(x => x.put(e.request, c)); return r; }).catch(() => caches.match(e.request, { ignoreSearch: true })));
});
