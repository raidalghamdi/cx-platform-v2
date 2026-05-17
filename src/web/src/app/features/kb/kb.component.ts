import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { I18nService } from '../../core/services/i18n.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { KbArticleDto, KbStatus, UpsertKbArticleRequest } from '../../core/models/types';

@Component({
  selector: 'app-kb',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <h1 class="title-h1">{{ t('kb.title') }}</h1>
    <p class="muted" style="margin: 0 0 16px">{{ t('kb.subtitle') }}</p>

    <div class="row row--wrap" style="gap: 12px; margin-bottom: 16px">
      <input class="input" style="max-width: 320px" [(ngModel)]="q" (ngModelChange)="load()" [placeholder]="t('kb.search')" />
      <span class="spacer"></span>
      <button *ngIf="canEdit" class="btn-primary" (click)="newArticle()">{{ t('kb.newArticle') }}</button>
    </div>

    <div class="kb-grid">
      <aside class="card categories">
        <p class="eyebrow" style="margin-bottom: 10px">{{ t('kb.categories') }}</p>
        <button class="cat-link" [class.active]="category() === null" (click)="setCategory(null)">{{ t('kb.allCategories') }} <span class="count">{{ allCount() }}</span></button>
        <button *ngFor="let c of categories()" class="cat-link" [class.active]="category() === c.key" (click)="setCategory(c.key)">
          {{ c.key }} <span class="count">{{ c.count }}</span>
        </button>
      </aside>

      <section class="articles">
        <article *ngFor="let a of filteredArticles()" class="card kb-row" (click)="open(a)">
          <div class="row" style="gap: 10px; margin-bottom: 4px">
            <strong>{{ i18n.pickPair(a.titleEn, a.titleAr) }}</strong>
            <span class="spacer"></span>
            <span class="badge" [class]="statusClass(a.status)">{{ t('kb.status.' + a.status) }}</span>
          </div>
          <p class="muted" style="margin: 0 0 4px; font-size: 12px">
            {{ a.category }} · {{ formatDate(a.updatedAt) }}
          </p>
          <p class="muted snippet">{{ snippet(i18n.pickPair(a.bodyEn, a.bodyAr)) }}</p>
        </article>
        <p *ngIf="!filteredArticles().length" class="muted">{{ t('kb.empty') }}</p>
      </section>
    </div>

    <!-- Editor drawer -->
    <div *ngIf="editor()" class="drawer-backdrop" (click)="closeEditor()">
      <div class="drawer" (click)="$event.stopPropagation()">
        <div class="row" style="margin-bottom: 14px">
          <span class="badge badge--navy">{{ t('kb.editor.title') }}</span>
          <span class="spacer"></span>
          <button *ngIf="editor()!.id && canEdit" class="btn-ghost" (click)="remove()">{{ t('common.cancel') }} → 🗑</button>
          <button class="btn-ghost" (click)="closeEditor()">{{ t('common.close') }}</button>
        </div>

        <label class="field">
          <span>{{ t('kb.field.titleEn') }}</span>
          <input class="input" [(ngModel)]="form.titleEn" />
        </label>
        <label class="field" style="margin-top: 10px">
          <span>{{ t('kb.field.titleAr') }}</span>
          <input class="input" [(ngModel)]="form.titleAr" dir="rtl" />
        </label>

        <div class="row" style="margin-top: 10px; gap: 10px">
          <label class="field" style="flex: 1">
            <span>{{ t('kb.field.category') }}</span>
            <input class="input" [(ngModel)]="form.category" />
          </label>
          <label class="field" style="flex: 1">
            <span>{{ t('kb.field.status') }}</span>
            <select class="input" [(ngModel)]="form.status">
              <option value="draft">{{ t('kb.status.draft') }}</option>
              <option value="published">{{ t('kb.status.published') }}</option>
              <option value="retired">{{ t('kb.status.retired') }}</option>
            </select>
          </label>
        </div>

        <label class="field" style="margin-top: 10px">
          <span>{{ t('kb.field.bodyEn') }}</span>
          <textarea class="input" [(ngModel)]="form.bodyEn" rows="6"></textarea>
        </label>
        <label class="field" style="margin-top: 10px">
          <span>{{ t('kb.field.bodyAr') }}</span>
          <textarea class="input" [(ngModel)]="form.bodyAr" rows="6" dir="rtl"></textarea>
        </label>

        <div style="margin-top: 14px; display:flex; justify-content:flex-end; gap: 8px">
          <button class="btn-primary" [disabled]="!canEdit || saving()" (click)="save()">
            {{ saving() ? t('common.loading') : t('common.save') }}
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .kb-grid { display: grid; grid-template-columns: 220px 1fr; gap: 16px; align-items: start; }
    @media (max-width: 800px) { .kb-grid { grid-template-columns: 1fr; } }
    .categories { padding: 14px; }
    .cat-link {
      display: flex; align-items: center; gap: 8px; width: 100%; padding: 7px 10px;
      border: none; background: transparent; cursor: pointer;
      border-radius: 8px; text-align: start; font-size: 13px;
      color: var(--gac-ink); margin-bottom: 2px;
    }
    .cat-link:hover { background: rgba(0,105,167,0.06); }
    .cat-link.active { background: var(--gac-navy); color: #fff; }
    .cat-link.active .count { background: rgba(255,255,255,0.18); color: #fff; }
    .count { margin-inline-start: auto; font-size: 11px; padding: 1px 8px; border-radius: var(--radius-pill); background: rgba(0,0,0,0.06); color: var(--gac-muted); }
    .articles { display: flex; flex-direction: column; gap: 10px; }
    .kb-row { padding: 14px 16px; cursor: pointer; transition: transform 80ms ease; }
    .kb-row:hover { transform: translateY(-1px); }
    .snippet { margin: 0; font-size: 13px; overflow: hidden; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; }
    .field { display: flex; flex-direction: column; gap: 4px; }
    .field > span { font-size: 12px; font-weight: 600; color: var(--gac-navy); }
  `],
})
export class KbComponent {
  private api = inject(ApiService);
  private toast = inject(ToastService);
  private auth = inject(AuthService);
  i18n = inject(I18nService);

  q = '';
  articles = signal<KbArticleDto[]>([]);
  category = signal<string | null>(null);
  editor = signal<KbArticleDto | { id: 0 } | null>(null);
  saving = signal(false);

  form: UpsertKbArticleRequest = { titleEn: '', titleAr: '', category: '', bodyEn: '', bodyAr: '', status: 'draft' };

  // Categories derived from current article set so the tree always matches reality.
  categories = computed(() => {
    const map = new Map<string, number>();
    for (const a of this.articles()) {
      if (this.q.trim()) {
        const hay = (a.titleEn + a.titleAr + a.bodyEn + a.bodyAr).toLowerCase();
        if (!hay.includes(this.q.toLowerCase())) continue;
      }
      const k = a.category || '—';
      map.set(k, (map.get(k) ?? 0) + 1);
    }
    return Array.from(map.entries()).map(([key, count]) => ({ key, count })).sort((a, b) => a.key.localeCompare(b.key));
  });

  filteredArticles = computed(() => this.articles().filter((a) => !this.category() || a.category === this.category()));
  allCount = computed(() => this.articles().length);

  get canEdit(): boolean {
    const r = this.auth.user()?.role ?? '';
    return ['admin', 'supervisor', 'agent'].includes(r);
  }

  constructor() { this.load(); }

  t(k: string) { return this.i18n.t(k); }

  async load() {
    try {
      const list = await this.api.kbList({ q: this.q.trim() || undefined });
      this.articles.set(list);
    } catch { /* interceptor */ }
  }

  setCategory(c: string | null) { this.category.set(c); }

  // Hide the explicit articles() projection inside the template by overriding here.
  // (We expose `articles()` directly above but filter via filteredArticles in the loop.)
  open(a: KbArticleDto) {
    this.editor.set(a);
    this.form = { titleEn: a.titleEn, titleAr: a.titleAr, category: a.category, bodyEn: a.bodyEn, bodyAr: a.bodyAr, status: a.status };
  }
  newArticle() {
    if (!this.canEdit) return;
    this.editor.set({ id: 0 });
    this.form = { titleEn: '', titleAr: '', category: this.category() ?? '', bodyEn: '', bodyAr: '', status: 'draft' };
  }
  closeEditor() { this.editor.set(null); }

  async save() {
    if (!this.canEdit) return;
    if (!this.form.titleEn.trim() || !this.form.titleAr.trim()) return;
    this.saving.set(true);
    try {
      const ed = this.editor()!;
      const id = (ed as any).id as number;
      const saved = id ? await this.api.kbUpdate(id, this.form) : await this.api.kbCreate(this.form);
      this.toast.ok(this.t('admin.saved'));
      this.editor.set(null);
      await this.load();
    } catch { /* interceptor */ } finally { this.saving.set(false); }
  }

  async remove() {
    const ed = this.editor();
    const id = (ed as any)?.id as number;
    if (!id) return;
    try {
      await this.api.kbDelete(id);
      this.toast.ok(this.t('kb.deleted'));
      this.editor.set(null);
      await this.load();
    } catch { /* interceptor */ }
  }

  snippet(s: string) { return s.length > 220 ? s.slice(0, 220) + '…' : s; }
  formatDate(iso: string) { try { return new Date(iso).toISOString().slice(0, 10); } catch { return iso; } }
  statusClass(s: KbStatus) {
    return s === 'published' ? 'badge--green' : s === 'draft' ? 'badge--amber' : 'badge--slate';
  }
}
