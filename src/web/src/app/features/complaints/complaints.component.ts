import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { I18nService } from '../../core/services/i18n.service';
import { ToastService } from '../../core/services/toast.service';
import { ComplaintDto, ComplaintListItemDto, ComplaintStatus } from '../../core/models/types';

type Tab = 'all' | 'down';

@Component({
  selector: 'app-complaints',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <h1 class="title-h1">{{ t('complaints.title') }}</h1>

    <div class="row row--wrap" style="margin: 8px 0 16px">
      <button class="pill" [class.pill--active]="tab() === 'all'" (click)="setTab('all')">{{ t('complaints.tab.all') }} ({{ all().length }})</button>
      <button class="pill" [class.pill--active]="tab() === 'down'" (click)="setTab('down')">{{ t('complaints.tab.down') }} ({{ downCount() }})</button>
    </div>

    <p *ngIf="tab() === 'down'" class="card" style="background:#FFF7E6; border-inline-start:4px solid var(--gac-gold); padding:12px 16px; margin-bottom:14px">
      {{ t('complaints.downBanner') }}
    </p>

    <div class="card" style="padding: 0; overflow-x: auto">
      <table class="table">
        <thead>
          <tr>
            <th>{{ t('complaints.code') }}</th>
            <th>{{ t('complaints.subject') }}</th>
            <th>{{ t('complaints.category') }}</th>
            <th>{{ t('complaints.priority') }}</th>
            <th>{{ t('complaints.status') }}</th>
            <th>{{ t('complaints.opened') }}</th>
            <th>{{ t('complaints.closed') }}</th>
            <th *ngIf="tab() === 'down'">{{ t('complaints.stage') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let c of rows()" (click)="open(c)">
            <td><code>{{ c.code }}</code></td>
            <td>{{ i18n.pickPair(c.subjectEn, c.subjectAr) }}</td>
            <td class="muted">{{ c.category }}</td>
            <td><span class="badge" [class]="priorityClass(c.priority)">{{ c.priority }}</span></td>
            <td><span class="badge" [class]="statusClass(c.status)">{{ c.status }}</span></td>
            <td class="muted">{{ formatDate(c.openedAt) }}</td>
            <td class="muted">{{ c.closedAt ? formatDate(c.closedAt) : '—' }}</td>
            <td *ngIf="tab() === 'down'">{{ i18n.pickPair(c.journeyStageEn, c.journeyStageAr) || '—' }}</td>
          </tr>
          <tr *ngIf="!rows().length"><td colspan="8" style="text-align:center; padding:36px" class="muted">{{ t('common.empty') }}</td></tr>
        </tbody>
      </table>
    </div>

    <!-- Drawer -->
    <div *ngIf="activeId()" class="drawer-backdrop" (click)="close()">
      <div class="drawer" (click)="$event.stopPropagation()">
        <ng-container *ngIf="active() as a">
          <div class="row" style="margin-bottom:16px">
            <code style="font-size:12px">{{ a.code }}</code>
            <span class="badge" [class]="priorityClass(a.priority)">{{ a.priority }}</span>
            <span class="badge" [class]="statusClass(a.status)">{{ a.status }}</span>
            <span class="spacer"></span>
            <button class="btn-ghost" (click)="close()">{{ t('common.close') }}</button>
          </div>
          <h2 class="title-h2">{{ i18n.pickPair(a.subjectEn, a.subjectAr) }}</h2>
          <p class="muted">{{ a.category }} · {{ a.channel }}</p>

          <p class="eyebrow" style="margin-top:16px">{{ t('complaints.body') }}</p>
          <p>{{ i18n.pickPair(a.bodyEn, a.bodyAr) }}</p>

          <div class="row row--wrap" style="margin-top:16px">
            <div><div class="eyebrow">{{ t('complaints.opened') }}</div><div>{{ formatDate(a.openedAt) }}</div></div>
            <div><div class="eyebrow">{{ t('complaints.closed') }}</div><div>{{ a.closedAt ? formatDate(a.closedAt) : '—' }}</div></div>
            <div *ngIf="a.journeyStageEn || a.journeyStageAr">
              <div class="eyebrow">{{ t('complaints.stage') }}</div>
              <div>{{ i18n.pickPair(a.journeyStageEn, a.journeyStageAr) }}</div>
            </div>
          </div>

          <div class="row row--wrap" style="margin-top:18px">
            <button class="btn-secondary" (click)="setStatus('InProgress')">{{ t('complaints.markInProgress') }}</button>
            <button class="btn-primary" (click)="setStatus('Resolved')">{{ t('complaints.resolve') }}</button>
            <button class="btn-ghost" (click)="setStatus('New')">{{ t('complaints.reopen') }}</button>
          </div>

          <div style="margin-top:18px">
            <p class="eyebrow">{{ t('complaints.addNote') }}</p>
            <textarea class="input" [(ngModel)]="noteText" rows="3" [placeholder]="t('complaints.notePlaceholder')"></textarea>
            <div style="margin-top:8px; display:flex; justify-content:flex-end">
              <button class="btn-secondary" [disabled]="!noteText().trim()" (click)="saveNote()">{{ t('common.save') }}</button>
            </div>
          </div>
        </ng-container>
      </div>
    </div>
  `,
  styles: [`
    code { background: rgba(0,0,0,0.05); padding: 1px 6px; border-radius: 6px; font-size: 12px; }
  `],
})
export class ComplaintsComponent {
  private api = inject(ApiService);
  i18n = inject(I18nService);
  private toast = inject(ToastService);

  tab = signal<Tab>('all');
  all = signal<ComplaintListItemDto[]>([]);
  activeId = signal<number | null>(null);
  active = signal<ComplaintDto | null>(null);
  noteText = signal('');

  downCount = computed(() => this.all().filter((c) => c.downJourney).length);
  rows = computed(() => this.tab() === 'all' ? this.all() : this.all().filter((c) => c.downJourney));

  constructor() {
    this.load();
  }

  t(k: string) { return this.i18n.t(k); }

  setTab(t: Tab) { this.tab.set(t); }

  async load() {
    try {
      const list = await this.api.complaints();
      this.all.set(list);
    } catch { /* interceptor toasts */ }
  }

  async open(c: ComplaintListItemDto) {
    this.activeId.set(c.id);
    this.noteText.set('');
    try {
      const full = await this.api.complaintGet(c.id);
      this.active.set(full);
    } catch { this.activeId.set(null); }
  }

  close() {
    this.activeId.set(null);
    this.active.set(null);
  }

  async setStatus(s: ComplaintStatus) {
    const id = this.activeId();
    if (!id) return;
    try {
      const updated = await this.api.complaintStatus(id, s);
      this.active.set(updated);
      // Patch list row in place.
      this.all.update((list) => list.map((c) => c.id === id ? {
        ...c,
        status: updated.status,
        closedAt: updated.closedAt,
      } : c));
      this.toast.ok(this.t('complaints.statusChanged'));
    } catch { /* interceptor */ }
  }

  async saveNote() {
    const id = this.activeId();
    if (!id || !this.noteText().trim()) return;
    try {
      await this.api.complaintNote(id, this.noteText().trim());
      this.noteText.set('');
      this.toast.ok(this.t('complaints.noteSaved'));
    } catch { /* interceptor */ }
  }

  formatDate(iso: string) {
    if (!iso) return '';
    try { return new Date(iso).toISOString().slice(0, 10); } catch { return iso; }
  }

  priorityClass(p: string) { return p === 'High' ? 'badge--rose' : p === 'Normal' ? 'badge--blue' : 'badge--slate'; }
  statusClass(s: string)   {
    switch (s) {
      case 'New': return 'badge--blue';
      case 'InProgress': return 'badge--amber';
      case 'Resolved': return 'badge--green';
      case 'Closed': return 'badge--slate';
      default: return 'badge--slate';
    }
  }
}
