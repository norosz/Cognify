import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

export interface ExtractedContentDto {
    id: string;
    documentId: string;
    moduleId: string;
    documentName: string;
    moduleName: string;
    text: string | null;
    extractedAt: string;
    status: 'Processing' | 'Ready' | 'Error';
    errorMessage?: string;
}

export interface PendingQuizDto {
    id: string;
    noteId: string;
    moduleId: string;
    title: string;
    noteName: string;
    moduleName: string;
    difficulty: string;
    questionType: string;
    questionCount: number;
    status: string;
    errorMessage?: string;
    actualQuestionCount?: number;
    createdAt: string;
}

@Injectable({
    providedIn: 'root'
})
export class PendingService {
    private http = inject(HttpClient);
    private apiUrl = '/api/pending';

    pendingCount = signal<number>(0);

    getExtractedContents(): Observable<ExtractedContentDto[]> {
        return this.http.get<ExtractedContentDto[]>(`${this.apiUrl}/extracted-contents`);
    }

    saveAsNote(id: string, title: string): Observable<{ noteId: string }> {
        return this.http.post<{ noteId: string }>(`${this.apiUrl}/extracted-contents/${id}/save-as-note`, { title });
    }

    deleteExtractedContent(id: string): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/extracted-contents/${id}`);
    }

    getPendingQuizzes(): Observable<PendingQuizDto[]> {
        return this.http.get<PendingQuizDto[]>(`${this.apiUrl}/quizzes`);
    }

    initiateQuiz(request: { noteId: string; title: string; difficulty: string; questionType: string; questionCount: number }): Observable<PendingQuizDto> {
        return this.http.post<PendingQuizDto>(`${this.apiUrl}/quizzes`, request);
    }

    saveQuiz(id: string): Observable<{ questionSetId: string }> {
        return this.http.post<{ questionSetId: string }>(`${this.apiUrl}/quizzes/${id}/save`, {});
    }

    deletePendingQuiz(id: string): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/quizzes/${id}`);
    }

    refreshPendingCount(): void {
        this.http.get<number>(`${this.apiUrl}/count`).subscribe(count => {
            this.pendingCount.set(count);
        });
    }
}
