import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, withNavigationErrorHandler } from '@angular/router';
import { checkForNewVersionAfterNavigationError } from './check-for-new-version';
import { NewVersionStore } from './new-version.store';

describe('checkForNewVersionAfterNavigationError', () => {
    it('checks for a new version when a screen fails to load', async () => {
        //given
        const check = vi.fn().mockResolvedValue(undefined);
        TestBed.configureTestingModule({
            providers: [
                provideZonelessChangeDetection(),
                { provide: NewVersionStore, useValue: { check } },
                provideRouter(
                    [
                        {
                            path: 'students',
                            loadComponent: () =>
                                Promise.reject(new TypeError('Failed to fetch dynamically imported module')),
                        },
                    ],
                    withNavigationErrorHandler(checkForNewVersionAfterNavigationError),
                ),
            ],
        });

        //when
        await TestBed.inject(Router).navigateByUrl('/students').catch(() => undefined);

        //then
        expect(check).toHaveBeenCalledOnce();
    });
});
