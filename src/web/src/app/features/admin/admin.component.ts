import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { I18nService } from '../../core/services/i18n.service';
import { ToastService } from '../../core/services/toast.service';
import { ContactChannelDto, RolePermissionDto } from '../../core/models/types';

type Tab = 'roles' | 'channels';

const ROLES = ['admin', 'supervisor', 'agent', 'quality', 'customer', 'executive'];
const PAGES = [
  '/about', '/dashboard', '/journeys', '/voc', '/complaints', '/inbox', '/kb',
  '/copilot', '/portal', '/programme', '/governance', '/architecture',
  '/audit', '/automation', '/admin', '/notifications', '/profile',
];

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <h1 class="title-h1">{{ t('admin.title') }}</h1>

    <div class="row" style="margin: 8px 0 16px">
      <button class="pill" [class.pill--active]="tab() === 'roles'" (click)="tab.set('roles')">{{ t('admin.tab.roles') }}</button>
      <button class="pill" [class.pill--active]="tab() === 'channels'" (click)="tab.set('channels')">{{ t('admin.tab.channels') }}</button>
    </div>

    <ng-container *ngIf="tab() === 'roles'">
      <div class="card" style="padding: 18px">
        <p class="muted" style="font-size: 12.5px; margin-top: 0">{{ t('admin.role.intro') }}</p>
        <div style="overflow-x:auto; margin-top: 12px">
          <table class="perm-grid">
            <thead>
              <tr>
                <th></th>
                <th *ngFor="let p of pages">{{ p.slice(1) }}</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let r of roles">
                <th class="rowhead">{{ t('role.' + r) }}</th>
                <td *ngFor="let p of pages">
                  <input type="checkbox"
                         [checked]="getAllowed(r, p)"
                         [disabled]="r === 'admin'"
                         (change)="setAllowed(r, p, $any($event.target).checked)" />
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <div style="margin-top: 12px; display:flex; justify-content:flex-end">
          <button class="btn-primary" (click)="savePerms()" [disabled]="saving()">{{ t('common.save') }}</button>
        </div>
      </div>
    </ng-container>

    <ng-container *ngIf="tab() === 'channels'">
      <div class="card" style="padding: 18px; max-width: 520px">
        <label *ngFor="let c of channels()" class="field" style="margin-bottom: 14px">
          <span>{{ channelLabel(c.key) }}</span>
          <input class="input" [ngModel]="c.value"
                 (ngModelChange)="updateChannelLocal(c.key, $event)" [dir]="c.key === 'whatsapp' || c.key === 'info_email' ? 'ltr' : null" />
        </label>
        <div style="display:flex; justify-content:flex-end">
          <button class="btn-primary" (click)="saveChannels()" [disabled]="saving()">{{ t('common.save') }}</button>
        </div>
      </div>
    </ng-container>
  `,
  styles: [`
    .perm-grid { width: 100%; border-collapse: collapse; font-size: 12px; }
    .perm-grid th, .perm-grid td {
      padding: 6px 8px; border-bottom: 1px solid var(--gac-border); text-align: center;
      font-weight: 500; white-space: nowrap;
    }
    .perm-grid .rowhead { text-align: start; font-weight: 600; color: var(--gac-navy); }
    .perm-grid thead th { font-size: 10px; text-transform: uppercase; color: var(--gac-muted); letter-spacing: 0.06em; }
    .field { display: flex; flex-direction: column; gap: 4px; }
    .field > span { font-size: 12px; font-weight: 600; color: var(--gac-navy); }
  `],
})
export class AdminComponent {
  private api = inject(ApiService);
  private toast = inject(ToastService);
  i18n = inject(I18nService);

  tab = signal<Tab>('roles');
  perms = signal<RolePermissionDto[]>([]);
  channels = signal<ContactChannelDto[]>([]);
  saving = signal(false);

  roles = ROLES;
  pages = PAGES;

  // Lookup by (role, page) for fast UI reads.
  private permMap = computed(() => {
    const map = new Map<string, RolePermissionDto>();
    this.perms().forEach((p) => map.set(`${p.role}|${p.pageKey}`, p));
    return map;
  });

  constructor() { this.load(); }

  t(k: string) { return this.i18n.t(k); }

  async load() {
    try {
      const [perms, channels] = await Promise.all([this.api.rolePerms(), this.api.contactChannels()]);
      this.perms.set(perms);
      this.channels.set(channels);
    } catch { /* interceptor */ }
  }

  getAllowed(role: string, pageKey: string): boolean {
    if (role === 'admin') return true;
    return this.permMap().get(`${role}|${pageKey}`)?.allowed ?? false;
  }

  setAllowed(role: string, pageKey: string, allowed: boolean) {
    if (role === 'admin') return;
    this.perms.update((list) => {
      const existing = list.find((p) => p.role === role && p.pageKey === pageKey);
      if (existing) return list.map((p) => p.role === role && p.pageKey === pageKey ? { ...p, allowed } : p);
      return [...list, { role, pageKey, allowed }];
    });
  }

  async savePerms() {
    this.saving.set(true);
    try {
      await this.api.saveRolePerms(this.perms());
      this.toast.ok(this.t('admin.saved'));
    } catch { /* interceptor */ } finally { this.saving.set(false); }
  }

  channelLabel(key: string): string {
    return this.t('admin.channels.' + key) || key;
  }

  updateChannelLocal(key: string, value: string) {
    this.channels.update((list) => list.map((c) => c.key === key ? { ...c, value } : c));
  }

  async saveChannels() {
    this.saving.set(true);
    try {
      await Promise.all(this.channels().map((c) => this.api.saveChannel(c.key, c.value)));
      this.toast.ok(this.t('admin.saved'));
    } catch { /* interceptor */ } finally { this.saving.set(false); }
  }
}
