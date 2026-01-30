import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { ExamAttemptService } from '../../../core/services/exam-attempt.service';
import { FinalExamService } from '../../../core/services/final-exam.service';
import { AttemptReviewDto } from '../../../core/models/quiz.models';
import { FinalExamDto } from '../../../core/models/exam.models';
import { ExamTakingComponent } from '../exam-taking/exam-taking.component';

@Component({
  selector: 'app-exam-attempt-result',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule
  ],
  templateUrl: './exam-attempt-result.component.html',
  styleUrl: './exam-attempt-result.component.scss'
})
export class ExamAttemptResultComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private examAttemptService = inject(ExamAttemptService);
  private finalExamService = inject(FinalExamService);
  private dialog = inject(MatDialog);

  review = signal<AttemptReviewDto | null>(null);
  finalExam = signal<FinalExamDto | null>(null);
  loading = signal<boolean>(false);
  error = signal<string | null>(null);
  moduleId = signal<string | null>(null);

  ngOnInit(): void {
    const moduleId = this.route.snapshot.paramMap.get('moduleId');
    const attemptId = this.route.snapshot.paramMap.get('examAttemptId');
    if (!moduleId || !attemptId) {
      this.error.set('Attempt not found.');
      return;
    }

    this.moduleId.set(moduleId);

    this.loading.set(true);

    this.examAttemptService.getExamAttemptReview(moduleId, attemptId).subscribe({
      next: (review) => {
        this.review.set(review);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load exam result.');
        this.loading.set(false);
      }
    });

    this.finalExamService.getFinalExam(moduleId).subscribe({
      next: (exam) => this.finalExam.set(exam),
      error: () => null
    });
  }

  retakeExam() {
    const moduleId = this.moduleId();
    const exam = this.finalExam();
    if (!moduleId || !exam?.currentQuizId) return;

    const dialogRef = this.dialog.open(ExamTakingComponent, {
      width: '700px',
      data: { moduleId, quizId: exam.currentQuizId }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result?.examAttemptId) {
        this.review.set(null);
        this.loading.set(true);
        this.examAttemptService.getExamAttemptReview(moduleId, result.examAttemptId).subscribe({
          next: (review) => {
            this.review.set(review);
            this.loading.set(false);
          },
          error: () => {
            this.error.set('Failed to load exam result.');
            this.loading.set(false);
          }
        });
      }
    });
  }
}
