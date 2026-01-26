import { Component, Input, Output, EventEmitter, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatMenuModule } from '@angular/material/menu';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { NoteService } from '../../../../core/services/note.service';
import { Note } from '../../../../core/models/note.model';
import { NoteEditorDialogComponent } from '../note-editor-dialog/note-editor-dialog.component';

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
        MatSnackBarModule,
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
    private dialog = inject(MatDialog);
    private snackBar = inject(MatSnackBar);

    // Lazy loaded via dialog but referenced in logic
    async generateQuiz(note: Note) {
        const { QuizGenerationComponent } = await import('../../../modules/components/quiz-generation/quiz-generation.component');

        const dialogRef = this.dialog.open(QuizGenerationComponent, {
            width: '600px',
            data: { noteId: note.id }
        });

        dialogRef.afterClosed().subscribe(result => {
            if (result) {
                this.snackBar.open('Quiz generated successfully!', 'Close', { duration: 3000 });
                this.quizGenerated.emit();
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
                this.snackBar.open('Failed to load notes', 'Close', { duration: 3000 });
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
                this.snackBar.open(note ? 'Note updated' : 'Note created', 'Close', { duration: 3000 });
            }
        });
    }

    deleteNote(note: Note): void {
        if (confirm(`Are you sure you want to delete note "${note.title}"?`)) {
            this.noteService.deleteNote(note.id).subscribe({
                next: () => {
                    this.loadNotes();
                    this.snackBar.open('Note deleted', 'Close', { duration: 3000 });
                },
                error: (err) => {
                    console.error('Error deleting note:', err);
                    this.snackBar.open('Failed to delete note', 'Close', { duration: 3000 });
                }
            });
        }
    }
}
