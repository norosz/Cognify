import { ComponentFixture, TestBed } from '@angular/core/testing';
import { QuizTakingComponent } from './quiz-taking.component';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { QuizService } from '../../services/quiz.service';
import { of } from 'rxjs';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { QuestionSetDto } from '../../../../core/models/quiz.models';

describe('QuizTakingComponent', () => {
  let component: QuizTakingComponent;
  let fixture: ComponentFixture<QuizTakingComponent>;
  let mockDialogRef: jasmine.SpyObj<MatDialogRef<QuizTakingComponent>>;
  let mockQuizService: jasmine.SpyObj<QuizService>;

  const mockQuestionSet: QuestionSetDto = {
    id: 'qs-1',
    noteId: 'note-1',
    createdAt: new Date().toISOString(),
    questions: [{ id: 'q1', prompt: 'Q1', type: 'MultipleChoice', options: ['A', 'B'], correctAnswer: 'A', explanation: '' }]
  };

  beforeEach(async () => {
    mockDialogRef = jasmine.createSpyObj('MatDialogRef', ['close']);
    mockQuizService = jasmine.createSpyObj('QuizService', ['submitAttempt']);

    await TestBed.configureTestingModule({
      imports: [QuizTakingComponent, NoopAnimationsModule],
      providers: [
        { provide: MatDialogRef, useValue: mockDialogRef },
        { provide: MAT_DIALOG_DATA, useValue: { questionSet: mockQuestionSet } },
        { provide: QuizService, useValue: mockQuizService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(QuizTakingComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create and init with data', () => {
    expect(component).toBeTruthy();
    expect(component.questionSet()).toEqual(mockQuestionSet);
  });

  it('should submit attempt', () => {
    component.answers['q1'] = 'A';
    mockQuizService.submitAttempt.and.returnValue(of({
      id: 'att-1', questionSetId: 'qs-1', userId: 'u1', score: 100, answers: { 'q1': 'A' }, createdAt: ''
    }));

    component.submit();

    expect(mockQuizService.submitAttempt).toHaveBeenCalled();
    expect(component.result()).toBeTruthy();
    expect(component.result()?.score).toBe(100);
  });

  it('should calculate color based on score', () => {
    expect(component.getScoreColor(90)).toBe('green');
    expect(component.getScoreColor(60)).toBe('orange');
    expect(component.getScoreColor(20)).toBe('red');
  });
});
