import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { Router } from '@angular/router';
import { ToastService } from '../services/toast.service';
import { AuthService } from '../services/auth.service';

// 401 → bounce to /login. Other errors → toast.
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(ToastService);
  const auth = inject(AuthService);
  const router = inject(Router);
  return next(req).pipe(
    catchError((err) => {
      if (err.status === 401 && !req.url.endsWith('/auth/login')) {
        auth.logout();
        router.navigateByUrl('/login');
      } else if (err.status >= 500) {
        toast.err('Server error', err?.error?.error ?? err?.message ?? 'Please retry shortly.');
      } else if (err.status === 403) {
        toast.err('Forbidden', 'Your role does not have access to that resource.');
      } else if (err.status === 429) {
        toast.err('Rate limit', 'Too many requests — try again in a minute.');
      }
      return throwError(() => err);
    }),
  );
};
