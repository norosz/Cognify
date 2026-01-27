import { Component, Inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { FormsModule } from '@angular/forms';
import { NoteService } from '../../../../core/services/note.service';
import { CreateNoteRequest } from '../../../../core/models/note.model';
import { NotificationService } from '../../../../core/services/notification.service';
import { MatTooltipModule } from '@angular/material/tooltip';

@Component({
  selector: 'app-handwriting-preview-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    FormsModule,
    MatTooltipModule
  ],
  templateUrl: './handwriting-preview-dialog.component.html',
  styleUrl: './handwriting-preview-dialog.component.scss'
})
export class HandwritingPreviewDialogComponent {
  title = signal<string>('');
  text = signal<string>('');
  moduleId: string;
  isSaving = signal<boolean>(false);

  constructor(
    private dialogRef: MatDialogRef<HandwritingPreviewDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { text: string, moduleId: string, mode?: 'save' | 'view' },
    private noteService: NoteService,
    private notification: NotificationService
  ) {
    this.text.set(data.text);
    this.moduleId = data.moduleId;
  }

  async copyToClipboard() {
    try {
      await navigator.clipboard.writeText(this.text());
      this.notification.success('Copied to clipboard!');
    } catch (err) {
      this.notification.error('Failed to copy.');
    }
  }

  saveAsNote() {
    if (!this.title().trim() || !this.text().trim()) return;

    // If we're just in 'view' mode (from Pending page), we return the title so the caller can save
    if (this.data.mode === 'view') {
      this.dialogRef.close({ title: this.title().trim(), text: this.text() });
      return;
    }

    this.isSaving.set(true);
    const dto: CreateNoteRequest = {
      moduleId: this.moduleId,
      title: this.title().trim(),
      content: this.text()
    };

    this.noteService.createNote(dto).subscribe({
      next: (note) => {
        this.notification.success('Note created!');
        this.dialogRef.close(note);
      },
      error: (err) => {
        console.error(err);
        this.notification.error('Failed to save note.');
        this.isSaving.set(false);
      }
    });
  }

  close() {
    this.dialogRef.close();
  }
}
