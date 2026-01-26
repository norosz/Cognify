import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ModuleDetailComponent } from './module-detail.component';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';

describe('ModuleDetailComponent', () => {
  let component: ModuleDetailComponent;
  let fixture: ComponentFixture<ModuleDetailComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ModuleDetailComponent, BrowserAnimationsModule],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([])
      ]
    })
      .compileComponents();

    fixture = TestBed.createComponent(ModuleDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
