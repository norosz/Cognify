import { Component, Inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatIconModule } from '@angular/material/icon';

@Component({
    selector: 'app-delete-module-confirmation-dialog',
    standalone: true,
    imports: [
        CommonModule,
        MatDialogModule,
        MatButtonModule,
        MatProgressBarModule,
        MatIconModule
    ],
    templateUrl: './delete-module-confirmation-dialog.component.html',
    styles: [`
    .text-warn {
      color: #f44336; /* MatWarn */
      display: flex;
      align-items: center;
      gap: 8px;
      margin-top: 16px;
    }
    .warn-icon {
      font-size: 20px;
      height: 20px;
      width: 20px;
    }
    .progress-container {
      display: flex;
      flex-direction: column;
      gap: 12px;
      padding: 16px 0;
      min-width: 300px;
    }
    .status-text {
      text-align: center;
      color: var(--text-secondary, #666);
      font-weight: 500;
    }
    .error-message {
      color: #f44336;
      display: flex;
      align-items: center;
      gap: 8px;
      margin-top: 16px;
    }
  `]
})
export class DeleteModuleConfirmationDialogComponent {
    constructor(
        public dialogRef: MatDialogRef<DeleteModuleConfirmationDialogComponent>,
        @Inject(MAT_DIALOG_DATA) public data: { moduleId: string, moduleTitle: string }
    ) { }

    cancel(): void {
        this.dialogRef.close(false);
    }

    confirmDelete(): void {
        this.dialogRef.close(true);
    }
}
