import { Component, Input, Output, EventEmitter, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatMenuModule } from '@angular/material/menu';
import { NotificationService } from '../../../../core/services/notification.service';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { NoteService } from '../../../../core/services/note.service';
import { Note } from '../../../../core/models/note.model';
import { PendingService } from '../../../../core/services/pending.service';
import { Router } from '@angular/router';
import { NoteEditorDialogComponent } from '../note-editor-dialog/note-editor-dialog.component';
import { QuizGenerationDialogComponent } from '../../../modules/components/quiz-generation-dialog/quiz-generation-dialog.component';

@Component({
    selector: 'app-notes-list',
    standalone: true,
    imports: [
        CommonModule,
        MatCardModule,
        MatButtonModule,
        MatIconModule,
        MatDialogModule,
        MatMenuModule,

        MatProgressSpinnerModule
    ],
    templateUrl: './notes-list.component.html',
    styleUrls: ['./notes-list.component.scss']
})
export class NotesListComponent implements OnInit {
    @Input() moduleId!: string;
    @Output() quizGenerated = new EventEmitter<void>();

    notes = signal<Note[]>([]);
    isLoading = signal<boolean>(false);

    private noteService = inject(NoteService);
    private pendingService = inject(PendingService);
    private dialog = inject(MatDialog);
    private notification = inject(NotificationService);
    private router = inject(Router);

    async generateQuiz(note: Note) {
        const dialogRef = this.dialog.open(QuizGenerationDialogComponent, {
            width: '550px',
            data: { noteId: note.id, noteTitle: note.title }
        });

        dialogRef.afterClosed().subscribe(result => {
            if (result) {
                this.pendingService.initiateQuiz({
                    noteId: note.id,
                    title: result.title,
                    difficulty: result.difficulty,
                    questionType: result.questionType,
                    questionCount: result.questionCount
                }).subscribe({
                    next: () => {
                        this.notification.success(
                            'Quiz generation started.',
                            ['/pending', { tab: 'quizzes' }],
                            'View Pending Quizzes'
                        );
                        this.pendingService.refreshPendingCount();
                        // Removed auto-redirect as per user request
                    },
                    error: () => this.notification.error('Failed to start quiz generation.')
                });
            }
        });
    }

    ngOnInit(): void {
        if (this.moduleId) {
            this.loadNotes();
        }
    }

    loadNotes(): void {
        this.isLoading.set(true);
        this.noteService.getNotesByModuleId(this.moduleId).subscribe({
            next: (notes) => {
                this.notes.set(notes);
                this.isLoading.set(false);
            },
            error: (err) => {
                console.error('Error loading notes:', err);
                this.notification.error('Failed to load notes');
                this.isLoading.set(false);
            }
        });
    }

    openEditor(note?: Note): void {
        const dialogRef = this.dialog.open(NoteEditorDialogComponent, {
            width: '600px',
            data: { moduleId: this.moduleId, note: note }
        });

        dialogRef.afterClosed().subscribe(result => {
            if (result) {
                this.loadNotes();
                this.notification.success(note ? 'Note updated' : 'Note created');
            }
        });
    }

    deleteNote(note: Note): void {
        if (confirm(`Are you sure you want to delete note "${note.title}"?`)) {
            this.noteService.deleteNote(note.id).subscribe({
                next: () => {
                    this.loadNotes();
                    this.notification.success('Note deleted');
                },
                error: (err) => {
                    console.error('Error deleting note:', err);
                    this.notification.error('Failed to delete note');
                }
            });
        }
    }
}
