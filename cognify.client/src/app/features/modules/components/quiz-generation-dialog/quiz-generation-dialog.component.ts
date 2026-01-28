import { Component, Inject, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators, FormGroup } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSliderModule } from '@angular/material/slider';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonToggleModule } from '@angular/material/button-toggle';

@Component({
  selector: 'app-quiz-generation-dialog',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatSliderModule,
    MatIconModule,
    MatButtonToggleModule
  ],
  templateUrl: './quiz-generation-dialog.component.html',
  styleUrl: './quiz-generation-dialog.component.scss'
})
export class QuizGenerationDialogComponent {
  private fb = inject(FormBuilder);
  private dialogRef = inject(MatDialogRef<QuizGenerationDialogComponent>);

  quizForm!: FormGroup;

  difficulties = ['Beginner', 'Intermediate', 'Advanced', 'Professional'];

  questionTypes = [
    { value: 'Mixed', label: 'Mixed', icon: 'auto_awesome' },
    { value: 'MultipleChoice', label: 'Multiple Choice', icon: 'list' },
    { value: 'TrueFalse', label: 'True / False', icon: 'flaky' },
    { value: 'OpenText', label: 'Open Ended', icon: 'short_text' },
    { value: 'Matching', label: 'Matching', icon: 'unite' },
    { value: 'Ordering', label: 'Ordering', icon: 'low_priority' },
    { value: 'MultipleSelect', label: 'Multiple Select', icon: 'checklist' }
  ];

  constructor(@Inject(MAT_DIALOG_DATA) public data: { noteId: string; noteTitle: string }) {
    this.quizForm = this.fb.group({
      title: [this.data.noteTitle + ' Quiz', [Validators.required, Validators.maxLength(100)]],
      difficulty: ['Intermediate', Validators.required],
      questionType: ['MultipleChoice', Validators.required],
      questionCount: [5, [Validators.required, Validators.min(1), Validators.max(20)]]
    });
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onGenerate(): void {
    if (this.quizForm.valid) {
      this.dialogRef.close(this.quizForm.value);
    }
  }
}
