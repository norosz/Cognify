import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NoteEditorDialogComponent } from './note-editor-dialog.component';
import { NoteService } from '../../../../core/services/note.service';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { ReactiveFormsModule } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';

describe('NoteEditorDialogComponent', () => {
    let component: NoteEditorDialogComponent;
    let fixture: ComponentFixture<NoteEditorDialogComponent>;
    let noteServiceSpy: jasmine.SpyObj<NoteService>;
    let dialogRefSpy: jasmine.SpyObj<MatDialogRef<NoteEditorDialogComponent>>;

    beforeEach(async () => {
        noteServiceSpy = jasmine.createSpyObj('NoteService', ['createNote', 'updateNote']);
        dialogRefSpy = jasmine.createSpyObj('MatDialogRef', ['close']);

        await TestBed.configureTestingModule({
            imports: [NoteEditorDialogComponent, ReactiveFormsModule, NoopAnimationsModule],
            providers: [
                { provide: NoteService, useValue: noteServiceSpy },
                { provide: MatDialogRef, useValue: dialogRefSpy },
                { provide: MAT_DIALOG_DATA, useValue: { moduleId: '123' } }
            ]
        }).compileComponents();

        fixture = TestBed.createComponent(NoteEditorDialogComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should initialize form', () => {
        expect(component.form).toBeDefined();
        expect(component.form.get('title')).toBeDefined();
    });

    it('should call createNote on save when in create mode', () => {
        component.form.patchValue({ title: 'New Note', content: 'Content' });
        noteServiceSpy.createNote.and.returnValue(of({} as any));

        component.save();

        expect(noteServiceSpy.createNote).toHaveBeenCalled();
        expect(dialogRefSpy.close).toHaveBeenCalled();
    });
});
