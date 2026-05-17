import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.isAuthed()) return true;
  router.navigateByUrl('/login');
  return false;
};

// Page-level RBAC. Reads the page key from the route data.
export const roleGuard: CanActivateFn = (route) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const pageKey = (route.data?.['pageKey'] as string) ?? '';
  if (!pageKey) return true;
  if (auth.allowedPage(pageKey)) return true;
  router.navigateByUrl(auth.user()?.landing ?? '/dashboard');
  return false;
};
