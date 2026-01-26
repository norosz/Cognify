import { ComponentFixture, TestBed } from '@angular/core/testing';

import { QuizGenerationComponent } from './quiz-generation.component';

describe('QuizGenerationComponent', () => {
  let component: QuizGenerationComponent;
  let fixture: ComponentFixture<QuizGenerationComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [QuizGenerationComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(QuizGenerationComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
