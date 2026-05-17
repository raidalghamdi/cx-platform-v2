import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/services/auth.service';
import { I18nService } from '../../core/services/i18n.service';
import { environment } from '../../../environments/environment';

interface DemoRow {
  email: string; role: string; nameEn: string; nameAr: string; titleEn: string; titleAr: string;
}

const DEMOS: DemoRow[] = [
  { email: 'admin@cx.gov.sa',      role: 'admin',      nameEn: 'Noor Al Noor',       nameAr: 'نور النور',     titleEn: 'System Administrator',         titleAr: 'مسؤول النظام' },
  { email: 'supervisor@cx.gov.sa', role: 'supervisor', nameEn: 'Fatima Al-Otaibi',   nameAr: 'فاطمة العتيبي', titleEn: 'CX Supervisor',                titleAr: 'مشرفة تجربة المستفيد' },
  { email: 'agent@cx.gov.sa',      role: 'agent',      nameEn: 'Ahmed Al-Harbi',     nameAr: 'أحمد الحربي',   titleEn: 'Service Agent',                titleAr: 'موظف خدمة المستفيدين' },
  { email: 'quality@cx.gov.sa',    role: 'quality',    nameEn: 'Layla Al-Qahtani',   nameAr: 'ليلى القحطاني', titleEn: 'Quality Officer',              titleAr: 'مسؤولة الجودة' },
  { email: 'customer@cx.gov.sa',   role: 'customer',   nameEn: 'Khalid Al-Mutairi',  nameAr: 'خالد المطيري',  titleEn: 'Citizen',                      titleAr: 'مواطن' },
  { email: 'executive@cx.gov.sa',  role: 'executive',  nameEn: 'Raid Al-Ghamdi',     nameAr: 'رائد الغامدي',  titleEn: 'Chief of Strategy & Excellence', titleAr: 'رئيس الاستراتيجية والتميز' },
];

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="login">
      <aside class="hero">
        <button class="lang-toggle" (click)="i18n.toggle()">{{ t('common.toggleLang') }}</button>
        <div class="brand-mark">
          <span class="brand-dot"></span>
          <div>
            <div class="brand-name">{{ t('brand.name') }}</div>
            <div class="brand-tagline">{{ t('brand.tagline') }}</div>
          </div>
        </div>
        <div class="hero-text">
          <h1>{{ t('brand.name') }}</h1>
          <p>{{ t('brand.tagline') }}</p>
        </div>
        <div class="hero-foot eyebrow">{{ t('brand.poweredBy') }}</div>
      </aside>
      <section class="panel">
        <div class="login-card">
          <p class="eyebrow">{{ t('common.signIn') }}</p>
          <h2 class="title-h1">{{ t('login.title') }}</h2>
          <p class="muted" style="margin: 4px 0 18px">{{ t('login.subtitle') }}</p>
          <form (ngSubmit)="onSubmit()" class="col" autocomplete="off">
            <label class="field">
              <span>{{ t('common.email') }}</span>
              <input class="input" name="email" type="email" [(ngModel)]="email" required autocomplete="username" placeholder="name@cx.gov.sa" />
            </label>
            <label class="field">
              <span>{{ t('common.password') }}</span>
              <input class="input" name="password" type="password" [(ngModel)]="password" required autocomplete="current-password" />
            </label>
            <p *ngIf="error()" class="error">{{ error() }}</p>
            <button class="btn-primary" type="submit" [disabled]="busy()">
              {{ busy() ? t('common.loading') : t('common.signIn') }}
            </button>
          </form>

          <section *ngIf="showDemos" class="demos">
            <div style="display:flex; align-items:center; justify-content:space-between">
              <div>
                <p class="eyebrow">Demo Access</p>
                <h3 class="title-h3">{{ t('login.demoTitle') }}</h3>
                <p class="muted" style="font-size:12px">{{ t('login.demoHint') }}</p>
              </div>
            </div>
            <ul>
              <li *ngFor="let d of demos">
                <button class="demo-row" (click)="fill(d)">
                  <span class="demo-initials">{{ initials(d.nameEn) }}</span>
                  <span class="demo-meta">
                    <span class="demo-name">{{ t('role.' + d.role) }} · <span class="muted">{{ i18n.pickPair(d.titleEn, d.titleAr) }}</span></span>
                    <span class="demo-email" dir="ltr">{{ d.email }}</span>
                  </span>
                </button>
              </li>
            </ul>
            <p class="muted" style="font-size:11px; margin-top:8px">{{ t('login.demoFooter') }}</p>
          </section>
        </div>
      </section>
    </div>
  `,
  styles: [`
    .login { display: grid; grid-template-columns: minmax(0,5fr) minmax(0,7fr); min-height: 100vh; }
    @media (max-width: 900px) { .login { grid-template-columns: 1fr; } .hero { display: none; } }
    .hero {
      position: relative; padding: 48px; color: #fff;
      background:
        radial-gradient(circle at 20% 30%, rgba(250,193,38,0.18) 0%, transparent 50%),
        linear-gradient(155deg, #00192B 0%, #001A37 45%, #002C4F 100%);
      display: flex; flex-direction: column; justify-content: space-between;
    }
    .lang-toggle {
      position: absolute; top: 18px; inset-inline-end: 18px;
      background: rgba(255,255,255,0.1); color: #fff;
      border: 1px solid rgba(255,255,255,0.2);
      border-radius: var(--radius-pill); padding: 6px 14px; cursor: pointer; font-size: 12px;
    }
    .brand-mark { display: flex; align-items: center; gap: 12px; }
    .brand-dot { width: 36px; height: 36px; border-radius: 10px; background: var(--gac-gold); box-shadow: 0 0 0 6px rgba(250,193,38,0.18); }
    .brand-name { font-weight: 600; font-size: 16px; }
    .brand-tagline { opacity: 0.7; font-size: 12px; }
    .hero-text h1 { font-size: 28px; line-height: 1.2; max-width: 380px; }
    .hero-text p { opacity: 0.8; max-width: 420px; }
    .hero-foot { opacity: 0.55; }
    .panel { display: flex; align-items: flex-start; justify-content: center; padding: 56px 24px; }
    .login-card { width: 100%; max-width: 440px; background: #fff; border-radius: var(--radius-card); padding: 28px; box-shadow: var(--shadow-card); }
    .field { display: flex; flex-direction: column; gap: 6px; }
    .field > span { font-size: 12px; font-weight: 600; color: var(--gac-navy); }
    .error { color: #B91C1C; font-size: 12px; margin: 0; }
    .demos { margin-top: 22px; padding-top: 18px; border-top: 1px solid var(--gac-border); }
    .demos ul { margin: 12px 0 0; padding: 0; list-style: none; display: flex; flex-direction: column; gap: 4px; }
    .demo-row {
      display: flex; align-items: center; gap: 10px;
      width: 100%; padding: 8px 10px; border: none; background: transparent;
      cursor: pointer; border-radius: 8px; text-align: start;
    }
    .demo-row:hover { background: rgba(0,105,167,0.05); }
    .demo-initials {
      width: 32px; height: 32px; border-radius: 50%;
      background: var(--gac-blue); color: #fff; font-size: 11px; font-weight: 700;
      display: flex; align-items: center; justify-content: center; flex-shrink: 0;
    }
    .demo-meta { display: flex; flex-direction: column; min-width: 0; flex: 1; }
    .demo-name { font-size: 12px; font-weight: 600; color: var(--gac-navy); }
    .demo-email { font-size: 11px; color: var(--gac-muted); }
  `],
})
export class LoginComponent {
  email = '';
  password = '';
  error = signal<string | null>(null);
  busy = signal(false);
  demos = DEMOS;
  showDemos = environment.showDemoAccounts;

  constructor(public auth: AuthService, public i18n: I18nService) {}

  t(k: string) { return this.i18n.t(k); }

  fill(d: DemoRow) {
    this.email = d.email;
    this.password = 'demo';
    this.error.set(null);
  }

  initials(name: string) {
    return name.split(' ').map((p) => p[0]).filter(Boolean).slice(0, 2).join('').toUpperCase();
  }

  async onSubmit() {
    this.busy.set(true);
    this.error.set(null);
    const res = await this.auth.login(this.email, this.password);
    this.busy.set(false);
    if (!res.ok) this.error.set(this.t('login.invalid'));
  }
}
