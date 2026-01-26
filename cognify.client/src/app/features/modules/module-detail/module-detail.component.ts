import { Component, inject, signal, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatTabsModule } from '@angular/material/tabs';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { ModuleService } from '../../../core/modules/module.service';
import { ModuleDto } from '../../../core/modules/module.models';
import { DocumentListComponent } from '../components/document-list/document-list.component';
import { NotesListComponent } from '../../notes/components/notes-list/notes-list.component';
import { UploadDocumentDialogComponent } from '../components/upload-document-dialog/upload-document-dialog.component';
import { DocumentsService } from '../services/documents.service';

@Component({
  selector: 'app-module-detail',
  standalone: true,
  imports: [
    CommonModule,
    MatTabsModule,
    MatIconModule,
    MatButtonModule,
    RouterLink,
    MatDialogModule,
    DocumentListComponent,
    NotesListComponent
  ],
  templateUrl: './module-detail.component.html',
  styleUrl: './module-detail.component.scss'
})
export class ModuleDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private moduleService = inject(ModuleService);
  private dialog = inject(MatDialog);
  private documentsService = inject(DocumentsService);

  module = signal<ModuleDto | null>(null);

  @ViewChild(DocumentListComponent) documentList!: DocumentListComponent;

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.moduleService.getModule(id).subscribe({
        next: (data) => this.module.set(data),
        error: (err) => console.error('Failed to load module', err)
      });
    }
  }

  openUploadDialog() {
    console.log('Opening upload dialog...');
    const currentModule = this.module();
    if (!currentModule) {
      console.error('No module data found!');
      return;
    }

    console.log('Module found:', currentModule.id);
    const dialogRef = this.dialog.open(UploadDocumentDialogComponent, {
      width: '400px',
      data: { moduleId: currentModule.id }
    });

    dialogRef.afterClosed().subscribe(result => {
      console.log('Dialog closed with result:', result);
      if (result) {
        if (this.documentList) {
          this.documentList.loadDocuments();
        } else {
          console.warn('DocumentList component not found!');
        }
      }
    });
  }
}
