import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { NoteService } from '../../../core/services/note.service';
import { Note, NoteEmbeddedImage, NoteSourcesDto } from '../../../core/models/note.model';
import { MarkdownLatexPipe } from '../../../shared/pipes/markdown-latex.pipe';
import { NoteEditorDialogComponent } from '../components/note-editor-dialog/note-editor-dialog.component';

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
    MarkdownLatexPipe
  ],
  templateUrl: './note-detail.component.html',
  styleUrl: './note-detail.component.scss'
})
export class NoteDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private noteService = inject(NoteService);
  private dialog = inject(MatDialog);

  note = signal<Note | null>(null);
  sources = signal<NoteSourcesDto | null>(null);
  loading = signal<boolean>(false);
  sourcesLoading = signal<boolean>(false);
  error = signal<string | null>(null);

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

  getCombinedContent(note: Note): string {
    const segments: string[] = [];

    if (note.userContent && note.userContent.trim()) {
      segments.push(`## Your Notes\n${note.userContent.trim()}`);
    } else if (note.content && note.content.trim()) {
      segments.push(note.content.trim());
    }

    if (note.aiContent && note.aiContent.trim()) {
      segments.push(`## AI Notes\n${note.aiContent.trim()}`);
    }

    const embedded = this.buildEmbeddedImagesMarkdown(note.embeddedImages);
    if (embedded) {
      segments.push(embedded);
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
