import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-note-image-preview-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatButtonModule],
  templateUrl: './note-image-preview-dialog.component.html',
  styleUrl: './note-image-preview-dialog.component.scss'
})
export class NoteImagePreviewDialogComponent {
  constructor(@Inject(MAT_DIALOG_DATA) public data: { url: string; title?: string }) {}
}
