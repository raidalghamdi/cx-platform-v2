import { Routes } from '@angular/router';
import { authGuard, roleGuard } from './core/guards/auth.guard';
import { AppShellComponent } from './layout/app-shell.component';

export const routes: Routes = [
  // Phase 2 — root now defaults to the public landing page.
  { path: '', pathMatch: 'full', redirectTo: 'landing' },
  {
    path: 'landing',
    loadComponent: () => import('./features/landing/landing.component').then((m) => m.LandingComponent),
  },
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
      // Phase 2 — the final 6 authenticated routes.
      {
        path: 'about',
        data: { pageKey: '/about' },
        canActivate: [roleGuard],
        loadComponent: () =>
          import('./features/about/about.component').then((m) => m.AboutComponent),
      },
      {
        path: 'architecture',
        data: { pageKey: '/architecture' },
        canActivate: [roleGuard],
        loadComponent: () =>
          import('./features/architecture/architecture.component').then((m) => m.ArchitectureComponent),
      },
      {
        path: 'portal',
        data: { pageKey: '/portal' },
        canActivate: [roleGuard],
        loadComponent: () =>
          import('./features/portal/portal.component').then((m) => m.PortalComponent),
      },
      {
        path: 'copilot',
        data: { pageKey: '/copilot' },
        canActivate: [roleGuard],
        loadComponent: () =>
          import('./features/copilot/copilot.component').then((m) => m.CopilotComponent),
      },
      {
        path: 'audit',
        data: { pageKey: '/audit' },
        canActivate: [roleGuard],
        loadComponent: () =>
          import('./features/audit/audit.component').then((m) => m.AuditComponent),
      },
      {
        path: 'automation',
        data: { pageKey: '/automation' },
        canActivate: [roleGuard],
        loadComponent: () =>
          import('./features/automation/automation.component').then((m) => m.AutomationComponent),
      },
    ],
  },
  // Unknown routes fall back to the public landing page.
  { path: '**', redirectTo: 'landing' },
];
