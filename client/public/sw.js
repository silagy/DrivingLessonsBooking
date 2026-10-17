const OFFLINE_CACHE = 'drivecal-offline-v1';
const OFFLINE_PAGE = '/offline.html';

self.addEventListener('install', event => {
  event.waitUntil(
    caches
      .open(OFFLINE_CACHE)
      .then(cache => cache.add(OFFLINE_PAGE))
      .then(() => self.skipWaiting()),
  );
});

self.addEventListener('activate', event => {
  event.waitUntil(
    caches
      .keys()
      .then(names => Promise.all(names.filter(name => name !== OFFLINE_CACHE).map(name => caches.delete(name))))
      .then(() => self.clients.claim()),
  );
});

self.addEventListener('fetch', event => {
  if (event.request.mode !== 'navigate') {
    return;
  }

  event.respondWith(fetch(event.request).catch(() => caches.match(OFFLINE_PAGE)));
});
