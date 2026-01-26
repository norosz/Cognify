import { Component, Inject, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { NoteService } from '../../../../core/services/note.service';
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
        MatProgressSpinnerModule
    ],
    templateUrl: './note-editor-dialog.component.html',
    styleUrls: ['./note-editor-dialog.component.scss']
})
export class NoteEditorDialogComponent {
    form: FormGroup;
    isEditMode = false;
    isSaving = false;

    private noteService = inject(NoteService);
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
