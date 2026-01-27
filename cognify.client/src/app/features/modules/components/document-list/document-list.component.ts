import { Component, Input, Output, EventEmitter, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DocumentsService, DocumentDto } from '../../services/documents.service';
import { AiService } from '../../../../core/services/ai.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { PendingService } from '../../../../core/services/pending.service';
import { HandwritingPreviewDialogComponent } from '../handwriting-preview-dialog/handwriting-preview-dialog.component';

@Component({
  selector: 'app-document-list',
  standalone: true,
  imports: [CommonModule, MatTableModule, MatButtonModule, MatIconModule, MatDialogModule, MatTooltipModule, MatProgressSpinnerModule],
  templateUrl: './document-list.component.html',
  styleUrl: './document-list.component.css'
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
    if (doc.status !== 1) return;
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

  getStatusLabel(status: number): string {
    switch (status) {
      case 0: return 'Processing';
      case 1: return 'Ready';
      case 2: return 'Error';
      default: return 'Unknown';
    }
  }
}
