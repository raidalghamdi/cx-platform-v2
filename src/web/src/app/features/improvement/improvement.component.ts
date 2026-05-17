import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { I18nService } from '../../core/services/i18n.service';
import { ToastService } from '../../core/services/toast.service';
import {
  ImprovementItemDto, ImprovementItemDetailDto, KpiThresholdDto, PdcaStage,
} from '../../core/models/types';

const STAGES: PdcaStage[] = ['Plan', 'Do', 'Check', 'Act', 'Closed'];

@Component({
  selector: 'app-improvement',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <h1 class="title-h1">{{ t('imp.title') }}</h1>
    <p class="muted" style="margin: 0 0 16px">{{ t('imp.subtitle') }}</p>

    <div class="row row--wrap" style="margin-bottom: 12px; gap: 12px; align-items: center">
      <strong style="font-size: 12px; color: var(--gac-navy)">{{ t('imp.filter.source') }}:</strong>
      <button class="pill" [class.pill--active]="!sourceFilter()" (click)="sourceFilter.set(null)">{{ t('imp.filter.all') }}</button>
      <button *ngFor="let s of sources" class="pill" [class.pill--active]="sourceFilter() === s" (click)="sourceFilter.set(s)">
        {{ t('imp.source.' + s) }}
      </button>
      <span class="spacer"></span>
      <strong style="font-size: 12px; color: var(--gac-navy)">{{ t('imp.filter.priority') }}:</strong>
      <button class="pill" [class.pill--active]="!priorityFilter()" (click)="priorityFilter.set(null)">{{ t('imp.filter.all') }}</button>
      <button *ngFor="let p of priorities" class="pill" [class.pill--active]="priorityFilter() === p" (click)="priorityFilter.set(p)">
        {{ t('imp.priority.' + p) }}
      </button>
    </div>

    <div class="kanban">
      <section *ngFor="let s of stages" class="lane card">
        <div class="row" style="margin-bottom: 8px">
          <h3 class="title-h3" style="margin: 0">{{ t('imp.pdca.' + s) }}</h3>
          <span class="spacer"></span>
          <span class="badge badge--slate">{{ countByStage(s) }}</span>
        </div>
        <article *ngFor="let i of itemsByStage(s)" class="ticket" (click)="open(i.id)">
          <div class="row" style="margin-bottom: 4px">
            <span class="badge" [class]="priorityClass(i.priority)">{{ t('imp.priority.' + i.priority) }}</span>
            <span class="spacer"></span>
            <span class="muted" style="font-size: 11px">{{ t('imp.source.' + i.sourceType) }}</span>
          </div>
          <strong style="font-size: 13px">{{ i18n.pickPair(i.titleEn, i.titleAr) }}</strong>
          <p class="muted" style="font-size: 11.5px; margin: 4px 0 0">
            {{ i.owner }} · {{ formatDate(i.createdAt) }}
            <span *ngIf="i.targetDate"> → {{ formatDate(i.targetDate) }}</span>
          </p>
        </article>
        <p *ngIf="!itemsByStage(s).length" class="muted" style="font-size: 12px; text-align: center; padding: 14px 0">
          {{ t('imp.empty') }}
        </p>
      </section>
    </div>

    <section class="card" style="margin-top: 16px">
      <h2 class="title-h2">{{ t('imp.thresholds') }}</h2>
      <div style="overflow-x: auto">
        <table class="table">
          <thead>
            <tr>
              <th>{{ t('imp.threshold.col.kpi') }}</th>
              <th>{{ t('imp.threshold.col.op') }}</th>
              <th>{{ t('imp.threshold.col.value') }}</th>
              <th>{{ t('imp.threshold.col.action') }}</th>
              <th>{{ t('imp.threshold.col.enabled') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let th of thresholds()">
              <td><strong>{{ th.kpiKey }}</strong></td>
              <td>{{ t('imp.threshold.' + th.comparisonOp) }}</td>
              <td><code>{{ th.thresholdValue }}</code></td>
              <td>{{ t('imp.threshold.' + th.breachAction) }}</td>
              <td><span class="badge" [class]="th.enabled ? 'badge--green' : 'badge--slate'">{{ th.enabled ? '✓' : '—' }}</span></td>
            </tr>
            <tr *ngIf="!thresholds().length"><td colspan="5" class="muted" style="text-align:center; padding: 24px">{{ t('common.empty') }}</td></tr>
          </tbody>
        </table>
      </div>
    </section>

    <!-- Item drawer with PDCA log -->
    <div *ngIf="detail() as d" class="drawer-backdrop" (click)="close()">
      <div class="drawer" (click)="$event.stopPropagation()">
        <div class="row" style="margin-bottom: 12px">
          <span class="badge" [class]="priorityClass(d.item.priority)">{{ t('imp.priority.' + d.item.priority) }}</span>
          <span class="badge badge--navy">{{ t('imp.pdca.' + d.item.pdcaStage) }}</span>
          <span class="spacer"></span>
          <button class="btn-ghost" (click)="close()">{{ t('common.close') }}</button>
        </div>
        <h2 class="title-h2">{{ i18n.pickPair(d.item.titleEn, d.item.titleAr) }}</h2>
        <p class="muted" style="font-size: 12px; margin: 0 0 12px">
          {{ d.item.owner }} · {{ t('imp.created') }} {{ formatDate(d.item.createdAt) }}
          <span *ngIf="d.item.targetDate"> · {{ t('imp.target') }} {{ formatDate(d.item.targetDate) }}</span>
        </p>
        <p style="white-space: pre-wrap">{{ i18n.pickPair(d.item.descriptionEn, d.item.descriptionAr) }}</p>

        <div *ngIf="canEdit" class="card" style="background: rgba(0,105,167,0.04); margin-top: 14px">
          <h3 class="title-h3">{{ t('imp.transition') }}</h3>
          <div class="row row--wrap" style="gap: 6px; margin-top: 8px">
            <button *ngFor="let s of stages" class="pill" [class.pill--active]="toStage === s" (click)="toStage = s">
              {{ t('imp.pdca.' + s) }}
            </button>
          </div>
          <label class="field" style="margin-top: 10px">
            <span>{{ t('imp.notesEn') }}</span>
            <textarea class="input" rows="2" [(ngModel)]="notesEn"></textarea>
          </label>
          <label class="field" style="margin-top: 8px">
            <span>{{ t('imp.notesAr') }}</span>
            <textarea class="input" rows="2" dir="rtl" [(ngModel)]="notesAr"></textarea>
          </label>
          <div style="margin-top: 10px; display: flex; justify-content: flex-end">
            <button class="btn-primary" (click)="transition(d.item.id)" [disabled]="saving() || !toStage">
              {{ saving() ? t('common.loading') : t('imp.transition') }}
            </button>
          </div>
        </div>

        <h3 class="title-h3" style="margin-top: 16px">{{ t('imp.log') }}</h3>
        <ol class="log">
          <li *ngFor="let l of d.log">
            <span class="badge badge--slate">{{ t('imp.pdca.' + l.fromStage) }} → {{ t('imp.pdca.' + l.toStage) }}</span>
            <span class="muted" style="font-size: 11px; margin-inline-start: 8px">{{ formatDt(l.changedAt) }}</span>
            <div *ngIf="i18n.pickPair(l.notesEn, l.notesAr) as note" style="font-size: 13px; margin-top: 4px">{{ note }}</div>
          </li>
          <li *ngIf="!d.log.length" class="muted" style="list-style: none">—</li>
        </ol>
      </div>
    </div>
  `,
  styles: [`
    .kanban { display: grid; grid-template-columns: repeat(5, minmax(0, 1fr)); gap: 10px; }
    @media (max-width: 1100px) { .kanban { grid-template-columns: repeat(2, 1fr); } }
    @media (max-width: 600px)  { .kanban { grid-template-columns: 1fr; } }
    .lane { padding: 12px; min-height: 180px; display: flex; flex-direction: column; gap: 8px; }
    .ticket { background: #fff; border: 1px solid var(--gac-border); border-radius: var(--radius-card); padding: 10px 12px; cursor: pointer; }
    .ticket:hover { border-color: var(--gac-blue-accent); }
    code { background: rgba(0,0,0,0.05); padding: 1px 6px; border-radius: 6px; font-size: 12px; }
    .field { display: flex; flex-direction: column; gap: 4px; }
    .field > span { font-size: 12px; font-weight: 600; color: var(--gac-navy); }
    .log { list-style: none; padding: 0; margin: 6px 0 0; display: flex; flex-direction: column; gap: 10px; }
    .log li { background: rgba(0,0,0,0.03); border-radius: 8px; padding: 8px 12px; }
  `],
})
export class ImprovementComponent {
  private api = inject(ApiService);
  private auth = inject(AuthService);
  private toast = inject(ToastService);
  i18n = inject(I18nService);

  stages = STAGES;
  sources = ['KpiBreach', 'AccessibilityAudit', 'ContentReview', 'Manual'];
  priorities = ['Low', 'Medium', 'High', 'Critical'];

  items = signal<ImprovementItemDto[]>([]);
  thresholds = signal<KpiThresholdDto[]>([]);
  sourceFilter = signal<string | null>(null);
  priorityFilter = signal<string | null>(null);
  detail = signal<ImprovementItemDetailDto | null>(null);
  toStage: PdcaStage = 'Do';
  notesEn = '';
  notesAr = '';
  saving = signal(false);

  filtered = computed(() => this.items()
    .filter((i) => !this.sourceFilter() || i.sourceType === this.sourceFilter())
    .filter((i) => !this.priorityFilter() || i.priority === this.priorityFilter()));

  get canEdit() {
    const r = this.auth.user()?.role ?? '';
    return r === 'admin' || r === 'supervisor' || r === 'quality';
  }

  constructor() { this.load(); }

  t(k: string) { return this.i18n.t(k); }

  itemsByStage(stage: PdcaStage): ImprovementItemDto[] {
    return this.filtered().filter((i) => i.pdcaStage === stage);
  }
  countByStage(stage: PdcaStage): number {
    return this.itemsByStage(stage).length;
  }

  async load() {
    try {
      const [items, thresholds] = await Promise.all([this.api.impItems({}), this.api.impThresholds()]);
      this.items.set(items);
      this.thresholds.set(thresholds);
    } catch { /* interceptor */ }
  }

  async open(id: number) {
    try {
      const d = await this.api.impItem(id);
      this.detail.set(d);
      this.toStage = d.item.pdcaStage;
      this.notesEn = '';
      this.notesAr = '';
    } catch { /* interceptor */ }
  }
  close() { this.detail.set(null); }

  async transition(id: number) {
    if (!this.canEdit) return;
    this.saving.set(true);
    try {
      const updated = await this.api.impTransition(id, {
        toStage: this.toStage,
        notesEn: this.notesEn, notesAr: this.notesAr,
      });
      this.detail.set(updated);
      this.items.update((list) => list.map((i) => i.id === id ? updated.item : i));
      this.toast.ok(this.t('imp.transitioned'));
      this.notesEn = ''; this.notesAr = '';
    } catch { /* interceptor */ } finally { this.saving.set(false); }
  }

  priorityClass(p: string) {
    return p === 'Critical' ? 'badge--rose' : p === 'High' ? 'badge--rose' : p === 'Medium' ? 'badge--amber' : 'badge--slate';
  }
  formatDate(iso: string) { try { return new Date(iso).toISOString().slice(0, 10); } catch { return iso; } }
  formatDt(iso: string) {
    try { return new Date(iso).toLocaleString(this.i18n.isRtl ? 'ar-SA' : 'en-GB'); } catch { return iso; }
  }
}
