import { Component, Input, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';
import { DocumentsService, DocumentDto } from '../../services/documents.service';
import { AiService } from '../../../../core/services/ai.service';
import { HandwritingPreviewDialogComponent } from '../handwriting-preview-dialog/handwriting-preview-dialog.component';

@Component({
  selector: 'app-document-list',
  standalone: true,
  imports: [CommonModule, MatTableModule, MatButtonModule, MatIconModule, MatSnackBarModule, MatDialogModule, MatTooltipModule],
  templateUrl: './document-list.component.html',
  styleUrl: './document-list.component.css'
})
export class DocumentListComponent implements OnInit {
  @Input() moduleId!: string;
  documents: DocumentDto[] = [];
  displayedColumns: string[] = ['fileName', 'createdAt', 'status', 'actions'];

  private documentsService = inject(DocumentsService);
  private aiService = inject(AiService);
  private snackBar = inject(MatSnackBar);
  private dialog = inject(MatDialog);

  ngOnInit(): void {
    if (this.moduleId) {
      this.loadDocuments();
    }
  }

  loadDocuments(): void {
    this.documentsService.getDocuments(this.moduleId).subscribe({
      next: (docs) => this.documents = docs,
      error: (err) => console.error('Failed to load documents', err)
    });
  }

  deleteDocument(doc: DocumentDto): void {
    if (confirm(`Are you sure you want to delete ${doc.fileName}?`)) {
      this.documentsService.deleteDocument(doc.id).subscribe({
        next: () => {
          this.snackBar.open('Document deleted', 'Close', { duration: 3000 });
          this.loadDocuments();
        },
        error: (err) => {
          console.error('Failed to delete document', err);
          this.snackBar.open('Failed to delete document', 'Close', { duration: 3000 });
        }
      });
    }
  }

  openDocument(doc: DocumentDto): void {
    if (doc.downloadUrl) {
      window.open(doc.downloadUrl, '_blank');
    }
  }

  extractText(doc: DocumentDto): void {
    if (doc.status !== 1) return; // Only verify Ready documents

    const snackBarRef = this.snackBar.open('Extracting text (this may take a few seconds)...', 'Dismiss', { duration: 10000 });

    this.aiService.extractText(doc.id).subscribe({
      next: (res) => {
        snackBarRef.dismiss();
        this.dialog.open(HandwritingPreviewDialogComponent, {
          data: { text: res.text, moduleId: this.moduleId },
          width: '800px',
          disableClose: true
        });
      },
      error: (err) => {
        snackBarRef.dismiss();
        console.error(err);
        this.snackBar.open('Failed to extract text. Ensure it is a valid image.', 'Close', { duration: 4000 });
      }
    });
  }

  getStatusLabel(status: number): string {
    switch (status) {
      case 0: return 'Processing';
      case 1: return 'Ready';
      case 2: return 'Error';
      default: return 'Unknown';
    }
  }
}
