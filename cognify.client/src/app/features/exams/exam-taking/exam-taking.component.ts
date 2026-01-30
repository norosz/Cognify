import { Component, Inject, signal } from '@angular/core';
import { CdkDragDrop, moveItemInArray } from '@angular/cdk/drag-drop';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatRadioModule } from '@angular/material/radio';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { FormsModule } from '@angular/forms';
import { DragDropModule } from '@angular/cdk/drag-drop';
import { QuizService } from '../../modules/services/quiz.service';
import { ExamAttemptService } from '../../../core/services/exam-attempt.service';
import { QuizDto } from '../../../core/models/quiz.models';
import { SubmitExamAttemptDto } from '../../../core/models/exam.models';
import { MarkdownLatexPipe } from '../../../shared/pipes/markdown-latex.pipe';

@Component({
  selector: 'app-exam-taking',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatRadioModule,
    MatIconModule,
    MatInputModule,
    MatFormFieldModule,
    FormsModule,
    DragDropModule,
    MarkdownLatexPipe,
    MatCheckboxModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './exam-taking.component.html',
  styleUrl: './exam-taking.component.scss'
})
export class ExamTakingComponent {
  quiz = signal<QuizDto | null>(null);
  answers: { [key: string]: string } = {};
  private startedAt = Date.now();

  submitting = signal<boolean>(false);
  error = signal<string | null>(null);

  orderingOptions: { [key: string]: string[] } = {};
  matchingState: { [key: string]: { terms: string[], definitions: string[], pairs: { [term: string]: string | null } } } = {};
  selectedTerm: string | null = null;
  selectedDef: string | null = null;

  constructor(
    private quizService: QuizService,
    private examAttemptService: ExamAttemptService,
    private dialogRef: MatDialogRef<ExamTakingComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { moduleId: string, quizId: string, quiz?: QuizDto }
  ) {
    if (data.quiz) {
      this.initQuiz(data.quiz);
    } else {
      this.loadQuiz(data.quizId);
    }
  }

  isDefMatched(questionId: string, def: string): boolean {
    const state = this.matchingState[questionId];
    if (!state) return false;
    return Object.values(state.pairs).includes(def);
  }

  getMatchedCount(questionId: string): number {
    const state = this.matchingState[questionId];
    if (!state) return 0;
    return Object.values(state.pairs).filter(v => v !== null).length;
  }

  loadQuiz(id: string) {
    this.quizService.getQuiz(id).subscribe({
      next: (qs) => this.initQuiz(qs),
      error: () => this.error.set('Failed to load exam.')
    });
  }

  initQuiz(qs: QuizDto) {
    this.quiz.set(qs);
    this.startedAt = Date.now();
    qs.questions.forEach(q => {
      if (q.type === 'Ordering' && q.options) {
        this.orderingOptions[q.id!] = [...q.options];
        this.answers[q.id!] = this.orderingOptions[q.id!].join('|');
      }
      else if (q.type === 'Matching' && q.options) {
        const terms: string[] = [];
        const definitions: string[] = [];
        q.options.forEach(pair => {
          const parts = pair.split(':');
          if (parts.length >= 2) {
            terms.push(parts[0].trim());
            definitions.push(parts.slice(1).join(':').trim());
          }
        });
        const shuffledDefs = [...definitions].sort(() => Math.random() - 0.5);

        this.matchingState[q.id!] = {
          terms: terms,
          definitions: shuffledDefs,
          pairs: {}
        };
        terms.forEach(t => this.matchingState[q.id!].pairs[t] = null);
      }
    });
  }

  drop(event: CdkDragDrop<string[]>, questionId: string) {
    if (this.orderingOptions[questionId]) {
      moveItemInArray(this.orderingOptions[questionId], event.previousIndex, event.currentIndex);
      this.answers[questionId] = this.orderingOptions[questionId].join('|');
    }
  }

  selectMatch(questionId: string, term: string, def: string) {
    const state = this.matchingState[questionId];

    Object.keys(state.pairs).forEach(t => {
      if (state.pairs[t] === def) state.pairs[t] = null;
    });

    state.pairs[term] = def;
    this.updateMatchingAnswer(questionId);

    this.selectedTerm = null;
    this.selectedDef = null;
  }

  onTermClick(questionId: string, term: string) {
    if (this.selectedDef) {
      this.selectMatch(questionId, term, this.selectedDef);
    } else {
      this.selectedTerm = term;
      this.selectedDef = null;
    }
  }

  onDefinitionClick(questionId: string, def: string) {
    if (this.selectedTerm) {
      this.selectMatch(questionId, this.selectedTerm, def);
    } else {
      this.selectedDef = def;
      this.selectedTerm = null;
    }
  }

  updateMatchingAnswer(questionId: string) {
    const state = this.matchingState[questionId];
    const pairs: string[] = [];
    state.terms.forEach(t => {
      if (state.pairs[t]) {
        pairs.push(`${t}:${state.pairs[t]}`);
      }
    });
    this.answers[questionId] = pairs.join('|');
  }

  submit() {
    const qs = this.quiz();
    if (!qs) return;

    this.submitting.set(true);
    const elapsedSeconds = Math.max(0, Math.round((Date.now() - this.startedAt) / 1000));
    const dto: SubmitExamAttemptDto = {
      moduleId: this.data.moduleId,
      quizId: qs.id,
      answers: this.answers,
      timeSpentSeconds: elapsedSeconds,
      difficulty: qs.difficulty ?? 'Intermediate'
    };

    this.examAttemptService.submitExamAttempt(this.data.moduleId, dto).subscribe({
      next: (attempt) => {
        this.submitting.set(false);
        this.dialogRef.close({ examAttemptId: attempt.id, quizId: attempt.quizId });
      },
      error: () => {
        this.error.set('Failed to submit exam attempt.');
        this.submitting.set(false);
      }
    });
  }

  close() {
    this.dialogRef.close();
  }

  // Unique colors for pairs (Green, Purple, Blue, Orange, Pink, Teal)
  private pairColors = ['pair-1', 'pair-2', 'pair-3', 'pair-4', 'pair-5', 'pair-6'];

  getPairColorClass(questionId: string, termOrDef: string): string {
    const state = this.matchingState[questionId];
    if (!state) return '';

    let term = Object.keys(state.pairs).find(t => t === termOrDef);
    if (!term) {
      term = Object.keys(state.pairs).find(t => state.pairs[t] === termOrDef);
    }

    if (term) {
      const index = state.terms.indexOf(term);
      if (index >= 0) {
        return this.pairColors[index % this.pairColors.length];
      }
    }
    return '';
  }

  onMultipleSelectChange(questionId: string, option: string, checked: boolean) {
    const currentAnswer = this.answers[questionId] || '';
    let selected = currentAnswer ? currentAnswer.split('|') : [];

    if (checked) {
      if (!selected.includes(option)) {
        selected.push(option);
      }
    } else {
      selected = selected.filter(s => s !== option);
    }

    this.answers[questionId] = selected.join('|');
  }

  isMultipleSelectChecked(questionId: string, option: string): boolean {
    const currentAnswer = this.answers[questionId];
    if (!currentAnswer) return false;
    return currentAnswer.split('|').includes(option);
  }

  getQuizTypeInfo(type: string | undefined) {
    if (!type) return { label: 'Exam', icon: 'school' };

    const lookup: Record<string, { label: string; icon: string }> = {
      Mixed: { label: 'Mixed', icon: 'layers' },
      MultipleChoice: { label: 'Multiple Choice', icon: 'list' },
      TrueFalse: { label: 'True/False', icon: 'check_circle' },
      OpenText: { label: 'Open Text', icon: 'edit' },
      Matching: { label: 'Matching', icon: 'compare_arrows' },
      Ordering: { label: 'Ordering', icon: 'reorder' },
      MultipleSelect: { label: 'Multiple Select', icon: 'checklist' }
    };

    return lookup[type] || { label: type, icon: 'school' };
  }
}
