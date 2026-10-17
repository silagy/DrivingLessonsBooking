import { provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { providePrimeNG } from 'primeng/config';
import { Drawer } from 'primeng/drawer';
import { AuthService } from '../../core/auth.service';
import { AppLanguage, LanguageService } from '../../core/language.service';
import { Role } from '../../shared/models/role.enum';
import { AdminShellComponent } from './admin-shell.component';

async function render(role: Role, lang: AppLanguage): Promise<ComponentFixture<AdminShellComponent>> {
    TestBed.configureTestingModule({
        imports: [
            AdminShellComponent,
            TranslocoTestingModule.forRoot({
                langs: { en: {} },
                translocoConfig: { availableLangs: ['en'], defaultLang: 'en' },
            }),
        ],
        providers: [
            provideZonelessChangeDetection(),
            provideRouter([{ path: '**', children: [] }]),
            providePrimeNG(),
            {
                provide: AuthService,
                useValue: { email: signal('admin@local.dev'), role: signal(role), logout: vi.fn() },
            },
            {
                provide: LanguageService,
                useValue: {
                    lang: signal(lang),
                    locale: signal('en-IL'),
                    isRtl: signal(lang === 'he'),
                    use: vi.fn(),
                },
            },
        ],
    });

    const fixture = TestBed.createComponent(AdminShellComponent);
    await fixture.whenStable();

    return fixture;
}

function menuButton(fixture: ComponentFixture<AdminShellComponent>): HTMLButtonElement {
    return (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('button.shell__menu-button')!;
}

async function openDrawer(fixture: ComponentFixture<AdminShellComponent>): Promise<void> {
    menuButton(fixture).click();
    await fixture.whenStable();
}

function drawerPosition(fixture: ComponentFixture<AdminShellComponent>): string {
    return (fixture.debugElement.query(By.directive(Drawer)).componentInstance as Drawer).position();
}

describe('AdminShellComponent', () => {
    afterEach(() => {
        TestBed.resetTestingModule();
        document.body.innerHTML = '';
    });

    it('opens the drawer with every Administrator link', async () => {
        //given
        const fixture = await render(Role.administrator, 'he');

        //when
        await openDrawer(fixture);

        //then
        const links = [...document.body.querySelectorAll<HTMLAnchorElement>('a.shell-drawer__link')];
        expect(links.map((link) => link.getAttribute('href'))).toEqual([
            '/',
            '/teachers',
            '/students',
            '/week-schedules',
            '/publications',
            '/publications/history',
            '/users',
        ]);
        expect(links.map((link) => link.textContent?.trim())).toContain('shell.nav.weeklyPrep');
    });

    it('lists only the Teacher links', async () => {
        //given
        const fixture = await render(Role.teacher, 'he');

        //when
        await openDrawer(fixture);

        //then
        const hrefs = [...document.body.querySelectorAll('a.shell-drawer__link')].map((link) =>
            link.getAttribute('href'),
        );
        expect(hrefs).toEqual(['/week-schedules', '/publications', '/publications/history']);
    });

    it('closes the drawer when a link is tapped', async () => {
        //given
        const fixture = await render(Role.administrator, 'he');
        await openDrawer(fixture);

        //when
        document.body.querySelector<HTMLAnchorElement>('a.shell-drawer__link')!.click();
        await fixture.whenStable();

        //then
        expect(menuButton(fixture).getAttribute('aria-expanded')).toBe('false');
    });

    it('signs out from the drawer', async () => {
        //given
        const fixture = await render(Role.administrator, 'he');
        await openDrawer(fixture);

        //when
        document.body.querySelector<HTMLButtonElement>('button.shell-drawer__sign-out')!.click();

        //then
        expect(TestBed.inject(AuthService).logout).toHaveBeenCalledOnce();
    });

    it('shows the email in the drawer footer', async () => {
        //given
        const fixture = await render(Role.administrator, 'he');

        //when
        await openDrawer(fixture);

        //then
        expect(document.body.querySelector('.shell-drawer__email')?.textContent?.trim()).toBe('admin@local.dev');
    });

    it('opens from the right in Hebrew', async () => {
        //given
        const fixture = await render(Role.administrator, 'he');

        //when
        await openDrawer(fixture);

        //then
        expect(drawerPosition(fixture)).toBe('right');
    });

    it('opens from the left in English', async () => {
        //given
        const fixture = await render(Role.administrator, 'en');

        //when
        await openDrawer(fixture);

        //then
        expect(drawerPosition(fixture)).toBe('left');
    });
});
