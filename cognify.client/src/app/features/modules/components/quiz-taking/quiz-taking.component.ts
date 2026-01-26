import { Component, Inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatRadioModule } from '@angular/material/radio';
import { MatIconModule } from '@angular/material/icon';
import { FormsModule } from '@angular/forms';
import { QuizService } from '../../services/quiz.service';
import { QuestionSetDto, SubmitAttemptDto, AttemptDto } from '../../../../core/models/quiz.models';

@Component({
  selector: 'app-quiz-taking',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatRadioModule,
    MatIconModule,
    FormsModule
  ],
  templateUrl: './quiz-taking.component.html',
  styleUrl: './quiz-taking.component.scss'
})
export class QuizTakingComponent {
  questionSet = signal<QuestionSetDto | null>(null);
  answers: { [key: string]: string } = {}; // questionId -> answer

  submitting = signal<boolean>(false);
  result = signal<AttemptDto | null>(null);
  error = signal<string | null>(null);

  constructor(
    private quizService: QuizService,
    private dialogRef: MatDialogRef<QuizTakingComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { questionSetId: string, questionSet?: QuestionSetDto }
  ) {
    if (data.questionSet) {
      this.questionSet.set(data.questionSet);
    } else {
      this.loadQuiz(data.questionSetId);
    }
  }

  loadQuiz(id: string) {
    this.quizService.getQuestionSet(id).subscribe({
      next: (qs) => this.questionSet.set(qs),
      error: (err) => this.error.set('Failed to load quiz.')
    });
  }

  submit() {
    const qs = this.questionSet();
    if (!qs) return;

    this.submitting.set(true);
    const dto: SubmitAttemptDto = {
      questionSetId: qs.id,
      answers: this.answers
    };

    this.quizService.submitAttempt(dto).subscribe({
      next: (attempt) => {
        this.result.set(attempt);
        this.submitting.set(false);
      },
      error: (err) => {
        this.error.set('Failed to submit attempt.');
        this.submitting.set(false);
      }
    });
  }

  close() {
    this.dialogRef.close(this.result() ? true : false);
  }

  getScoreColor(score: number): string {
    if (score >= 80) return 'green';
    if (score >= 50) return 'orange';
    return 'red';
  }
}
