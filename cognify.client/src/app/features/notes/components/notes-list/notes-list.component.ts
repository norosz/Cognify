import { Component, Input, Output, EventEmitter, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatMenuModule } from '@angular/material/menu';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { NotificationService } from '../../../../core/services/notification.service';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { NoteService } from '../../../../core/services/note.service';
import { Note, NoteEmbeddedImage } from '../../../../core/models/note.model';
import { PendingService } from '../../../../core/services/pending.service';
import { Router } from '@angular/router';
import { NoteEditorDialogComponent } from '../note-editor-dialog/note-editor-dialog.component';
import { QuizGenerationDialogComponent } from '../../../modules/components/quiz-generation-dialog/quiz-generation-dialog.component';
import { MarkdownLatexPipe } from '../../../../shared/pipes/markdown-latex.pipe';

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
        MatSlideToggleModule,
        MatProgressSpinnerModule,
        MarkdownLatexPipe
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
                            'View Pending Quizzes & Exams'
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
            width: '900px',
            maxWidth: '95vw',
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

    updateExamInclusion(note: Note, includeInFinalExam: boolean): void {
        const previousValue = note.includeInFinalExam ?? false;

        this.notes.set(this.notes().map(existing =>
            existing.id === note.id
                ? { ...existing, includeInFinalExam }
                : existing
        ));

        this.noteService.updateFinalExamInclusion(note.id, { includeInFinalExam }).subscribe({
            next: (updated) => {
                this.notes.set(this.notes().map(existing =>
                    existing.id === updated.id
                        ? { ...existing, includeInFinalExam: updated.includeInFinalExam }
                        : existing
                ));
            },
            error: () => {
                this.notes.set(this.notes().map(existing =>
                    existing.id === note.id
                        ? { ...existing, includeInFinalExam: previousValue }
                        : existing
                ));
                this.notification.error('Failed to update final exam inclusion.');
            }
        });
    }

    openNoteDetail(note: Note): void {
        this.router.navigate(['/notes', note.id]);
    }

    getNoteContent(note: Note): string {
        const baseContent = this.buildCombinedContent(note);
        const embeddedMarkdown = this.buildEmbeddedImagesMarkdown(note.embeddedImages);
        if (!embeddedMarkdown) {
            return baseContent;
        }

        if (!baseContent.trim()) {
            return embeddedMarkdown;
        }

        return `${baseContent}\n\n---\n\n${embeddedMarkdown}`;
    }

    getEmbeddedImageThumbnails(note: Note): NoteEmbeddedImage[] {
        return (note.embeddedImages ?? [])
            .filter(image => !!image.downloadUrl)
            .slice(0, 3);
    }

    private buildEmbeddedImagesMarkdown(images?: NoteEmbeddedImage[] | null): string {
        if (!images || images.length === 0) return '';

        const withUrls = images.filter(image => !!image.downloadUrl);
        if (withUrls.length === 0) return '';

        const markdownImages = withUrls
            .map(image => `![${image.fileName} (page ${image.pageNumber})](${image.downloadUrl})`)
            .join('\n\n');

        return `## Embedded Images\n${markdownImages}`;
    }

    private buildCombinedContent(note: Note): string {
        const segments: string[] = [];

        if (note.userContent && note.userContent.trim()) {
            segments.push(`## Your Notes\n${note.userContent.trim()}`);
        } else if (note.content && note.content.trim()) {
            segments.push(note.content.trim());
        }

        if (note.aiContent && note.aiContent.trim()) {
            segments.push(`## AI Notes\n${note.aiContent.trim()}`);
        }

        return segments.join('\n\n');
    }
}
