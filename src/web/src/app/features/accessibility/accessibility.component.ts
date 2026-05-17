import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { I18nService } from '../../core/services/i18n.service';
import { ToastService } from '../../core/services/toast.service';
import {
  AccessibilityAuditDto, AccessibilityRemediationDto, AccessibilityItemStatus,
} from '../../core/models/types';

@Component({
  selector: 'app-accessibility',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <h1 class="title-h1">{{ t('a11y.title') }}</h1>
    <p class="muted" style="margin: 0 0 16px">{{ t('a11y.subtitle') }}</p>

    <div class="card" style="background: linear-gradient(135deg, #00192B 0%, #002C4F 100%); color: #fff; margin-bottom: 16px">
      <div class="row" style="gap: 12px; align-items: flex-start">
        <span class="badge badge--gold" style="margin-top: 4px">WCAG 2.2 AA</span>
        <div>
          <h2 class="title-h2" style="color: #fff; margin: 0 0 6px">{{ t('a11y.statement.title') }}</h2>
          <p style="margin: 0; opacity: 0.85; font-size: 13px; line-height: 1.55">{{ t('a11y.statement.body') }}</p>
        </div>
      </div>
    </div>

    <div class="grid">
      <section class="card audits">
        <h2 class="title-h2">{{ t('a11y.audits') }}</h2>
        <div style="overflow-x: auto">
          <table class="table">
            <thead>
              <tr>
                <th>{{ t('a11y.col.date') }}</th>
                <th>{{ t('a11y.col.auditor') }}</th>
                <th>{{ t('a11y.col.scope') }}</th>
                <th>{{ t('a11y.col.level') }}</th>
                <th>{{ t('a11y.col.total') }}</th>
                <th>{{ t('a11y.col.open') }}</th>
                <th>{{ t('a11y.col.report') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let a of audits()" [class.row--selected]="selectedId() === a.id" (click)="select(a.id)">
                <td>{{ formatDate(a.auditDate) }}</td>
                <td>{{ a.auditor }}</td>
                <td class="muted">{{ a.scopePages.join(' · ') }}</td>
                <td><span class="badge badge--navy">{{ a.wcagLevel }}</span></td>
                <td class="muted">{{ a.totalIssues }}</td>
                <td><span class="badge" [class]="a.openIssues > 0 ? 'badge--amber' : 'badge--green'">{{ a.openIssues }}</span></td>
                <td><a *ngIf="a.reportUrl" [href]="a.reportUrl" target="_blank" rel="noopener" (click)="$event.stopPropagation()">PDF →</a></td>
              </tr>
              <tr *ngIf="!audits().length"><td colspan="7" class="muted" style="text-align:center; padding: 24px">{{ t('common.empty') }}</td></tr>
            </tbody>
          </table>
        </div>
      </section>

      <section class="card accommodate">
        <h3 class="title-h3">{{ t('a11y.accommodate.title') }}</h3>
        <p style="font-size: 13px; line-height: 1.55; margin: 4px 0 0">{{ t('a11y.accommodate.body') }}</p>
      </section>
    </div>

    <section class="card" style="margin-top: 16px">
      <h2 class="title-h2">{{ t('a11y.remediation.title') }}</h2>
      <p *ngIf="!selectedId()" class="muted">{{ t('a11y.remediation.empty') }}</p>
      <div *ngIf="selectedId()" style="overflow-x: auto">
        <table class="table">
          <thead>
            <tr>
              <th>{{ t('a11y.rem.col.criterion') }}</th>
              <th>{{ t('a11y.rem.col.severity') }}</th>
              <th>{{ t('a11y.rem.col.description') }}</th>
              <th>{{ t('a11y.rem.col.owner') }}</th>
              <th>{{ t('a11y.rem.col.status') }}</th>
              <th>{{ t('a11y.rem.col.target') }}</th>
              <th *ngIf="canEdit"></th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let r of items()">
              <td><code>{{ r.wcagCriterion }}</code></td>
              <td><span class="badge" [class]="severityClass(r.severity)">{{ t('a11y.severity.' + r.severity) }}</span></td>
              <td>{{ i18n.pickPair(r.descriptionEn, r.descriptionAr) }}</td>
              <td class="muted">{{ r.owner }}</td>
              <td><span class="badge" [class]="statusClass(r.status)">{{ t('a11y.status.' + r.status) }}</span></td>
              <td class="muted">{{ r.targetDate ? formatDate(r.targetDate) : '—' }}</td>
              <td *ngIf="canEdit" style="white-space: nowrap">
                <button *ngIf="r.status !== 'Resolved'" class="btn-ghost" (click)="updateStatus(r, 'Resolved')">{{ t('a11y.markResolved') }}</button>
                <button *ngIf="r.status === 'Open'" class="btn-ghost" (click)="updateStatus(r, 'InProgress')">{{ t('a11y.markInProgress') }}</button>
              </td>
            </tr>
            <tr *ngIf="!items().length"><td colspan="7" class="muted" style="text-align:center; padding: 24px">{{ t('common.empty') }}</td></tr>
          </tbody>
        </table>
      </div>
    </section>
  `,
  styles: [`
    .grid { display: grid; grid-template-columns: 2fr 1fr; gap: 16px; }
    @media (max-width: 900px) { .grid { grid-template-columns: 1fr; } }
    .audits { padding: 16px; }
    .accommodate { padding: 16px; background: rgba(250,193,38,0.10); border: 1px solid rgba(250,193,38,0.4); }
    .row--selected { background: rgba(0,105,167,0.08) !important; }
    code { background: rgba(0,0,0,0.05); padding: 1px 6px; border-radius: 6px; font-size: 12px; font-family: ui-monospace, Menlo, monospace; }
  `],
})
export class AccessibilityComponent {
  private api = inject(ApiService);
  private auth = inject(AuthService);
  private toast = inject(ToastService);
  i18n = inject(I18nService);

  audits = signal<AccessibilityAuditDto[]>([]);
  selectedId = signal<number | null>(null);
  items = signal<AccessibilityRemediationDto[]>([]);

  get canEdit() {
    const r = this.auth.user()?.role ?? '';
    return r === 'admin' || r === 'supervisor' || r === 'quality';
  }

  constructor() { this.load(); }

  t(k: string) { return this.i18n.t(k); }

  async load() {
    try {
      const a = await this.api.a11yAudits();
      this.audits.set(a);
      if (a.length && !this.selectedId()) await this.select(a[0].id);
    } catch { /* interceptor */ }
  }

  async select(id: number) {
    this.selectedId.set(id);
    try {
      const detail = await this.api.a11yAudit(id);
      this.items.set(detail.items);
    } catch { /* interceptor */ }
  }

  async updateStatus(r: AccessibilityRemediationDto, status: AccessibilityItemStatus) {
    if (!this.canEdit) return;
    try {
      const updated = await this.api.a11yUpdateRemediation(r.id, {
        status, targetDate: r.targetDate ?? null, owner: r.owner,
      });
      this.items.update((list) => list.map((x) => x.id === r.id ? updated : x));
      this.toast.ok(this.t('admin.saved'));
      // Refresh open-issue counts on the audits table.
      const refreshed = await this.api.a11yAudits();
      this.audits.set(refreshed);
    } catch { /* interceptor */ }
  }

  severityClass(s: string) {
    return s === 'Critical' ? 'badge--rose' : s === 'High' ? 'badge--rose' : s === 'Medium' ? 'badge--amber' : 'badge--slate';
  }
  statusClass(s: string) {
    return s === 'Resolved' ? 'badge--green' : s === 'InProgress' ? 'badge--blue' : s === 'Deferred' ? 'badge--slate' : 'badge--amber';
  }
  formatDate(iso: string) { try { return new Date(iso).toISOString().slice(0, 10); } catch { return iso; } }
}
