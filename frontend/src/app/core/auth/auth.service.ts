import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthResponse, AuthUser, LoginRequest, RefreshRequest, RegisterRequest, RegisterResponse } from './auth.models';

interface StoredAuthState {
  accessToken: string;
  refreshToken: string;
  user: AuthUser;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly storageKey = 'helpdesk.auth';
  private readonly state = signal<StoredAuthState | null>(this.loadStoredState());

  readonly currentUser = computed(() => this.state()?.user ?? null);
  readonly isAuthenticated = computed(() => this.state() !== null);

  get accessToken(): string | null {
    return this.state()?.accessToken ?? null;
  }

  register(request: RegisterRequest): Observable<RegisterResponse> {
    return this.http.post<RegisterResponse>(`${environment.apiBaseUrl}/auth/register`, request);
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${environment.apiBaseUrl}/auth/login`, request).pipe(
      tap((response) => this.storeAuthResponse(response)),
    );
  }

  refresh(): Observable<AuthResponse> {
    const refreshToken = this.state()?.refreshToken;
    if (!refreshToken) {
      throw new Error('No refresh token is available.');
    }

    const request: RefreshRequest = { refreshToken };
    return this.http.post<AuthResponse>(`${environment.apiBaseUrl}/auth/refresh`, request).pipe(
      tap((response) => this.storeAuthResponse(response)),
    );
  }

  logout(): void {
    this.state.set(null);
    this.removeStoredState();
  }

  private storeAuthResponse(response: AuthResponse): void {
    const nextState: StoredAuthState = {
      accessToken: response.accessToken,
      refreshToken: response.refreshToken,
      user: response.user,
    };

    this.state.set(nextState);
    this.saveStoredState(nextState);
  }

  private loadStoredState(): StoredAuthState | null {
    if (!this.hasStorage()) {
      return null;
    }

    const raw = localStorage.getItem(this.storageKey);
    if (!raw) {
      return null;
    }

    try {
      return JSON.parse(raw) as StoredAuthState;
    } catch {
      localStorage.removeItem(this.storageKey);
      return null;
    }
  }

  private saveStoredState(state: StoredAuthState): void {
    if (this.hasStorage()) {
      localStorage.setItem(this.storageKey, JSON.stringify(state));
    }
  }

  private removeStoredState(): void {
    if (this.hasStorage()) {
      localStorage.removeItem(this.storageKey);
    }
  }

  private hasStorage(): boolean {
    return typeof localStorage !== 'undefined';
  }
}