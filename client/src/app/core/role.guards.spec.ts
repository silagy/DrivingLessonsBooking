import { provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, provideRouter, Route, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { AuthService } from './auth.service';
import { ToastService } from './services/toast.service';
import { administratorGuard, homeGuard } from './role.guards';

interface SignedInAs {
    isAuthenticated: boolean;
    isAdministrator: boolean;
}

function setUp(user: SignedInAs) {
    const info = vi.fn().mockResolvedValue(undefined);

    TestBed.configureTestingModule({
        providers: [
            provideZonelessChangeDetection(),
            provideRouter([]),
            {
                provide: AuthService,
                useValue: { isAuthenticated: signal(user.isAuthenticated), isAdministrator: signal(user.isAdministrator) },
            },
            { provide: ToastService, useValue: { info } },
        ],
    });

    return { info, router: TestBed.inject(Router) };
}

function urlOf(router: Router, result: unknown): string {
    return router.serializeUrl(result as UrlTree);
}

function runAdministratorGuard(): unknown {
    return TestBed.runInInjectionContext(() => administratorGuard({} as Route, []));
}

function runHomeGuard(): unknown {
    return TestBed.runInInjectionContext(() => homeGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot));
}

describe('administratorGuard', () => {
    it('lets an Administrator into an Administrator-only screen', () => {
        //given
        const { info } = setUp({ isAuthenticated: true, isAdministrator: true });

        //when
        const result = runAdministratorGuard();

        //then
        expect(result).toBe(true);
        expect(info).not.toHaveBeenCalled();
    });

    it('sends a Teacher to weekly prep with the no-access toast', () => {
        //given
        const { info, router } = setUp({ isAuthenticated: true, isAdministrator: false });

        //when
        const result = runAdministratorGuard();

        //then
        expect(urlOf(router, result)).toBe('/week-schedules');
        expect(info).toHaveBeenCalledWith('access.refusedTitle', { key: 'access.refusedDetail' });
    });

    it('sends a signed-out visitor to sign in without the no-access toast', () => {
        //given
        const { info, router } = setUp({ isAuthenticated: false, isAdministrator: false });

        //when
        const result = runAdministratorGuard();

        //then
        expect(urlOf(router, result)).toBe('/login');
        expect(info).not.toHaveBeenCalled();
    });
});

describe('homeGuard', () => {
    it('keeps an Administrator on the dashboard', () => {
        //given
        setUp({ isAuthenticated: true, isAdministrator: true });

        //when
        const result = runHomeGuard();

        //then
        expect(result).toBe(true);
    });

    it('sends a Teacher from the dashboard to weekly prep without a toast', () => {
        //given
        const { info, router } = setUp({ isAuthenticated: true, isAdministrator: false });

        //when
        const result = runHomeGuard();

        //then
        expect(urlOf(router, result)).toBe('/week-schedules');
        expect(info).not.toHaveBeenCalled();
    });
});
