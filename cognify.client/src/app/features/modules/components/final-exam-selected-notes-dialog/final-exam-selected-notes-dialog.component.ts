import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { Note } from '../../../../core/models/note.model';

export interface FinalExamSelectedNotesDialogData {
  notes: Note[];
}

@Component({
  selector: 'app-final-exam-selected-notes-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatButtonModule],
  templateUrl: './final-exam-selected-notes-dialog.component.html',
  styleUrl: './final-exam-selected-notes-dialog.component.scss'
})
export class FinalExamSelectedNotesDialogComponent {
  private dialogRef = inject(MatDialogRef<FinalExamSelectedNotesDialogComponent>);
  data = inject<FinalExamSelectedNotesDialogData>(MAT_DIALOG_DATA);

  confirm() {
    this.dialogRef.close(true);
  }

  cancel() {
    this.dialogRef.close(false);
  }
}
