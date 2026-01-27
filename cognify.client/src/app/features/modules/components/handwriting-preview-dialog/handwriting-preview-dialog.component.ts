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
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';

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
    MatSnackBarModule
  ],
  templateUrl: './handwriting-preview-dialog.component.html',
  styleUrl: './handwriting-preview-dialog.component.scss'
})
export class HandwritingPreviewDialogComponent {
  text = signal<string>('');
  moduleId: string;
  isSaving = signal<boolean>(false);

  constructor(
    private dialogRef: MatDialogRef<HandwritingPreviewDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { text: string, moduleId: string },
    private noteService: NoteService,
    private snackBar: MatSnackBar
  ) {
    this.text.set(data.text);
    this.moduleId = data.moduleId;
  }

  async copyToClipboard() {
    try {
      await navigator.clipboard.writeText(this.text());
      this.snackBar.open('Copied to clipboard!', 'Close', { duration: 2000 });
    } catch (err) {
      this.snackBar.open('Failed to copy.', 'Close', { duration: 2000 });
    }
  }

  saveAsNote() {
    if (!this.text().trim()) return;

    this.isSaving.set(true);
    const dto: CreateNoteRequest = {
      moduleId: this.moduleId,
      title: 'Extracted Content ' + new Date().toLocaleString(),
      content: this.text()
    };

    this.noteService.createNote(dto).subscribe({
      next: (note) => {
        this.snackBar.open('Note created!', 'Close', { duration: 2000 });
        this.dialogRef.close(note);
      },
      error: (err) => {
        console.error(err);
        this.snackBar.open('Failed to save note.', 'Close', { duration: 2000 });
        this.isSaving.set(false);
      }
    });
  }

  close() {
    this.dialogRef.close();
  }
}
