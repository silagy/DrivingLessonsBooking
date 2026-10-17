import { provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { NewVersionStore } from '../../../core/pwa/new-version.store';
import { NewVersionNoticeComponent } from './new-version-notice.component';

describe('NewVersionNoticeComponent', () => {
    let isShown: ReturnType<typeof signal<boolean>>;
    let reload: ReturnType<typeof vi.fn>;
    let dismiss: ReturnType<typeof vi.fn>;

    async function renderNotice(): Promise<ComponentFixture<NewVersionNoticeComponent>> {
        TestBed.configureTestingModule({
            imports: [
                TranslocoTestingModule.forRoot({
                    langs: {
                        en: {
                            pwa: { newVersionReady: 'A new version of DriveCal is ready.', later: 'Not now' },
                            general: { refresh: 'Refresh' },
                        },
                    },
                    translocoConfig: { availableLangs: ['en'], defaultLang: 'en' },
                    preloadLangs: true,
                }),
            ],
            providers: [
                provideZonelessChangeDetection(),
                { provide: NewVersionStore, useValue: { isShown, reload, dismiss } },
            ],
        });
        const fixture = TestBed.createComponent(NewVersionNoticeComponent);
        await fixture.whenStable();

        return fixture;
    }

    async function renderShownNotice(): Promise<HTMLElement> {
        const fixture = await renderNotice();
        isShown.set(true);
        await fixture.whenStable();

        return fixture.nativeElement as HTMLElement;
    }

    beforeEach(() => {
        isShown = signal(false);
        reload = vi.fn();
        dismiss = vi.fn();
    });

    it('keeps an empty live region in the page while the running version is current', async () => {
        //given
        const fixture = await renderNotice();

        //when
        const liveRegion = (fixture.nativeElement as HTMLElement).querySelector('[role=status]');

        //then
        expect(liveRegion?.textContent?.trim()).toBe('');
        expect(liveRegion?.querySelector('.new-version')).toBeNull();
    });

    it('announces a new version inside the live region', async () => {
        //given
        const element = await renderShownNotice();

        //when
        const notice = element.querySelector('[role=status] .new-version');

        //then
        expect(notice?.textContent).toContain('A new version of DriveCal is ready.');
        expect(notice?.querySelector('.new-version__refresh')?.textContent).toContain('Refresh');
        expect(notice?.querySelector('.new-version__later')?.textContent).toContain('Not now');
    });

    it('reloads into the new version when the User asks', async () => {
        //given
        const element = await renderShownNotice();

        //when
        element.querySelector<HTMLButtonElement>('.new-version__refresh')?.click();

        //then
        expect(reload).toHaveBeenCalledOnce();
    });

    it('puts the notice off when the User asks', async () => {
        //given
        const element = await renderShownNotice();

        //when
        element.querySelector<HTMLButtonElement>('.new-version__later')?.click();

        //then
        expect(dismiss).toHaveBeenCalledOnce();
        expect(reload).not.toHaveBeenCalled();
    });
});
