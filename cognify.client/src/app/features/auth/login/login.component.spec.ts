import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LoginComponent } from './login.component';
import { AuthService } from '../../../core/auth/auth.service';
import { Router, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { provideNoopAnimations } from '@angular/platform-browser/animations';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let router: Router;

  beforeEach(async () => {
    authServiceSpy = jasmine.createSpyObj('AuthService', ['login']);

    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: AuthService, useValue: authServiceSpy }
      ]
    })
      .compileComponents();

    router = TestBed.inject(Router);
    spyOn(router, 'navigate');

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should be invalid when form is empty', () => {
    expect(component.loginForm.valid).toBeFalse();
  });

  it('should call AuthService.login and navigate on success', () => {
    const email = 'test@example.com';
    const password = 'password123';

    component.loginForm.setValue({ email, password });
    authServiceSpy.login.and.returnValue(of({ token: 't', email, userId: 'u' }));

    component.onSubmit();

    expect(authServiceSpy.login).toHaveBeenCalledWith({ email, password });
    expect(router.navigate).toHaveBeenCalledWith(['/']);
  });

  it('should display error message on login failure', () => {
    component.loginForm.setValue({ email: 'wrong@example.com', password: 'wrong' });
    authServiceSpy.login.and.returnValue(throwError(() => new Error('Unauthorized')));

    component.onSubmit();

    expect(component.errorMsg).toBe('Invalid email or password');
  });
});
