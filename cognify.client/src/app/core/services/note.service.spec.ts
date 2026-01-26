import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { NoteService } from './note.service';
import { Note } from '../models/note.model';

describe('NoteService', () => {
    let service: NoteService;
    let httpMock: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [
                NoteService,
                provideHttpClient(),
                provideHttpClientTesting()
            ]
        });
        service = TestBed.inject(NoteService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should retrieve notes by module id', () => {
        const dummyNotes: Note[] = [
            { id: '1', moduleId: 'm1', title: 'N1', content: 'C1', createdAt: 'date' },
            { id: '2', moduleId: 'm1', title: 'N2', content: 'C2', createdAt: 'date' }
        ];

        service.getNotesByModuleId('m1').subscribe(notes => {
            expect(notes.length).toBe(2);
            expect(notes).toEqual(dummyNotes);
        });

        const req = httpMock.expectOne('/api/notes/module/m1');
        expect(req.request.method).toBe('GET');
        req.flush(dummyNotes);
    });

    it('should create a note', () => {
        const newNote: Note = { id: '3', moduleId: 'm1', title: 'N3', content: 'C3', createdAt: 'now' };

        service.createNote({ moduleId: 'm1', title: 'N3', content: 'C3' }).subscribe(note => {
            expect(note).toEqual(newNote);
        });

        const req = httpMock.expectOne('/api/notes');
        expect(req.request.method).toBe('POST');
        req.flush(newNote);
    });
});
