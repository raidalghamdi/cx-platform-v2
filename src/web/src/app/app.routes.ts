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
      // Phase 1 — Journeys list + detail share /journeys page-key.
      {
        path: 'journeys',
        data: { pageKey: '/journeys' },
        canActivate: [roleGuard],
        loadComponent: () =>
          import('./features/journeys/journeys.component').then((m) => m.JourneysComponent),
      },
      {
        path: 'journeys/:id',
        data: { pageKey: '/journeys' },
        canActivate: [roleGuard],
        loadComponent: () =>
          import('./features/journeys/journey-detail.component').then((m) => m.JourneyDetailComponent),
      },
      {
        path: 'voc',
        data: { pageKey: '/voc' },
        canActivate: [roleGuard],
        loadComponent: () =>
          import('./features/voc/voc.component').then((m) => m.VocComponent),
      },
      {
        path: 'kb',
        data: { pageKey: '/kb' },
        canActivate: [roleGuard],
        loadComponent: () =>
          import('./features/kb/kb.component').then((m) => m.KbComponent),
      },
      {
        path: 'programme',
        data: { pageKey: '/programme' },
        canActivate: [roleGuard],
        loadComponent: () =>
          import('./features/programme/programme.component').then((m) => m.ProgrammeComponent),
      },
      {
        path: 'governance',
        data: { pageKey: '/governance' },
        canActivate: [roleGuard],
        loadComponent: () =>
          import('./features/governance/governance.component').then((m) => m.GovernanceComponent),
      },
    ],
  },
  { path: '**', redirectTo: 'login' },
];
