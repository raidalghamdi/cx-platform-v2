import { Component, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../core/services/api.service';
import { I18nService } from '../../core/services/i18n.service';
import { ComplaintsByCategoryDto, KpiDto } from '../../core/models/types';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  template: `
    <h1 class="title-h1">{{ t('dashboard.title') }}</h1>

    <span class="badge badge--slate" style="margin-bottom:16px">{{ t('dashboard.kpiSource') }}</span>

    <div class="kpi-grid">
      <div *ngFor="let k of kpis()" class="card kpi">
        <div class="kpi-head">
          <span class="eyebrow">{{ i18n.pickPair(k.nameEn, k.nameAr) }}</span>
          <span class="badge" [class.badge--green]="k.source === 'API'" [class.badge--slate]="k.source !== 'API'">{{ k.source }}</span>
        </div>
        <div class="kpi-value">
          {{ k.value }}<span class="kpi-unit"> {{ k.unit }}</span>
        </div>
        <div class="kpi-delta" [class.up]="k.delta > 0" [class.down]="k.delta < 0">
          <span *ngIf="k.delta > 0">▲ {{ k.delta }}</span>
          <span *ngIf="k.delta < 0">▼ {{ -k.delta }}</span>
          <span *ngIf="k.delta === 0">—</span>
          <span *ngIf="k.target !== null" class="muted"> / {{ k.target }}</span>
        </div>
      </div>
      <div *ngIf="!kpis().length" class="card">{{ t('common.loading') }}</div>
    </div>

    <div class="card chart">
      <div style="display:flex; align-items:center; justify-content:space-between">
        <h2 class="title-h2">{{ t('dashboard.byCategory') }}</h2>
        <span class="badge badge--slate">{{ t('dashboard.catSource') }}</span>
      </div>
      <ul class="bars">
        <li *ngFor="let c of categories()">
          <span class="bars-label">{{ c.category }}</span>
          <span class="bars-track"><span class="bars-fill" [style.width.%]="pct(c.count)"></span></span>
          <span class="bars-count">{{ c.count }}</span>
        </li>
        <li *ngIf="!categories().length" class="muted">{{ t('common.loading') }}</li>
      </ul>
    </div>
  `,
  styles: [`
    .kpi-grid {
      display: grid; gap: 12px; margin: 8px 0 24px;
      grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
    }
    .kpi { padding: 14px 16px; }
    .kpi-head { display: flex; justify-content: space-between; gap: 8px; align-items: center; }
    .kpi-value { font-size: 26px; font-weight: 600; color: var(--gac-navy); margin-top: 6px; font-feature-settings: "tnum"; }
    .kpi-unit { font-size: 14px; color: var(--gac-muted); font-weight: 400; }
    .kpi-delta { font-size: 12px; margin-top: 4px; color: var(--gac-muted); }
    .kpi-delta .muted { margin-inline-start: 4px; }
    .kpi-delta.up { color: #15803D; }
    .kpi-delta.down { color: #B91C1C; }
    .chart .bars { list-style: none; padding: 0; margin: 16px 0 0; display: flex; flex-direction: column; gap: 8px; }
    .chart .bars li { display: grid; grid-template-columns: 160px 1fr 48px; gap: 12px; align-items: center; font-size: 12.5px; }
    .bars-label { color: var(--gac-navy); font-weight: 500; }
    .bars-track { height: 8px; background: rgba(0,0,0,0.06); border-radius: 100px; overflow: hidden; }
    .bars-fill { display: block; height: 100%; background: linear-gradient(90deg, var(--gac-blue-accent) 0%, var(--gac-blue) 100%); border-radius: 100px; }
    .bars-count { text-align: end; font-variant-numeric: tabular-nums; color: var(--gac-muted); }
  `],
})
export class DashboardComponent {
  private api = inject(ApiService);
  i18n = inject(I18nService);

  kpis = signal<KpiDto[]>([]);
  categories = signal<ComplaintsByCategoryDto[]>([]);
  maxCount = computed(() => Math.max(1, ...this.categories().map((c) => c.count)));

  constructor() {
    this.load();
  }

  t(key: string) { return this.i18n.t(key); }

  pct(count: number) { return Math.round((count / this.maxCount()) * 100); }

  async load() {
    try {
      const [kpis, cats] = await Promise.all([this.api.kpis(), this.api.complaintsByCategory()]);
      this.kpis.set(kpis);
      this.categories.set(cats);
    } catch {
      /* errors surfaced by interceptor */
    }
  }
}
