import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NotesListComponent } from './notes-list.component';
import { NoteService } from '../../../../core/services/note.service';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { of } from 'rxjs';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';

describe('NotesListComponent', () => {
    let component: NotesListComponent;
    let fixture: ComponentFixture<NotesListComponent>;
    let noteServiceSpy: jasmine.SpyObj<NoteService>;
    let dialogSpy: jasmine.SpyObj<MatDialog>;

    beforeEach(async () => {
        noteServiceSpy = jasmine.createSpyObj('NoteService', ['getNotesByModuleId', 'deleteNote']);
        dialogSpy = jasmine.createSpyObj('MatDialog', ['open']);
        const snackBarSpy = jasmine.createSpyObj('MatSnackBar', ['open']);

        await TestBed.configureTestingModule({
            imports: [NotesListComponent, NoopAnimationsModule],
            providers: [
                { provide: NoteService, useValue: noteServiceSpy },
                { provide: MatDialog, useValue: dialogSpy },
                { provide: MatSnackBar, useValue: snackBarSpy }
            ]
        }).compileComponents();

        fixture = TestBed.createComponent(NotesListComponent);
        component = fixture.componentInstance;
    });

    it('should load notes on init if moduleId is present', () => {
        component.moduleId = '123';
        noteServiceSpy.getNotesByModuleId.and.returnValue(of([]));

        component.ngOnInit();

        expect(noteServiceSpy.getNotesByModuleId).toHaveBeenCalledWith('123');
        expect(component.isLoading()).toBeFalse();
    });
});
