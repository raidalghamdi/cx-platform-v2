import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { I18nService } from '../../core/services/i18n.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { CreateGovernanceDecisionRequest, GovernanceBodyDetailDto, GovernanceBodyDto } from '../../core/models/types';

@Component({
  selector: 'app-governance',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <h1 class="title-h1">{{ t('governance.title') }}</h1>
    <p class="muted" style="margin: 0 0 16px">{{ t('governance.subtitle') }}</p>

    <div class="bodies-grid">
      <article *ngFor="let b of bodies()" class="card body-card" [class.selected]="selectedId() === b.id" (click)="select(b.id)">
        <div class="row" style="margin-bottom: 4px">
          <h2 class="title-h2" style="margin: 0">{{ i18n.pickPair(b.nameEn, b.nameAr) }}</h2>
          <span class="spacer"></span>
          <span class="badge badge--navy">{{ t('governance.cadence.' + b.cadence) }}</span>
        </div>
        <p class="muted" style="margin: 0; font-size: 12px">
          <strong>{{ t('governance.body.chair') }}:</strong> {{ b.chair }}
        </p>
        <p class="muted" style="margin: 4px 0 0; font-size: 12px">
          <strong>{{ t('governance.body.members') }}:</strong> {{ b.members.join(' · ') }}
        </p>
        <a *ngIf="b.charterUrl" [href]="b.charterUrl" target="_blank" rel="noopener" style="font-size: 12px">
          {{ t('governance.body.charter') }} →
        </a>
      </article>
      <p *ngIf="!bodies().length" class="muted">{{ t('common.empty') }}</p>
    </div>

    <ng-container *ngIf="detail() as d">
      <div class="row" style="margin-top: 24px; gap: 12px">
        <h2 class="title-h2" style="margin: 0">{{ i18n.pickPair(d.body.nameEn, d.body.nameAr) }} — {{ t('governance.decisions.title') }}</h2>
        <span class="spacer"></span>
        <button *ngIf="canEdit" class="btn-primary" (click)="openNew()">{{ t('governance.addDecision') }}</button>
      </div>

      <div class="card" style="padding: 0; overflow-x: auto; margin-top: 12px">
        <table class="table">
          <thead>
            <tr>
              <th>{{ t('governance.col.title') }}</th>
              <th>{{ t('governance.col.owner') }}</th>
              <th>{{ t('governance.col.decidedAt') }}</th>
              <th>{{ t('governance.col.due') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let dec of d.decisions">
              <td>
                <strong>{{ i18n.pickPair(dec.titleEn, dec.titleAr) }}</strong>
                <div class="muted" style="font-size: 12px; margin-top: 2px">{{ dec.decision }}</div>
              </td>
              <td class="muted">{{ i18n.pickPair(dec.ownerEn, dec.ownerAr) }}</td>
              <td class="muted">{{ formatDate(dec.decidedAt) }}</td>
              <td class="muted">{{ dec.dueDate ? formatDate(dec.dueDate) : '—' }}</td>
            </tr>
            <tr *ngIf="!d.decisions.length"><td colspan="4" style="text-align:center; padding:24px" class="muted">{{ t('common.empty') }}</td></tr>
          </tbody>
        </table>
      </div>
    </ng-container>

    <!-- New decision drawer -->
    <div *ngIf="creating()" class="drawer-backdrop" (click)="closeNew()">
      <div class="drawer" (click)="$event.stopPropagation()">
        <div class="row" style="margin-bottom: 14px">
          <span class="badge badge--gold">{{ t('governance.addDecision') }}</span>
          <span class="spacer"></span>
          <button class="btn-ghost" (click)="closeNew()">{{ t('common.close') }}</button>
        </div>
        <label class="field">
          <span>EN — {{ t('governance.col.title') }}</span>
          <input class="input" [(ngModel)]="form.titleEn" />
        </label>
        <label class="field" style="margin-top: 10px">
          <span>AR — {{ t('governance.col.title') }}</span>
          <input class="input" [(ngModel)]="form.titleAr" dir="rtl" />
        </label>
        <label class="field" style="margin-top: 10px">
          <span>{{ t('governance.col.title') }} — body</span>
          <textarea class="input" [(ngModel)]="form.decision" rows="4"></textarea>
        </label>
        <div class="row" style="margin-top: 10px; gap: 10px">
          <label class="field" style="flex: 1">
            <span>EN — {{ t('governance.col.owner') }}</span>
            <input class="input" [(ngModel)]="form.ownerEn" />
          </label>
          <label class="field" style="flex: 1">
            <span>AR — {{ t('governance.col.owner') }}</span>
            <input class="input" [(ngModel)]="form.ownerAr" dir="rtl" />
          </label>
        </div>
        <label class="field" style="margin-top: 10px">
          <span>{{ t('governance.col.due') }}</span>
          <input class="input" type="date" [(ngModel)]="form.dueDate" />
        </label>
        <div style="margin-top: 14px; display:flex; justify-content:flex-end">
          <button class="btn-primary" (click)="save()" [disabled]="saving()">
            {{ saving() ? t('common.loading') : t('common.save') }}
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .bodies-grid { display: grid; gap: 12px; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); }
    .body-card { cursor: pointer; transition: box-shadow 120ms ease, transform 80ms ease; }
    .body-card:hover { transform: translateY(-2px); }
    .body-card.selected { outline: 2px solid var(--gac-blue); }
    .field { display: flex; flex-direction: column; gap: 4px; }
    .field > span { font-size: 12px; font-weight: 600; color: var(--gac-navy); }
  `],
})
export class GovernanceComponent {
  private api = inject(ApiService);
  private toast = inject(ToastService);
  private auth = inject(AuthService);
  i18n = inject(I18nService);

  bodies = signal<GovernanceBodyDto[]>([]);
  selectedId = signal<number | null>(null);
  detail = signal<GovernanceBodyDetailDto | null>(null);
  creating = signal(false);
  saving = signal(false);
  form: CreateGovernanceDecisionRequest & { dueDate?: string | null } = {
    titleEn: '', titleAr: '', decision: '', ownerEn: '', ownerAr: '', dueDate: null,
  };

  get canEdit(): boolean {
    const r = this.auth.user()?.role ?? '';
    return ['admin', 'supervisor', 'quality', 'executive'].includes(r);
  }

  constructor() { this.load(); }

  t(k: string) { return this.i18n.t(k); }

  async load() {
    try {
      const list = await this.api.governanceBodies();
      this.bodies.set(list);
      if (list.length && this.selectedId() === null) await this.select(list[0].id);
    } catch { /* interceptor */ }
  }

  async select(id: number) {
    this.selectedId.set(id);
    try {
      this.detail.set(await this.api.governanceBody(id));
    } catch { /* interceptor */ }
  }

  openNew() {
    if (!this.canEdit || this.selectedId() === null) return;
    this.creating.set(true);
    this.form = { titleEn: '', titleAr: '', decision: '', ownerEn: '', ownerAr: '', dueDate: null };
  }
  closeNew() { this.creating.set(false); }

  async save() {
    const bodyId = this.selectedId();
    if (!bodyId || !this.canEdit) return;
    if (!this.form.titleEn.trim() || !this.form.titleAr.trim()) return;
    this.saving.set(true);
    try {
      await this.api.governanceDecision(bodyId, {
        titleEn: this.form.titleEn,
        titleAr: this.form.titleAr,
        decision: this.form.decision,
        ownerEn: this.form.ownerEn,
        ownerAr: this.form.ownerAr,
        dueDate: this.form.dueDate ? new Date(this.form.dueDate).toISOString() : null,
      });
      this.toast.ok(this.t('governance.decisionSaved'));
      this.creating.set(false);
      await this.select(bodyId);
    } catch { /* interceptor */ } finally { this.saving.set(false); }
  }

  formatDate(iso: string) { try { return new Date(iso).toISOString().slice(0, 10); } catch { return iso; } }
}
