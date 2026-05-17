import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/services/api.service';
import { I18nService } from '../../core/services/i18n.service';
import { JourneyDto } from '../../core/models/types';

@Component({
  selector: 'app-journeys',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <h1 class="title-h1">{{ t('journeys.title') }}</h1>
    <p class="muted" style="margin: 0 0 16px">{{ t('journeys.subtitle') }}</p>

    <div class="card" style="padding: 0; overflow-x: auto">
      <table class="table">
        <thead>
          <tr>
            <th>{{ t('journeys.col.name') }}</th>
            <th>{{ t('journeys.col.persona') }}</th>
            <th>{{ t('journeys.col.stages') }}</th>
            <th>{{ t('journeys.col.status') }}</th>
            <th>{{ t('journeys.col.created') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let j of rows()" [routerLink]="['/journeys', j.id]">
            <td>
              <strong>{{ i18n.pickPair(j.nameEn, j.nameAr) }}</strong>
            </td>
            <td class="muted">{{ j.persona }}</td>
            <td class="muted">{{ j.stageCount }}</td>
            <td><span class="badge" [class]="statusClass(j.status)">{{ j.status }}</span></td>
            <td class="muted">{{ formatDate(j.createdAt) }}</td>
          </tr>
          <tr *ngIf="!rows().length">
            <td colspan="5" style="text-align:center; padding:36px" class="muted">{{ t('journeys.empty') }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  `,
})
export class JourneysComponent {
  private api = inject(ApiService);
  i18n = inject(I18nService);
  rows = signal<JourneyDto[]>([]);

  constructor() { this.load(); }

  t(k: string) { return this.i18n.t(k); }

  async load() {
    try { this.rows.set(await this.api.journeys()); } catch { /* interceptor */ }
  }

  formatDate(iso: string) {
    try { return new Date(iso).toISOString().slice(0, 10); } catch { return iso; }
  }

  statusClass(s: string) {
    return s === 'active' ? 'badge--green' : s === 'draft' ? 'badge--amber' : 'badge--slate';
  }
}
