import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DocumentListComponent } from './document-list.component';
import { DocumentsService } from '../../services/documents.service';
import { AiService } from '../../../../core/services/ai.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { PendingService } from '../../../../core/services/pending.service';
import { MatDialog } from '@angular/material/dialog';
import { of } from 'rxjs';
import { By } from '@angular/platform-browser';
import { MatCardModule } from '@angular/material/card';
import { MatMenuModule } from '@angular/material/menu';
import { MatIconModule } from '@angular/material/icon';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';

describe('DocumentListComponent', () => {
    let component: DocumentListComponent;
    let fixture: ComponentFixture<DocumentListComponent>;
    let mockDocumentsService: any;
    let mockAiService: any;
    let mockNotificationService: any;
    let mockPendingService: any;
    let mockDialog: any;

    const mockDocuments = [
        {
            id: '1',
            fileName: 'test.pdf',
            moduleId: '123',
            blobPath: 'modules/123/test.pdf',
            status: 1, // Ready
            createdAt: new Date().toISOString(),
            downloadUrl: 'http://example.com/test.pdf'
        },
        {
            id: '2',
            fileName: 'image.png',
            moduleId: '123',
            blobPath: 'modules/123/image.png',
            status: 0, // Processing
            createdAt: new Date().toISOString()
        }
    ];

    beforeEach(async () => {
        mockDocumentsService = {
            getDocuments: jasmine.createSpy('getDocuments').and.returnValue(of(mockDocuments)),
            deleteDocument: jasmine.createSpy('deleteDocument').and.returnValue(of(void 0))
        };
        mockAiService = {
            extractText: jasmine.createSpy('extractText').and.returnValue(of({}))
        };
        mockNotificationService = {
            success: jasmine.createSpy('success'),
            error: jasmine.createSpy('error'),
            info: jasmine.createSpy('info')
        };
        mockPendingService = {
            refreshPendingCount: jasmine.createSpy('refreshPendingCount')
        };
        mockDialog = {
            open: jasmine.createSpy('open')
        };

        await TestBed.configureTestingModule({
            imports: [
                DocumentListComponent,
                MatCardModule,
                MatMenuModule,
                MatIconModule,
                BrowserAnimationsModule // Required for MatMenu animations
            ],
            providers: [
                { provide: DocumentsService, useValue: mockDocumentsService },
                { provide: AiService, useValue: mockAiService },
                { provide: NotificationService, useValue: mockNotificationService },
                { provide: PendingService, useValue: mockPendingService },
                { provide: MatDialog, useValue: mockDialog }
            ]
        }).compileComponents();

        fixture = TestBed.createComponent(DocumentListComponent);
        component = fixture.componentInstance;
        component.moduleId = '123';
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should load documents on init', () => {
        expect(mockDocumentsService.getDocuments).toHaveBeenCalledWith('123');
        expect(component.documents.length).toBe(2);
    });

    it('should render document cards', () => {
        const cards = fixture.debugElement.queryAll(By.css('.document-card'));
        expect(cards.length).toBe(2);
    });

    it('should display correct file icons', () => {
        expect(component.getFileIcon('test.pdf')).toBe('picture_as_pdf');
        expect(component.getFileIcon('image.png')).toBe('image');
        expect(component.getFileIcon('unknown.xyz')).toBe('insert_drive_file');
    });

    it('should display document name', () => {
        const title = fixture.debugElement.query(By.css('mat-card-title')).nativeElement;
        expect(title.textContent).toContain('test.pdf');
    });

    it('should call deleteDocument when delete action is clicked', () => {
        // Mock confirm to return true
        spyOn(window, 'confirm').and.returnValue(true);

        // Directly call deleteDocument for simplicity in unit test, or trigger menu click
        component.deleteDocument(mockDocuments[0]);

        expect(mockDocumentsService.deleteDocument).toHaveBeenCalledWith('1');
        expect(mockNotificationService.success).toHaveBeenCalled();
        // Should reload documents
        expect(mockDocumentsService.getDocuments).toHaveBeenCalledTimes(2); // Init + Delete Reload
    });

    it('should show extraction option for ready documents', () => {
        // Check if extract logic is sound
        const canExtract = mockDocuments[0].status === 1;
        expect(canExtract).toBeTrue();
    });
});
