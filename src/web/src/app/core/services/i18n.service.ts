import { Injectable, signal } from '@angular/core';
import { STRINGS, Lang } from '../i18n/strings';

@Injectable({ providedIn: 'root' })
export class I18nService {
  // Signal so templates re-render on toggle.
  readonly lang = signal<Lang>('en');

  toggle() {
    const next: Lang = this.lang() === 'en' ? 'ar' : 'en';
    this.set(next);
  }

  set(lang: Lang) {
    this.lang.set(lang);
    if (typeof document !== 'undefined') {
      document.documentElement.lang = lang;
      document.documentElement.dir = lang === 'ar' ? 'rtl' : 'ltr';
    }
  }

  t(key: string): string {
    const entry = STRINGS[key];
    if (!entry) return key;
    return entry[this.lang()] ?? entry.en ?? key;
  }

  // Pick from a bilingual object the server returned.
  pick<T extends { en?: string; ar?: string }>(obj: T | null | undefined): string {
    if (!obj) return '';
    return (this.lang() === 'ar' ? obj.ar : obj.en) ?? '';
  }

  pickPair(en?: string | null, ar?: string | null): string {
    return (this.lang() === 'ar' ? ar : en) ?? en ?? ar ?? '';
  }

  get isRtl() { return this.lang() === 'ar'; }
}
