import { Component, Inject, inject, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatIconModule } from '@angular/material/icon';
import { DocumentsService, UploadInitiateResponse } from '../../services/documents.service';
import { NotificationService } from '../../../../core/services/notification.service';

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
  private notification = inject(NotificationService);
  private ngZone = inject(NgZone);

  constructor(
    public dialogRef: MatDialogRef<UploadDocumentDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { moduleId: string }
  ) {
    console.log('UploadDialog initialized');
  }

  onFileSelected(event: any): void {
    if (event.preventDefault) event.preventDefault();
    if (event.stopPropagation) event.stopPropagation();

    this.ngZone.runOutsideAngular(() => {
      try {
        if (!event.target || !event.target.files || event.target.files.length === 0) {
          return;
        }

        const file = event.target.files[0];
        if (file) {
          this.ngZone.run(() => {
            this.selectedFile = file;
            this.uploadError = null;
          });
        }
      } catch (e) {
        console.error('Error in handler:', e);
      }
    });
  }

  upload(): void {
    if (!this.selectedFile) {
      return;
    }

    this.isUploading = true;
    this.uploadError = null;

    this.documentsService.uploadDocument(this.data.moduleId, this.selectedFile).subscribe({
      next: (doc) => {
        this.isUploading = false;
        this.notification.success('File uploaded successfully.');
        this.dialogRef.close(true);
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
