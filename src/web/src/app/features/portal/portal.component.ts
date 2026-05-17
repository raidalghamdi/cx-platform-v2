import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { I18nService } from '../../core/services/i18n.service';
import { ToastService } from '../../core/services/toast.service';
import { CreatePortalRequestRequest, PortalRequestDto, PortalRequestType } from '../../core/models/types';

@Component({
  selector: 'app-portal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <h1 class="title-h1">{{ t('portal.title') }}</h1>
    <p class="muted" style="margin: 0 0 16px">{{ t('portal.subtitle') }}</p>

    <div class="actions">
      <button class="action card" (click)="openNew()">
        <span class="action-eyebrow">{{ t('portal.actions.lodge') }}</span>
        <span class="action-arrow">→</span>
      </button>
      <button class="action card" (click)="scroll('requests')">
        <span class="action-eyebrow">{{ t('portal.actions.track') }}</span>
        <span class="action-arrow">→</span>
      </button>
      <a class="action card" href="javascript:void(0)" (click)="$event.preventDefault()">
        <span class="action-eyebrow">{{ t('portal.actions.kb') }}</span>
        <span class="action-arrow">→</span>
      </a>
    </div>

    <section id="requests" style="margin-top: 24px">
      <h2 class="title-h2" style="margin: 0 0 8px">{{ t('portal.requests') }}</h2>
      <div class="card" style="padding: 0; overflow-x: auto">
        <table class="table">
          <thead>
            <tr>
              <th>{{ t('portal.col.type') }}</th>
              <th>{{ t('portal.col.subject') }}</th>
              <th>{{ t('portal.col.status') }}</th>
              <th>{{ t('portal.col.createdAt') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let r of rows()">
              <td><span class="badge badge--slate">{{ t('portal.type.' + r.type) }}</span></td>
              <td><strong>{{ i18n.pickPair(r.subjectEn, r.subjectAr) }}</strong></td>
              <td><span class="badge" [class]="statusClass(r.status)">{{ r.status }}</span></td>
              <td class="muted">{{ formatDate(r.createdAt) }}</td>
            </tr>
            <tr *ngIf="!rows().length">
              <td colspan="4" style="text-align:center; padding:36px" class="muted">{{ t('portal.empty') }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>

    <div *ngIf="creating()" class="drawer-backdrop" (click)="close()">
      <div class="drawer" (click)="$event.stopPropagation()">
        <div class="row" style="margin-bottom: 14px">
          <span class="badge badge--gold">{{ t('portal.new.title') }}</span>
          <span class="spacer"></span>
          <button class="btn-ghost" (click)="close()">{{ t('common.close') }}</button>
        </div>
        <label class="field">
          <span>{{ t('portal.col.type') }}</span>
          <select class="input" [(ngModel)]="form.type">
            <option value="complaint">{{ t('portal.type.complaint') }}</option>
            <option value="inquiry">{{ t('portal.type.inquiry') }}</option>
            <option value="appointment">{{ t('portal.type.appointment') }}</option>
          </select>
        </label>
        <label class="field" style="margin-top: 10px">
          <span>{{ t('portal.new.subjectEn') }}</span>
          <input class="input" [(ngModel)]="form.subjectEn" />
        </label>
        <label class="field" style="margin-top: 10px">
          <span>{{ t('portal.new.subjectAr') }}</span>
          <input class="input" [(ngModel)]="form.subjectAr" dir="rtl" />
        </label>
        <label class="field" style="margin-top: 10px">
          <span>{{ t('portal.new.bodyEn') }}</span>
          <textarea class="input" [(ngModel)]="form.bodyEn" rows="4"></textarea>
        </label>
        <label class="field" style="margin-top: 10px">
          <span>{{ t('portal.new.bodyAr') }}</span>
          <textarea class="input" [(ngModel)]="form.bodyAr" rows="4" dir="rtl"></textarea>
        </label>
        <div style="margin-top: 14px; display:flex; justify-content:flex-end">
          <button class="btn-primary" (click)="submit()" [disabled]="saving()">
            {{ saving() ? t('common.loading') : t('common.save') }}
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .actions { display: grid; gap: 12px; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); }
    .action {
      display: flex; align-items: center; justify-content: space-between; gap: 8px;
      text-align: start; cursor: pointer; text-decoration: none; color: var(--gac-navy);
      border: none; background: var(--gac-bg-card);
    }
    .action:hover { box-shadow: 0 18px 40px -14px rgba(0,25,43,0.15); }
    .action-eyebrow { font-weight: 600; }
    .action-arrow { color: var(--gac-blue); font-weight: 700; }
    .field { display: flex; flex-direction: column; gap: 4px; }
    .field > span { font-size: 12px; font-weight: 600; color: var(--gac-navy); }
  `],
})
export class PortalComponent {
  private api = inject(ApiService);
  private toast = inject(ToastService);
  i18n = inject(I18nService);

  rows = signal<PortalRequestDto[]>([]);
  creating = signal(false);
  saving = signal(false);
  form: CreatePortalRequestRequest = { type: 'complaint', subjectEn: '', subjectAr: '', bodyEn: '', bodyAr: '' };

  constructor() { this.load(); }

  t(k: string) { return this.i18n.t(k); }

  async load() {
    try { this.rows.set(await this.api.portalMyRequests()); } catch { /* interceptor */ }
  }

  openNew() {
    this.creating.set(true);
    this.form = { type: 'complaint', subjectEn: '', subjectAr: '', bodyEn: '', bodyAr: '' };
  }
  close() { this.creating.set(false); }

  scroll(id: string) {
    document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  async submit() {
    if (!this.form.subjectEn.trim() && !this.form.subjectAr.trim()) return;
    this.saving.set(true);
    try {
      const row = await this.api.portalCreate(this.form);
      this.rows.update((list) => [row, ...list]);
      this.toast.ok(this.t('portal.created'));
      this.creating.set(false);
    } catch { /* interceptor */ } finally { this.saving.set(false); }
  }

  statusClass(s: string) {
    return s === 'resolved' || s === 'closed' ? 'badge--green' : s === 'in_progress' ? 'badge--amber' : 'badge--blue';
  }
  formatDate(iso: string) { try { return new Date(iso).toISOString().slice(0, 10); } catch { return iso; } }
}
