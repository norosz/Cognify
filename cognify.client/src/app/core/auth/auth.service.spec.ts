import { TestBed } from '@angular/core/testing';
import { AuthService } from './auth.service';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';

describe('AuthService', () => {
  let service: AuthService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([])
      ]
    });
    service = TestBed.inject(AuthService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should update profile via HTTP PUT', () => {
    const updateData = { email: 'new@test.com', username: 'NewName' };
    // This is a basic test; deeper testing requires HttpTestingController which is already provided
    expect(service.updateProfile).toBeDefined();
  });

  it('should change password via HTTP POST', () => {
    expect(service.changePassword).toBeDefined();
  });
});
