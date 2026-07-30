import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { LoginPageComponent } from './features/auth/feature/login-page.component';
import { RegisterPageComponent } from './features/auth/feature/register-page.component';
import { PlaceholderPageComponent } from './features/dashboard/feature/placeholder-page.component';

export const routes: Routes = [
  {
    path: 'login',
    component: LoginPageComponent,
  },
  {
    path: 'register',
    component: RegisterPageComponent,
  },
  {
    path: '',
    component: PlaceholderPageComponent,
    canActivate: [authGuard],
    data: {
      eyebrow: 'Support chat',
      title: 'Chat workspace',
      summary: 'Ask IT questions, track support context, and continue into ticket creation when an issue needs human follow-up.',
    },
  },
  {
    path: 'tickets',
    component: PlaceholderPageComponent,
    canActivate: [authGuard],
    data: {
      eyebrow: 'Ticket queue',
      title: 'Tickets',
      summary: 'Review assigned work, open support requests, and follow resolution status from one protected workspace.',
    },
  },
  {
    path: 'dashboard',
    component: PlaceholderPageComponent,
    canActivate: [authGuard],
    data: {
      eyebrow: 'Operations',
      title: 'Dashboard',
      summary: 'Monitor open work, response health, and operational trends for the IT support queue.',
    },
  },
  {
    path: 'knowledge-base',
    component: PlaceholderPageComponent,
    canActivate: [authGuard],
    data: {
      eyebrow: 'Knowledge base',
      title: 'Documents',
      summary: 'Manage internal support documents that will ground AI answers for employees and IT staff.',
    },
  },
  {
    path: '**',
    redirectTo: '',
  },
];