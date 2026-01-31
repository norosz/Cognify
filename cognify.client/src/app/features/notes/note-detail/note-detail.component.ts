import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { NoteService } from '../../../core/services/note.service';
import { Note, NoteEmbeddedImage, NoteSourceDocumentDto, NoteSourcesDto } from '../../../core/models/note.model';
import { MarkdownLatexPipe } from '../../../shared/pipes/markdown-latex.pipe';
import { NoteEditorDialogComponent } from '../components/note-editor-dialog/note-editor-dialog.component';
import { QuizGenerationDialogComponent } from '../../modules/components/quiz-generation-dialog/quiz-generation-dialog.component';
import { PendingService } from '../../../core/services/pending.service';
import { NotificationService } from '../../../core/services/notification.service';
import { AiService } from '../../../core/services/ai.service';
import { DocumentsService } from '../../modules/services/documents.service';
import { NoteImagePreviewDialogComponent } from '../components/note-image-preview-dialog/note-image-preview-dialog.component';
import { ConfirmationDialogComponent } from '../../../shared/components/confirmation-dialog/confirmation-dialog.component';

@Component({
  selector: 'app-note-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule,
    MatProgressSpinnerModule,
    MatSlideToggleModule,
    MarkdownLatexPipe
  ],
  templateUrl: './note-detail.component.html',
  styleUrl: './note-detail.component.scss'
})
export class NoteDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private noteService = inject(NoteService);
  private dialog = inject(MatDialog);
  private pendingService = inject(PendingService);
  private notification = inject(NotificationService);
  private aiService = inject(AiService);
  private documentsService = inject(DocumentsService);

  note = signal<Note | null>(null);
  sources = signal<NoteSourcesDto | null>(null);
  loading = signal<boolean>(false);
  sourcesLoading = signal<boolean>(false);
  error = signal<string | null>(null);
  extractingDocIds = signal<Set<string>>(new Set());

  ngOnInit(): void {
    const noteId = this.route.snapshot.paramMap.get('noteId');
    if (!noteId) {
      this.error.set('Note not found.');
      return;
    }

    this.loadNote(noteId);
    this.loadSources(noteId);
  }

  loadNote(noteId: string) {
    this.loading.set(true);
    this.noteService.getNoteById(noteId).subscribe({
      next: (note) => {
        this.note.set(note);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load note.');
        this.loading.set(false);
      }
    });
  }

  loadSources(noteId: string) {
    this.sourcesLoading.set(true);
    this.noteService.getNoteSources(noteId).subscribe({
      next: (sources) => {
        this.sources.set(sources);
        this.sourcesLoading.set(false);
      },
      error: () => {
        this.sources.set(null);
        this.sourcesLoading.set(false);
      }
    });
  }

  editNote() {
    const current = this.note();
    if (!current) return;

    const dialogRef = this.dialog.open(NoteEditorDialogComponent, {
      width: '900px',
      maxWidth: '95vw',
      data: { moduleId: current.moduleId, note: current }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result?.id) {
        this.loadNote(result.id);
      }
    });
  }

  updateExamInclusion(includeInFinalExam: boolean) {
    const current = this.note();
    if (!current) return;

    const previousValue = current.includeInFinalExam ?? false;
    this.note.set({ ...current, includeInFinalExam });

    this.noteService.updateFinalExamInclusion(current.id, { includeInFinalExam }).subscribe({
      next: (updated) => this.note.set(updated),
      error: () => {
        this.note.set({ ...current, includeInFinalExam: previousValue });
        this.notification.error('Failed to update final exam inclusion.');
      }
    });
  }

  generateQuiz() {
    const current = this.note();
    if (!current) return;

    const dialogRef = this.dialog.open(QuizGenerationDialogComponent, {
      width: '550px',
      data: { noteId: current.id, noteTitle: current.title }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.pendingService.initiateQuiz({
          noteId: current.id,
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
          },
          error: () => this.notification.error('Failed to start quiz generation.')
        });
      }
    });
  }

  deleteDocument(doc: NoteSourceDocumentDto) {
    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      width: '400px',
      data: {
        title: 'Delete Document',
        message: `Delete ${doc.fileName}?`,
        confirmText: 'Delete',
        isDestructive: true
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.documentsService.deleteDocument(doc.documentId).subscribe({
          next: () => {
            this.notification.success('Document deleted');
            const noteId = this.note()?.id;
            if (noteId) {
              this.loadSources(noteId);
            }
          },
          error: () => this.notification.error('Failed to delete document')
        });
      }
    });
  }

  isExtracting(docId: string): boolean {
    return this.extractingDocIds().has(docId);
  }

  extractContent(doc: NoteSourceDocumentDto) {
    if (this.isExtracting(doc.documentId)) return;

    const updated = new Set(this.extractingDocIds());
    updated.add(doc.documentId);
    this.extractingDocIds.set(updated);

    this.aiService.extractText(doc.documentId).subscribe({
      next: () => {
        const after = new Set(this.extractingDocIds());
        after.delete(doc.documentId);
        this.extractingDocIds.set(after);

        this.notification.info(
          `Extraction processing started for ${doc.fileName}. Check Pending tab.`,
          ['/pending', { tab: 'extractions' }],
          'View Pending Note'
        );
        this.pendingService.refreshPendingCount();
      },
      error: () => {
        const after = new Set(this.extractingDocIds());
        after.delete(doc.documentId);
        this.extractingDocIds.set(after);
        this.notification.error('Failed to extract content. Ensure it is a valid image or PDF.');
      }
    });
  }

  openImagePreview(image: NoteEmbeddedImage) {
    if (!image.downloadUrl) return;
    this.dialog.open(NoteImagePreviewDialogComponent, {
      width: '900px',
      maxWidth: '95vw',
      data: { url: image.downloadUrl, title: image.fileName }
    });
  }

  getCombinedContent(note: Note): string {
    const segments: string[] = [];

    if (note.userContent && note.userContent.trim()) {
      segments.push(`## Your Notes\n${note.userContent.trim()}`);
    } else if (note.content && note.content.trim()) {
      segments.push(`## AI Notes\n${note.content.trim()}`);
    }

    if (note.aiContent && note.aiContent.trim()) {
      segments.push(`## AI Notes\n${note.aiContent.trim()}`);
    }

    return segments.join('\n\n');
  }

  getEmbeddedImageThumbnails(note: Note): NoteEmbeddedImage[] {
    return (note.embeddedImages ?? [])
      .filter(image => !!image.downloadUrl)
      .slice(0, 6);
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
}
