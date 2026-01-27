import { Component, Inject, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
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
        MatSnackBarModule
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
    private snackBar = inject(MatSnackBar);
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
                const snackRef = this.snackBar.open(`Extracting text from ${doc.fileName}...`, 'Dismiss', { duration: 10000 });

                this.aiService.extractText(doc.id).subscribe({
                    next: (res) => {
                        snackRef.dismiss();
                        this.isImporting = false;
                        const currentContent = this.form.get('content')?.value || '';
                        const separator = currentContent ? '\n\n---\n\n' : '';
                        this.form.patchValue({
                            content: currentContent + separator + res.text
                        });
                        this.snackBar.open('Content imported!', 'Close', { duration: 2000 });
                    },
                    error: (err) => {
                        snackRef.dismiss();
                        console.error(err);
                        this.isImporting = false;
                        this.snackBar.open('Failed to import content.', 'Close', { duration: 3000 });
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
