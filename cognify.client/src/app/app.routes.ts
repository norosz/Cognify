import { Routes } from '@angular/router';
import { LoginComponent } from './features/auth/login/login.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { authGuard } from './core/auth/auth.guard';
import { MainLayoutComponent } from './core/layout/main-layout/main-layout.component';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { ModuleDetailComponent } from './features/modules/module-detail/module-detail.component';

export const routes: Routes = [
    { path: 'login', component: LoginComponent },
    { path: 'register', component: RegisterComponent },
    {
        path: '',
        component: MainLayoutComponent,
        canActivate: [authGuard],
        children: [
            { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
            { path: 'dashboard', component: DashboardComponent },
            { path: 'statistics', loadComponent: () => import('./features/statistics/statistics.component').then(m => m.StatisticsComponent) },
            { path: 'modules/:id', component: ModuleDetailComponent },
            { path: 'modules/:moduleId/exams/:examAttemptId/results', loadComponent: () => import('./features/exams/exam-attempt-result/exam-attempt-result.component').then(m => m.ExamAttemptResultComponent) },
            { path: 'modules/:moduleId/exams/:examAttemptId/review', loadComponent: () => import('./features/exams/exam-attempt-review/exam-attempt-review.component').then(m => m.ExamAttemptReviewComponent) },
            { path: 'quizzes/:quizId', loadComponent: () => import('./features/quizzes/quiz-detail/quiz-detail.component').then(m => m.QuizDetailComponent) },
            { path: 'quizzes/:quizId/attempts/:attemptId/results', loadComponent: () => import('./features/quizzes/quiz-attempt-result/quiz-attempt-result.component').then(m => m.QuizAttemptResultComponent) },
            { path: 'quizzes/:quizId/attempts/:attemptId/review', loadComponent: () => import('./features/quizzes/quiz-attempt-review/quiz-attempt-review.component').then(m => m.QuizAttemptReviewComponent) },
            { path: 'pending', loadComponent: () => import('./features/pending/pending.component').then(m => m.PendingComponent) },
            { path: 'profile', loadComponent: () => import('./features/profile/profile.component').then(m => m.ProfileComponent) }
        ]
    },
    { path: '**', redirectTo: 'dashboard' }
];
