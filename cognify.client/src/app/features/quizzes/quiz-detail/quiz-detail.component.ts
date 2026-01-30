import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { QuizService } from '../../modules/services/quiz.service';
import { forkJoin } from 'rxjs';
import { QuizDto, QuizStatsDto, AttemptDto } from '../../../core/models/quiz.models';
import { QuizTakingComponent } from '../../modules/components/quiz-taking/quiz-taking.component';

@Component({
  selector: 'app-quiz-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule
  ],
  templateUrl: './quiz-detail.component.html',
  styleUrl: './quiz-detail.component.scss'
})
export class QuizDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private quizService = inject(QuizService);
  private dialog = inject(MatDialog);

  quiz = signal<QuizDto | null>(null);
  stats = signal<QuizStatsDto | null>(null);
  attempts = signal<AttemptDto[]>([]);
  loading = signal<boolean>(false);
  error = signal<string | null>(null);
  returnTo = signal<string>('/dashboard');

  ngOnInit(): void {
    const quizId = this.route.snapshot.paramMap.get('quizId');
    const returnTo = this.route.snapshot.queryParamMap.get('returnTo');
    if (returnTo) {
      this.returnTo.set(returnTo);
    }
    if (!quizId) {
      this.error.set('Quiz not found.');
      return;
    }

    this.loading.set(true);
    forkJoin({
      quiz: this.quizService.getQuiz(quizId),
      stats: this.quizService.getQuizStats(quizId),
      attempts: this.quizService.getAttempts(quizId)
    }).subscribe({
      next: ({ quiz, stats, attempts }) => {
        this.quiz.set(quiz);
        this.stats.set(stats);
        this.attempts.set(attempts);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load quiz details.');
        this.loading.set(false);
      }
    });
  }

  takeQuiz() {
    const quiz = this.quiz();
    if (!quiz) {
      return;
    }

    this.dialog.open(QuizTakingComponent, {
      width: '600px',
      data: { quiz }
    }).afterClosed().subscribe(() => {
      this.quizService.getQuizStats(quiz.id).subscribe({
        next: (stats) => this.stats.set(stats)
      });
      this.quizService.getAttempts(quiz.id).subscribe({
        next: (attempts) => this.attempts.set(attempts)
      });
    });
  }
}
