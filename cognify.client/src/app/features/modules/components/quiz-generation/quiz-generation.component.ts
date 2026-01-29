import { Component, Inject, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatSliderModule } from '@angular/material/slider';
import { FormsModule } from '@angular/forms';
import { QuizService } from '../../services/quiz.service';
import { AiService } from '../../../../core/services/ai.service';
import { QuestionDto } from '../../../../core/models/quiz.models';
import { QuestionType, GenerateQuestionsRequest } from '../../../../core/models/ai.models';
import { MatInputModule } from '@angular/material/input';

@Component({
  selector: 'app-quiz-generation',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatExpansionModule,
    MatFormFieldModule,
    MatSelectModule,
    MatSliderModule,
    MatInputModule,
    FormsModule
  ],
  templateUrl: './quiz-generation.component.html',
  styleUrl: './quiz-generation.component.scss'
})
export class QuizGenerationComponent {
  loading = signal<boolean>(false);
  questions = signal<QuestionDto[]>([]);
  error = signal<string | null>(null);

  // Form Controls
  selectedType: QuestionType = QuestionType.MultipleChoice;
  difficulty = 2;
  count = 5;

  questionTypes = Object.values(QuestionType);

  private quizService = inject(QuizService);
  private aiService = inject(AiService);

  constructor(
    private dialogRef: MatDialogRef<QuizGenerationComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { noteId: string }
  ) {
    // Initial generation? Or wait for user?
    // Let's wait for user to configure settings first.
    // But maybe default generate once?
    // I'll auto-generate on open with defaults.
    this.generate();
  }

  generate() {
    this.loading.set(true);
    this.error.set(null);
    this.questions.set([]);

    const req: GenerateQuestionsRequest = {
      noteId: this.data.noteId,
      type: this.selectedType,
      difficulty: this.difficulty,
      count: this.count
    };

    this.aiService.generateQuestions(req).subscribe({
      next: (generated) => {
        const dtos: QuestionDto[] = generated.map(g => {
          // For Matching, use pairs as options so they are saved to backend
          const options = (g.type === QuestionType.Matching && g.pairs) ? g.pairs : (g.options || []);

          return {
            prompt: g.text,
            type: g.type as any, // Cast to any to satisfy the complex string union mapping
            options: options, // Saved to DB
            correctAnswer: g.correctAnswer || '',
            explanation: g.explanation || '',
            pairs: g.pairs // Local use
          };
        });
        this.questions.set(dtos);
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
    const difficultyLabel = this.getDifficultyLabel(this.difficulty);
    const dto = {
      noteId: this.data.noteId,
      title: 'Generated Quiz',
      questions: this.questions(),
      difficulty: difficultyLabel
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

  private getDifficultyLabel(value: number): string {
    return value switch
    {
      1 => 'Beginner',
      3 => 'Advanced',
      _ => 'Intermediate'
    };
  }
}
