import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { ToastService } from './core/services/toast.service';
import { I18nService } from './core/services/i18n.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet],
  template: `
    <router-outlet></router-outlet>
    <div class="toast-container" *ngIf="toasts.toasts().length">
      <div *ngFor="let t of toasts.toasts()" class="toast" [class.toast--err]="t.kind === 'err'" [class.toast--ok]="t.kind === 'ok'">
        <div style="font-weight:600">{{ t.title }}</div>
        <div *ngIf="t.body" style="opacity:.8; font-size:12px; margin-top:4px">{{ t.body }}</div>
      </div>
    </div>
  `,
  styles: [`:host { display: contents; }`],
})
export class AppComponent {
  toasts = inject(ToastService);
  private i18n = inject(I18nService);

  constructor() {
    // Apply the initial direction from default lang signal.
    this.i18n.set(this.i18n.lang());
  }
}
