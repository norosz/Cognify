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

    emailControl?.setValue('valid@example.com');
    passwordControl?.setValue('123'); // Too short

    expect(component.registerForm.valid).toBeFalse();
    expect(passwordControl?.hasError('minlength')).toBeTrue();
  });

  it('should call AuthService.register and navigate to login on success', () => {
    const email = 'newuser@example.com';
    const password = 'password123';

    component.registerForm.setValue({ email, password });
    authServiceSpy.register.and.returnValue(of({ token: 't', email, userId: 'u' }));

    component.onSubmit();

    expect(authServiceSpy.register).toHaveBeenCalledWith({ email, password });
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('should display conflict error message', () => {
    component.registerForm.setValue({ email: 'taken@example.com', password: 'password123' });
    const errorResponse = new HttpErrorResponse({ status: 409 });
    authServiceSpy.register.and.returnValue(throwError(() => errorResponse));

    component.onSubmit();

    expect(component.errorMsg).toBe('User with this email already exists');
  });
});
