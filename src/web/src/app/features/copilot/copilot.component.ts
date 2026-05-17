import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { I18nService } from '../../core/services/i18n.service';
import { ToastService } from '../../core/services/toast.service';
import { CopilotIntent, CopilotInteractionDto } from '../../core/models/types';

@Component({
  selector: 'app-copilot',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <h1 class="title-h1">{{ t('copilot.title') }}</h1>
    <p class="muted" style="margin: 0 0 16px">{{ t('copilot.subtitle') }}</p>

    <div class="copilot-grid">
      <section class="card chat">
        <div class="suggestions">
          <button *ngFor="let i of intents" class="pill"
                  [class.pill--active]="intent() === i" (click)="intent.set(i)">
            {{ t('copilot.intent.' + i) }}
          </button>
        </div>

        <div class="thread">
          <article *ngFor="let m of history()" class="msg" [class.msg--ok]="m.success" [class.msg--fail]="!m.success">
            <div class="row" style="gap: 6px; margin-bottom: 6px">
              <span class="badge badge--navy">{{ t('copilot.intent.' + m.intent) }}</span>
              <span class="muted" style="font-size: 11px">{{ m.latencyMs }} ms</span>
              <span class="spacer"></span>
              <span class="muted" style="font-size: 11px">{{ formatDt(m.createdAt) }}</span>
            </div>
            <p class="prompt">
              <strong>{{ t('copilot.you') }}:</strong> {{ i18n.pickPair(m.promptEn, m.promptAr) }}
            </p>
            <p class="reply">
              <strong>{{ t('copilot.copilot') }}:</strong>
              <span style="white-space: pre-wrap">{{ i18n.pickPair(m.responseEn, m.responseAr) }}</span>
            </p>
          </article>
          <p *ngIf="!history().length" class="muted">{{ t('copilot.history') }} —</p>
        </div>

        <div class="composer">
          <textarea class="input" [(ngModel)]="prompt" rows="3" [placeholder]="t('copilot.prompt')"
                    [dir]="i18n.isRtl ? 'rtl' : 'ltr'"></textarea>
          <div style="margin-top: 8px; display: flex; justify-content: flex-end; gap: 8px">
            <span *ngIf="busy()" class="muted" style="font-size: 12px; align-self: center">{{ t('copilot.thinking') }}</span>
            <button class="btn-primary" (click)="ask()" [disabled]="busy() || !prompt.trim()">
              {{ t('copilot.send') }}
            </button>
          </div>
        </div>
      </section>
    </div>
  `,
  styles: [`
    .copilot-grid { display: grid; gap: 12px; grid-template-columns: 1fr; }
    .chat { padding: 14px; }
    .suggestions { display: flex; gap: 6px; flex-wrap: wrap; margin-bottom: 12px; }
    .thread { display: flex; flex-direction: column; gap: 10px; max-height: 460px; overflow-y: auto; padding-end: 4px; }
    .msg { background: #fff; border: 1px solid var(--gac-border); border-radius: var(--radius-card); padding: 12px 14px; }
    .msg--fail { background: #FEF2F2; border-color: #FECACA; }
    .prompt, .reply { margin: 0; font-size: 13px; line-height: 1.5; }
    .prompt { margin-bottom: 6px; }
    .reply { color: var(--gac-navy); }
    .composer { margin-top: 14px; }
  `],
})
export class CopilotComponent {
  private api = inject(ApiService);
  private toast = inject(ToastService);
  i18n = inject(I18nService);

  intents: CopilotIntent[] = ['ask', 'draft_reply', 'summarise', 'find_similar'];
  intent = signal<CopilotIntent>('ask');
  prompt = '';
  busy = signal(false);
  history = signal<CopilotInteractionDto[]>([]);

  constructor() { this.load(); }

  t(k: string) { return this.i18n.t(k); }

  async load() {
    try { this.history.set(await this.api.copilotHistory()); } catch { /* interceptor */ }
  }

  async ask() {
    if (!this.prompt.trim()) return;
    const text = this.prompt.trim();
    this.busy.set(true);
    try {
      const isAr = this.i18n.isRtl;
      const reply = await this.api.copilotAsk({
        intent: this.intent(),
        promptEn: isAr ? '' : text,
        promptAr: isAr ? text : '',
      });
      this.history.update((list) => [reply, ...list]);
      this.prompt = '';
      if (!reply.success) this.toast.err(this.t('copilot.error'));
    } catch { /* interceptor */ } finally { this.busy.set(false); }
  }

  formatDt(iso: string) {
    try { return new Date(iso).toLocaleString(this.i18n.isRtl ? 'ar-SA' : 'en-GB'); } catch { return iso; }
  }
}
