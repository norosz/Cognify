import { ComponentFixture, TestBed } from '@angular/core/testing';
import { QuizGenerationComponent } from './quiz-generation.component';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { QuizService } from '../../services/quiz.service';
import { AiService } from '../../../../core/services/ai.service';
import { of, throwError } from 'rxjs';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { QuestionType, GeneratedQuestion } from '../../../../core/models/ai.models';

describe('QuizGenerationComponent', () => {
  let component: QuizGenerationComponent;
  let fixture: ComponentFixture<QuizGenerationComponent>;
  let mockDialogRef: jasmine.SpyObj<MatDialogRef<QuizGenerationComponent>>;
  let mockQuizService: jasmine.SpyObj<QuizService>;
  let mockAiService: jasmine.SpyObj<AiService>;

  beforeEach(async () => {
    mockDialogRef = jasmine.createSpyObj('MatDialogRef', ['close']);
    mockQuizService = jasmine.createSpyObj('QuizService', ['createQuestionSet']);
    mockAiService = jasmine.createSpyObj('AiService', ['generateQuestions']);

    await TestBed.configureTestingModule({
      imports: [QuizGenerationComponent, NoopAnimationsModule],
      providers: [
        { provide: MatDialogRef, useValue: mockDialogRef },
        { provide: MAT_DIALOG_DATA, useValue: { noteId: 'note-1' } },
        { provide: QuizService, useValue: mockQuizService },
        { provide: AiService, useValue: mockAiService }
      ]
    }).compileComponents();

    // Default behavior for generate
    mockAiService.generateQuestions.and.returnValue(of([{ text: 'Q1', type: QuestionType.MultipleChoice, options: ['A', 'B'], correctAnswer: 'A', explanation: 'Explanation' } as GeneratedQuestion]));

    fixture = TestBed.createComponent(QuizGenerationComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create and generate on init', () => {
    expect(component).toBeTruthy();
    expect(mockAiService.generateQuestions).toHaveBeenCalled();
    expect(component.questions().length).toBe(1);
  });

  it('should regenerate when requested', () => {
    component.selectedType = QuestionType.TrueFalse;
    component.count = 3;
    component.generate();

    expect(mockAiService.generateQuestions).toHaveBeenCalledTimes(2); // Init + manual
    const args = mockAiService.generateQuestions.calls.mostRecent().args[0];
    expect(args.type).toBe(QuestionType.TrueFalse);
    expect(args.count).toBe(3);
  });

  it('should handle generation error', () => {
    spyOn(console, 'error');
    mockAiService.generateQuestions.and.returnValue(throwError(() => new Error('Error')));
    component.generate();
    fixture.detectChanges();
    expect(component.error()).toBeTruthy();
    expect(component.loading()).toBe(false);
  });

  it('should call quizService.createQuestionSet on save', () => {
    mockQuizService.createQuestionSet.and.returnValue(of<any>({ id: 'qs-1' }));

    component.save();

    expect(mockQuizService.createQuestionSet).toHaveBeenCalled();
    expect(mockDialogRef.close).toHaveBeenCalledWith({ id: 'qs-1' });
  });
});
