import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RegisterComponent } from './register.component';
import { AuthService } from '../../../core/auth/auth.service';
import { Router, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { HttpErrorResponse } from '@angular/common/http';

describe('RegisterComponent', () => {
  let component: RegisterComponent;
  let fixture: ComponentFixture<RegisterComponent>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let router: Router;

  beforeEach(async () => {
    authServiceSpy = jasmine.createSpyObj('AuthService', ['register']);

    await TestBed.configureTestingModule({
      imports: [RegisterComponent],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: AuthService, useValue: authServiceSpy }
      ]
    })
      .compileComponents();

    router = TestBed.inject(Router);
    spyOn(router, 'navigate');

    fixture = TestBed.createComponent(RegisterComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should require a 6+ char password', () => {
    const emailControl = component.registerForm.get('email');
    const passwordControl = component.registerForm.get('password');
    const confirmControl = component.registerForm.get('confirmPassword');

    emailControl?.setValue('valid@example.com');
    passwordControl?.setValue('123'); // Too short
    confirmControl?.setValue('123');

    expect(component.registerForm.valid).toBeFalse();
    expect(passwordControl?.hasError('minlength')).toBeTrue();
  });

  it('should validate password match', () => {
    component.registerForm.patchValue({
      email: 'valid@example.com',
      password: 'password123',
      confirmPassword: 'otherpassword'
    });

    expect(component.registerForm.valid).toBeFalse();
    expect(component.registerForm.get('confirmPassword')?.hasError('passwordMismatch')).toBeTrue();
  });

  it('should accept optional username', () => {
    component.registerForm.patchValue({
      email: 'valid@example.com',
      username: 'CoolUser123',
      password: 'password123',
      confirmPassword: 'password123'
    });
    expect(component.registerForm.valid).toBeTrue();
  });

  it('should validate username max length', () => {
    component.registerForm.patchValue({
      username: 'a'.repeat(101) // Max is 100
    });
    expect(component.registerForm.get('username')?.hasError('maxlength')).toBeTrue();
  });

  it('should call AuthService.register and navigate to login on success', () => {
    const email = 'newuser@example.com';
    const password = 'password123';
    const username = 'TestUser';

    component.registerForm.setValue({ email, password, confirmPassword: password, username });
    authServiceSpy.register.and.returnValue(of({ token: 't', email, userId: 'u' }));

    component.onSubmit();

    expect(authServiceSpy.register).toHaveBeenCalledWith({ email, password, username });
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('should display conflict error message', () => {
    component.registerForm.setValue({
      email: 'taken@example.com',
      username: '',
      password: 'password123',
      confirmPassword: 'password123'
    });
    const errorResponse = new HttpErrorResponse({ status: 409 });
    authServiceSpy.register.and.returnValue(throwError(() => errorResponse));

    component.onSubmit();

    expect(component.errorMsg).toBe('User with this email already exists');
  });
});
