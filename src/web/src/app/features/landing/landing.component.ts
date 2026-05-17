import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { I18nService } from '../../core/services/i18n.service';
import { AuthService } from '../../core/services/auth.service';

// Public landing page — full-bleed hero, no app shell. Users hit this before
// they ever see the login screen; an EN/AR toggle lives in the top right
// and the primary CTA pushes them into the auth flow.
@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="landing">
      <header class="topbar">
        <div class="brand">
          <span class="brand-dot"></span>
          <span class="brand-name">{{ t('brand.short') }}</span>
        </div>
        <div class="spacer"></div>
        <button class="btn-ghost" (click)="i18n.toggle()">{{ t('common.toggleLang') }}</button>
        <button class="btn-primary" (click)="cta()">{{ t('landing.cta') }}</button>
      </header>

      <section class="hero">
        <p class="eyebrow gold">{{ t('landing.eyebrow') }}</p>
        <h1>{{ t('landing.title') }}</h1>
        <p class="hero-sub">{{ t('landing.subtitle') }}</p>
        <div style="margin-top: 22px">
          <button class="btn-primary" (click)="cta()">{{ t('landing.cta') }}</button>
        </div>
      </section>

      <section class="values">
        <article class="value">
          <h3>{{ t('landing.value.1.title') }}</h3>
          <p>{{ t('landing.value.1.body') }}</p>
        </article>
        <article class="value">
          <h3>{{ t('landing.value.2.title') }}</h3>
          <p>{{ t('landing.value.2.body') }}</p>
        </article>
        <article class="value">
          <h3>{{ t('landing.value.3.title') }}</h3>
          <p>{{ t('landing.value.3.body') }}</p>
        </article>
      </section>

      <footer class="footer">
        {{ t('landing.footer') }}
      </footer>
    </div>
  `,
  styles: [`
    .landing { min-height: 100vh; display: flex; flex-direction: column; color: #fff;
      background:
        radial-gradient(circle at 20% 20%, rgba(250,193,38,0.20) 0%, transparent 50%),
        radial-gradient(circle at 80% 70%, rgba(0,105,167,0.45) 0%, transparent 55%),
        linear-gradient(155deg, #00192B 0%, #001A37 45%, #002C4F 100%);
    }
    .topbar { display: flex; align-items: center; gap: 12px; padding: 16px 32px; }
    .brand { display: flex; align-items: center; gap: 10px; }
    .brand-dot { width: 28px; height: 28px; border-radius: 8px; background: var(--gac-gold); box-shadow: 0 0 0 5px rgba(250,193,38,0.18); }
    .brand-name { font-weight: 600; font-size: 14px; }
    .spacer { flex: 1; }
    .btn-ghost { background: rgba(255,255,255,0.08); color: #fff; border: 1px solid rgba(255,255,255,0.18); }
    .btn-ghost:hover { background: rgba(255,255,255,0.16); }
    .hero { flex: 1; display: flex; flex-direction: column; justify-content: center; padding: 40px 8vw; max-width: 1100px; }
    .hero .eyebrow.gold { color: var(--gac-gold); letter-spacing: 0.18em; }
    .hero h1 { font-size: clamp(28px, 5vw, 52px); line-height: 1.1; max-width: 820px; margin: 14px 0 18px; }
    .hero-sub { max-width: 640px; font-size: 16px; opacity: 0.78; line-height: 1.6; }
    .values {
      display: grid; gap: 16px;
      grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
      padding: 30px 8vw 60px;
      max-width: 1100px;
    }
    .value {
      background: rgba(255,255,255,0.06);
      border: 1px solid rgba(255,255,255,0.10);
      border-radius: var(--radius-card);
      padding: 18px 20px;
      backdrop-filter: blur(6px);
    }
    .value h3 { margin: 0 0 6px; color: var(--gac-gold); font-size: 14px; }
    .value p  { margin: 0; opacity: 0.78; font-size: 13px; line-height: 1.5; }
    .footer { padding: 16px 32px; font-size: 11px; opacity: 0.55; border-top: 1px solid rgba(255,255,255,0.08); }
  `],
})
export class LandingComponent {
  i18n = inject(I18nService);
  private auth = inject(AuthService);
  private router = inject(Router);

  t(k: string) { return this.i18n.t(k); }

  cta() {
    // If already signed in, skip login and jump to the user's landing route.
    if (this.auth.isAuthed()) this.router.navigateByUrl(this.auth.user()?.landing ?? '/dashboard');
    else this.router.navigateByUrl('/login');
  }
}
