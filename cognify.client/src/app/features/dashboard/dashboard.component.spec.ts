import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DashboardComponent } from './dashboard.component';
import { ModuleService } from '../../core/modules/module.service';
import { MatDialog } from '@angular/material/dialog';
import { of } from 'rxjs';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ActivatedRoute } from '@angular/router';

describe('DashboardComponent', () => {
    let component: DashboardComponent;
    let fixture: ComponentFixture<DashboardComponent>;
    let moduleServiceSpy: jasmine.SpyObj<ModuleService>;
    let dialogSpy: jasmine.SpyObj<MatDialog>;

    beforeEach(async () => {
        moduleServiceSpy = jasmine.createSpyObj('ModuleService', ['getModules', 'deleteModule']);
        dialogSpy = jasmine.createSpyObj('MatDialog', ['open']);

        moduleServiceSpy.getModules.and.returnValue(of([]));

        await TestBed.configureTestingModule({
            imports: [DashboardComponent, NoopAnimationsModule],
            providers: [
                { provide: ModuleService, useValue: moduleServiceSpy },
                { provide: MatDialog, useValue: dialogSpy },
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
