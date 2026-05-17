import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { I18nService } from '../../core/services/i18n.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { ProgrammeInitiativeDto, RagStatus } from '../../core/models/types';

@Component({
  selector: 'app-programme',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <h1 class="title-h1">{{ t('programme.title') }}</h1>
    <p class="muted" style="margin: 0 0 16px">{{ t('programme.subtitle') }}</p>

    <div class="card" style="padding: 0; overflow-x: auto">
      <table class="table">
        <thead>
          <tr>
            <th>{{ t('programme.col.name') }}</th>
            <th>{{ t('programme.col.owner') }}</th>
            <th>{{ t('programme.col.rag') }}</th>
            <th>{{ t('programme.col.progress') }}</th>
            <th>{{ t('programme.col.dates') }}</th>
            <th>{{ t('programme.col.notes') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let p of rows()" (click)="open(p)">
            <td><strong>{{ i18n.pickPair(p.nameEn, p.nameAr) }}</strong></td>
            <td class="muted">{{ p.owner }}</td>
            <td>
              <span class="rag-dot" [class]="ragClass(p.ragStatus)"></span>
              <span class="badge" [class]="ragBadge(p.ragStatus)">{{ t('programme.rag.' + p.ragStatus) }}</span>
            </td>
            <td>
              <div class="progress">
                <span class="progress-fill" [style.width.%]="p.progressPct" [class]="ragClass(p.ragStatus)"></span>
              </div>
              <span class="muted" style="font-size: 11px">{{ p.progressPct }}%</span>
            </td>
            <td class="muted" style="font-size: 12px">{{ formatDate(p.startDate) }} → {{ formatDate(p.targetDate) }}</td>
            <td class="muted snippet">{{ p.notes }}</td>
          </tr>
          <tr *ngIf="!rows().length"><td colspan="6" style="text-align:center; padding:36px" class="muted">{{ t('common.empty') }}</td></tr>
        </tbody>
      </table>
    </div>

    <!-- Status drawer -->
    <div *ngIf="active() as a" class="drawer-backdrop" (click)="close()">
      <div class="drawer" (click)="$event.stopPropagation()">
        <div class="row" style="margin-bottom: 14px">
          <span class="badge" [class]="ragBadge(a.ragStatus)">{{ t('programme.rag.' + a.ragStatus) }}</span>
          <span class="spacer"></span>
          <button class="btn-ghost" (click)="close()">{{ t('common.close') }}</button>
        </div>
        <h2 class="title-h2">{{ i18n.pickPair(a.nameEn, a.nameAr) }}</h2>
        <p class="muted" style="margin: 0 0 16px">{{ a.owner }} · {{ formatDate(a.startDate) }} → {{ formatDate(a.targetDate) }}</p>

        <ng-container *ngIf="canEdit; else readOnly">
          <label class="field">
            <span>{{ t('programme.col.rag') }}</span>
            <div class="row" style="gap: 6px">
              <button *ngFor="let r of rags" class="pill" [class.pill--active]="form.ragStatus === r" (click)="form.ragStatus = r">
                {{ t('programme.rag.' + r) }}
              </button>
            </div>
          </label>
          <label class="field" style="margin-top: 10px">
            <span>{{ t('programme.col.progress') }} — {{ form.progressPct }}%</span>
            <input type="range" min="0" max="100" step="5" [(ngModel)]="form.progressPct" />
          </label>
          <label class="field" style="margin-top: 10px">
            <span>{{ t('programme.col.notes') }}</span>
            <textarea class="input" [(ngModel)]="form.notes" rows="4"></textarea>
          </label>
          <div style="margin-top: 12px; display:flex; justify-content:flex-end">
            <button class="btn-primary" (click)="save()" [disabled]="saving()">
              {{ saving() ? t('common.loading') : t('programme.editStatus') }}
            </button>
          </div>
        </ng-container>
        <ng-template #readOnly>
          <p class="eyebrow">{{ t('programme.col.notes') }}</p>
          <p>{{ a.notes || '—' }}</p>
        </ng-template>
      </div>
    </div>
  `,
  styles: [`
    .rag-dot { display: inline-block; width: 10px; height: 10px; border-radius: 50%; margin-inline-end: 8px; vertical-align: middle; }
    .rag-dot.red   { background: #DC2626; }
    .rag-dot.amber { background: #F59E0B; }
    .rag-dot.green { background: var(--gac-service-green); }
    .progress { width: 140px; height: 8px; background: rgba(0,0,0,0.06); border-radius: var(--radius-pill); overflow: hidden; }
    .progress-fill { display: block; height: 100%; border-radius: var(--radius-pill); }
    .progress-fill.red   { background: #DC2626; }
    .progress-fill.amber { background: #F59E0B; }
    .progress-fill.green { background: var(--gac-service-green); }
    .snippet { max-width: 260px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .field { display: flex; flex-direction: column; gap: 4px; }
    .field > span { font-size: 12px; font-weight: 600; color: var(--gac-navy); }
  `],
})
export class ProgrammeComponent {
  private api = inject(ApiService);
  private toast = inject(ToastService);
  private auth = inject(AuthService);
  i18n = inject(I18nService);

  rows = signal<ProgrammeInitiativeDto[]>([]);
  active = signal<ProgrammeInitiativeDto | null>(null);
  saving = signal(false);
  form: { ragStatus: RagStatus; progressPct: number; notes: string } = { ragStatus: 'amber', progressPct: 0, notes: '' };
  rags: RagStatus[] = ['red', 'amber', 'green'];

  get canEdit(): boolean {
    const r = this.auth.user()?.role ?? '';
    return ['admin', 'supervisor', 'executive'].includes(r);
  }

  constructor() { this.load(); }

  t(k: string) { return this.i18n.t(k); }

  async load() {
    try { this.rows.set(await this.api.programme()); } catch { /* interceptor */ }
  }

  open(p: ProgrammeInitiativeDto) {
    this.active.set(p);
    this.form = { ragStatus: p.ragStatus, progressPct: p.progressPct, notes: p.notes };
  }
  close() { this.active.set(null); }

  async save() {
    const a = this.active();
    if (!a) return;
    this.saving.set(true);
    try {
      const updated = await this.api.programmeStatus(a.id, this.form);
      this.rows.update((list) => list.map((r) => r.id === a.id ? updated : r));
      this.active.set(updated);
      this.toast.ok(this.t('programme.statusSaved'));
    } catch { /* interceptor */ } finally { this.saving.set(false); }
  }

  formatDate(iso: string) { try { return new Date(iso).toISOString().slice(0, 10); } catch { return iso; } }
  ragClass(r: RagStatus) { return r; }
  ragBadge(r: RagStatus) {
    return r === 'green' ? 'badge--green' : r === 'amber' ? 'badge--amber' : 'badge--rose';
  }
}
