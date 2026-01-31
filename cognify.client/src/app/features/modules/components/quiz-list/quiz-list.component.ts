import { Component, Input, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { NoteService } from '../../../../core/services/note.service';
import { QuizService } from '../../services/quiz.service';
import { QuizDto } from '../../../../core/models/quiz.models';
import { QuizTakingComponent } from '../quiz-taking/quiz-taking.component';
import { forkJoin, map, switchMap, of } from 'rxjs';
import { ConfirmationDialogComponent } from '../../../../shared/components/confirmation-dialog/confirmation-dialog.component';

interface QuizItem {
  quiz: QuizDto;
  noteTitle: string;
}

@Component({
  selector: 'app-quiz-list',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule,
    MatProgressSpinnerModule,
    RouterLink
  ],
  templateUrl: './quiz-list.component.html',
  styleUrl: './quiz-list.component.scss'
})
export class QuizListComponent implements OnInit {
  @Input() moduleId!: string;

  quizzes = signal<QuizItem[]>([]);
  isLoading = signal<boolean>(false);

  private noteService = inject(NoteService);
  private quizService = inject(QuizService);
  private dialog = inject(MatDialog);

  ngOnInit() {
    if (this.moduleId) {
      this.loadQuizzes();
    }
  }

  loadQuizzes() {
    this.isLoading.set(true);

    // 1. Get Notes for Module
    this.noteService.getNotesByModuleId(this.moduleId).pipe(
      switchMap(notes => {
        if (notes.length === 0) return of([]);

        // 2. For each note, get quizzes
        const requests = notes.map(note =>
          this.quizService.getQuizzesByNote(note.id).pipe(
            map(quizzes => quizzes.map(q => ({ quiz: q, noteTitle: note.title } as QuizItem)))
          )
        );

        return forkJoin(requests).pipe(
          map(results => results.flat())
        );
      })
    ).subscribe({
      next: (items) => {
        this.quizzes.set(items);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error(err);
        this.isLoading.set(false);
      }
    });
  }

  takeQuiz(quiz: QuizDto) {
    this.dialog.open(QuizTakingComponent, {
      width: '900px', // Wider
      height: '85vh',  // Taller
      maxWidth: '95vw',
      data: { quiz }
    });
  }

  deleteQuiz(quiz: QuizDto) {
    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      width: '400px',
      data: {
        title: 'Delete Quiz',
        message: 'Are you sure you want to delete this quiz?',
        confirmText: 'Delete',
        isDestructive: true
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.quizService.deleteQuiz(quiz.id).subscribe(() => this.loadQuizzes());
      }
    });
  }

  getQuizTypeInfo(type: string) {
    switch (type) {
      case 'MultipleChoice': return { label: 'Multiple Choice', icon: 'list' };
      case 'TrueFalse': return { label: 'True / False', icon: 'flaky' };
      case 'OpenText': return { label: 'Open Ended', icon: 'short_text' };
      case 'Matching': return { label: 'Matching', icon: 'unite' };
      case 'Ordering': return { label: 'Ordering', icon: 'low_priority' };
      case 'MultipleSelect': return { label: 'Multiple Select', icon: 'checklist' };
      case 'Mixed': return { label: 'Mixed', icon: 'auto_awesome' };
      default: return { label: type, icon: 'quiz' };
    }
  }

  getDifficultyClass(difficulty?: string) {
    const value = (difficulty || 'Intermediate').toLowerCase();
    switch (value) {
      case 'beginner':
        return 'difficulty-beginner';
      case 'advanced':
        return 'difficulty-advanced';
      case 'professional':
        return 'difficulty-professional';
      case 'intermediate':
      default:
        return 'difficulty-intermediate';
    }
  }
}
