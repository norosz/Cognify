import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { AuthService } from '../../../core/auth/auth.service';
import { CommonModule } from '@angular/common';

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
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  errorMsg = '';

  onSubmit() {
    if (this.registerForm.valid) {
      this.errorMsg = '';
      const { email, password } = this.registerForm.value;

      this.authService.register({ email: email!, password: password! })
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
