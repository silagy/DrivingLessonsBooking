import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, RouterOutlet, Routes } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { AuthService } from '../../core/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { ADMIN_ROUTES } from './admin.routes';

@Component({ imports: [RouterOutlet], template: '<router-outlet />' })
class StubPage {}

function withStubPages(routes: Routes): Routes {
    return routes.map(({ component, loadChildren, children, ...route }) => ({
        ...route,
        ...(component || loadChildren ? { component: StubPage } : {}),
        ...(children ? { children: withStubPages(children) } : {}),
    }));
}

function setUp(isAdministrator: boolean) {
    const info = vi.fn().mockResolvedValue(undefined);

    TestBed.configureTestingModule({
        providers: [
            provideZonelessChangeDetection(),
            provideRouter(withStubPages(ADMIN_ROUTES)),
            { provide: AuthService, useValue: { isAuthenticated: signal(true), isAdministrator: signal(isAdministrator) } },
            { provide: ToastService, useValue: { info } },
        ],
    });

    return { info, router: TestBed.inject(Router) };
}

describe('admin routes', () => {
    it.each([
        { who: 'an Administrator', isAdministrator: true },
        { who: 'a Teacher', isAdministrator: false },
    ])('lands $who on weekly prep', async ({ isAdministrator }) => {
        //given
        const { router } = setUp(isAdministrator);
        const harness = await RouterTestingHarness.create();

        //when
        await harness.navigateByUrl('/');

        //then
        expect(router.url).toBe('/week-schedules');
    });

    it('opens the dashboard for an Administrator', async () => {
        //given
        const { info, router } = setUp(true);
        const harness = await RouterTestingHarness.create();

        //when
        await harness.navigateByUrl('/dashboard');

        //then
        expect(router.url).toBe('/dashboard');
        expect(info).not.toHaveBeenCalled();
    });

    it('sends a Teacher from the dashboard to weekly prep with the no-access toast', async () => {
        //given
        const { info, router } = setUp(false);
        const harness = await RouterTestingHarness.create();

        //when
        await harness.navigateByUrl('/dashboard');

        //then
        expect(router.url).toBe('/week-schedules');
        expect(info).toHaveBeenCalledWith('access.refusedTitle', { key: 'access.refusedDetail' });
    });
});
