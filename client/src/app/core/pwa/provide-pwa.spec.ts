import { ApplicationInitStatus } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { providePwa } from './provide-pwa';

describe('providePwa', () => {
    let register: ReturnType<typeof vi.fn>;

    beforeEach(() => {
        register = vi.fn().mockResolvedValue({});
        Object.defineProperty(navigator, 'serviceWorker', { configurable: true, value: { register } });
    });

    afterEach(() => {
        delete (navigator as { serviceWorker?: unknown }).serviceWorker;
        window.history.pushState({}, '', '/');
    });

    it('registers the worker when the app starts', async () => {
        //given
        TestBed.configureTestingModule({ providers: [providePwa()] });

        //when
        await TestBed.inject(ApplicationInitStatus).donePromise;

        //then
        expect(register).toHaveBeenCalledExactlyOnceWith('/sw.js', { scope: '/' });
    });

    it('holds back the install prompt on the student form once the app starts', async () => {
        //given
        TestBed.configureTestingModule({ providers: [providePwa()] });
        await TestBed.inject(ApplicationInitStatus).donePromise;
        window.history.pushState({}, '', '/s/3f9c2a7d');

        //when
        const prompt = new Event('beforeinstallprompt', { cancelable: true });
        window.dispatchEvent(prompt);

        //then
        expect(prompt.defaultPrevented).toBe(true);
    });
});
