import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiService } from '../../core/services/api.service';
import { I18nService } from '../../core/services/i18n.service';
import { JourneyDetailDto } from '../../core/models/types';

@Component({
  selector: 'app-journey-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <a routerLink="/journeys" class="back-link">{{ t('journeys.detail.back') }}</a>

    <ng-container *ngIf="detail() as d">
      <h1 class="title-h1">{{ i18n.pickPair(d.journey.nameEn, d.journey.nameAr) }}</h1>
      <p class="muted" style="margin: 0 0 24px">
        {{ d.journey.persona }} · {{ d.journey.stageCount }} {{ t('journeys.col.stages') }}
        · <span class="badge" [class]="statusClass(d.journey.status)">{{ d.journey.status }}</span>
      </p>

      <ol class="timeline">
        <li *ngFor="let s of d.stages">
          <span class="seq">{{ s.sequence }}</span>
          <div class="content">
            <div class="row" style="margin-bottom: 6px; gap: 10px">
              <h2 class="title-h2" style="margin: 0">{{ i18n.pickPair(s.nameEn, s.nameAr) }}</h2>
              <span class="badge" [class]="emotionClass(s.emotionScore)">{{ emotionLabel(s.emotionScore) }}</span>
            </div>
            <p class="eyebrow">{{ t('journeys.detail.touchpoint') }}</p>
            <p style="margin: 2px 0 12px">{{ i18n.pickPair(s.touchpointEn, s.touchpointAr) || '—' }}</p>
            <p class="eyebrow">{{ t('journeys.detail.pain') }}</p>
            <p style="margin: 2px 0 0">{{ i18n.pickPair(s.painPointEn, s.painPointAr) || '—' }}</p>
          </div>
        </li>
      </ol>
    </ng-container>

    <p *ngIf="!detail()" class="muted">{{ t('common.loading') }}</p>
  `,
  styles: [`
    .back-link { display: inline-block; margin-bottom: 12px; font-size: 12px; color: var(--gac-blue); }
    .timeline { list-style: none; padding: 0; margin: 0; display: flex; flex-direction: column; gap: 16px; }
    .timeline li {
      display: grid; grid-template-columns: 48px 1fr; gap: 16px; align-items: flex-start;
      background: var(--gac-bg-card); border-radius: var(--radius-card); padding: 18px 22px;
      box-shadow: var(--shadow-card);
    }
    .seq {
      width: 40px; height: 40px; border-radius: 50%;
      background: var(--gac-navy); color: #fff;
      font-weight: 700; font-size: 14px;
      display: flex; align-items: center; justify-content: center;
    }
    .content { min-width: 0; }
    .eyebrow { margin: 0; }
  `],
})
export class JourneyDetailComponent {
  private api = inject(ApiService);
  private route = inject(ActivatedRoute);
  i18n = inject(I18nService);

  detail = signal<JourneyDetailDto | null>(null);

  constructor() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (id) this.load(id);
  }

  t(k: string) { return this.i18n.t(k); }

  async load(id: number) {
    try { this.detail.set(await this.api.journey(id)); } catch { /* interceptor */ }
  }

  statusClass(s: string) {
    return s === 'active' ? 'badge--green' : s === 'draft' ? 'badge--amber' : 'badge--slate';
  }

  emotionLabel(score: number) {
    const key = score >= 2 ? 'delighted' : score === 1 ? 'satisfied' : score === 0 ? 'neutral' : score === -1 ? 'frustrated' : 'angry';
    return this.t('journeys.emotion.' + key);
  }
  emotionClass(score: number) {
    if (score >= 1) return 'badge--green';
    if (score === 0) return 'badge--slate';
    return 'badge--rose';
  }
}
