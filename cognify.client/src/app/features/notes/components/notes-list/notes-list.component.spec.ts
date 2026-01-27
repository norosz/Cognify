import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NotesListComponent } from './notes-list.component';
import { NoteService } from '../../../../core/services/note.service';
import { MatDialog } from '@angular/material/dialog';
import { NotificationService } from '../../../../core/services/notification.service';
import { PendingService } from '../../../../core/services/pending.service';
import { of, Subject } from 'rxjs';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';

describe('NotesListComponent', () => {
    let component: NotesListComponent;
    let fixture: ComponentFixture<NotesListComponent>;
    let noteServiceSpy: jasmine.SpyObj<NoteService>;
    let dialogSpy: jasmine.SpyObj<MatDialog>;
    let notificationSpy: jasmine.SpyObj<NotificationService>;
    let pendingServiceSpy: jasmine.SpyObj<PendingService>;

    beforeEach(async () => {
        noteServiceSpy = jasmine.createSpyObj('NoteService', ['getNotesByModuleId', 'deleteNote']);
        dialogSpy = jasmine.createSpyObj('MatDialog', ['open']);
        notificationSpy = jasmine.createSpyObj('NotificationService', ['success', 'error']);
        pendingServiceSpy = jasmine.createSpyObj('PendingService', ['refreshPendingCount']);

        await TestBed.configureTestingModule({
            imports: [NotesListComponent, NoopAnimationsModule],
            providers: [
                { provide: NoteService, useValue: noteServiceSpy },
                { provide: MatDialog, useValue: dialogSpy },
                { provide: NotificationService, useValue: notificationSpy },
                { provide: PendingService, useValue: pendingServiceSpy }
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

    it('should delete note and show success notification', () => {
        component.moduleId = '123';
        noteServiceSpy.getNotesByModuleId.and.returnValue(of([]));
        noteServiceSpy.deleteNote.and.returnValue(of(void 0));
        spyOn(window, 'confirm').and.returnValue(true);

        component.deleteNote({ id: 'note-1', title: 'Test', content: '', moduleId: '123', createdAt: '' });

        expect(noteServiceSpy.deleteNote).toHaveBeenCalledWith('note-1');
        expect(notificationSpy.success).toHaveBeenCalledWith('Note deleted');
    });
});
