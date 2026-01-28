import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CreateModuleDialogComponent } from './create-module-dialog.component';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';

describe('CreateModuleDialogComponent', () => {
  let component: CreateModuleDialogComponent;
  let fixture: ComponentFixture<CreateModuleDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CreateModuleDialogComponent, BrowserAnimationsModule],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: MatDialogRef, useValue: { close: () => { } } },
        { provide: MAT_DIALOG_DATA, useValue: {} } // Default to empty data (Create mode)
      ]
    })
      .compileComponents();

    fixture = TestBed.createComponent(CreateModuleDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize empty form in create mode', () => {
    expect(component.isEditMode).toBeFalse();
    expect(component.form.get('title')?.value).toBe('');
  });
});
