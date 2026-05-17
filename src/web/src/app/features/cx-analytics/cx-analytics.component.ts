import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { I18nService } from '../../core/services/i18n.service';
import { CxAnalyticsSnapshotDto, CxAnalyticsTrendDto, CxSegment, RootCauseLinkDto } from '../../core/models/types';

type Metric = 'csat' | 'nps' | 'ces';

@Component({
  selector: 'app-cx-analytics',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <h1 class="title-h1">{{ t('cxa.title') }}</h1>
    <p class="muted" style="margin: 0 0 16px">{{ t('cxa.subtitle') }}</p>

    <div class="row row--wrap" style="gap: 12px; margin-bottom: 12px; align-items: center">
      <strong style="font-size: 12px; color: var(--gac-navy)">{{ t('cxa.segment') }}:</strong>
      <button *ngFor="let s of segments" class="pill" [class.pill--active]="segment() === s" (click)="setSegment(s)">
        {{ t('cxa.segment.' + s) }}
      </button>
      <span class="spacer"></span>
      <strong style="font-size: 12px; color: var(--gac-navy)">{{ t('cxa.range') }}:</strong>
      <button *ngFor="let d of ranges" class="pill" [class.pill--active]="days() === d" (click)="setDays(d)">
        {{ t('cxa.range.' + d) }}
      </button>
    </div>

    <section class="card chart">
      <div class="row" style="margin-bottom: 8px">
        <strong>{{ t('cxa.segment.' + segment()) }} · {{ days() }}d</strong>
        <span class="spacer"></span>
        <span class="legend"><span class="dot" style="background:#0069A7"></span>{{ t('cxa.metric.csat') }}</span>
        <span class="legend"><span class="dot" style="background:#FAC126"></span>{{ t('cxa.metric.nps') }}</span>
        <span class="legend"><span class="dot" style="background:#5F9600"></span>{{ t('cxa.metric.ces') }}</span>
      </div>
      <p *ngIf="!points().length" class="muted" style="text-align: center; padding: 32px">{{ t('cxa.empty') }}</p>

      <svg *ngIf="points().length" viewBox="0 0 760 220" xmlns="http://www.w3.org/2000/svg" class="trend-chart" role="img" [attr.aria-label]="t('cxa.title')">
        <!-- Y axis ticks at 0/25/50/75/100 (CSAT is 0-100; NPS 0-100; CES scaled to 0-100). -->
        <line x1="40" y1="190" x2="740" y2="190" stroke="#cbd5e1" stroke-width="1"/>
        <line x1="40" y1="10"  x2="40"  y2="190" stroke="#cbd5e1" stroke-width="1"/>
        <text *ngFor="let v of [0,25,50,75,100]" x="32" [attr.y]="ty(v) + 4" fill="#64748b" font-size="10" text-anchor="end">{{ v }}</text>
        <line *ngFor="let v of [25,50,75]" x1="40" [attr.y1]="ty(v)" x2="740" [attr.y2]="ty(v)" stroke="#e2e8f0" stroke-width="1" stroke-dasharray="3 3"/>

        <!-- CSAT polyline -->
        <polyline [attr.points]="line('csat')" fill="none" stroke="#0069A7" stroke-width="2"/>
        <!-- NPS polyline -->
        <polyline [attr.points]="line('nps')"  fill="none" stroke="#FAC126" stroke-width="2"/>
        <!-- CES polyline (scaled ×20 so a CES of 4.0 → 80 on the 0-100 axis). -->
        <polyline [attr.points]="line('ces')"  fill="none" stroke="#5F9600" stroke-width="2"/>

        <!-- X axis labels (first / mid / last point) -->
        <text [attr.x]="40"  y="208" fill="#64748b" font-size="10" text-anchor="start">{{ formatDate(points()[0].snapshotDate) }}</text>
        <text [attr.x]="390" y="208" fill="#64748b" font-size="10" text-anchor="middle">{{ formatDate(points()[Math.floor(points().length / 2)].snapshotDate) }}</text>
        <text [attr.x]="740" y="208" fill="#64748b" font-size="10" text-anchor="end">{{ formatDate(points()[points().length - 1].snapshotDate) }}</text>
      </svg>
    </section>

    <div class="grid">
      <section class="card">
        <h2 class="title-h2">{{ t('cxa.metric.complaints') }} · {{ t('cxa.metric.resolution') }}</h2>
        <div style="overflow-x: auto">
          <table class="table">
            <thead>
              <tr>
                <th>Date</th>
                <th>{{ t('cxa.metric.csat') }}</th>
                <th>{{ t('cxa.metric.nps') }}</th>
                <th>{{ t('cxa.metric.ces') }}</th>
                <th>{{ t('cxa.metric.complaints') }}</th>
                <th>{{ t('cxa.metric.resolution') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let p of pointsReversed()">
                <td class="muted">{{ formatDate(p.snapshotDate) }}</td>
                <td>{{ p.csat.toFixed(2) }}</td>
                <td>{{ p.nps.toFixed(2) }}</td>
                <td>{{ p.ces.toFixed(2) }}</td>
                <td class="muted">{{ p.complaintVolume }}</td>
                <td class="muted">{{ p.resolutionRateP95Hours.toFixed(1) }}</td>
              </tr>
              <tr *ngIf="!points().length"><td colspan="6" class="muted" style="text-align:center; padding: 24px">{{ t('cxa.empty') }}</td></tr>
            </tbody>
          </table>
        </div>
      </section>

      <section class="card">
        <h2 class="title-h2">{{ t('cxa.rootCause') }}</h2>
        <div style="overflow-x: auto">
          <table class="table">
            <thead>
              <tr>
                <th>{{ t('cxa.col.from') }}</th>
                <th>{{ t('cxa.col.to') }}</th>
                <th>{{ t('cxa.col.strength') }}</th>
                <th>{{ t('cxa.col.notes') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let l of rootCauseLinks()">
                <td><code>{{ l.fromType }}#{{ l.fromRefId }}</code></td>
                <td><code>{{ l.toType }}#{{ l.toRefId }}</code></td>
                <td>
                  <span class="strength-bar"><span class="strength-fill" [style.width.%]="l.linkStrength * 100"></span></span>
                  <span class="muted" style="margin-inline-start: 6px; font-size: 11px">{{ (l.linkStrength * 100).toFixed(0) }}%</span>
                </td>
                <td class="muted">{{ l.notes }}</td>
              </tr>
              <tr *ngIf="!rootCauseLinks().length"><td colspan="4" class="muted" style="text-align:center; padding: 24px">{{ t('common.empty') }}</td></tr>
            </tbody>
          </table>
        </div>
      </section>
    </div>
  `,
  styles: [`
    .grid { display: grid; gap: 16px; grid-template-columns: repeat(auto-fit, minmax(320px, 1fr)); margin-top: 16px; }
    .chart .trend-chart { width: 100%; height: auto; max-height: 240px; }
    .legend { display: inline-flex; align-items: center; gap: 6px; font-size: 11px; color: var(--gac-muted); margin-inline-start: 12px; }
    .legend .dot { width: 10px; height: 10px; border-radius: 50%; display: inline-block; }
    code { background: rgba(0,0,0,0.05); padding: 1px 6px; border-radius: 6px; font-size: 12px; font-family: ui-monospace, Menlo, monospace; }
    .strength-bar { display: inline-block; width: 80px; height: 6px; background: rgba(0,0,0,0.08); border-radius: var(--radius-pill); overflow: hidden; vertical-align: middle; }
    .strength-fill { display: block; height: 100%; background: linear-gradient(90deg, var(--gac-blue-accent), var(--gac-blue)); border-radius: var(--radius-pill); }
  `],
})
export class CxAnalyticsComponent {
  private api = inject(ApiService);
  i18n = inject(I18nService);
  Math = Math;

  segments: CxSegment[] = ['All', 'NewCustomer', 'Returning', 'VIP'];
  ranges = [30, 60, 90];

  segment = signal<CxSegment>('All');
  days = signal<number>(90);
  trend = signal<CxAnalyticsTrendDto | null>(null);
  rootCauseLinks = signal<RootCauseLinkDto[]>([]);

  points = computed<CxAnalyticsSnapshotDto[]>(() => this.trend()?.points ?? []);
  pointsReversed = computed<CxAnalyticsSnapshotDto[]>(() => [...this.points()].reverse().slice(0, 30));

  constructor() { this.reload(); this.loadLinks(); }

  t(k: string) { return this.i18n.t(k); }

  async reload() {
    try {
      this.trend.set(await this.api.cxaTrend(this.segment(), this.days()));
    } catch { /* interceptor */ }
  }

  async loadLinks() {
    try {
      this.rootCauseLinks.set(await this.api.cxaRootCauseLinks());
    } catch { /* interceptor */ }
  }

  setSegment(s: CxSegment) { this.segment.set(s); this.reload(); }
  setDays(d: number) { this.days.set(d); this.reload(); }

  // Map a numeric value (0..100) to SVG y inside the [10..190] band.
  ty(v: number): number {
    const clamped = Math.max(0, Math.min(100, v));
    return 190 - (clamped / 100) * 180;
  }
  // Map a date index to X within [40..740].
  tx(i: number, n: number): number {
    if (n <= 1) return 40;
    return 40 + (i / (n - 1)) * 700;
  }
  line(metric: Metric): string {
    const pts = this.points();
    if (!pts.length) return '';
    return pts.map((p, i) => {
      const raw = metric === 'csat' ? p.csat : metric === 'nps' ? p.nps : p.ces * 20;
      return `${this.tx(i, pts.length).toFixed(1)},${this.ty(Number(raw) || 0).toFixed(1)}`;
    }).join(' ');
  }

  formatDate(iso: string | undefined) {
    if (!iso) return '';
    try { return new Date(iso).toISOString().slice(0, 10); } catch { return iso; }
  }
}
