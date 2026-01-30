import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Note, CreateNoteRequest, UpdateNoteRequest, NoteSourcesDto, NoteExamInclusionRequest } from '../models/note.model';

@Injectable({
    providedIn: 'root'
})
export class NoteService {
    private apiUrl = '/api/notes';

    constructor(private http: HttpClient) { }

    getNotesByModuleId(moduleId: string): Observable<Note[]> {
        return this.http.get<Note[]>(`${this.apiUrl}/module/${moduleId}`);
    }

    getNoteById(id: string): Observable<Note> {
        return this.http.get<Note>(`${this.apiUrl}/${id}`);
    }

    getNoteSources(id: string): Observable<NoteSourcesDto> {
        return this.http.get<NoteSourcesDto>(`${this.apiUrl}/${id}/sources`);
    }

    createNote(request: CreateNoteRequest): Observable<Note> {
        return this.http.post<Note>(this.apiUrl, request);
    }

    updateNote(id: string, request: UpdateNoteRequest): Observable<Note> {
        return this.http.put<Note>(`${this.apiUrl}/${id}`, request);
    }

    updateFinalExamInclusion(id: string, request: NoteExamInclusionRequest): Observable<Note> {
        return this.http.put<Note>(`${this.apiUrl}/${id}/final-exam-inclusion`, request);
    }

    deleteNote(id: string): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/${id}`);
    }
}
