import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { I18nService } from '../../core/services/i18n.service';
import { ToastService } from '../../core/services/toast.service';
import { ServiceHealthMetricDto, ServiceIncidentDto, SyntheticCheckDto } from '../../core/models/types';

@Component({
  selector: 'app-service-health',
  standalone: true,
  imports: [CommonModule],
  template: `
    <h1 class="title-h1">{{ t('sh.title') }}</h1>
    <p class="muted" style="margin: 0 0 16px">{{ t('sh.subtitle') }}</p>

    <div class="tiles">
      <div class="card tile">
        <div class="eyebrow">{{ t('sh.tile.uptime') }}</div>
        <div class="tile-val">{{ avgUptime() }}<span class="unit">%</span></div>
      </div>
      <div class="card tile">
        <div class="eyebrow">{{ t('sh.tile.p95') }}</div>
        <div class="tile-val">{{ avgP95() }}<span class="unit"> ms</span></div>
      </div>
      <div class="card tile">
        <div class="eyebrow">{{ t('sh.tile.errorRate') }}</div>
        <div class="tile-val">{{ avgErr() }}<span class="unit">%</span></div>
      </div>
      <div class="card tile">
        <div class="eyebrow">{{ t('sh.tile.mttr') }}</div>
        <div class="tile-val">{{ avgMttr() }}<span class="unit"> m</span></div>
      </div>
    </div>

    <section class="card" style="margin-top: 16px">
      <div class="row" style="margin-bottom: 12px">
        <h2 class="title-h2" style="margin: 0">{{ t('sh.checks') }}</h2>
        <span class="spacer"></span>
        <button *ngIf="canEdit" class="btn-primary" (click)="runNow()" [disabled]="running()">
          {{ t('sh.runNow') }}
        </button>
      </div>
      <div style="overflow-x: auto">
        <table class="table">
          <thead>
            <tr>
              <th>{{ t('sh.check.col.name') }}</th>
              <th>{{ t('sh.check.col.endpoint') }}</th>
              <th>{{ t('sh.check.col.lastRun') }}</th>
              <th>{{ t('sh.check.col.latency') }}</th>
              <th>{{ t('sh.check.col.status') }}</th>
              <th>{{ t('sh.check.col.enabled') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let c of checks()">
              <td><strong>{{ c.name }}</strong></td>
              <td><code>{{ c.endpoint }}</code></td>
              <td class="muted">{{ c.lastRunAt ? formatDt(c.lastRunAt) : '—' }}</td>
              <td class="muted">{{ c.lastLatencyMs }} ms</td>
              <td><span class="badge" [class]="c.lastStatus === 'Pass' ? 'badge--green' : 'badge--rose'">{{ t('sh.check.status.' + c.lastStatus) }}</span></td>
              <td>
                <label class="toggle" [class.disabled]="!canEdit">
                  <input type="checkbox" [checked]="c.enabled" [disabled]="!canEdit"
                         (change)="toggleCheck(c, $any($event.target).checked)" />
                  <span></span>
                </label>
              </td>
            </tr>
            <tr *ngIf="!checks().length"><td colspan="6" class="muted" style="text-align:center; padding: 24px">{{ t('common.empty') }}</td></tr>
          </tbody>
        </table>
      </div>
    </section>

    <section class="card" style="margin-top: 16px">
      <h2 class="title-h2">{{ t('sh.incidents') }}</h2>
      <div style="overflow-x: auto">
        <table class="table">
          <thead>
            <tr>
              <th>{{ t('sh.incident.col.opened') }}</th>
              <th>{{ t('sh.incident.col.severity') }}</th>
              <th>{{ t('sh.incident.col.service') }}</th>
              <th>{{ t('sh.incident.col.title') }}</th>
              <th>{{ t('sh.incident.col.status') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let i of incidents()">
              <td class="muted">{{ formatDt(i.openedAt) }}</td>
              <td><span class="badge" [class]="severityClass(i.severity)">{{ t('sh.severity.' + i.severity) }}</span></td>
              <td><code>{{ i.serviceName }}</code></td>
              <td><strong>{{ i18n.pickPair(i.titleEn, i.titleAr) }}</strong></td>
              <td><span class="badge" [class]="incidentClass(i.status)">{{ t('sh.incident.status.' + i.status) }}</span></td>
            </tr>
            <tr *ngIf="!incidents().length"><td colspan="5" class="muted" style="text-align:center; padding: 24px">{{ t('common.empty') }}</td></tr>
          </tbody>
        </table>
      </div>
    </section>

    <section class="card" style="margin-top: 16px">
      <h2 class="title-h2">Recent metrics</h2>
      <div style="overflow-x: auto">
        <table class="table">
          <thead>
            <tr>
              <th>{{ t('sh.col.service') }}</th>
              <th>{{ t('sh.col.measuredAt') }}</th>
              <th>{{ t('sh.col.uptime') }}</th>
              <th>{{ t('sh.col.p95') }}</th>
              <th>{{ t('sh.col.err') }}</th>
              <th>{{ t('sh.col.mttr') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let m of recentMetrics()">
              <td><code>{{ m.serviceName }}</code></td>
              <td class="muted">{{ formatDt(m.measuredAt) }}</td>
              <td>{{ m.uptimePct.toFixed(3) }} %</td>
              <td>{{ m.p95LatencyMs }}</td>
              <td>{{ m.errorRatePct.toFixed(2) }}</td>
              <td>{{ m.mttrMinutes }}</td>
            </tr>
            <tr *ngIf="!metrics().length"><td colspan="6" class="muted" style="text-align:center; padding: 24px">{{ t('common.empty') }}</td></tr>
          </tbody>
        </table>
      </div>
    </section>
  `,
  styles: [`
    .tiles { display: grid; gap: 12px; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); }
    .tile { padding: 16px; }
    .tile-val { margin-top: 6px; font-size: 26px; font-weight: 600; color: var(--gac-navy); font-feature-settings: "tnum"; }
    .tile-val .unit { font-size: 13px; color: var(--gac-muted); font-weight: 400; }
    code { background: rgba(0,0,0,0.05); padding: 1px 6px; border-radius: 6px; font-size: 12px; font-family: ui-monospace, Menlo, monospace; }
    .toggle { position: relative; display: inline-block; width: 36px; height: 20px; }
    .toggle input { opacity: 0; width: 0; height: 0; }
    .toggle span { position: absolute; inset: 0; background: #cbd5e1; border-radius: var(--radius-pill); cursor: pointer; }
    .toggle span::before { content: ""; position: absolute; left: 2px; top: 2px; width: 16px; height: 16px; border-radius: 50%; background: #fff; transition: transform 120ms ease; }
    .toggle input:checked + span { background: var(--gac-blue); }
    .toggle input:checked + span::before { transform: translateX(16px); }
    .toggle.disabled span { opacity: 0.5; cursor: not-allowed; }
  `],
})
export class ServiceHealthComponent {
  private api = inject(ApiService);
  private auth = inject(AuthService);
  private toast = inject(ToastService);
  i18n = inject(I18nService);

  metrics = signal<ServiceHealthMetricDto[]>([]);
  incidents = signal<ServiceIncidentDto[]>([]);
  checks = signal<SyntheticCheckDto[]>([]);
  running = signal(false);

  // Latest one row per service (highest measuredAt) for the table preview.
  recentMetrics = computed(() => {
    const byService = new Map<string, ServiceHealthMetricDto>();
    for (const m of this.metrics()) {
      const prev = byService.get(m.serviceName);
      if (!prev || new Date(m.measuredAt) > new Date(prev.measuredAt)) byService.set(m.serviceName, m);
    }
    return Array.from(byService.values()).sort((a, b) => a.serviceName.localeCompare(b.serviceName));
  });

  avgUptime = computed(() => fmt(avg(this.metrics().map((m) => m.uptimePct)), 3));
  avgP95    = computed(() => Math.round(avg(this.metrics().map((m) => m.p95LatencyMs))));
  avgErr    = computed(() => fmt(avg(this.metrics().map((m) => m.errorRatePct)), 2));
  avgMttr   = computed(() => Math.round(avg(this.metrics().map((m) => m.mttrMinutes))));

  get canEdit() {
    const r = this.auth.user()?.role ?? '';
    return r === 'admin' || r === 'supervisor';
  }

  constructor() { this.load(); }

  t(k: string) { return this.i18n.t(k); }

  async load() {
    try {
      const [m, i, c] = await Promise.all([
        this.api.shMetrics({}), this.api.shIncidents(), this.api.shChecks(),
      ]);
      this.metrics.set(m);
      this.incidents.set(i);
      this.checks.set(c);
    } catch { /* interceptor */ }
  }

  async toggleCheck(c: SyntheticCheckDto, enabled: boolean) {
    if (!this.canEdit) return;
    try {
      const updated = await this.api.shToggleCheck(c.id, enabled);
      this.checks.update((list) => list.map((x) => x.id === c.id ? updated : x));
    } catch { /* interceptor */ }
  }

  async runNow() {
    if (!this.canEdit) return;
    this.running.set(true);
    try {
      await this.api.shRunChecks();
      this.toast.ok(this.t('sh.runQueued'));
      await this.load();
    } catch { /* interceptor */ } finally { this.running.set(false); }
  }

  severityClass(s: string) {
    return s === 'Sev1' ? 'badge--rose' : s === 'Sev2' ? 'badge--rose' : s === 'Sev3' ? 'badge--amber' : 'badge--slate';
  }
  incidentClass(s: string) {
    return s === 'Resolved' ? 'badge--green' : s === 'Mitigating' ? 'badge--amber' : 'badge--rose';
  }
  formatDt(iso: string) {
    try { return new Date(iso).toLocaleString(this.i18n.isRtl ? 'ar-SA' : 'en-GB'); } catch { return iso; }
  }
}

function avg(xs: number[]): number {
  if (!xs.length) return 0;
  let s = 0; for (const x of xs) s += Number(x) || 0;
  return s / xs.length;
}
function fmt(n: number, d: number): string {
  return Number.isFinite(n) ? n.toFixed(d) : '0';
}
