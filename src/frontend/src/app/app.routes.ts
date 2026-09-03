import { Routes } from '@angular/router';
import { authGuard } from './auth-guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./login').then((m) => m.Login),
  },
  {
    path: 'accounts',
    loadComponent: () => import('./accounts').then((m) => m.Accounts),
    canActivate: [authGuard],
  },
  {
    path: 'transfer',
    loadComponent: () => import('./transfer').then((m) => m.Transfer),
    canActivate: [authGuard],
  },
  { path: '', redirectTo: 'accounts', pathMatch: 'full' },
  { path: '**', redirectTo: 'accounts' },
];
