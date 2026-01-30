import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogRef, MatDialogModule } from '@angular/material/dialog';

@Component({
    selector: 'app-final-exam-note-dialog',
    standalone: true,
    imports: [MatDialogModule, MatButtonModule],
    templateUrl: './final-exam-note-dialog.component.html',
    styleUrl: './final-exam-note-dialog.component.scss'
})
export class FinalExamNoteDialogComponent {
    private dialogRef = inject(MatDialogRef<FinalExamNoteDialogComponent>);

    includeAllNotes() {
        this.dialogRef.close(true);
    }

    cancel() {
        this.dialogRef.close(false);
    }
}
