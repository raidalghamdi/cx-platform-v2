import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { I18nService } from '../../core/services/i18n.service';
import { ToastService } from '../../core/services/toast.service';
import { InboxChannel, InboxStatus, InboxThreadDto } from '../../core/models/types';

type ChannelPill = 'all' | InboxChannel;
type StatusPill = 'all' | InboxStatus;

@Component({
  selector: 'app-inbox',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <h1 class="title-h1">{{ t('inbox.title') }}</h1>

    <p class="card" style="background:#E0F2FE; border-inline-start:4px solid var(--gac-blue); padding:12px 16px; margin: 8px 0 16px">
      {{ t('inbox.banner') }}
    </p>

    <div class="row row--wrap" style="margin-bottom:8px">
      <button class="pill" [class.pill--active]="channelFilter() === 'all'" (click)="channelFilter.set('all')">{{ t('inbox.channel.all') }} ({{ all().length }})</button>
      <button *ngFor="let ch of channels" class="pill" [class.pill--active]="channelFilter() === ch" (click)="channelFilter.set(ch)">
        {{ t('inbox.channel.' + ch) }} ({{ countByChannel(ch) }})
      </button>
    </div>

    <div class="row row--wrap" style="margin-bottom:16px">
      <button class="pill" [class.pill--active]="statusFilter() === 'all'" (click)="statusFilter.set('all')">{{ t('common.all') }}</button>
      <button *ngFor="let st of statuses" class="pill" [class.pill--active]="statusFilter() === st" (click)="statusFilter.set(st)">
        {{ t('inbox.status.' + st) }} ({{ countByStatus(st) }})
      </button>
    </div>

    <div class="card" style="padding:0">
      <ul class="thread-list">
        <li *ngFor="let th of filtered()" (click)="open(th)">
          <span class="prio" [class]="priorityDot(th.priority)"></span>
          <span class="chan-icon">{{ chanInitial(th.channel) }}</span>
          <span class="thread-main">
            <span class="thread-head">
              <strong>{{ th.fromName }}</strong>
              <span class="muted" style="font-size:11px" dir="ltr">{{ th.fromAddress }}</span>
              <span class="spacer"></span>
              <span class="muted" style="font-size:11px">{{ relative(th.receivedAt) }}</span>
            </span>
            <span *ngIf="th.subject" class="thread-subject">{{ th.subject }}</span>
            <span class="thread-snippet muted">{{ snippet(th.body) }}</span>
            <span class="row" style="margin-top:4px">
              <span class="badge" [class]="statusClass(th.status)">{{ t('inbox.status.' + th.status) }}</span>
              <span class="badge badge--slate">{{ th.channel }}</span>
            </span>
          </span>
        </li>
        <li *ngIf="!filtered().length" class="empty muted">{{ t('common.empty') }}</li>
      </ul>
    </div>

    <!-- Reply drawer -->
    <div *ngIf="active() as a" class="drawer-backdrop" (click)="close()">
      <div class="drawer" (click)="$event.stopPropagation()">
        <div class="row" style="margin-bottom:14px">
          <span class="badge" [class]="statusClass(a.status)">{{ t('inbox.status.' + a.status) }}</span>
          <span class="badge badge--slate">{{ a.channel }}</span>
          <span class="spacer"></span>
          <button class="btn-ghost" (click)="close()">{{ t('common.close') }}</button>
        </div>
        <h2 class="title-h2">{{ a.subject ?? (a.fromName + ' · ' + a.channel) }}</h2>
        <p class="muted" style="font-size:12px">
          <strong>{{ a.fromName }}</strong>
          · <span dir="ltr">{{ a.fromAddress }}</span>
          · {{ formatDt(a.receivedAt) }}
        </p>

        <div class="msg-block">
          <p class="eyebrow">{{ t('inbox.original') }}</p>
          <p style="white-space:pre-wrap">{{ a.body }}</p>
        </div>

        <div *ngIf="a.replyBody" class="msg-block reply">
          <p class="eyebrow">{{ t('inbox.reply.your') }} · {{ formatDt(a.repliedAt!) }}</p>
          <p *ngIf="a.replySubject" style="font-weight:600">{{ a.replySubject }}</p>
          <p style="white-space:pre-wrap">{{ a.replyBody }}</p>
        </div>

        <div class="compose">
          <p class="eyebrow">Compose reply</p>
          <label *ngIf="a.channel === 'Email'" class="field">
            <span>{{ t('inbox.reply.subject') }}</span>
            <input class="input" [(ngModel)]="replySubject" placeholder="Re: …" />
          </label>
          <label class="field" style="margin-top:8px">
            <span>{{ t('inbox.reply.body') }}</span>
            <textarea class="input" [(ngModel)]="replyBody" rows="5"
              [maxlength]="a.channel === 'WhatsApp' ? 4096 : null"
              [dir]="i18n.isRtl ? 'rtl' : 'ltr'"></textarea>
            <span *ngIf="a.channel === 'WhatsApp'" class="muted" style="font-size:11px; text-align:end" dir="ltr">
              {{ replyBody().length }} / 4096 {{ t('inbox.charCount') }}
            </span>
          </label>
          <div style="margin-top:10px; display:flex; justify-content:flex-end">
            <button class="btn-primary" [disabled]="!replyBody().trim() || sending()" (click)="send()">
              {{ sending() ? t('inbox.reply.sending') : t('inbox.reply.send') }}
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .thread-list { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; }
    .thread-list li { padding: 12px 14px; border-bottom: 1px solid var(--gac-border); display: grid; grid-template-columns: 8px 32px 1fr; gap: 10px; align-items: flex-start; cursor: pointer; }
    .thread-list li:hover { background: rgba(0,105,167,0.04); }
    .thread-list .empty { text-align: center; padding: 36px; }
    .prio { width: 8px; height: 8px; border-radius: 50%; margin-top: 8px; }
    .prio.high { background: #DC2626; }
    .prio.normal { background: var(--gac-blue); }
    .prio.low { background: var(--gac-muted); }
    .chan-icon {
      width: 32px; height: 32px; border-radius: 50%;
      background: rgba(0,105,167,0.1); color: var(--gac-blue);
      font-weight: 700; font-size: 12px;
      display: flex; align-items: center; justify-content: center;
    }
    .thread-main { display: flex; flex-direction: column; gap: 2px; min-width: 0; }
    .thread-head { display: flex; align-items: center; gap: 8px; }
    .thread-subject { font-weight: 600; font-size: 13px; color: var(--gac-navy); }
    .thread-snippet { font-size: 12px; }
    .msg-block { background: #F9FAFB; border: 1px solid var(--gac-border); border-radius: var(--radius-card); padding: 12px 14px; margin-top: 14px; }
    .msg-block.reply { background: #ECFDF5; border-color: #A7F3D0; }
    .compose { margin-top: 16px; padding-top: 14px; border-top: 1px solid var(--gac-border); }
    .field { display: flex; flex-direction: column; gap: 4px; }
    .field > span { font-size: 12px; font-weight: 600; color: var(--gac-navy); }
  `],
})
export class InboxComponent {
  private api = inject(ApiService);
  private toast = inject(ToastService);
  i18n = inject(I18nService);

  channels: InboxChannel[] = ['Email', 'WhatsApp', 'Chat'];
  statuses: InboxStatus[] = ['New', 'Open', 'Replied', 'Closed'];

  all = signal<InboxThreadDto[]>([]);
  channelFilter = signal<ChannelPill>('all');
  statusFilter = signal<StatusPill>('all');
  active = signal<InboxThreadDto | null>(null);
  replySubject = signal('');
  replyBody = signal('');
  sending = signal(false);

  filtered = computed(() =>
    this.all()
      .filter((t) => this.channelFilter() === 'all' || t.channel === this.channelFilter())
      .filter((t) => this.statusFilter() === 'all' || t.status === this.statusFilter())
      .sort((a, b) => new Date(b.receivedAt).getTime() - new Date(a.receivedAt).getTime()),
  );

  constructor() {
    this.load();
  }

  t(k: string) { return this.i18n.t(k); }

  countByChannel(ch: InboxChannel) { return this.all().filter((t) => t.channel === ch).length; }
  countByStatus(st: InboxStatus) { return this.all().filter((t) => t.status === st).length; }

  async load() {
    try { this.all.set(await this.api.threads()); } catch { /* interceptor */ }
  }

  open(th: InboxThreadDto) {
    this.active.set(th);
    this.replySubject.set(th.subject ? `Re: ${th.subject}` : '');
    this.replyBody.set('');
  }

  close() { this.active.set(null); }

  async send() {
    const a = this.active();
    if (!a || !this.replyBody().trim()) return;
    this.sending.set(true);
    try {
      const updated = await this.api.threadReply(
        a.id,
        this.replyBody().trim(),
        a.channel === 'Email' ? this.replySubject() : undefined,
      );
      this.active.set(updated);
      this.all.update((list) => list.map((t) => t.id === a.id ? updated : t));
      this.toast.ok(this.t('inbox.reply.sent'));
      this.replyBody.set('');
    } catch (e: any) {
      this.toast.err(this.t('inbox.reply.failed'), e?.error?.error);
    } finally {
      this.sending.set(false);
    }
  }

  snippet(body: string) { return body.length > 120 ? body.slice(0, 120) + '…' : body; }

  chanInitial(ch: InboxChannel) { return ch === 'Email' ? '@' : ch === 'WhatsApp' ? 'W' : 'C'; }

  priorityDot(p: string) { return p.toLowerCase(); }
  statusClass(s: string) {
    switch (s) {
      case 'New': return 'badge--blue';
      case 'Open': return 'badge--amber';
      case 'Replied': return 'badge--green';
      case 'Closed': return 'badge--slate';
      default: return 'badge--slate';
    }
  }

  relative(iso: string) {
    const diff = Math.round((Date.now() - new Date(iso).getTime()) / 60000);
    if (diff < 1) return this.i18n.isRtl ? 'الآن' : 'just now';
    if (diff < 60) return this.i18n.isRtl ? `قبل ${diff} د` : `${diff}m ago`;
    const h = Math.round(diff / 60);
    if (h < 24) return this.i18n.isRtl ? `قبل ${h} س` : `${h}h ago`;
    const d = Math.round(h / 24);
    return this.i18n.isRtl ? `قبل ${d} ي` : `${d}d ago`;
  }

  formatDt(iso: string) {
    if (!iso) return '';
    try { return new Date(iso).toLocaleString(this.i18n.isRtl ? 'ar-SA' : 'en-GB'); } catch { return iso; }
  }
}
