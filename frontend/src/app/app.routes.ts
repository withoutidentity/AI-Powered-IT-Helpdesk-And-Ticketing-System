import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { LoginPageComponent } from './features/auth/feature/login-page.component';
import { RegisterPageComponent } from './features/auth/feature/register-page.component';
import { ChatPageComponent } from './features/chat/feature/chat-page.component';
import { PlaceholderPageComponent } from './features/dashboard/feature/placeholder-page.component';
import { TicketListPageComponent } from './features/tickets/feature/ticket-list-page.component';

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
    component: ChatPageComponent,
    canActivate: [authGuard],
  },
  {
    path: 'tickets',
    component: TicketListPageComponent,
    canActivate: [authGuard],
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

