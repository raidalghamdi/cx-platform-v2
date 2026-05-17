import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { I18nService } from '../../core/services/i18n.service';
import { ToastService } from '../../core/services/toast.service';
import { AboutSectionDto, UpdateAboutSectionRequest } from '../../core/models/types';

@Component({
  selector: 'app-about',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <h1 class="title-h1">{{ t('about.title') }}</h1>
    <p class="muted" style="margin: 0 0 16px">{{ t('about.subtitle') }}</p>

    <div class="col">
      <article *ngFor="let s of sections()" class="card">
        <div class="row" style="gap: 12px; margin-bottom: 8px">
          <h2 class="title-h2" style="margin: 0">{{ i18n.pickPair(s.keyEn, s.keyAr) }}</h2>
          <span class="spacer"></span>
          <button *ngIf="canEdit" class="btn-ghost" (click)="open(s)">{{ t('about.edit') }}</button>
        </div>
        <p style="white-space: pre-wrap; margin: 0">{{ i18n.pickPair(s.bodyEn, s.bodyAr) }}</p>
      </article>
      <p *ngIf="!sections().length" class="muted">{{ t('common.loading') }}</p>
    </div>

    <div *ngIf="editing() as e" class="drawer-backdrop" (click)="close()">
      <div class="drawer" (click)="$event.stopPropagation()">
        <div class="row" style="margin-bottom: 14px">
          <span class="badge badge--gold">{{ t('about.edit') }}</span>
          <span class="spacer"></span>
          <button class="btn-ghost" (click)="close()">{{ t('common.close') }}</button>
        </div>
        <label class="field">
          <span>{{ t('about.field.keyEn') }}</span>
          <input class="input" [(ngModel)]="form.keyEn" />
        </label>
        <label class="field" style="margin-top: 10px">
          <span>{{ t('about.field.keyAr') }}</span>
          <input class="input" [(ngModel)]="form.keyAr" dir="rtl" />
        </label>
        <label class="field" style="margin-top: 10px">
          <span>{{ t('about.field.bodyEn') }}</span>
          <textarea class="input" [(ngModel)]="form.bodyEn" rows="6"></textarea>
        </label>
        <label class="field" style="margin-top: 10px">
          <span>{{ t('about.field.bodyAr') }}</span>
          <textarea class="input" [(ngModel)]="form.bodyAr" rows="6" dir="rtl"></textarea>
        </label>
        <label class="field" style="margin-top: 10px">
          <span>{{ t('about.field.order') }}</span>
          <input class="input" type="number" [(ngModel)]="form.orderIndex" />
        </label>
        <div style="margin-top: 14px; display:flex; justify-content:flex-end">
          <button class="btn-primary" (click)="save()" [disabled]="saving()">
            {{ saving() ? t('common.loading') : t('common.save') }}
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .field { display: flex; flex-direction: column; gap: 4px; }
    .field > span { font-size: 12px; font-weight: 600; color: var(--gac-navy); }
  `],
})
export class AboutComponent {
  private api = inject(ApiService);
  private auth = inject(AuthService);
  private toast = inject(ToastService);
  i18n = inject(I18nService);

  sections = signal<AboutSectionDto[]>([]);
  editing = signal<AboutSectionDto | null>(null);
  saving = signal(false);
  form: UpdateAboutSectionRequest = { keyEn: '', keyAr: '', bodyEn: '', bodyAr: '', orderIndex: 0 };

  get canEdit() { return this.auth.user()?.role === 'admin'; }

  constructor() { this.load(); }

  t(k: string) { return this.i18n.t(k); }

  async load() {
    try { this.sections.set(await this.api.aboutList()); } catch { /* interceptor */ }
  }

  open(s: AboutSectionDto) {
    if (!this.canEdit) return;
    this.editing.set(s);
    this.form = { keyEn: s.keyEn, keyAr: s.keyAr, bodyEn: s.bodyEn, bodyAr: s.bodyAr, orderIndex: s.orderIndex };
  }
  close() { this.editing.set(null); }

  async save() {
    const e = this.editing();
    if (!e || !this.canEdit) return;
    this.saving.set(true);
    try {
      const updated = await this.api.aboutUpdate(e.id, this.form);
      this.sections.update((list) => list.map((s) => s.id === e.id ? updated : s).sort((a, b) => a.orderIndex - b.orderIndex));
      this.toast.ok(this.t('about.saved'));
      this.editing.set(null);
    } catch { /* interceptor */ } finally { this.saving.set(false); }
  }
}
