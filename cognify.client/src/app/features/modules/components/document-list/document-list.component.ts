import { Component, Input, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { DocumentsService, DocumentDto } from '../../services/documents.service';

@Component({
  selector: 'app-document-list',
  standalone: true,
  imports: [CommonModule, MatTableModule, MatButtonModule, MatIconModule, MatSnackBarModule],
  templateUrl: './document-list.component.html',
  styleUrl: './document-list.component.css'
})
export class DocumentListComponent implements OnInit {
  @Input() moduleId!: string;
  documents: DocumentDto[] = [];
  displayedColumns: string[] = ['fileName', 'createdAt', 'status', 'actions'];

  private documentsService = inject(DocumentsService);
  private snackBar = inject(MatSnackBar);

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

  getStatusLabel(status: number): string {
    switch (status) {
      case 0: return 'Processing';
      case 1: return 'Ready';
      case 2: return 'Error';
      default: return 'Unknown';
    }
  }
}
