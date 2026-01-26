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
import { UploadDocumentDialogComponent } from '../components/upload-document-dialog/upload-document-dialog.component';

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
    DocumentListComponent
  ],
  templateUrl: './module-detail.component.html',
  styleUrl: './module-detail.component.scss'
})
export class ModuleDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private moduleService = inject(ModuleService);
  private dialog = inject(MatDialog);

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
    const currentModule = this.module();
    if (!currentModule) return;

    const dialogRef = this.dialog.open(UploadDocumentDialogComponent, {
      width: '400px',
      data: { moduleId: currentModule.id }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.documentList.loadDocuments();
      }
    });
  }
}
