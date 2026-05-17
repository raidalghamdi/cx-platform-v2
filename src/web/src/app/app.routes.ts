import { Routes } from '@angular/router';
import { authGuard, roleGuard } from './core/guards/auth.guard';
import { AppShellComponent } from './layout/app-shell.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'login' },
  {
    path: 'login',
    loadComponent: () => import('./features/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: '',
    component: AppShellComponent,
    canActivate: [authGuard],
    children: [
      {
        path: 'dashboard',
        data: { pageKey: '/dashboard' },
        canActivate: [roleGuard],
        loadComponent: () =>
          import('./features/dashboard/dashboard.component').then((m) => m.DashboardComponent),
      },
      {
        path: 'complaints',
        data: { pageKey: '/complaints' },
        canActivate: [roleGuard],
        loadComponent: () =>
          import('./features/complaints/complaints.component').then((m) => m.ComplaintsComponent),
      },
      {
        path: 'inbox',
        data: { pageKey: '/inbox' },
        canActivate: [roleGuard],
        loadComponent: () =>
          import('./features/inbox/inbox.component').then((m) => m.InboxComponent),
      },
      {
        path: 'admin',
        data: { pageKey: '/admin' },
        canActivate: [roleGuard],
        loadComponent: () =>
          import('./features/admin/admin.component').then((m) => m.AdminComponent),
      },
    ],
  },
  { path: '**', redirectTo: 'login' },
];
