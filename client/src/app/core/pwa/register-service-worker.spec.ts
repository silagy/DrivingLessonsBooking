import { registerServiceWorker } from './register-service-worker';

describe('registerServiceWorker', () => {
    it('registers the worker for the whole app', async () => {
        //given
        const container = { register: vi.fn().mockResolvedValue({}) };

        //when
        await registerServiceWorker(container as unknown as ServiceWorkerContainer);

        //then
        expect(container.register).toHaveBeenCalledExactlyOnceWith('/sw.js', { scope: '/' });
    });

    it('does nothing in a browser without service workers', async () => {
        //when
        const registering = registerServiceWorker(undefined);

        //then
        await expect(registering).resolves.toBeUndefined();
    });

    it('keeps the app starting when the browser refuses the worker', async () => {
        //given
        const container = { register: vi.fn().mockRejectedValue(new DOMException('insecure', 'SecurityError')) };

        //when
        const registering = registerServiceWorker(container as unknown as ServiceWorkerContainer);

        //then
        await expect(registering).resolves.toBeUndefined();
    });
});
