import { Routes } from '@angular/router';

export const AUTH_ROUTES: Routes = [
  { path: 'login', loadComponent: () => import('./login/login.page').then(module => module.LoginPage) },
  { path: 'register', loadComponent: () => import('./register/register.page').then(module => module.RegisterPage) },
  { path: 'forgot-password', loadComponent: () => import('./forgot-password/forgot-password.page').then(module => module.ForgotPasswordPage) },
  { path: 'reset-password', loadComponent: () => import('./reset-password/reset-password.page').then(module => module.ResetPasswordPage) },
];
