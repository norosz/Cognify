import { Component, Inject, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { NotificationService } from '../../../../core/services/notification.service';
import { NoteService } from '../../../../core/services/note.service';
import { AiService } from '../../../../core/services/ai.service';
import { DocumentSelectionDialogComponent } from '../../../modules/components/document-selection-dialog/document-selection-dialog.component';
import { Note } from '../../../../core/models/note.model';

@Component({
    selector: 'app-note-editor-dialog',
    standalone: true,
    imports: [
        CommonModule,
        ReactiveFormsModule,
        MatDialogModule,
        MatButtonModule,
        MatFormFieldModule,
        MatInputModule,
        MatProgressSpinnerModule,
        MatIconModule,

    ],
    templateUrl: './note-editor-dialog.component.html',
    styleUrls: ['./note-editor-dialog.component.scss']
})
export class NoteEditorDialogComponent {
    form: FormGroup;
    isEditMode = false;
    isSaving = false;
    isImporting = false;

    private noteService = inject(NoteService);
    private aiService = inject(AiService);
    private dialog = inject(MatDialog);
    private notification = inject(NotificationService);
    private fb = inject(FormBuilder);

    constructor(
        public dialogRef: MatDialogRef<NoteEditorDialogComponent>,
        @Inject(MAT_DIALOG_DATA) public data: { moduleId: string, note?: Note }
    ) {
        this.isEditMode = !!data.note;

        this.form = this.fb.group({
            title: [data.note?.title || '', [Validators.required, Validators.maxLength(200)]],
            content: [data.note?.content || '']
        });
    }

    importFromDocument() {
        const ref = this.dialog.open(DocumentSelectionDialogComponent, {
            data: { moduleId: this.data.moduleId },
            width: '400px'
        });

        ref.afterClosed().subscribe(doc => {
            if (doc) {
                this.isImporting = true;
                const notifId = this.notification.loading(`Extracting text from ${doc.fileName}...`);

                this.aiService.extractText(doc.id).subscribe({
                    next: (res) => {
                        this.notification.update(notifId, { type: 'success', message: 'Content imported!', autoClose: true });
                        this.isImporting = false;
                        const currentContent = this.form.get('content')?.value || '';
                        const separator = currentContent ? '\n\n---\n\n' : '';
                        this.form.patchValue({
                            content: currentContent + separator + res.text
                        });
                    },
                    error: (err) => {
                        this.notification.update(notifId, { type: 'error', message: 'Failed to import content.', autoClose: true });
                        console.error(err);
                        this.isImporting = false;
                    }
                });
            }
        });
    }

    save(): void {
        if (this.form.invalid) return;

        this.isSaving = true;
        const formValue = this.form.value;

        if (this.isEditMode && this.data.note) {
            this.noteService.updateNote(this.data.note.id, formValue).subscribe({
                next: (note) => {
                    this.isSaving = false;
                    this.dialogRef.close(note);
                },
                error: (err) => {
                    console.error('Update failed', err);
                    this.isSaving = false;
                }
            });
        } else {
            this.noteService.createNote({
                moduleId: this.data.moduleId,
                title: formValue.title,
                content: formValue.content
            }).subscribe({
                next: (note) => {
                    this.isSaving = false;
                    this.dialogRef.close(note);
                },
                error: (err) => {
                    console.error('Create failed', err);
                    this.isSaving = false;
                }
            });
        }
    }
}
