import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { QuizService } from '../../modules/services/quiz.service';
import { AttemptReviewDto, AttemptReviewItemDto } from '../../../core/models/quiz.models';
import { AiService } from '../../../core/services/ai.service';
import { ExplainMistakeResponse } from '../../../core/models/ai.models';
import { MarkdownLatexPipe } from '../../../shared/pipes/markdown-latex.pipe';

@Component({
  selector: 'app-quiz-attempt-review',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MarkdownLatexPipe
  ],
  templateUrl: './quiz-attempt-review.component.html',
  styleUrl: './quiz-attempt-review.component.scss'
})
export class QuizAttemptReviewComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private quizService = inject(QuizService);
  private aiService = inject(AiService);

  review = signal<AttemptReviewDto | null>(null);
  loading = signal<boolean>(false);
  error = signal<string | null>(null);
  explanations = signal<Record<string, ExplainMistakeResponse | null>>({});
  explanationLoading = signal<Record<string, boolean>>({});

  ngOnInit(): void {
    const quizId = this.route.snapshot.paramMap.get('quizId');
    const attemptId = this.route.snapshot.paramMap.get('attemptId');
    if (!quizId || !attemptId) {
      this.error.set('Review not found.');
      return;
    }

    this.loading.set(true);
    this.quizService.getAttemptReview(quizId, attemptId).subscribe({
      next: (review) => {
        this.review.set(review);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load review.');
        this.loading.set(false);
      }
    });
  }

  getExplanation(questionId: string) {
    return this.explanations()[questionId];
  }

  isExplanationLoading(questionId: string) {
    return this.explanationLoading()[questionId] === true;
  }

  requestExplanation(item: AttemptReviewItemDto) {
    if (!item.userAnswer || !item.correctAnswer) {
      return;
    }

    const current = this.explanations();
    if (current[item.questionId]) {
      return;
    }

    this.explanationLoading.set({
      ...this.explanationLoading(),
      [item.questionId]: true
    });

    this.aiService.explainMistake({
      questionPrompt: item.prompt,
      userAnswer: item.userAnswer,
      correctAnswer: item.correctAnswer,
      detectedMistakes: item.detectedMistakes || []
    }).subscribe({
      next: (response) => {
        this.explanations.set({
          ...this.explanations(),
          [item.questionId]: response
        });
        this.explanationLoading.set({
          ...this.explanationLoading(),
          [item.questionId]: false
        });
      },
      error: () => {
        this.explanationLoading.set({
          ...this.explanationLoading(),
          [item.questionId]: false
        });
      }
    });
  }

  trackByQuestion(_: number, item: AttemptReviewItemDto) {
    return item.questionId;
  }
}
