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

type TokenClaim = 'email' | 'sub';

const BASE64_BLOCK = 4;

function decodePayload(token: string): Record<string, unknown> {
  const segment = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
  const padded = segment.padEnd(Math.ceil(segment.length / BASE64_BLOCK) * BASE64_BLOCK, '=');

  return JSON.parse(atob(padded)) as Record<string, unknown>;
}

function readClaim(token: string | null, claim: TokenClaim): string | null {
  if (!token) {
    return null;
  }
  try {
    const value = decodePayload(token)[claim];
    return typeof value === 'string' ? value : null;
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
  readonly email = computed(() => readClaim(this.token(), 'email'));
  readonly userId = computed(() => readClaim(this.token(), 'sub'));

  login(email: string, password: string) {
    return this.http
      .post<LoginResponse>('/api/auth/login', { email, password })
      .pipe(tap((response) => this.useToken(response.accessToken)));
  }

  useToken(accessToken: string): void {
    localStorage.setItem(TOKEN_KEY, accessToken);
    this.token.set(accessToken);
  }

  storedToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    this.token.set(null);
    void this.router.navigate(['/', AppRoutes.login]);
  }
}
