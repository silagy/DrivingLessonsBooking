import { ApplicationInitStatus } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { NewVersionStore } from './new-version.store';
import { providePwa } from './provide-pwa';

describe('providePwa', () => {
    let register: ReturnType<typeof vi.fn>;
    let check: ReturnType<typeof vi.fn>;

    async function startApp(): Promise<void> {
        TestBed.configureTestingModule({ providers: [providePwa(), { provide: NewVersionStore, useValue: { check } }] });
        await TestBed.inject(ApplicationInitStatus).donePromise;
    }

    beforeEach(() => {
        register = vi.fn().mockResolvedValue({});
        check = vi.fn().mockResolvedValue(undefined);
        Object.defineProperty(navigator, 'serviceWorker', { configurable: true, value: { register } });
    });

    afterEach(() => {
        delete (navigator as { serviceWorker?: unknown }).serviceWorker;
        delete (document as { visibilityState?: unknown }).visibilityState;
        window.history.pushState({}, '', '/');
    });

    it('registers the worker when the app starts', async () => {
        //given
        TestBed.configureTestingModule({ providers: [providePwa(), { provide: NewVersionStore, useValue: { check } }] });

        //when
        await TestBed.inject(ApplicationInitStatus).donePromise;

        //then
        expect(register).toHaveBeenCalledExactlyOnceWith('/sw.js', { scope: '/' });
    });

    it('holds back the install prompt on the student form once the app starts', async () => {
        //given
        await startApp();
        window.history.pushState({}, '', '/s/3f9c2a7d');

        //when
        const prompt = new Event('beforeinstallprompt', { cancelable: true });
        window.dispatchEvent(prompt);

        //then
        expect(prompt.defaultPrevented).toBe(true);
    });

    it('checks for a new version when the app comes back to the screen', async () => {
        //given
        await startApp();
        Object.defineProperty(document, 'visibilityState', { configurable: true, value: 'visible' });

        //when
        document.dispatchEvent(new Event('visibilitychange'));

        //then
        expect(check).toHaveBeenCalledOnce();
    });

    it('leaves the check alone when the app goes to the background', async () => {
        //given
        await startApp();
        Object.defineProperty(document, 'visibilityState', { configurable: true, value: 'hidden' });

        //when
        document.dispatchEvent(new Event('visibilitychange'));

        //then
        expect(check).not.toHaveBeenCalled();
    });
});
