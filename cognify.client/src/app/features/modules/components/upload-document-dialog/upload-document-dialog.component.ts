import { Component, Inject, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatIconModule } from '@angular/material/icon';
import { DocumentsService } from '../../services/documents.service';

@Component({
  selector: 'app-upload-document-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatButtonModule, MatProgressBarModule, MatIconModule],
  templateUrl: './upload-document-dialog.component.html',
  styleUrl: './upload-document-dialog.component.css'
})
export class UploadDocumentDialogComponent {
  selectedFile: File | null = null;
  isUploading = false;
  uploadError: string | null = null;

  private documentsService = inject(DocumentsService);

  constructor(
    public dialogRef: MatDialogRef<UploadDocumentDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { moduleId: string }
  ) { }

  onFileSelected(event: any): void {
    const file = event.target.files[0];
    if (file) {
      this.selectedFile = file;
      this.uploadError = null;
    }
  }

  upload(): void {
    if (!this.selectedFile) return;

    this.isUploading = true;
    this.uploadError = null;

    this.documentsService.uploadDocument(this.data.moduleId, this.selectedFile).subscribe({
      next: (doc) => {
        this.isUploading = false;
        this.dialogRef.close(true); // Return true to indicate success
      },
      error: (err) => {
        console.error('Upload failed', err);
        this.isUploading = false;
        this.uploadError = 'Failed to upload file. Please try again.';
      }
    });
  }

  cancel(): void {
    this.dialogRef.close(false);
  }
}
