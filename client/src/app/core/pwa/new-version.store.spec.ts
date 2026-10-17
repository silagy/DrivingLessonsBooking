import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { NewVersionStore } from './new-version.store';

function servedPage(main: string): string {
    return `<!doctype html><html><head></head><body><app-root></app-root><script src="${main}" type="module"></script></body></html>`;
}

describe('NewVersionStore', () => {
    let runningBundle: HTMLScriptElement;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [provideZonelessChangeDetection(), provideHttpClient(), provideHttpClientTesting()],
        });
        runningBundle = document.createElement('script');
        runningBundle.setAttribute('src', 'main-AAAAAAAA.js');
        runningBundle.setAttribute('type', 'module');
        document.head.appendChild(runningBundle);
    });

    afterEach(() => {
        runningBundle.remove();
    });

    it('announces a new version when the server serves another main bundle', async () => {
        //given
        const store = TestBed.inject(NewVersionStore);
        const checking = store.check();

        //when
        TestBed.inject(HttpTestingController).expectOne('/index.html').flush(servedPage('main-BBBBBBBB.js'));
        await checking;

        //then
        expect(store.isReady()).toBe(true);
    });

    it('stays quiet while the server serves the running version', async () => {
        //given
        const store = TestBed.inject(NewVersionStore);
        const checking = store.check();

        //when
        TestBed.inject(HttpTestingController).expectOne('/index.html').flush(servedPage('main-AAAAAAAA.js'));
        await checking;

        //then
        expect(store.isReady()).toBe(false);
    });

    it('stays quiet when the server cannot be reached', async () => {
        //given
        const store = TestBed.inject(NewVersionStore);
        const checking = store.check();

        //when
        TestBed.inject(HttpTestingController).expectOne('/index.html').error(new ProgressEvent('error'));

        //then
        await expect(checking).resolves.toBeUndefined();
        expect(store.isReady()).toBe(false);
    });

    it('stays quiet when a page has no fingerprinted main bundle', async () => {
        //given
        runningBundle.setAttribute('src', 'main.js');
        const store = TestBed.inject(NewVersionStore);
        const checking = store.check();

        //when
        TestBed.inject(HttpTestingController).expectOne('/index.html').flush(servedPage('main.js'));
        await checking;

        //then
        expect(store.isReady()).toBe(false);
    });

    it('stops asking the server once it has found a new version', async () => {
        //given
        const store = TestBed.inject(NewVersionStore);
        const http = TestBed.inject(HttpTestingController);
        const first = store.check();
        http.expectOne('/index.html').flush(servedPage('main-BBBBBBBB.js'));
        await first;

        //when
        await store.check();

        //then
        http.verify();
    });

    it('hides the notice when the User puts it off', async () => {
        //given
        const store = TestBed.inject(NewVersionStore);
        const checking = store.check();
        TestBed.inject(HttpTestingController).expectOne('/index.html').flush(servedPage('main-BBBBBBBB.js'));
        await checking;

        //when
        store.dismiss();

        //then
        expect(store.isShown()).toBe(false);
    });

    it('shows the notice again when the app comes back after the User put it off', async () => {
        //given
        const store = TestBed.inject(NewVersionStore);
        const http = TestBed.inject(HttpTestingController);
        const checking = store.check();
        http.expectOne('/index.html').flush(servedPage('main-BBBBBBBB.js'));
        await checking;
        store.dismiss();

        //when
        await store.check();

        //then
        expect(store.isShown()).toBe(true);
        http.verify();
    });

    it('asks the server once while a check is in flight', async () => {
        //given
        const store = TestBed.inject(NewVersionStore);

        //when
        const checks = [store.check(), store.check()];

        //then
        TestBed.inject(HttpTestingController).expectOne('/index.html').flush(servedPage('main-AAAAAAAA.js'));
        await Promise.all(checks);
    });
});
