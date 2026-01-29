import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DashboardComponent } from './dashboard.component';
import { ModuleService } from '../../core/modules/module.service';
import { MatDialog } from '@angular/material/dialog';
import { of } from 'rxjs';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ActivatedRoute } from '@angular/router';
import { KnowledgeService } from '../../core/services/knowledge.service';
import { AdaptiveQuizService } from '../../core/services/adaptive-quiz.service';
import { PendingService } from '../../core/services/pending.service';
import { NotificationService } from '../../core/services/notification.service';
import { LearningAnalyticsService } from '../../core/services/learning-analytics.service';

describe('DashboardComponent', () => {
    let component: DashboardComponent;
    let fixture: ComponentFixture<DashboardComponent>;
    let moduleServiceSpy: jasmine.SpyObj<ModuleService>;
    let dialogSpy: jasmine.SpyObj<MatDialog>;
    let knowledgeServiceSpy: jasmine.SpyObj<KnowledgeService>;
    let adaptiveQuizServiceSpy: jasmine.SpyObj<AdaptiveQuizService>;
    let pendingServiceSpy: jasmine.SpyObj<PendingService>;
    let notificationServiceSpy: jasmine.SpyObj<NotificationService>;
    let analyticsServiceSpy: jasmine.SpyObj<LearningAnalyticsService>;

    beforeEach(async () => {
        moduleServiceSpy = jasmine.createSpyObj('ModuleService', ['getModules', 'deleteModule']);
        dialogSpy = jasmine.createSpyObj('MatDialog', ['open']);
        knowledgeServiceSpy = jasmine.createSpyObj('KnowledgeService', ['getStates', 'getReviewQueue']);
        adaptiveQuizServiceSpy = jasmine.createSpyObj('AdaptiveQuizService', ['createAdaptiveQuiz']);
        pendingServiceSpy = jasmine.createSpyObj('PendingService', ['refreshPendingCount']);
        notificationServiceSpy = jasmine.createSpyObj('NotificationService', ['loading', 'update']);
        analyticsServiceSpy = jasmine.createSpyObj('LearningAnalyticsService', ['getSummary', 'getTrends', 'getTopics', 'getRetentionHeatmap', 'getDecayForecast']);

        moduleServiceSpy.getModules.and.returnValue(of([]));
        knowledgeServiceSpy.getStates.and.returnValue(of([]));
        knowledgeServiceSpy.getReviewQueue.and.returnValue(of([]));
        adaptiveQuizServiceSpy.createAdaptiveQuiz.and.returnValue(of({
            pendingQuiz: {
                id: '1',
                noteId: 'note-1',
                moduleId: 'module-1',
                title: 'Review Quiz',
                noteName: 'Note',
                moduleName: 'Module',
                difficulty: 'Intermediate',
                questionType: 'Mixed',
                questionCount: 5,
                status: 'Generating',
                createdAt: new Date().toISOString()
            },
            selectedTopic: 'Topic',
            masteryScore: 0.5,
            forgettingRisk: 0.6
        }));
        notificationServiceSpy.loading.and.returnValue('loading-id');
        analyticsServiceSpy.getSummary.and.returnValue(of({
            totalTopics: 0,
            averageMastery: 0,
            averageForgettingRisk: 0,
            weakTopicsCount: 0,
            totalAttempts: 0,
            accuracyRate: 0,
            examReadinessScore: 0,
            learningVelocity: 0,
            lastActivityAt: null
        }));
        analyticsServiceSpy.getTrends.and.returnValue(of({
            from: new Date().toISOString(),
            to: new Date().toISOString(),
            bucketDays: 7,
            points: []
        }));
        analyticsServiceSpy.getTopics.and.returnValue(of({
            topics: [],
            weakestTopics: []
        }));
        analyticsServiceSpy.getRetentionHeatmap.and.returnValue(of([]));
        analyticsServiceSpy.getDecayForecast.and.returnValue(of({
            days: 14,
            stepDays: 2,
            topics: []
        }));

        await TestBed.configureTestingModule({
            imports: [DashboardComponent, NoopAnimationsModule],
            providers: [
                { provide: ModuleService, useValue: moduleServiceSpy },
                { provide: MatDialog, useValue: dialogSpy },
                { provide: KnowledgeService, useValue: knowledgeServiceSpy },
                { provide: AdaptiveQuizService, useValue: adaptiveQuizServiceSpy },
                { provide: PendingService, useValue: pendingServiceSpy },
                { provide: NotificationService, useValue: notificationServiceSpy },
                { provide: LearningAnalyticsService, useValue: analyticsServiceSpy },
                {
                    provide: ActivatedRoute,
                    useValue: { snapshot: { paramMap: { get: () => null } } }
                }
            ]
        })
            .overrideComponent(DashboardComponent, {
                set: {
                    template: '<div></div>'
                }
            })
            .compileComponents();

        fixture = TestBed.createComponent(DashboardComponent);
        component = fixture.componentInstance;
        // Explicitly override injected service with spy to avoid MatDialog errors in test env
        (component as any).dialog = dialogSpy;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should load modules on init', () => {
        expect(moduleServiceSpy.getModules).toHaveBeenCalled();
    });

    it('should load knowledge data on init', () => {
        expect(knowledgeServiceSpy.getStates).toHaveBeenCalled();
        expect(knowledgeServiceSpy.getReviewQueue).toHaveBeenCalled();
    });

    it('should load analytics summary on init', () => {
        expect(analyticsServiceSpy.getSummary).toHaveBeenCalled();
        expect(analyticsServiceSpy.getTrends).toHaveBeenCalled();
        expect(analyticsServiceSpy.getTopics).toHaveBeenCalled();
        expect(analyticsServiceSpy.getRetentionHeatmap).toHaveBeenCalled();
        expect(analyticsServiceSpy.getDecayForecast).toHaveBeenCalled();
    });

    it('should open dialog for editing', () => {
        const mockModule = {
            id: '1',
            title: 'Test',
            createdAt: '2023-01-01',
            documentsCount: 0,
            notesCount: 0,
            quizzesCount: 0
        };
        const event = new Event('click');
        spyOn(event, 'stopPropagation');

        // Mock dialog return
        const dialogRefSpy = jasmine.createSpyObj({ afterClosed: of(true) });
        dialogSpy.open.and.returnValue(dialogRefSpy);

        component.openEditModuleDialog(event, mockModule);

        expect(dialogSpy.open).toHaveBeenCalled();
        expect(event.stopPropagation).toHaveBeenCalled();
    });
});
