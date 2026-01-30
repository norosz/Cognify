import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { QuizService } from '../../modules/services/quiz.service';
import { forkJoin } from 'rxjs';
import { QuizDto, QuizStatsDto, AttemptDto } from '../../../core/models/quiz.models';
import { QuizTakingComponent } from '../../modules/components/quiz-taking/quiz-taking.component';
import { CategoryHistoryBatchDto } from '../../../core/models/category.models';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-quiz-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatAutocompleteModule
  ],
  templateUrl: './quiz-detail.component.html',
  styleUrl: './quiz-detail.component.scss'
})
export class QuizDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private quizService = inject(QuizService);
  private dialog = inject(MatDialog);
  private notificationService = inject(NotificationService);

  quiz = signal<QuizDto | null>(null);
  stats = signal<QuizStatsDto | null>(null);
  attempts = signal<AttemptDto[]>([]);
  loading = signal<boolean>(false);
  error = signal<string | null>(null);
  returnTo = signal<string>('/dashboard');
  categoryInput = signal<string>('');
  categoryOptions = signal<CategoryOption[]>([]);
  categoryHistoryLoading = signal<boolean>(false);
  categoryHistoryCursor = signal<string | null>(null);
  categoryHistoryHasMore = signal<boolean>(false);
  suggestingCategory = signal<boolean>(false);
  private categoryBatches = signal<CategoryHistoryBatchDto[]>([]);

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
        this.categoryInput.set(quiz.categoryLabel ?? '');
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

  onCategoryFocus() {
    this.loadCategoryHistory(true);
  }

  selectCategory(label: string) {
    this.categoryInput.set(label);
  }

  canSuggestCategory(): boolean {
    const quiz = this.quiz();
    if (!quiz) return false;
    return (quiz.questionCount ?? 0) >= 3;
  }

  suggestCategory() {
    const quiz = this.quiz();
    if (!quiz || this.suggestingCategory() || !this.canSuggestCategory()) return;

    this.suggestingCategory.set(true);
    this.quizService.suggestCategories(quiz.id, 5).subscribe({
      next: () => {
        this.loadCategoryHistory(true);
        this.suggestingCategory.set(false);
      },
      error: (err) => {
        console.error(err);
        this.suggestingCategory.set(false);
      }
    });
  }

  applyCategory() {
    const quiz = this.quiz();
    const label = this.categoryInput().trim();
    if (!quiz || !label) return;

    this.quizService.setCategory(quiz.id, label).subscribe({
      next: () => {
        this.notificationService.success(`Quiz category set to ${label}.`);
        this.quizService.getQuiz(quiz.id).subscribe({
          next: (data) => this.quiz.set(data)
        });
        this.loadCategoryHistory(true);
      },
      error: () => this.notificationService.error('Failed to update quiz category.')
    });
  }

  loadCategoryHistory(reset: boolean) {
    const quiz = this.quiz();
    if (!quiz || this.categoryHistoryLoading()) return;

    const cursor = reset ? null : this.categoryHistoryCursor();
    if (!reset && !cursor) return;

    this.categoryHistoryLoading.set(true);
    this.quizService.getCategoryHistory(quiz.id, 10, cursor ?? undefined).subscribe({
      next: (response) => {
        const batches = reset ? response.items : [...this.categoryBatches(), ...response.items];
        this.categoryBatches.set(batches);
        this.categoryHistoryCursor.set(response.nextCursor ?? null);
        this.categoryHistoryHasMore.set(!!response.nextCursor);
        this.categoryOptions.set(this.buildCategoryOptions(batches));
        this.categoryHistoryLoading.set(false);
      },
      error: () => {
        this.categoryHistoryLoading.set(false);
      }
    });
  }

  private buildCategoryOptions(batches: CategoryHistoryBatchDto[]): CategoryOption[] {
    const map = new Map<string, CategoryOption>();

    for (const batch of batches) {
      for (const item of batch.items) {
        const existing = map.get(item.label);
        const createdAt = new Date(batch.createdAt).getTime();
        if (!existing) {
          map.set(item.label, {
            label: item.label,
            count: 1,
            source: batch.source,
            lastCreatedAt: createdAt
          });
        } else {
          existing.count += 1;
          if (createdAt >= existing.lastCreatedAt) {
            existing.lastCreatedAt = createdAt;
          }
          if (batch.source === 'Applied') {
            existing.source = 'Applied';
          }
        }
      }
    }

    return Array.from(map.values()).sort((a, b) => b.lastCreatedAt - a.lastCreatedAt);
  }
}

interface CategoryOption {
  label: string;
  count: number;
  source: 'AI' | 'Applied';
  lastCreatedAt: number;
}
