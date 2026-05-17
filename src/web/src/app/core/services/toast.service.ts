import { Injectable, signal } from '@angular/core';

export type ToastKind = 'info' | 'ok' | 'err';
export interface Toast { id: number; kind: ToastKind; title: string; body?: string; }

@Injectable({ providedIn: 'root' })
export class ToastService {
  private nextId = 1;
  readonly toasts = signal<Toast[]>([]);

  show(title: string, opts?: { kind?: ToastKind; body?: string; ttl?: number }) {
    const t: Toast = { id: this.nextId++, kind: opts?.kind ?? 'info', title, body: opts?.body };
    this.toasts.update((list) => [...list, t]);
    const ttl = opts?.ttl ?? 3500;
    setTimeout(() => this.dismiss(t.id), ttl);
  }

  ok(title: string, body?: string) { this.show(title, { kind: 'ok', body }); }
  err(title: string, body?: string) { this.show(title, { kind: 'err', body }); }

  dismiss(id: number) {
    this.toasts.update((list) => list.filter((t) => t.id !== id));
  }
}
