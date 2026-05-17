import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { I18nService } from '../../core/services/i18n.service';
import { ToastService } from '../../core/services/toast.service';
import {
  ChannelPerformanceMetricDto, ContentReviewCycleDto, ContentReviewStatus,
  StaleArticleDto, UpdateContentReviewCycleRequest,
} from '../../core/models/types';

@Component({
  selector: 'app-content-governance',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <h1 class="title-h1">{{ t('cg.title') }}</h1>
    <p class="muted" style="margin: 0 0 16px">{{ t('cg.subtitle') }}</p>

    <section class="card">
      <h2 class="title-h2">{{ t('cg.reviews') }}</h2>
      <div style="overflow-x: auto">
        <table class="table">
          <thead>
            <tr>
              <th>{{ t('cg.col.article') }}</th>
              <th>{{ t('cg.col.due') }}</th>
              <th>{{ t('cg.col.reviewer') }}</th>
              <th>{{ t('cg.col.status') }}</th>
              <th>{{ t('cg.col.freshness') }}</th>
              <th>{{ t('cg.col.parity') }}</th>
              <th *ngIf="canEdit"></th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let c of cycles()">
              <td><strong>{{ i18n.pickPair(c.articleTitleEn, c.articleTitleAr) || ('Article #' + c.kbArticleId) }}</strong></td>
              <td class="muted">
                {{ formatDate(c.dueDate) }}
                <span *ngIf="isOverdue(c)" class="badge badge--rose" style="margin-inline-start: 6px">overdue</span>
              </td>
              <td class="muted">{{ c.assignedReviewer }}</td>
              <td><span class="badge" [class]="statusClass(c.status)">{{ t('cg.status.' + c.status) }}</span></td>
              <td>
                <span class="bar"><span class="bar-fill" [class]="freshnessClass(c.freshnessScore)" [style.width.%]="c.freshnessScore"></span></span>
                <span class="muted" style="margin-inline-start: 6px; font-size: 11px">{{ c.freshnessScore }}</span>
              </td>
              <td><span class="badge" [class]="c.enArParityFlag ? 'badge--green' : 'badge--rose'">{{ c.enArParityFlag ? t('cg.parity.ok') : t('cg.parity.gap') }}</span></td>
              <td *ngIf="canEdit" style="white-space: nowrap">
                <button *ngIf="c.status !== 'Approved'" class="btn-ghost" (click)="approve(c)">{{ t('cg.status.Approved') }}</button>
                <button *ngIf="c.status === 'Pending'" class="btn-ghost" (click)="startReview(c)">{{ t('cg.status.InReview') }}</button>
              </td>
            </tr>
            <tr *ngIf="!cycles().length"><td colspan="7" class="muted" style="text-align:center; padding: 24px">{{ t('cg.empty') }}</td></tr>
          </tbody>
        </table>
      </div>
    </section>

    <div class="grid">
      <section class="card">
        <h2 class="title-h2">{{ t('cg.stale') }}</h2>
        <div style="overflow-x: auto">
          <table class="table">
            <thead>
              <tr>
                <th>{{ t('cg.stale.col.title') }}</th>
                <th>{{ t('cg.stale.col.category') }}</th>
                <th>{{ t('cg.stale.col.updatedAt') }}</th>
                <th>{{ t('cg.stale.col.score') }}</th>
                <th>{{ t('cg.col.parity') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let s of stale()">
                <td>{{ i18n.pickPair(s.titleEn, s.titleAr) }}</td>
                <td class="muted">{{ s.category }}</td>
                <td class="muted">{{ formatDate(s.updatedAt) }}</td>
                <td>
                  <span class="bar"><span class="bar-fill" [class]="freshnessClass(s.freshnessScore)" [style.width.%]="s.freshnessScore"></span></span>
                  <span class="muted" style="margin-inline-start: 6px; font-size: 11px">{{ s.freshnessScore }}</span>
                </td>
                <td><span class="badge" [class]="s.enArParityFlag ? 'badge--green' : 'badge--rose'">{{ s.enArParityFlag ? t('cg.parity.ok') : t('cg.parity.gap') }}</span></td>
              </tr>
              <tr *ngIf="!stale().length"><td colspan="5" class="muted" style="text-align:center; padding: 24px">{{ t('cg.empty') }}</td></tr>
            </tbody>
          </table>
        </div>
      </section>

      <section class="card">
        <h2 class="title-h2">{{ t('cg.channels') }}</h2>
        <div style="overflow-x: auto">
          <table class="table">
            <thead>
              <tr>
                <th>{{ t('cg.ch.col.channel') }}</th>
                <th>{{ t('cg.ch.col.volume') }}</th>
                <th>{{ t('cg.ch.col.avgResp') }}</th>
                <th>{{ t('cg.ch.col.resolution') }}</th>
                <th>{{ t('cg.ch.col.csat') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let r of channelRollup()">
                <td><strong>{{ r.channel }}</strong></td>
                <td class="muted">{{ r.volume }}</td>
                <td class="muted">{{ r.avgRespMin.toFixed(1) }}</td>
                <td>{{ r.resolutionPct.toFixed(1) }} %</td>
                <td>{{ r.csat.toFixed(2) }}</td>
              </tr>
              <tr *ngIf="!channelRollup().length"><td colspan="5" class="muted" style="text-align:center; padding: 24px">{{ t('cg.empty') }}</td></tr>
            </tbody>
          </table>
        </div>
      </section>
    </div>
  `,
  styles: [`
    .grid { display: grid; gap: 16px; grid-template-columns: repeat(auto-fit, minmax(340px, 1fr)); margin-top: 16px; }
    .bar { display: inline-block; width: 90px; height: 6px; background: rgba(0,0,0,0.08); border-radius: var(--radius-pill); overflow: hidden; vertical-align: middle; }
    .bar-fill { display: block; height: 100%; border-radius: var(--radius-pill); }
    .bar-fill.high { background: var(--gac-service-green); }
    .bar-fill.mid  { background: #F59E0B; }
    .bar-fill.low  { background: #DC2626; }
  `],
})
export class ContentGovernanceComponent {
  private api = inject(ApiService);
  private auth = inject(AuthService);
  private toast = inject(ToastService);
  i18n = inject(I18nService);

  cycles = signal<ContentReviewCycleDto[]>([]);
  stale = signal<StaleArticleDto[]>([]);
  channels = signal<ChannelPerformanceMetricDto[]>([]);

  // Per-channel average over the loaded window.
  channelRollup = computed(() => {
    const map = new Map<string, { volume: number; avgRespSum: number; n: number; resPctSum: number; csatSum: number }>();
    for (const m of this.channels()) {
      const r = map.get(m.channel) ?? { volume: 0, avgRespSum: 0, n: 0, resPctSum: 0, csatSum: 0 };
      r.volume += m.volumeCount;
      r.avgRespSum += Number(m.avgResponseMinutes) || 0;
      r.resPctSum += Number(m.resolutionRatePct) || 0;
      r.csatSum += Number(m.csatScore) || 0;
      r.n++;
      map.set(m.channel, r);
    }
    return Array.from(map.entries()).map(([channel, r]) => ({
      channel,
      volume: r.volume,
      avgRespMin: r.n ? r.avgRespSum / r.n : 0,
      resolutionPct: r.n ? r.resPctSum / r.n : 0,
      csat: r.n ? r.csatSum / r.n : 0,
    }));
  });

  get canEdit() {
    const r = this.auth.user()?.role ?? '';
    return r === 'admin' || r === 'supervisor' || r === 'quality' || r === 'agent';
  }

  constructor() { this.load(); }

  t(k: string) { return this.i18n.t(k); }

  async load() {
    try {
      const [c, s, ch] = await Promise.all([
        this.api.cgReviewCycles(), this.api.cgStaleArticles(), this.api.cgChannelPerformance({}),
      ]);
      this.cycles.set(c);
      this.stale.set(s);
      this.channels.set(ch);
    } catch { /* interceptor */ }
  }

  async approve(c: ContentReviewCycleDto) { await this.updateStatus(c, 'Approved'); }
  async startReview(c: ContentReviewCycleDto) { await this.updateStatus(c, 'InReview'); }

  private async updateStatus(c: ContentReviewCycleDto, status: ContentReviewStatus) {
    if (!this.canEdit) return;
    try {
      const req: UpdateContentReviewCycleRequest = {
        status, assignedReviewer: c.assignedReviewer, notes: c.notes,
      };
      const updated = await this.api.cgUpdateCycle(c.id, req);
      this.cycles.update((list) => list.map((x) => x.id === c.id ? updated : x));
      this.toast.ok(this.t('admin.saved'));
    } catch { /* interceptor */ }
  }

  isOverdue(c: ContentReviewCycleDto) {
    if (c.status === 'Approved' || c.status === 'Rejected') return false;
    return new Date(c.dueDate).getTime() < Date.now();
  }
  statusClass(s: ContentReviewStatus) {
    return s === 'Approved' ? 'badge--green' : s === 'Rejected' ? 'badge--rose' : s === 'InReview' ? 'badge--amber' : 'badge--blue';
  }
  freshnessClass(score: number) {
    return score >= 75 ? 'high' : score >= 50 ? 'mid' : 'low';
  }
  formatDate(iso: string) { try { return new Date(iso).toISOString().slice(0, 10); } catch { return iso; } }
}
