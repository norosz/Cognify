import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ProfileComponent } from './profile.component';
import { AuthService } from '../../core/auth/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import { of, throwError } from 'rxjs';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';

describe('ProfileComponent', () => {
    let component: ProfileComponent;
    let fixture: ComponentFixture<ProfileComponent>;
    let authServiceSpy: jasmine.SpyObj<AuthService>;
    let notificationServiceSpy: jasmine.SpyObj<NotificationService>;

    beforeEach(async () => {
        authServiceSpy = jasmine.createSpyObj('AuthService', ['updateProfile', 'changePassword'], {
            currentUser: signal({ id: '1', email: 'test@example.com', username: 'TestUser' })
        });
        notificationServiceSpy = jasmine.createSpyObj('NotificationService', ['success', 'error', 'warning']);

        await TestBed.configureTestingModule({
            imports: [ProfileComponent],
            providers: [
                provideNoopAnimations(),
                { provide: AuthService, useValue: authServiceSpy },
                { provide: NotificationService, useValue: notificationServiceSpy }
            ]
        })
            .compileComponents();

        fixture = TestBed.createComponent(ProfileComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should initialize profile form with current user data', () => {
        expect(component.profileForm.value).toEqual({
            email: 'test@example.com',
            username: 'TestUser'
        });
    });

    it('should call updateProfile on valid submission', () => {
        // Return a valid object matching UserProfile or just containing id/email
        authServiceSpy.updateProfile.and.returnValue(of({ id: '1', email: 'test@example.com' } as any));

        component.profileForm.patchValue({ username: 'NewName' });
        component.updateProfile();

        expect(authServiceSpy.updateProfile).toHaveBeenCalledWith(jasmine.objectContaining({
            email: 'test@example.com',
            username: 'NewName'
        }));
        expect(notificationServiceSpy.success).toHaveBeenCalledWith('Profile updated successfully');
    });

    it('should warn if passwords do not match', () => {
        component.passwordForm.patchValue({
            currentPassword: 'old',
            newPassword: 'newpass',
            confirmPassword: 'mismatch'
        });

        component.changePassword({ resetForm: () => { } });

        expect(notificationServiceSpy.warning).toHaveBeenCalledWith('Passwords do not match');
        expect(authServiceSpy.changePassword).not.toHaveBeenCalled();
    });

    it('should call changePassword and reset form on success', () => {
        authServiceSpy.changePassword.and.returnValue(of(void 0));
        const resetFormSpy = jasmine.createSpy('resetForm');

        component.passwordForm.patchValue({
            currentPassword: 'old',
            newPassword: 'newpass',
            confirmPassword: 'newpass'
        });

        component.changePassword({ resetForm: resetFormSpy });

        expect(authServiceSpy.changePassword).toHaveBeenCalledWith({
            currentPassword: 'old',
            newPassword: 'newpass'
        });
        expect(notificationServiceSpy.success).toHaveBeenCalled();
        expect(resetFormSpy).toHaveBeenCalled();
    });
});
