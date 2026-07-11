import { computed, inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs';
import { AppRoutes } from '../shared/config/app-routes';

export interface LoginResponse {
  accessToken: string;
  expiresAtUtc: string;
}

const TOKEN_KEY = 'auth_token';

function readEmailClaim(token: string | null): string | null {
  if (!token) {
    return null;
  }
  try {
    const payload = JSON.parse(atob(token.split('.')[1])) as { email?: string };
    return payload.email ?? null;
  } catch {
    return null;
  }
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  readonly token = signal<string | null>(localStorage.getItem(TOKEN_KEY));
  readonly isAuthenticated = computed(() => this.token() !== null);
  readonly email = computed(() => readEmailClaim(this.token()));

  login(email: string, password: string) {
    return this.http
      .post<LoginResponse>('/api/auth/login', { email, password })
      .pipe(
        tap((response) => {
          localStorage.setItem(TOKEN_KEY, response.accessToken);
          this.token.set(response.accessToken);
        }),
      );
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    this.token.set(null);
    void this.router.navigate(['/', AppRoutes.login]);
  }
}
