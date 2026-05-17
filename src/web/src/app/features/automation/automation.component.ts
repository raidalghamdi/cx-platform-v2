import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { I18nService } from '../../core/services/i18n.service';
import { ToastService } from '../../core/services/toast.service';
import { AutomationRuleDto } from '../../core/models/types';

@Component({
  selector: 'app-automation',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <h1 class="title-h1">{{ t('automation.title') }}</h1>
    <p class="muted" style="margin: 0 0 16px">{{ t('automation.subtitle') }}</p>

    <div class="card" style="padding: 0; overflow-x: auto">
      <table class="table">
        <thead>
          <tr>
            <th>{{ t('automation.col.name') }}</th>
            <th>{{ t('automation.col.trigger') }}</th>
            <th>{{ t('automation.col.action') }}</th>
            <th>{{ t('automation.col.lastRun') }}</th>
            <th>{{ t('automation.col.runs') }}</th>
            <th>{{ t('automation.col.enabled') }}</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let r of rows()">
            <td>
              <strong>{{ i18n.pickPair(r.nameEn, r.nameAr) }}</strong>
              <div class="muted" style="font-size: 11px">id #{{ r.id }}</div>
            </td>
            <td><code>{{ r.triggerType }}</code></td>
            <td><span class="badge badge--navy">{{ r.actionType }}</span></td>
            <td class="muted">
              <span *ngIf="r.lastRunAt">{{ formatDt(r.lastRunAt) }} ·
                <span [class]="statusClass(r.lastRunStatus)">{{ t('automation.status.' + r.lastRunStatus) }}</span>
              </span>
              <span *ngIf="!r.lastRunAt">{{ t('automation.status.never') }}</span>
            </td>
            <td class="muted">{{ r.runCount }}</td>
            <td>
              <label class="toggle" [class.disabled]="!canEdit">
                <input type="checkbox" [checked]="r.enabled" [disabled]="!canEdit"
                       (change)="toggle(r, $any($event.target).checked)" />
                <span></span>
              </label>
            </td>
            <td>
              <button class="btn-ghost" *ngIf="canEdit && r.enabled" (click)="run(r)" [disabled]="running() === r.id">
                {{ running() === r.id ? t('common.loading') : t('automation.run') }}
              </button>
            </td>
          </tr>
          <tr *ngIf="!rows().length"><td colspan="7" style="text-align:center; padding:36px" class="muted">{{ t('common.empty') }}</td></tr>
        </tbody>
      </table>
    </div>
  `,
  styles: [`
    code { background: rgba(0,0,0,0.05); padding: 1px 6px; border-radius: 6px; font-size: 12px; font-family: ui-monospace, Menlo, monospace; }
    .status-success { color: #15803D; font-weight: 600; }
    .status-failure { color: #B91C1C; font-weight: 600; }
    .status-never, .status-skipped { color: var(--gac-muted); }
    .toggle { position: relative; display: inline-block; width: 36px; height: 20px; }
    .toggle input { opacity: 0; width: 0; height: 0; }
    .toggle span { position: absolute; inset: 0; background: #cbd5e1; border-radius: var(--radius-pill); transition: background-color 120ms ease; cursor: pointer; }
    .toggle span::before {
      content: ""; position: absolute; left: 2px; top: 2px; width: 16px; height: 16px; border-radius: 50%; background: #fff; transition: transform 120ms ease;
    }
    .toggle input:checked + span { background: var(--gac-blue); }
    .toggle input:checked + span::before { transform: translateX(16px); }
    .toggle.disabled span { opacity: 0.5; cursor: not-allowed; }
  `],
})
export class AutomationComponent {
  private api = inject(ApiService);
  private toast = inject(ToastService);
  private auth = inject(AuthService);
  i18n = inject(I18nService);

  rows = signal<AutomationRuleDto[]>([]);
  running = signal<number | null>(null);

  get canEdit() {
    const r = this.auth.user()?.role ?? '';
    return r === 'admin' || r === 'supervisor';
  }

  constructor() { this.load(); }

  t(k: string) { return this.i18n.t(k); }

  async load() {
    try { this.rows.set(await this.api.automationRules()); } catch { /* interceptor */ }
  }

  async toggle(r: AutomationRuleDto, enabled: boolean) {
    if (!this.canEdit) return;
    try {
      const updated = await this.api.automationToggle(r.id, enabled);
      this.rows.update((list) => list.map((x) => x.id === r.id ? updated : x));
      this.toast.ok(this.t('automation.toggled'));
    } catch { /* interceptor */ }
  }

  async run(r: AutomationRuleDto) {
    if (!this.canEdit) return;
    this.running.set(r.id);
    try {
      const res = await this.api.automationRun(r.id);
      if (res.ok) this.toast.ok(this.t('automation.runOk'), res.note ?? '');
      else        this.toast.err(this.t('automation.runFail'), res.note ?? '');
      await this.load();
    } catch { /* interceptor */ } finally { this.running.set(null); }
  }

  formatDt(iso: string) {
    try { return new Date(iso).toLocaleString(this.i18n.isRtl ? 'ar-SA' : 'en-GB'); } catch { return iso; }
  }

  statusClass(s: string) {
    return 'status-' + (s || 'never');
  }
}
