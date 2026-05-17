import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { LoginResponse, RolePermissionDto, UserDto } from '../models/types';
import { environment } from '../../../environments/environment';

// Tokens live in memory (not localStorage) per PT requirement. The refresh
// token would typically come back in a HttpOnly cookie in production —
// Phase 0 keeps it as a body field so the SPA can demonstrate the flow.

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly _user = signal<UserDto | null>(null);
  private readonly _permissions = signal<RolePermissionDto[]>([]);
  private accessToken: string | null = null;
  private refreshToken: string | null = null;

  readonly user = this._user.asReadonly();
  readonly permissions = this._permissions.asReadonly();
  readonly isAuthed = computed(() => !!this._user());

  constructor(private http: HttpClient, private router: Router) {}

  async login(email: string, password: string): Promise<{ ok: boolean; error?: string }> {
    try {
      const res = await firstValueFrom(
        this.http.post<LoginResponse>(`${environment.apiBase}/auth/login`, { email, password }),
      );
      this.applyLogin(res);
      this.router.navigateByUrl(res.user.landing || '/dashboard');
      return { ok: true };
    } catch (err: any) {
      return { ok: false, error: err?.error?.error ?? 'login failed' };
    }
  }

  async logout(): Promise<void> {
    const token = this.refreshToken;
    this.clear();
    if (token) {
      try {
        await firstValueFrom(
          this.http.post(`${environment.apiBase}/auth/logout`, { refreshToken: token }),
        );
      } catch {
        /* ignore */
      }
    }
    this.router.navigateByUrl('/login');
  }

  getAccessToken(): string | null {
    return this.accessToken;
  }

  allowedPage(pageKey: string): boolean {
    const user = this._user();
    if (!user) return false;
    if (user.role === 'admin') return true;
    return this._permissions().some((p) => p.pageKey === pageKey && p.allowed);
  }

  private applyLogin(res: LoginResponse) {
    this.accessToken = res.accessToken;
    this.refreshToken = res.refreshToken;
    this._user.set(res.user);
    this._permissions.set(res.permissions);
  }

  private clear() {
    this.accessToken = null;
    this.refreshToken = null;
    this._user.set(null);
    this._permissions.set([]);
  }
}
