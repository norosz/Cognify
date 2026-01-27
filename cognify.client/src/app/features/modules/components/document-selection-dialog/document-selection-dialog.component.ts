import { Component, Inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { DocumentsService, DocumentDto } from '../../services/documents.service';

@Component({
  selector: 'app-document-selection-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatButtonModule, MatListModule, MatIconModule],
  templateUrl: './document-selection-dialog.component.html',
  styleUrl: './document-selection-dialog.component.scss'
})
export class DocumentSelectionDialogComponent implements OnInit {
  documents = signal<DocumentDto[]>([]);
  loading = signal<boolean>(true);

  constructor(
    private dialogRef: MatDialogRef<DocumentSelectionDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { moduleId: string },
    private documentsService: DocumentsService
  ) { }

  ngOnInit() {
    this.documentsService.getDocuments(this.data.moduleId).subscribe({
      next: (docs) => {
        // Filter only Ready documents (status = 1)
        this.documents.set(docs.filter(d => d.status === 1));
        this.loading.set(false);
      },
      error: (err) => {
        console.error(err);
        this.loading.set(false);
      }
    });
  }

  select(doc: DocumentDto) {
    this.dialogRef.close(doc);
  }

  close() {
    this.dialogRef.close();
  }
}
