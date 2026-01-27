import { Component, Inject, signal } from '@angular/core';
import { CdkDragDrop, moveItemInArray } from '@angular/cdk/drag-drop';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatRadioModule } from '@angular/material/radio';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { FormsModule } from '@angular/forms';
import { QuizService } from '../../services/quiz.service';
import { QuestionSetDto, SubmitAttemptDto, AttemptDto } from '../../../../core/models/quiz.models';

import { DragDropModule } from '@angular/cdk/drag-drop';

@Component({
  selector: 'app-quiz-taking',
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
    DragDropModule
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

  // State for interactive questions
  orderingOptions: { [key: string]: string[] } = {};
  matchingState: { [key: string]: { terms: string[], definitions: string[], pairs: { [term: string]: string | null } } } = {};
  selectedTerm: string | null = null;
  selectedDef: string | null = null;

  constructor(
    private quizService: QuizService,
    private dialogRef: MatDialogRef<QuizTakingComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { questionSetId: string, questionSet?: QuestionSetDto }
  ) {
    if (data.questionSet) {
      this.initQuiz(data.questionSet);
    } else {
      this.loadQuiz(data.questionSetId);
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
    this.quizService.getQuestionSet(id).subscribe({
      next: (qs) => this.initQuiz(qs),
      error: (err) => this.error.set('Failed to load quiz.')
    });
  }

  initQuiz(qs: QuestionSetDto) {
    this.questionSet.set(qs);
    // Initialize interactive states
    qs.questions.forEach(q => {
      if (q.type === 'Ordering' && q.options) {
        // Copy options for reordering
        this.orderingOptions[q.id!] = [...q.options];
        // Set initial answer
        this.answers[q.id!] = this.orderingOptions[q.id!].join('|');
      }
      else if (q.type === 'Matching' && q.options) {
        // Parse "Term:Definition" pairs
        const terms: string[] = [];
        const definitions: string[] = [];
        q.options.forEach(pair => {
          const parts = pair.split(':');
          if (parts.length >= 2) {
            terms.push(parts[0].trim());
            definitions.push(parts.slice(1).join(':').trim());
          }
        });
        // Shuffle definitions
        const shuffledDefs = [...definitions].sort(() => Math.random() - 0.5);

        this.matchingState[q.id!] = {
          terms: terms,
          definitions: shuffledDefs,
          pairs: {}
        };
        // Init empty pairs
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

  // Matching logic
  selectMatch(questionId: string, term: string, def: string) {
    const state = this.matchingState[questionId];

    // Unpair this definition from any other term (enforce 1:1)
    Object.keys(state.pairs).forEach(t => {
      if (state.pairs[t] === def) state.pairs[t] = null;
    });

    state.pairs[term] = def;
    this.updateMatchingAnswer(questionId);

    // Reset selections
    this.selectedTerm = null;
    this.selectedDef = null;
  }

  onTermClick(questionId: string, term: string) {
    if (this.selectedDef) {
      // Complete the pair (Reverse: Def -> Term)
      this.selectMatch(questionId, term, this.selectedDef);
    } else {
      // Select term
      this.selectedTerm = term;
      this.selectedDef = null;
    }
  }

  onDefinitionClick(questionId: string, def: string) {
    if (this.selectedTerm) {
      this.selectMatch(questionId, this.selectedTerm, def);
    } else {
      // Select definition
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

  // Unique colors for pairs (Green, Purple, Blue, Orange, Pink, Teal)
  private pairColors = ['pair-1', 'pair-2', 'pair-3', 'pair-4', 'pair-5', 'pair-6'];

  getPairColorClass(questionId: string, termOrDef: string): string {
    const state = this.matchingState[questionId];
    if (!state) return '';

    // Find which pair this belongs to
    let term = Object.keys(state.pairs).find(t => t === termOrDef); // Is it a term?
    if (!term) {
      // Is it a definition?
      term = Object.keys(state.pairs).find(t => state.pairs[t] === termOrDef);
    }

    if (term) {
      // Find index of this term to assign consistent color
      const index = state.terms.indexOf(term);
      if (index >= 0) {
        return this.pairColors[index % this.pairColors.length];
      }
    }
    return '';
  }
}
