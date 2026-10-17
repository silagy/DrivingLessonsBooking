import { provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { NewVersionStore } from '../../../core/pwa/new-version.store';
import { NewVersionNoticeComponent } from './new-version-notice.component';

describe('NewVersionNoticeComponent', () => {
    let isReady: ReturnType<typeof signal<boolean>>;
    let reload: ReturnType<typeof vi.fn>;

    async function renderNotice(): Promise<ComponentFixture<NewVersionNoticeComponent>> {
        TestBed.configureTestingModule({
            imports: [
                TranslocoTestingModule.forRoot({
                    langs: {
                        en: {
                            pwa: { newVersionReady: 'A new version of DriveCal is ready.' },
                            general: { refresh: 'Refresh' },
                        },
                    },
                    translocoConfig: { availableLangs: ['en'], defaultLang: 'en' },
                    preloadLangs: true,
                }),
            ],
            providers: [provideZonelessChangeDetection(), { provide: NewVersionStore, useValue: { isReady, reload } }],
        });
        const fixture = TestBed.createComponent(NewVersionNoticeComponent);
        await fixture.whenStable();

        return fixture;
    }

    beforeEach(() => {
        isReady = signal(false);
        reload = vi.fn();
    });

    it('shows nothing while the running version is current', async () => {
        //given
        const fixture = await renderNotice();

        //when
        const notice = (fixture.nativeElement as HTMLElement).querySelector('[role=status]');

        //then
        expect(notice).toBeNull();
    });

    it('offers a refresh once a new version is ready', async () => {
        //given
        const fixture = await renderNotice();
        isReady.set(true);
        await fixture.whenStable();

        //when
        const notice = (fixture.nativeElement as HTMLElement).querySelector('[role=status]');

        //then
        expect(notice?.textContent).toContain('A new version of DriveCal is ready.');
        expect(notice?.querySelector('button')?.textContent).toContain('Refresh');
    });

    it('reloads into the new version when the User asks', async () => {
        //given
        const fixture = await renderNotice();
        isReady.set(true);
        await fixture.whenStable();

        //when
        (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('[role=status] button')?.click();

        //then
        expect(reload).toHaveBeenCalledOnce();
    });
});
