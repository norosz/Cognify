import { Component, inject } from '@angular/core';
import { NotificationService } from '../../../core/services/notification.service';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { ModuleService } from '../../../core/modules/module.service';
import { CreateModuleDto, ModuleDto, UpdateModuleDto } from '../../../core/modules/module.models';

@Component({
  selector: 'app-create-module-dialog',
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
  templateUrl: './create-module-dialog.component.html',
  styleUrl: './create-module-dialog.component.scss'
})
export class CreateModuleDialogComponent {
  private fb = inject(FormBuilder);
  private moduleService = inject(ModuleService);
  private dialogRef = inject(MatDialogRef<CreateModuleDialogComponent>);
  private data = inject<{ module?: ModuleDto }>(MAT_DIALOG_DATA, { optional: true });

  isEditMode = !!this.data?.module;
  isLoading = false;

  form = this.fb.group({
    title: [this.data?.module?.title || '', [Validators.required, Validators.maxLength(100)]],
    description: [this.data?.module?.description || '', [Validators.maxLength(500)]],
    category: [this.data?.module?.categoryLabel || '', [Validators.maxLength(200)]]
  });

  private notificationService = inject(NotificationService);

  onSubmit() {
    if (this.form.valid) {
      this.isLoading = true;
      if (this.isEditMode && this.data?.module) {
        const dto: UpdateModuleDto = {
          title: this.form.value.title!,
          description: this.form.value.description || ''
        };
        this.moduleService.updateModule(this.data.module.id, dto).subscribe({
          next: (result) => {
            this.isLoading = false;
            this.dialogRef.close(result);
          },
          error: (err) => {
            this.isLoading = false;
            console.error('Failed to update module', err);
          }
        });
      } else {
        const dto: CreateModuleDto = {
          title: this.form.value.title!,
          description: this.form.value.description || '',
          categoryLabel: this.form.value.category?.trim() || undefined
        };
        this.moduleService.createModule(dto).subscribe({
          next: (result) => {
            this.isLoading = false;
            this.notificationService.success('Module created successfully!');
            this.dialogRef.close(result);
          },
          error: (err) => {
            this.isLoading = false;
            console.error('Failed to create module', err);
          }
        });
      }
    }
  }

  onCancel() {
    this.dialogRef.close();
  }
}
