import { Component, Inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatExpansionModule } from '@angular/material/expansion';
import { QuizService } from '../../services/quiz.service';
import { QuestionDto } from '../../../../core/models/quiz.models';

@Component({
  selector: 'app-quiz-generation',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatExpansionModule
  ],
  templateUrl: './quiz-generation.component.html',
  styleUrl: './quiz-generation.component.scss'
})
export class QuizGenerationComponent {
  loading = signal<boolean>(true);
  questions = signal<QuestionDto[]>([]);
  error = signal<string | null>(null);

  constructor(
    private quizService: QuizService,
    private dialogRef: MatDialogRef<QuizGenerationComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { noteId: string }
  ) {
    this.generate();
  }

  generate() {
    this.loading.set(true);
    this.error.set(null);

    this.quizService.generateQuestions(this.data.noteId).subscribe({
      next: (qs) => {
        this.questions.set(qs);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set('Failed to generate questions. Please try again.');
        this.loading.set(false);
        console.error(err);
      }
    });
  }

  save() {
    this.loading.set(true);
    const dto = {
      noteId: this.data.noteId,
      questions: this.questions()
    };

    this.quizService.createQuestionSet(dto).subscribe({
      next: (res) => {
        this.dialogRef.close(res);
      },
      error: (err) => {
        this.error.set('Failed to save quiz.');
        this.loading.set(false);
      }
    });
  }

  close() {
    this.dialogRef.close();
  }
}
