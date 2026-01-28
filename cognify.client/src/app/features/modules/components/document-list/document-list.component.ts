import { Component, Input, Output, EventEmitter, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatCardModule } from '@angular/material/card';
import { MatMenuModule } from '@angular/material/menu';
import { DocumentsService, DocumentDto } from '../../services/documents.service';
import { AiService } from '../../../../core/services/ai.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { PendingService } from '../../../../core/services/pending.service';
import { HandwritingPreviewDialogComponent } from '../handwriting-preview-dialog/handwriting-preview-dialog.component';

@Component({
  selector: 'app-document-list',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule,
    MatTooltipModule,
    MatProgressSpinnerModule,
    MatCardModule,
    MatMenuModule
  ],
  templateUrl: './document-list.component.html',
  styleUrl: './document-list.component.scss'
})
export class DocumentListComponent implements OnInit {
  @Input() moduleId!: string;
  @Output() noteCreated = new EventEmitter<void>();

  documents: DocumentDto[] = [];
  displayedColumns: string[] = ['fileName', 'createdAt', 'status', 'actions'];

  extractingDocIds = signal<Set<string>>(new Set());

  private documentsService = inject(DocumentsService);
  private aiService = inject(AiService);
  private notification = inject(NotificationService);
  private pendingService = inject(PendingService);
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
          this.notification.success('Document deleted');
          this.loadDocuments();
        },
        error: (err) => {
          console.error('Failed to delete document', err);
          this.notification.error('Failed to delete document');
        }
      });
    }
  }

  openDocument(doc: DocumentDto): void {
    if (doc.downloadUrl) {
      window.open(doc.downloadUrl, '_blank');
    }
  }

  isExtracting(docId: string): boolean {
    return this.extractingDocIds().has(docId);
  }

  extractText(doc: DocumentDto): void {
    if (doc.status !== 'Uploaded') return;
    if (this.isExtracting(doc.id)) return;

    const newSet = new Set(this.extractingDocIds());
    newSet.add(doc.id);
    this.extractingDocIds.set(newSet);

    // const notifId = this.notification.loading(`Extracting text from ${doc.fileName}...`);


    this.aiService.extractText(doc.id).subscribe({
      next: (res) => {
        // Response is now just { extractedContentId, status: "Processing" }
        // We do NOT have the text yet.

        const updated = new Set(this.extractingDocIds());
        updated.delete(doc.id);
        this.extractingDocIds.set(updated);

        // Notify user to check pending
        this.notification.info(
          `Extraction processing started for ${doc.fileName}. Check Pending tab.`,
          ['/pending', { tab: 'extractions' }],
          'View Pending Note'
        );

        this.pendingService.refreshPendingCount();
      },
      error: (err) => {
        console.error(err);
        this.notification.error('Failed to extract text. Ensure it is a valid image.');

        const updated = new Set(this.extractingDocIds());
        updated.delete(doc.id);
        this.extractingDocIds.set(updated);
      }
    });
  }

  // Removed openExtractedText and hasExtractedText methods as they are no longer used

  getStatusLabel(status: string): string {
    return status;
  }

  getFileIcon(fileName: string): string {
    const ext = fileName.split('.').pop()?.toLowerCase();
    switch (ext) {
      case 'pdf': return 'picture_as_pdf';
      case 'doc':
      case 'docx': return 'description';
      case 'xls':
      case 'xlsx': return 'table_chart';
      case 'ppt':
      case 'pptx': return 'slideshow';
      case 'txt': return 'article';
      case 'jpg':
      case 'jpeg':
      case 'png': return 'image';
      default: return 'insert_drive_file';
    }
  }

  getFileExtension(fileName: string): string {
    return (fileName.split('.').pop() || '').toUpperCase();
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }
}
