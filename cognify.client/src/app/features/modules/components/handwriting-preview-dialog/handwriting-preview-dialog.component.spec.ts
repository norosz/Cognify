import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { HandwritingPreviewDialogComponent } from './handwriting-preview-dialog.component';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { NoteService } from '../../../../core/services/note.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { of, throwError } from 'rxjs';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';

describe('HandwritingPreviewDialogComponent', () => {
    let component: HandwritingPreviewDialogComponent;
    let fixture: ComponentFixture<HandwritingPreviewDialogComponent>;
    let mockDialogRef: jasmine.SpyObj<MatDialogRef<HandwritingPreviewDialogComponent>>;
    let mockNoteService: jasmine.SpyObj<NoteService>;
    let mockNotification: jasmine.SpyObj<NotificationService>;

    beforeEach(async () => {
        mockDialogRef = jasmine.createSpyObj('MatDialogRef', ['close']);
        mockNoteService = jasmine.createSpyObj('NoteService', ['createNote']);
        mockNotification = jasmine.createSpyObj('NotificationService', ['success', 'error']);

        await TestBed.configureTestingModule({
            imports: [HandwritingPreviewDialogComponent, NoopAnimationsModule],
            providers: [
                { provide: MatDialogRef, useValue: mockDialogRef },
                { provide: MAT_DIALOG_DATA, useValue: { text: 'Sample text', moduleId: 'mod-1' } },
                { provide: NoteService, useValue: mockNoteService },
                { provide: NotificationService, useValue: mockNotification }
            ]
        }).compileComponents();

        fixture = TestBed.createComponent(HandwritingPreviewDialogComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should initialize with data', () => {
        expect(component.text()).toBe('Sample text');
        expect(component.moduleId).toBe('mod-1');
        expect(component.title()).toBe(''); // Title starts empty
    });

    it('should not save if title is empty', () => {
        component.text.set('Valid text');
        component.title.set(''); // Empty title

        component.saveAsNote();

        expect(mockNoteService.createNote).not.toHaveBeenCalled();
    });

    it('should call noteService.createNote on save with title', fakeAsync(() => {
        mockNoteService.createNote.and.returnValue(of({ id: '1', title: 'My Title', content: 'test', moduleId: 'mod-1', createdAt: '' }));
        component.text.set('Valid text');
        component.title.set('My Title');

        component.saveAsNote();
        tick();

        expect(mockNoteService.createNote).toHaveBeenCalledWith({
            moduleId: 'mod-1',
            title: 'My Title',
            content: 'Valid text'
        });
        expect(mockNotification.success).toHaveBeenCalledWith('Note created!');
        expect(mockDialogRef.close).toHaveBeenCalled();
    }));

    it('should handle save error', fakeAsync(() => {
        spyOn(console, 'error');
        mockNoteService.createNote.and.returnValue(throwError(() => new Error('Error')));
        component.text.set('Valid text');
        component.title.set('My Title');

        component.saveAsNote();
        tick();

        expect(component.isSaving()).toBe(false);
        expect(mockNotification.error).toHaveBeenCalledWith('Failed to save note.');
        expect(console.error).toHaveBeenCalled();
    }));

    it('should close dialog on close', () => {
        component.close();
        expect(mockDialogRef.close).toHaveBeenCalled();
    });
});
