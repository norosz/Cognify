import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter, ActivatedRoute } from '@angular/router';
import { of, Subject } from 'rxjs';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MatDialog } from '@angular/material/dialog';

import { PendingComponent } from './pending.component';
import { PendingService, ExtractedContentDto, PendingQuizDto } from '../../core/services/pending.service';
import { NotificationService } from '../../core/services/notification.service';

describe('PendingComponent', () => {
    let component: PendingComponent;
    let fixture: ComponentFixture<PendingComponent>;
    let pendingServiceSpy: jasmine.SpyObj<PendingService>;
    let notificationServiceSpy: jasmine.SpyObj<NotificationService>;
    let dialogSpy: jasmine.SpyObj<MatDialog>;
    let routeParams$: Subject<any>;

    const mockExtractions: ExtractedContentDto[] = [
        { id: '1', documentId: 'd1', moduleId: 'm1', documentName: 'Doc1', moduleName: 'Mod1', text: 'content', extractedAt: '2024-01-01', status: 'Ready' }
    ];

    const mockQuizzes: PendingQuizDto[] = [
        { id: '1', noteId: 'n1', moduleId: 'm1', title: 'Quiz1', noteName: 'Note1', moduleName: 'Mod1', difficulty: 'medium', questionType: 'MCQ', questionCount: 5, status: 'Ready', createdAt: '2024-01-01' }
    ];

    beforeEach(async () => {
        routeParams$ = new Subject();

        pendingServiceSpy = jasmine.createSpyObj('PendingService', [
            'getExtractedContents', 'getPendingQuizzes', 'refreshPendingCount',
            'saveAsNote', 'deleteExtractedContent', 'saveQuiz', 'deletePendingQuiz'
        ]);
        pendingServiceSpy.getExtractedContents.and.returnValue(of(mockExtractions));
        pendingServiceSpy.getPendingQuizzes.and.returnValue(of(mockQuizzes));
        pendingServiceSpy.refreshPendingCount.and.stub();

        notificationServiceSpy = jasmine.createSpyObj('NotificationService', ['success', 'error']);
        dialogSpy = jasmine.createSpyObj('MatDialog', ['open']);

        await TestBed.configureTestingModule({
            imports: [PendingComponent, NoopAnimationsModule],
            providers: [
                provideHttpClient(),
                provideHttpClientTesting(),
                provideRouter([]),
                { provide: PendingService, useValue: pendingServiceSpy },
                { provide: NotificationService, useValue: notificationServiceSpy },
                { provide: MatDialog, useValue: dialogSpy },
                { provide: ActivatedRoute, useValue: { params: routeParams$.asObservable() } }
            ]
        }).compileComponents();

        fixture = TestBed.createComponent(PendingComponent);
        component = fixture.componentInstance;
    });

    afterEach(() => {
        component.ngOnDestroy();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should load pending items on init', fakeAsync(() => {
        fixture.detectChanges();
        tick();

        expect(pendingServiceSpy.getExtractedContents).toHaveBeenCalled();
        expect(pendingServiceSpy.getPendingQuizzes).toHaveBeenCalled();
        expect(component.extractedContents().length).toBe(1);
        expect(component.pendingQuizzes().length).toBe(1);
    }));

    it('should render the extraction workflow banner', fakeAsync(() => {
        fixture.detectChanges();
        tick();

        const element: HTMLElement = fixture.nativeElement;
        expect(element.textContent).toContain('Review & Save creates a Note.');
    }));

    it('should switch to quizzes tab when route param is tab=quizzes', fakeAsync(() => {
        fixture.detectChanges();
        routeParams$.next({ tab: 'quizzes' });
        tick();

        expect(component.selectedTabIndex()).toBe(1);
    }));

    it('should switch to extractions tab when route param is tab=extractions', fakeAsync(() => {
        component.selectedTabIndex.set(1); // Start on quizzes
        fixture.detectChanges();
        routeParams$.next({ tab: 'extractions' });
        tick();

        expect(component.selectedTabIndex()).toBe(0);
    }));

    describe('getExtractionIcon', () => {
        it('should return correct icon for Ready status', () => {
            expect(component.getExtractionIcon('Ready')).toBe('history_edu');
        });

        it('should return correct icon for Processing status', () => {
            expect(component.getExtractionIcon('Processing')).toBe('sync');
        });

        it('should return correct icon for Error status', () => {
            expect(component.getExtractionIcon('Error')).toBe('error');
        });
    });

    describe('getQuizStatusIcon', () => {
        it('should return correct icon for ready status', () => {
            expect(component.getQuizStatusIcon('ready')).toBe('check_circle');
        });

        it('should return correct icon for generating status', () => {
            expect(component.getQuizStatusIcon('generating')).toBe('sync');
        });

        it('should return correct icon for error status', () => {
            expect(component.getQuizStatusIcon('error')).toBe('error');
        });
    });

    describe('saveQuiz', () => {
        it('should call pendingService.saveQuiz and show success notification', fakeAsync(() => {
            const quiz = mockQuizzes[0];
            pendingServiceSpy.saveQuiz.and.returnValue(of({ quizId: 'qs-1' }));

            component.saveQuiz(quiz);
            tick();

            expect(pendingServiceSpy.saveQuiz).toHaveBeenCalledWith(quiz.id);
            expect(notificationServiceSpy.success).toHaveBeenCalled();
        }));
    });

    describe('deleteQuiz', () => {
        it('should call pendingService.deletePendingQuiz when confirmed', fakeAsync(() => {
            spyOn(window, 'confirm').and.returnValue(true);
            pendingServiceSpy.deletePendingQuiz.and.returnValue(of(undefined));

            component.deleteQuiz('quiz-1');
            tick();

            expect(pendingServiceSpy.deletePendingQuiz).toHaveBeenCalledWith('quiz-1');
            expect(notificationServiceSpy.success).toHaveBeenCalled();
        }));

        it('should not call pendingService when cancelled', () => {
            spyOn(window, 'confirm').and.returnValue(false);

            component.deleteQuiz('quiz-1');

            expect(pendingServiceSpy.deletePendingQuiz).not.toHaveBeenCalled();
        });
    });
});
