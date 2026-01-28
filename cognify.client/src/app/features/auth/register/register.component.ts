import { Component, inject } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { AuthService } from '../../../core/auth/auth.service';
import { CommonModule } from '@angular/common';

const passwordMatchValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const password = control.get('password');
  const confirmPassword = control.get('confirmPassword');

  if (password && confirmPassword && password.value !== confirmPassword.value) {
    confirmPassword.setErrors({ passwordMismatch: true });
    return { passwordMismatch: true };
  } else {
    // If confirmPassword already has other errors, don't clear them, 
    // but if it only had passwordMismatch, clear it.
    if (confirmPassword?.hasError('passwordMismatch')) {
      confirmPassword.setErrors(null);
    }
  }
  return null;
};

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatInputModule,
    MatButtonModule,
    MatFormFieldModule,
    RouterLink
  ],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss'
})
export class RegisterComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);

  registerForm = this.fb.group({
    username: ['', [Validators.maxLength(100)]], // Optional
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    confirmPassword: ['', [Validators.required]]
  }, { validators: passwordMatchValidator });

  errorMsg = '';

  onSubmit() {
    if (this.registerForm.valid) {
      this.errorMsg = '';
      const { email, password, username } = this.registerForm.value;

      this.authService.register({
        email: email!,
        password: password!,
        username: username || undefined // Send undefined if empty string
      })
        .subscribe({
          next: () => {
            // Redirect to login after successful registration
            this.router.navigate(['/login']);
          },
          error: (err) => {
            if (err.status === 409) {
              this.errorMsg = 'User with this email already exists';
            } else {
              this.errorMsg = 'Registration failed. Please try again.';
              console.error('Register error', err);
            }
          }
        });
    }
  }
}
