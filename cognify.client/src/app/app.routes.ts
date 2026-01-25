import { Routes } from '@angular/router';
import { LoginComponent } from './features/auth/login/login.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
    { path: 'login', component: LoginComponent },
    { path: 'register', component: RegisterComponent },
    {
        path: '',
        canActivate: [authGuard],
        children: [
            // Future protected routes (Dashboard, etc.)
        ]
    },
    { path: '**', redirectTo: 'login' }
];
