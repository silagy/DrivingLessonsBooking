import { readFileSync } from 'node:fs';
import type { Mock } from 'vitest';

interface FakeRequest {
    mode: string;
    url: string;
}

interface WorkerEvent {
    request?: FakeRequest;
    waitUntil(promise: Promise<unknown>): void;
    respondWith(promise: Promise<unknown>): void;
}

interface FakeSelf {
    addEventListener(type: string, listener: (event: WorkerEvent) => void): void;
    skipWaiting: Mock<() => Promise<void>>;
    clients: { claim: Mock<() => Promise<void>> };
}

interface DispatchResult {
    responded: boolean;
    response: Promise<unknown> | undefined;
}

const OFFLINE_RESPONSE = { page: 'offline' };

function startWorker(network: (request: FakeRequest) => Promise<unknown>) {
    const listeners = new Map<string, (event: WorkerEvent) => void>();
    const stored = new Map<string, Map<string, unknown>>();
    const self: FakeSelf = {
        addEventListener: (type, listener) => void listeners.set(type, listener),
        skipWaiting: vi.fn(() => Promise.resolve()),
        clients: { claim: vi.fn(() => Promise.resolve()) },
    };
    const caches = {
        open: async (name: string) => {
            const cache = stored.get(name) ?? new Map<string, unknown>();
            stored.set(name, cache);

            return {
                add: async (url: string) => void cache.set(url, OFFLINE_RESPONSE),
                match: async (url: string) => cache.get(url),
            };
        },
        match: async (url: string) => [...stored.values()].find(cache => cache.has(url))?.get(url),
        keys: async () => [...stored.keys()],
        delete: async (name: string) => stored.delete(name),
    };
    new Function('self', 'caches', 'fetch', readFileSync('public/sw.js', 'utf8'))(self, caches, network);

    const dispatch = async (type: string, request?: FakeRequest): Promise<DispatchResult> => {
        const waits: Promise<unknown>[] = [];
        let response: Promise<unknown> | undefined;
        let responded = false;
        listeners.get(type)?.({
            request,
            waitUntil: promise => void waits.push(promise),
            respondWith: promise => {
                responded = true;
                response = promise;
            },
        });
        await Promise.all(waits);

        return { responded, response };
    };

    return { self, stored, dispatch };
}

describe('offline service worker', () => {
    it('caches the offline page and takes over as soon as it installs', async () => {
        //given
        const worker = startWorker(() => Promise.resolve({}));

        //when
        await worker.dispatch('install');

        //then
        expect([...worker.stored.keys()]).toEqual(['drivecal-offline-v1']);
        expect(worker.stored.get('drivecal-offline-v1')?.get('/offline.html')).toBe(OFFLINE_RESPONSE);
        expect(worker.self.skipWaiting).toHaveBeenCalled();
    });

    it('answers a navigation from the network while online', async () => {
        //given
        const page = { page: 'students' };
        const worker = startWorker(() => Promise.resolve(page));
        await worker.dispatch('install');

        //when
        const result = await worker.dispatch('fetch', { mode: 'navigate', url: '/students' });

        //then
        expect(result.responded).toBe(true);
        expect(await result.response).toBe(page);
    });

    it('answers a navigation with the offline page when the network fails', async () => {
        //given
        const worker = startWorker(() => Promise.reject(new TypeError('Failed to fetch')));
        await worker.dispatch('install');

        //when
        const result = await worker.dispatch('fetch', { mode: 'navigate', url: '/students' });

        //then
        expect(await result.response).toBe(OFFLINE_RESPONSE);
    });

    it.each([
        { mode: 'cors', url: '/api/publications' },
        { mode: 'same-origin', url: '/i18n/he.json?revision=2026-10-17' },
        { mode: 'no-cors', url: '/main-IUEPGTXJ.js' },
        { mode: 'cors', url: '/api/publications/7f3c/excel' },
    ])('leaves api calls, bundles and translations to the browser ($url)', async request => {
        //given
        const worker = startWorker(() => Promise.reject(new TypeError('Failed to fetch')));

        //when
        const result = await worker.dispatch('fetch', request);

        //then
        expect(result.responded).toBe(false);
    });

    it('keeps only its own cache when it activates', async () => {
        //given
        const worker = startWorker(() => Promise.resolve({}));
        worker.stored.set('drivecal-offline-v0', new Map());
        worker.stored.set('ngsw:/:db:control', new Map());
        await worker.dispatch('install');

        //when
        await worker.dispatch('activate');

        //then
        expect([...worker.stored.keys()]).toEqual(['drivecal-offline-v1']);
        expect(worker.self.clients.claim).toHaveBeenCalled();
    });
});
