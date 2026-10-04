import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../../core/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { ChangeMyPasswordRequest } from '../data/change-my-password.request';
import { MeApiService } from '../data/me-api.service';

@Injectable()
export class MyPasswordStore {
    private readonly api = inject(MeApiService);
    private readonly auth = inject(AuthService);
    private readonly toast = inject(ToastService);

    private readonly saving = signal(false);
    private readonly refusalMessage = signal<string | null>(null);

    readonly isSaving = this.saving.asReadonly();
    readonly refusal = this.refusalMessage.asReadonly();

    async change(request: ChangeMyPasswordRequest): Promise<boolean> {
        this.saving.set(true);
        this.refusalMessage.set(null);

        try {
            const response = await firstValueFrom(this.api.changeMyPassword(request));
            this.auth.useToken(response.accessToken);
            this.toast.success('myPassword.changed', { key: 'myPassword.changedDetail' });
            return true;
        } catch (error) {
            this.refusalMessage.set(this.toast.messageOf(error));
            return false;
        } finally {
            this.saving.set(false);
        }
    }
}
