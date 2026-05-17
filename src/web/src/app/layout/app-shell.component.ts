import { Component, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/services/auth.service';
import { I18nService } from '../core/services/i18n.service';
import { ToastService } from '../core/services/toast.service';

interface NavItem { href: string; key: string; pageKey: string; }

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="shell">
      <aside class="sidebar">
        <div class="brand">
          <span class="brand-dot"></span>
          <div>
            <div class="brand-name">{{ t('brand.short') }}</div>
            <div class="eyebrow">Government</div>
          </div>
        </div>
        <nav>
          <ng-container *ngFor="let n of visibleNav()">
            <a [routerLink]="n.href" routerLinkActive="nav-item--active" class="nav-item">
              <span class="nav-dot"></span>
              <span>{{ t(n.key) }}</span>
            </a>
          </ng-container>
        </nav>
        <div class="sidebar-foot eyebrow">{{ t('brand.poweredBy') }}</div>
      </aside>

      <main>
        <header class="topbar">
          <div class="spacer"></div>
          <button class="btn-ghost" (click)="toggleLang()">{{ t('common.toggleLang') }}</button>
          <div class="user-chip">
            <div class="avatar">{{ initials() }}</div>
            <div class="user-meta">
              <div class="user-name">{{ userName() }}</div>
              <div class="eyebrow">{{ userRoleLabel() }}</div>
            </div>
            <button class="btn-ghost" (click)="logout()">{{ t('nav.logout') }}</button>
          </div>
        </header>
        <section class="page">
          <router-outlet></router-outlet>
        </section>
      </main>
    </div>
  `,
  styles: [`
    .shell { display: grid; grid-template-columns: 240px 1fr; min-height: 100vh; }
    [dir="rtl"] .shell { grid-template-columns: 1fr 240px; }
    .sidebar {
      background: #fff;
      border-inline-end: 1px solid var(--gac-border);
      display: flex; flex-direction: column;
      padding: 18px 12px;
      gap: 8px;
    }
    .brand { display: flex; align-items: center; gap: 10px; padding: 4px 10px 18px; border-bottom: 1px solid var(--gac-border); margin-bottom: 8px; }
    .brand-dot { width: 28px; height: 28px; border-radius: 8px; background: var(--gac-gold); box-shadow: 0 0 0 4px rgba(250,193,38,0.18); }
    .brand-name { font-weight: 600; color: var(--gac-navy); font-size: 14px; }
    nav { display: flex; flex-direction: column; gap: 2px; padding: 0 2px; }
    .nav-item {
      display: flex; align-items: center; gap: 10px;
      padding: 9px 12px; border-radius: 8px;
      color: var(--gac-ink); font-weight: 500; font-size: 13px;
      text-decoration: none;
    }
    .nav-item:hover { background: rgba(0, 105, 167, 0.08); text-decoration: none; }
    .nav-item--active { background: var(--gac-blue); color: #fff; }
    .nav-item--active .nav-dot { background: var(--gac-gold); }
    .nav-dot { width: 8px; height: 8px; border-radius: 50%; background: var(--gac-muted); flex: 0 0 8px; }
    .sidebar-foot { margin-top: auto; padding: 12px 10px; }
    .topbar {
      height: 60px; display: flex; align-items: center; gap: 12px;
      padding: 0 24px;
      background: #fff; border-bottom: 1px solid var(--gac-border);
    }
    .topbar :focus-visible { box-shadow: var(--focus-ring); }
    .user-chip { display: flex; align-items: center; gap: 10px; }
    .avatar {
      width: 32px; height: 32px; border-radius: 50%;
      background: var(--gac-blue); color: #fff;
      font-size: 12px; font-weight: 700;
      display: flex; align-items: center; justify-content: center;
    }
    .user-meta { line-height: 1.1; }
    .user-name { font-size: 12.5px; font-weight: 600; color: var(--gac-navy); }
    main { display: flex; flex-direction: column; min-width: 0; }
    .page { padding: 24px; flex: 1; }
  `],
})
export class AppShellComponent {
  private auth = inject(AuthService);
  private i18n = inject(I18nService);
  private router = inject(Router);
  toasts = inject(ToastService);

  private readonly NAV: NavItem[] = [
    { href: '/dashboard',    key: 'nav.dashboard',    pageKey: '/dashboard' },
    { href: '/complaints',   key: 'nav.complaints',   pageKey: '/complaints' },
    { href: '/inbox',        key: 'nav.inbox',        pageKey: '/inbox' },
    // Phase 1
    { href: '/journeys',     key: 'nav.journeys',     pageKey: '/journeys' },
    { href: '/voc',          key: 'nav.voc',          pageKey: '/voc' },
    { href: '/kb',           key: 'nav.kb',           pageKey: '/kb' },
    { href: '/programme',    key: 'nav.programme',    pageKey: '/programme' },
    { href: '/governance',   key: 'nav.governance',   pageKey: '/governance' },
    // Phase 2 — landing is public, not in nav
    { href: '/portal',       key: 'nav.portal',       pageKey: '/portal' },
    { href: '/copilot',      key: 'nav.copilot',      pageKey: '/copilot' },
    { href: '/architecture', key: 'nav.architecture', pageKey: '/architecture' },
    { href: '/about',        key: 'nav.about',        pageKey: '/about' },
    { href: '/audit',        key: 'nav.audit',        pageKey: '/audit' },
    { href: '/automation',   key: 'nav.automation',   pageKey: '/automation' },
    { href: '/admin',        key: 'nav.admin',        pageKey: '/admin' },
  ];

  visibleNav = computed(() => this.NAV.filter((n) => this.auth.allowedPage(n.pageKey)));
  userName = computed(() => this.i18n.pickPair(this.auth.user()?.nameEn, this.auth.user()?.nameAr));
  userRoleLabel = computed(() => this.i18n.t('role.' + (this.auth.user()?.role ?? 'customer')));
  initials = computed(() => {
    const en = this.auth.user()?.nameEn ?? '';
    return en.split(' ').map((p) => p[0]).filter(Boolean).slice(0, 2).join('').toUpperCase();
  });

  t(key: string) { return this.i18n.t(key); }
  toggleLang() { this.i18n.toggle(); }
  async logout() { await this.auth.logout(); }
}
