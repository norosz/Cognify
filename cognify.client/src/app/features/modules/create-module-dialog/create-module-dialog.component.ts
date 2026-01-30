import { Component, inject } from '@angular/core';
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
    MatInputModule
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

  form = this.fb.group({
    title: [this.data?.module?.title || '', [Validators.required, Validators.maxLength(100)]],
    description: [this.data?.module?.description || '', [Validators.maxLength(500)]],
    category: [this.data?.module?.categoryLabel || '', [Validators.maxLength(200)]]
  });

  onSubmit() {
    if (this.form.valid) {
      if (this.isEditMode && this.data?.module) {
        const dto: UpdateModuleDto = {
          title: this.form.value.title!,
          description: this.form.value.description || ''
        };
        this.moduleService.updateModule(this.data.module.id, dto).subscribe({
          next: (result) => this.dialogRef.close(result),
          error: (err) => console.error('Failed to update module', err)
        });
      } else {
        const dto: CreateModuleDto = {
          title: this.form.value.title!,
          description: this.form.value.description || '',
          categoryLabel: this.form.value.category?.trim() || undefined
        };

        this.moduleService.createModule(dto).subscribe({
          next: (result) => this.dialogRef.close(result),
          error: (err) => console.error('Failed to create module', err)
        });
      }
    }
  }

  onCancel() {
    this.dialogRef.close();
  }
}
