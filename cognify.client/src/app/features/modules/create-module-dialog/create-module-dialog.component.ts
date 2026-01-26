import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { ModuleService } from '../../../core/modules/module.service';
import { CreateModuleDto } from '../../../core/modules/module.models';

@Component({
  selector: 'app-create-module-dialog',
  standalone: true,
  imports: [
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

  form = this.fb.group({
    title: ['', [Validators.required, Validators.maxLength(100)]],
    description: ['', [Validators.maxLength(500)]]
  });

  onSubmit() {
    if (this.form.valid) {
      const dto: CreateModuleDto = {
        title: this.form.value.title!,
        description: this.form.value.description || ''
      };

      this.moduleService.createModule(dto).subscribe({
        next: (result) => this.dialogRef.close(result),
        error: (err) => console.error('Failed to create module', err)
      });
    }
  }

  onCancel() {
    this.dialogRef.close();
  }
}
