import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { QuizService } from '../../modules/services/quiz.service';
import { AttemptReviewDto, QuizDto } from '../../../core/models/quiz.models';
import { QuizTakingComponent } from '../../modules/components/quiz-taking/quiz-taking.component';

@Component({
  selector: 'app-quiz-attempt-result',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule
  ],
  templateUrl: './quiz-attempt-result.component.html',
  styleUrl: './quiz-attempt-result.component.scss'
})
export class QuizAttemptResultComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private quizService = inject(QuizService);
  private dialog = inject(MatDialog);

  quiz = signal<QuizDto | null>(null);
  review = signal<AttemptReviewDto | null>(null);
  loading = signal<boolean>(false);
  error = signal<string | null>(null);

  ngOnInit(): void {
    const quizId = this.route.snapshot.paramMap.get('quizId');
    const attemptId = this.route.snapshot.paramMap.get('attemptId');
    if (!quizId || !attemptId) {
      this.error.set('Attempt not found.');
      return;
    }

    this.loading.set(true);
    this.quizService.getQuiz(quizId).subscribe({
      next: (quiz) => this.quiz.set(quiz),
      error: () => this.error.set('Failed to load quiz.')
    });

    this.quizService.getAttemptReview(quizId, attemptId).subscribe({
      next: (review) => {
        this.review.set(review);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load attempt result.');
        this.loading.set(false);
      }
    });
  }

  retakeQuiz() {
    const quiz = this.quiz();
    if (!quiz) return;

    this.dialog.open(QuizTakingComponent, {
      width: '600px',
      data: { quiz }
    });
  }
}
