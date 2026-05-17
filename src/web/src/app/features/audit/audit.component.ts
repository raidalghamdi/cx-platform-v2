import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { I18nService } from '../../core/services/i18n.service';
import { ToastService } from '../../core/services/toast.service';
import { AuditEventDto, AuditVerifyResultDto } from '../../core/models/types';

@Component({
  selector: 'app-audit',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <h1 class="title-h1">{{ t('audit.title') }}</h1>
    <p class="muted" style="margin: 0 0 16px">{{ t('audit.subtitle') }}</p>

    <div class="row row--wrap" style="gap: 8px; margin-bottom: 16px">
      <input class="input" style="max-width: 220px" [(ngModel)]="kind" (ngModelChange)="reload()" [placeholder]="t('audit.filter.kind')" />
      <input class="input" style="max-width: 140px" [(ngModel)]="userId" (ngModelChange)="reload()" [placeholder]="t('audit.filter.user')" type="number" />
      <input class="input" style="max-width: 180px" [(ngModel)]="fromDt" (ngModelChange)="reload()" type="date" />
      <input class="input" style="max-width: 180px" [(ngModel)]="toDt"   (ngModelChange)="reload()" type="date" />
      <span class="spacer"></span>
      <button class="btn-primary" (click)="verify()" [disabled]="verifying()">
        {{ verifying() ? t('audit.verifying') : t('audit.verify') }}
      </button>
    </div>

    <div *ngIf="lastVerify() as v" class="verify" [class.verify--ok]="v.ok" [class.verify--broken]="!v.ok" style="margin-bottom: 12px">
      <strong>{{ v.ok ? t('audit.verify.ok') : t('audit.verify.broken') }}</strong>
      — {{ v.total }} {{ t('audit.verify.detail') }}
      <span *ngIf="!v.ok">· first broken row #{{ v.firstBrokenIndex }} (id {{ v.firstBrokenId }})</span>
    </div>

    <div class="card" style="padding: 0; overflow-x: auto">
      <table class="table">
        <thead>
          <tr>
            <th>{{ t('audit.col.id') }}</th>
            <th>{{ t('audit.col.at') }}</th>
            <th>{{ t('audit.col.kind') }}</th>
            <th>{{ t('audit.col.actor') }}</th>
            <th>{{ t('audit.col.target') }}</th>
            <th>{{ t('audit.col.hash') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let a of rows()" (click)="open(a)">
            <td class="muted">{{ a.id }}</td>
            <td class="muted">{{ formatDt(a.at) }}</td>
            <td><code>{{ a.kind }}</code></td>
            <td class="muted">{{ a.actorUserId ?? '—' }}</td>
            <td class="muted">{{ a.targetKind }}{{ a.targetId ? ' #' + a.targetId : '' }}</td>
            <td class="muted hash">{{ a.entryHash.slice(0, 12) }}…</td>
          </tr>
          <tr *ngIf="!rows().length"><td colspan="6" style="text-align:center; padding:36px" class="muted">{{ t('audit.empty') }}</td></tr>
        </tbody>
      </table>
    </div>

    <div *ngIf="active() as a" class="drawer-backdrop" (click)="close()">
      <div class="drawer" (click)="$event.stopPropagation()">
        <div class="row" style="margin-bottom: 14px">
          <code>{{ a.kind }}</code>
          <span class="spacer"></span>
          <button class="btn-ghost" (click)="close()">{{ t('common.close') }}</button>
        </div>
        <p class="muted" style="font-size: 12px; margin: 0">
          {{ formatDt(a.at) }} · actor {{ a.actorUserId ?? '—' }} · target {{ a.targetKind }}{{ a.targetId ? ' #' + a.targetId : '' }}
        </p>
        <p class="eyebrow" style="margin-top: 14px">prev hash</p>
        <code class="block">{{ a.prevHash }}</code>
        <p class="eyebrow" style="margin-top: 10px">entry hash</p>
        <code class="block">{{ a.entryHash }}</code>
        <p class="eyebrow" style="margin-top: 10px">{{ t('audit.payload') }}</p>
        <pre class="block">{{ pretty(a.payloadJson) }}</pre>
      </div>
    </div>
  `,
  styles: [`
    code { background: rgba(0,0,0,0.05); padding: 1px 6px; border-radius: 6px; font-size: 12px; font-family: ui-monospace, Menlo, monospace; }
    .hash { font-family: ui-monospace, Menlo, monospace; }
    .verify { padding: 10px 14px; border-radius: var(--radius-card); font-size: 13px; }
    .verify--ok { background: #DCFCE7; color: #14532D; border-inline-start: 4px solid #15803D; }
    .verify--broken { background: #FEE2E2; color: #991B1B; border-inline-start: 4px solid #B91C1C; }
    .block { display: block; background: rgba(0,0,0,0.04); padding: 8px 10px; border-radius: 8px; word-break: break-all; white-space: pre-wrap; }
    pre.block { font-family: ui-monospace, Menlo, monospace; font-size: 11.5px; }
  `],
})
export class AuditComponent {
  private api = inject(ApiService);
  private toast = inject(ToastService);
  i18n = inject(I18nService);

  rows = signal<AuditEventDto[]>([]);
  active = signal<AuditEventDto | null>(null);
  verifying = signal(false);
  lastVerify = signal<AuditVerifyResultDto | null>(null);

  kind = '';
  userId: number | null = null;
  fromDt = '';
  toDt = '';

  constructor() { this.reload(); }

  t(k: string) { return this.i18n.t(k); }

  async reload() {
    try {
      const res = await this.api.auditEvents({
        kind: this.kind || undefined,
        userId: this.userId ?? undefined,
        from: this.fromDt ? new Date(this.fromDt).toISOString() : undefined,
        to:   this.toDt   ? new Date(this.toDt).toISOString()   : undefined,
        pageSize: 50,
      });
      this.rows.set(res.items);
    } catch { /* interceptor */ }
  }

  async verify() {
    this.verifying.set(true);
    try {
      const v = await this.api.auditVerify();
      this.lastVerify.set(v);
      if (v.ok) this.toast.ok(this.t('audit.verify.ok'));
      else      this.toast.err(this.t('audit.verify.broken'));
    } catch { /* interceptor */ } finally { this.verifying.set(false); }
  }

  open(a: AuditEventDto) { this.active.set(a); }
  close() { this.active.set(null); }

  formatDt(iso: string) {
    try { return new Date(iso).toLocaleString(this.i18n.isRtl ? 'ar-SA' : 'en-GB'); } catch { return iso; }
  }
  pretty(json: string) {
    try { return JSON.stringify(JSON.parse(json), null, 2); } catch { return json; }
  }
}
