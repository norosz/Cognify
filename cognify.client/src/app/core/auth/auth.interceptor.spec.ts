import { TestBed } from '@angular/core/testing';
import { HttpInterceptorFn, HttpRequest, HttpHandlerFn, HttpEvent } from '@angular/common/http';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';
import { of } from 'rxjs';

describe('authInterceptor', () => {
  const interceptor: HttpInterceptorFn = (req, next) =>
    TestBed.runInInjectionContext(() => authInterceptor(req, next));

  let authServiceSpy: jasmine.SpyObj<AuthService>;

  beforeEach(() => {
    authServiceSpy = jasmine.createSpyObj('AuthService', ['getToken']);

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: authServiceSpy }
      ]
    });
  });

  it('should be created', () => {
    expect(interceptor).toBeTruthy();
  });

  it('should add Authorization header if token exists', () => {
    authServiceSpy.getToken.and.returnValue('valid-token');

    const req = new HttpRequest('GET', '/test');
    const next: HttpHandlerFn = (r) => {
      expect(r.headers.get('Authorization')).toBe('Bearer valid-token');
      return of({} as HttpEvent<any>);
    };

    interceptor(req, next).subscribe();
  });

  it('should NOT add Authorization header if token is missing', () => {
    authServiceSpy.getToken.and.returnValue(null);

    const req = new HttpRequest('GET', '/test');
    const next: HttpHandlerFn = (r) => {
      expect(r.headers.has('Authorization')).toBeFalse();
      return of({} as HttpEvent<any>);
    };

    interceptor(req, next).subscribe();
  });
});
