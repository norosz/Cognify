import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { HandwritingPreviewDialogComponent } from './handwriting-preview-dialog.component';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { NoteService } from '../../../../core/services/note.service';
import { MatSnackBar } from '@angular/material/snack-bar';
import { of, throwError } from 'rxjs';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';

describe('HandwritingPreviewDialogComponent', () => {
    let component: HandwritingPreviewDialogComponent;
    let fixture: ComponentFixture<HandwritingPreviewDialogComponent>;
    let mockDialogRef: jasmine.SpyObj<MatDialogRef<HandwritingPreviewDialogComponent>>;
    let mockNoteService: jasmine.SpyObj<NoteService>;

    beforeEach(async () => {
        mockDialogRef = jasmine.createSpyObj('MatDialogRef', ['close']);
        mockNoteService = jasmine.createSpyObj('NoteService', ['createNote']);

        await TestBed.configureTestingModule({
            imports: [HandwritingPreviewDialogComponent, NoopAnimationsModule],
            providers: [
                { provide: MatDialogRef, useValue: mockDialogRef },
                { provide: MAT_DIALOG_DATA, useValue: { text: 'Sample text', moduleId: 'mod-1' } },
                { provide: NoteService, useValue: mockNoteService }
            ]
        }).compileComponents();

        spyOn(MatSnackBar.prototype, 'open');

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
    });

    it('should call noteService.createNote on save', fakeAsync(() => {
        mockNoteService.createNote.and.returnValue(of({ id: '1', title: 'test', content: 'test', moduleId: 'mod-1', createdAt: '' }));
        component.text.set('Valid text');

        component.saveAsNote();
        tick();
        fixture.detectChanges();

        expect(component.isSaving()).toBe(true);
        expect(mockNoteService.createNote).toHaveBeenCalled();
        expect(MatSnackBar.prototype.open).toHaveBeenCalledWith('Note created!', 'Close', jasmine.any(Object));
        expect(mockDialogRef.close).toHaveBeenCalled();
    }));

    it('should handle save error', fakeAsync(() => {
        spyOn(console, 'error');
        mockNoteService.createNote.and.returnValue(throwError(() => new Error('Error')));
        component.text.set('Valid text');

        component.saveAsNote();
        tick();

        expect(component.isSaving()).toBe(false);
        expect(MatSnackBar.prototype.open).toHaveBeenCalledWith('Failed to save note.', 'Close', jasmine.any(Object));
        expect(console.error).toHaveBeenCalled();
    }));

    it('should close dialog on close', () => {
        component.close();
        expect(mockDialogRef.close).toHaveBeenCalled();
    });
});
