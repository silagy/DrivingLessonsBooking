import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { providePrimeNG } from 'primeng/config';
import { Role } from '../../../../../shared/models/role.enum';
import { User } from '../../../domain/user.model';
import { UserCardComponent } from './user-card.component';

const TEACHER_USER: User = {
    id: 'user-ziv',
    name: 'Ziv Peer',
    signInEmail: 'ziv.peer.driving.instructor@drivecal.co.il',
    role: Role.teacher,
    teacherId: 'teacher-peer',
    teacherName: 'Ziv Peer',
    isDeleted: false,
};

interface CardOptions {
    isYou?: boolean;
    disabled?: boolean;
}

async function render(overrides: Partial<User>, options: CardOptions = {}): Promise<ComponentFixture<UserCardComponent>> {
    TestBed.configureTestingModule({
        imports: [
            UserCardComponent,
            TranslocoTestingModule.forRoot({
                langs: { en: {} },
                translocoConfig: { availableLangs: ['en'], defaultLang: 'en' },
            }),
        ],
        providers: [provideZonelessChangeDetection(), providePrimeNG()],
    });

    const fixture = TestBed.createComponent(UserCardComponent);
    fixture.componentRef.setInput('user', { ...TEACHER_USER, ...overrides });
    fixture.componentRef.setInput('isYou', options.isYou ?? false);
    fixture.componentRef.setInput('disabled', options.disabled ?? false);
    await fixture.whenStable();

    return fixture;
}

function card(fixture: ComponentFixture<UserCardComponent>): HTMLElement {
    return (fixture.nativeElement as HTMLElement).querySelector('article.user-card') as HTMLElement;
}

describe('UserCardComponent', () => {
    it('shows the name, email, Role and linked Teacher', async () => {
        //when
        const fixture = await render({});

        //then
        expect(card(fixture).querySelector('.user-card__name')?.textContent?.trim()).toBe('Ziv Peer');
        expect(card(fixture).querySelector('.user-card__email')?.textContent?.trim()).toBe(
            'ziv.peer.driving.instructor@drivecal.co.il',
        );
        expect(card(fixture).querySelector('.user-card__details')?.textContent).toContain('users.roles.teacher');
        expect(card(fixture).querySelector('.user-card__details')?.textContent).toContain('Ziv Peer');
    });

    it('tags the signed-in User as you', async () => {
        //when
        const fixture = await render({}, { isYou: true });

        //then
        expect(card(fixture).querySelector('.user-card__top')?.textContent).toContain('users.you');
    });

    it('shows a dash for an Administrator without a Teacher', async () => {
        //when
        const fixture = await render({ role: Role.administrator, teacherId: null, teacherName: null });

        //then
        expect(card(fixture).querySelector('.user-card__details')?.textContent).toContain('users.noTeacher');
    });

    it('asks for the row menu on an active User', async () => {
        //given
        const fixture = await render({});
        const requested: Event[] = [];
        fixture.componentInstance.actionsRequested.subscribe((event) => requested.push(event));

        //when
        card(fixture).querySelector<HTMLButtonElement>('button.user-card__actions')?.click();

        //then
        expect(requested.length).toBe(1);
    });

    it('offers Restore instead of the menu on a Deleted User', async () => {
        //given
        const fixture = await render({ isDeleted: true });
        let restoreCount = 0;
        fixture.componentInstance.restoreRequested.subscribe(() => restoreCount++);

        //when
        card(fixture).querySelector<HTMLButtonElement>('button.user-card__restore')?.click();

        //then
        expect(card(fixture).classList).toContain('user-card--deleted');
        expect(card(fixture).querySelector('button.user-card__actions')).toBeNull();
        expect(restoreCount).toBe(1);
    });

    it('disables Restore while a change is saving', async () => {
        //when
        const fixture = await render({ isDeleted: true }, { disabled: true });

        //then
        expect(card(fixture).querySelector<HTMLButtonElement>('button.user-card__restore')?.disabled).toBe(true);
    });
});
