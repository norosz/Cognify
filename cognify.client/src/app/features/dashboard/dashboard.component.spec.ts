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

describe('DashboardComponent', () => {
    let component: DashboardComponent;
    let fixture: ComponentFixture<DashboardComponent>;
    let moduleServiceSpy: jasmine.SpyObj<ModuleService>;
    let dialogSpy: jasmine.SpyObj<MatDialog>;
    let knowledgeServiceSpy: jasmine.SpyObj<KnowledgeService>;
    let adaptiveQuizServiceSpy: jasmine.SpyObj<AdaptiveQuizService>;
    let pendingServiceSpy: jasmine.SpyObj<PendingService>;
    let notificationServiceSpy: jasmine.SpyObj<NotificationService>;

    beforeEach(async () => {
        moduleServiceSpy = jasmine.createSpyObj('ModuleService', ['getModules', 'deleteModule']);
        dialogSpy = jasmine.createSpyObj('MatDialog', ['open']);
        knowledgeServiceSpy = jasmine.createSpyObj('KnowledgeService', ['getStates', 'getReviewQueue']);
        adaptiveQuizServiceSpy = jasmine.createSpyObj('AdaptiveQuizService', ['createAdaptiveQuiz']);
        pendingServiceSpy = jasmine.createSpyObj('PendingService', ['refreshPendingCount']);
        notificationServiceSpy = jasmine.createSpyObj('NotificationService', ['loading', 'update']);

        moduleServiceSpy.getModules.and.returnValue(of([]));
        knowledgeServiceSpy.getStates.and.returnValue(of([]));
        knowledgeServiceSpy.getReviewQueue.and.returnValue(of([]));
        adaptiveQuizServiceSpy.createAdaptiveQuiz.and.returnValue(of({ pendingQuiz: { id: '1' } }));
        notificationServiceSpy.loading.and.returnValue('loading-id');

        await TestBed.configureTestingModule({
            imports: [DashboardComponent, NoopAnimationsModule],
            providers: [
                { provide: ModuleService, useValue: moduleServiceSpy },
                { provide: MatDialog, useValue: dialogSpy },
                { provide: KnowledgeService, useValue: knowledgeServiceSpy },
                { provide: AdaptiveQuizService, useValue: adaptiveQuizServiceSpy },
                { provide: PendingService, useValue: pendingServiceSpy },
                { provide: NotificationService, useValue: notificationServiceSpy },
                {
                    provide: ActivatedRoute,
                    useValue: { snapshot: { paramMap: { get: () => null } } }
                }
            ]
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
