import { Signal } from '@angular/core';
import { User } from '../../domain/user.model';

export interface UserDialogData<TRequest> {
    user: User;
    refusal: Signal<string | null>;
    isSaving: Signal<boolean>;
    confirm: (request: TRequest) => Promise<boolean>;
}
