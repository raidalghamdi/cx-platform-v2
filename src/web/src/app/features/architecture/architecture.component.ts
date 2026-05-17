import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../core/services/api.service';
import { I18nService } from '../../core/services/i18n.service';
import { ArchitectureReferenceDto } from '../../core/models/types';

@Component({
  selector: 'app-architecture',
  standalone: true,
  imports: [CommonModule],
  template: `
    <h1 class="title-h1">{{ t('architecture.title') }}</h1>
    <p class="muted" style="margin: 0 0 16px">{{ t('architecture.subtitle') }}</p>

    <section class="card">
      <h2 class="title-h2">{{ t('architecture.domains') }}</h2>
      <p class="muted" style="font-size: 12px; margin-top: 0">5 stacked layers — top is closest to the customer.</p>

      <!-- Inline SVG of the 5 GAC domains. Stack runs top→bottom: channels →
           experience → services → integration → foundations. Each is a
           rounded GAC-navy band with gold accent on the left edge. -->
      <svg viewBox="0 0 720 320" xmlns="http://www.w3.org/2000/svg" class="diagram" role="img"
           [attr.aria-label]="t('architecture.domains')">
        <defs>
          <linearGradient id="domain-fill" x1="0" y1="0" x2="1" y2="0">
            <stop offset="0%" stop-color="#0069A7" stop-opacity="0.92"/>
            <stop offset="100%" stop-color="#00192B" stop-opacity="0.92"/>
          </linearGradient>
        </defs>
        <ng-container *ngFor="let d of ref()?.domains; let i = index">
          <g [attr.transform]="'translate(20, ' + (i * 58 + 10) + ')'">
            <rect width="680" height="48" rx="10" ry="10" fill="url(#domain-fill)"/>
            <rect width="6" height="48" rx="3" ry="3" fill="#FAC126"/>
            <text x="22" y="20" fill="#fff" font-family="Exo 2, sans-serif" font-weight="600" font-size="13">
              {{ i + 1 }}. {{ i18n.pickPair(d.nameEn, d.nameAr) }}
            </text>
            <text x="22" y="36" fill="#cfd8e2" font-family="Exo 2, sans-serif" font-size="11">
              {{ i18n.pickPair(d.descriptionEn, d.descriptionAr) }}
            </text>
          </g>
        </ng-container>
      </svg>
    </section>

    <section class="card" style="margin-top: 16px">
      <h2 class="title-h2">{{ t('architecture.patterns') }}</h2>
      <div style="overflow-x: auto">
        <table class="table">
          <thead>
            <tr>
              <th>{{ t('architecture.col.name') }}</th>
              <th>{{ t('architecture.col.style') }}</th>
              <th>{{ t('architecture.col.usage') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let p of ref()?.patterns">
              <td><strong>{{ i18n.pickPair(p.nameEn, p.nameAr) }}</strong></td>
              <td><span class="badge" [class]="styleClass(p.style)">{{ t('architecture.style.' + p.style) }}</span></td>
              <td class="muted">{{ i18n.pickPair(p.usageEn, p.usageAr) }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>
  `,
  styles: [`
    .diagram { width: 100%; max-width: 760px; height: auto; margin-top: 8px; }
  `],
})
export class ArchitectureComponent {
  private api = inject(ApiService);
  i18n = inject(I18nService);
  ref = signal<ArchitectureReferenceDto | null>(null);

  constructor() { this.load(); }

  t(k: string) { return this.i18n.t(k); }

  async load() {
    try { this.ref.set(await this.api.architecture()); } catch { /* interceptor */ }
  }

  styleClass(s: string) {
    return s === 'synchronous' ? 'badge--blue' : s === 'asynchronous' ? 'badge--gold' : 'badge--slate';
  }
}
