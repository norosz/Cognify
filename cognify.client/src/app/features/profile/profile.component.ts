import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { AuthService } from '../../core/auth/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import { CommonModule } from '@angular/common';
import { MatDividerModule } from '@angular/material/divider';

@Component({
    selector: 'app-profile',
    standalone: true,
    imports: [
        CommonModule,
        ReactiveFormsModule,
        MatCardModule,
        MatFormFieldModule,
        MatInputModule,
        MatButtonModule,
        MatDividerModule
    ],
    templateUrl: './profile.component.html',
    styleUrl: './profile.component.scss'
})
export class ProfileComponent {
    private authService = inject(AuthService);
    private fb = inject(FormBuilder);
    private notificationService = inject(NotificationService);

    user = this.authService.currentUser;

    profileForm = this.fb.group({
        email: ['', [Validators.required, Validators.email]],
        username: ['', [Validators.maxLength(100)]]
    });

    passwordForm = this.fb.group({
        currentPassword: ['', [Validators.required]],
        newPassword: ['', [Validators.required, Validators.minLength(6)]],
        confirmPassword: ['', [Validators.required]]
    });

    constructor() {
        // Initialize form with current user data
        const currentUser = this.user();
        if (currentUser) {
            this.profileForm.patchValue({
                email: currentUser.email,
                username: currentUser.username
            });
        }
    }

    updateProfile() {
        if (this.profileForm.invalid) return;

        const { email, username } = this.profileForm.value;

        this.authService.updateProfile({
            email: email!,
            username: username || undefined
        }).subscribe({
            next: () => {
                this.notificationService.success('Profile updated successfully');
            },
            error: (err) => {
                this.notificationService.error(err.error?.detail || 'Failed to update profile');
            }
        });
    }

    changePassword(formDirective: any) {
        if (this.passwordForm.invalid) return;

        const { currentPassword, newPassword, confirmPassword } = this.passwordForm.value;

        if (newPassword !== confirmPassword) {
            this.notificationService.warning('Passwords do not match');
            return;
        }

        this.authService.changePassword({
            currentPassword: currentPassword!,
            newPassword: newPassword!
        }).subscribe({
            next: () => {
                this.notificationService.success('Password changed successfully');
                formDirective.resetForm();
                this.passwordForm.reset();
            },
            error: (err) => {
                this.notificationService.error(err.error?.detail || 'Failed to change password');
            }
        });
    }
}
