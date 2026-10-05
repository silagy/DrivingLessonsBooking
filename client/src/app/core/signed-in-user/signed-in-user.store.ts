import { computed, inject, Injectable, resource } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../auth.service';
import { SignedInUserApiService } from './signed-in-user-api.service';
import { GetUserResponse } from './get-user.response';

@Injectable({ providedIn: 'root' })
export class SignedInUserStore {
    private readonly api = inject(SignedInUserApiService);
    private readonly auth = inject(AuthService);

    private readonly meResource = resource({
        params: () => this.auth.token() ?? undefined,
        loader: () => firstValueFrom(this.api.getMe()),
    });

    private readonly signedInUser = computed<GetUserResponse | null>(() =>
        this.meResource.hasValue() ? this.meResource.value() : null,
    );

    readonly name = computed(() => this.signedInUser()?.name ?? null);
    readonly teacherName = computed(() => this.signedInUser()?.teacherName ?? null);
}
