import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { I18nService } from '../../core/services/i18n.service';
import { ToastService } from '../../core/services/toast.service';
import { Sentiment, VocResponseDto } from '../../core/models/types';

type ChannelFilter = 'all' | string;
type SentimentFilter = 'all' | Sentiment;

@Component({
  selector: 'app-voc',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <h1 class="title-h1">{{ t('voc.title') }}</h1>
    <p class="muted" style="margin: 0 0 16px">{{ t('voc.subtitle') }}</p>

    <div class="row row--wrap" style="margin-bottom: 8px">
      <strong style="font-size: 12px; color: var(--gac-navy)">{{ t('voc.filter.channel') }}:</strong>
      <button class="pill" [class.pill--active]="channel() === 'all'" (click)="channel.set('all')">{{ t('common.all') }}</button>
      <button *ngFor="let c of channels" class="pill" [class.pill--active]="channel() === c" (click)="channel.set(c)">{{ c }}</button>
    </div>
    <div class="row row--wrap" style="margin-bottom: 16px">
      <strong style="font-size: 12px; color: var(--gac-navy)">{{ t('voc.filter.sentiment') }}:</strong>
      <button class="pill" [class.pill--active]="sentiment() === 'all'" (click)="sentiment.set('all')">{{ t('common.all') }}</button>
      <button *ngFor="let s of sentiments" class="pill" [class.pill--active]="sentiment() === s" (click)="sentiment.set(s)">
        {{ t('voc.sentiment.' + s) }}
      </button>
    </div>

    <div class="card" style="padding: 0; overflow-x: auto">
      <table class="table">
        <thead>
          <tr>
            <th>{{ t('voc.col.customer') }}</th>
            <th>{{ t('voc.col.survey') }}</th>
            <th>{{ t('voc.col.channel') }}</th>
            <th>{{ t('voc.col.nps') }}</th>
            <th>{{ t('voc.col.sentiment') }}</th>
            <th>{{ t('voc.col.respondedAt') }}</th>
            <th>{{ t('voc.col.comment') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let v of filtered()" (click)="open(v)">
            <td><strong>{{ v.customerName }}</strong></td>
            <td>{{ i18n.pickPair(v.surveyEn, v.surveyAr) }}</td>
            <td class="muted">{{ v.channel }}</td>
            <td><span class="nps" [class]="npsClass(v.npsScore)">{{ v.npsScore }}</span></td>
            <td><span class="badge" [class]="sentimentClass(v.sentiment)">{{ t('voc.sentiment.' + v.sentiment) }}</span></td>
            <td class="muted">{{ formatDate(v.respondedAt) }}</td>
            <td class="muted snippet">{{ i18n.pickPair(v.commentEn, v.commentAr) }}</td>
          </tr>
          <tr *ngIf="!filtered().length"><td colspan="7" style="text-align:center; padding:36px" class="muted">{{ t('common.empty') }}</td></tr>
        </tbody>
      </table>
    </div>

    <!-- Edit drawer -->
    <div *ngIf="active() as a" class="drawer-backdrop" (click)="close()">
      <div class="drawer" (click)="$event.stopPropagation()">
        <div class="row" style="margin-bottom: 14px">
          <span class="badge" [class]="sentimentClass(a.sentiment)">{{ t('voc.sentiment.' + a.sentiment) }}</span>
          <span class="badge badge--slate">{{ a.channel }}</span>
          <span class="spacer"></span>
          <button class="btn-ghost" (click)="close()">{{ t('common.close') }}</button>
        </div>
        <h2 class="title-h2">{{ a.customerName }}</h2>
        <p class="muted" style="font-size:12px; margin-top: 0">
          {{ i18n.pickPair(a.surveyEn, a.surveyAr) }} · NPS {{ a.npsScore }} · {{ formatDt(a.respondedAt) }}
        </p>

        <label class="field" style="margin-top: 14px">
          <span>EN</span>
          <textarea class="input" [(ngModel)]="commentEn" rows="4"></textarea>
        </label>
        <label class="field" style="margin-top: 10px">
          <span>AR</span>
          <textarea class="input" [(ngModel)]="commentAr" rows="4" dir="rtl"></textarea>
        </label>
        <div style="margin-top: 12px; display:flex; justify-content:flex-end">
          <button class="btn-primary" (click)="save()" [disabled]="saving()">{{ saving() ? t('common.loading') : t('common.save') }}</button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .nps { display: inline-block; min-width: 28px; padding: 2px 8px; border-radius: var(--radius-pill); font-weight: 700; text-align: center; }
    .nps.high { background: #DCFCE7; color: #14532D; }
    .nps.mid  { background: #FEF3C7; color: #92400E; }
    .nps.low  { background: #FEE2E2; color: #991B1B; }
    .snippet { max-width: 280px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .field { display: flex; flex-direction: column; gap: 4px; }
    .field > span { font-size: 12px; font-weight: 600; color: var(--gac-navy); }
  `],
})
export class VocComponent {
  private api = inject(ApiService);
  private toast = inject(ToastService);
  i18n = inject(I18nService);

  rows = signal<VocResponseDto[]>([]);
  channel = signal<ChannelFilter>('all');
  sentiment = signal<SentimentFilter>('all');
  active = signal<VocResponseDto | null>(null);
  commentEn = '';
  commentAr = '';
  saving = signal(false);

  sentiments: Sentiment[] = ['positive', 'neutral', 'negative'];
  channels = ['email', 'whatsapp', 'portal', 'branch'];

  filtered = computed(() =>
    this.rows()
      .filter((v) => this.channel() === 'all' || v.channel === this.channel())
      .filter((v) => this.sentiment() === 'all' || v.sentiment === this.sentiment()),
  );

  constructor() { this.load(); }

  t(k: string) { return this.i18n.t(k); }

  async load() {
    try { this.rows.set(await this.api.voc()); } catch { /* interceptor */ }
  }

  open(v: VocResponseDto) {
    this.active.set(v);
    this.commentEn = v.commentEn;
    this.commentAr = v.commentAr;
  }
  close() { this.active.set(null); }

  async save() {
    const a = this.active();
    if (!a) return;
    this.saving.set(true);
    try {
      const updated = await this.api.updateVocComment(a.id, this.commentEn, this.commentAr);
      this.rows.update((list) => list.map((r) => r.id === a.id ? updated : r));
      this.active.set(updated);
      this.toast.ok(this.t('voc.commentSaved'));
    } catch { /* interceptor */ } finally { this.saving.set(false); }
  }

  formatDate(iso: string) { try { return new Date(iso).toISOString().slice(0, 10); } catch { return iso; } }
  formatDt(iso: string) { try { return new Date(iso).toLocaleString(this.i18n.isRtl ? 'ar-SA' : 'en-GB'); } catch { return iso; } }
  npsClass(n: number) { return n >= 9 ? 'high' : n >= 7 ? 'mid' : 'low'; }
  sentimentClass(s: string) { return s === 'positive' ? 'badge--green' : s === 'negative' ? 'badge--rose' : 'badge--slate'; }
}
