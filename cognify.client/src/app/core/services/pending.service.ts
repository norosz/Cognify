import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, Subscription, timer, forkJoin } from 'rxjs';
import { switchMap } from 'rxjs/operators';
import { NotificationService } from './notification.service';

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
    images?: ExtractedImageMetadataDto[] | null;
}

export interface ExtractedImageMetadataDto {
    id: string;
    blobPath: string;
    fileName: string;
    pageNumber: number;
    width?: number | null;
    height?: number | null;
    downloadUrl?: string | null;
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
    private notificationService = inject(NotificationService);
    private apiUrl = '/api/pending';

    pendingCount = signal<number>(0);

    private pollingSubscription: Subscription | null = null;
    private lastExtractedState = new Map<string, string>();
    private lastQuizState = new Map<string, string>();

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

    saveQuiz(id: string): Observable<{ quizId: string }> {
        return this.http.post<{ quizId: string }>(`${this.apiUrl}/quizzes/${id}/save`, {});
    }

    deletePendingQuiz(id: string): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/quizzes/${id}`);
    }

    refreshPendingCount(): void {
        this.http.get<number>(`${this.apiUrl}/count`).subscribe(count => {
            this.pendingCount.set(count);
        });
    }

    startPolling(): void {
        if (this.pollingSubscription) return;

        // Poll every 10 seconds
        this.pollingSubscription = timer(0, 10000).pipe(
            switchMap(() => forkJoin({
                extracted: this.getExtractedContents(),
                quizzes: this.getPendingQuizzes(),
                count: this.http.get<number>(`${this.apiUrl}/count`)
            }))
        ).subscribe({
            next: (results) => {
                this.pendingCount.set(results.count);
                this.checkExtractedStatus(results.extracted);
                this.checkQuizStatus(results.quizzes);
            },
            error: (err) => console.error('Polling error', err)
        });
    }

    stopPolling(): void {
        this.pollingSubscription?.unsubscribe();
        this.pollingSubscription = null;
    }

    private checkExtractedStatus(items: ExtractedContentDto[]): void {
        const currentIds = new Set(items.map(i => i.id));

        // Detect changes
        for (const item of items) {
            const prevState = this.lastExtractedState.get(item.id);

            // If we knew about it and it was Processing
            if (prevState === 'Processing') {
                if (item.status === 'Ready') {
                    this.notificationService.success(
                        `Document "${item.documentName}" is ready!`,
                        ['/pending', { tab: 'extractions' }],
                        'View Details'
                    );
                } else if (item.status === 'Error') {
                    this.notificationService.error(
                        `Failed to process "${item.documentName}": ${item.errorMessage}`
                    );
                }
            }

            // Initialize state if new (detected as Processing first time)
            if (!this.lastExtractedState.has(item.id) || this.lastExtractedState.get(item.id) !== item.status) {
                this.lastExtractedState.set(item.id, item.status);
            }
        }

        // Cleanup old IDs
        for (const id of this.lastExtractedState.keys()) {
            if (!currentIds.has(id)) {
                this.lastExtractedState.delete(id);
            }
        }
    }

    private checkQuizStatus(items: PendingQuizDto[]): void {
        const currentIds = new Set(items.map(i => i.id));

        for (const item of items) {
            const prevState = this.lastQuizState.get(item.id);

            if (prevState === 'Generating' || prevState === 'Processing') { // API uses 'Generating', DTO says status is string
                // The backend uses 'Generating' for created quizzes.
                // We should check what the string value actually is. 
                // In PendingQuizService.cs: Status = PendingQuizStatus.Generating (which is likely 0 or 1).
                // In PendingController, q.Status.ToString() is used.
                // Let's assume 'Generating' based on PendingQuizService.cs enum.

                if (item.status === 'Ready') {
                    this.notificationService.success(
                        `Quiz "${item.title}" is ready!`,
                        ['/pending', { tab: 'quizzes' }],
                        'View Quiz'
                    );
                } else if (item.status === 'Failed' || item.status === 'Error') {
                    this.notificationService.error(
                        `Failed to generate quiz "${item.title}": ${item.errorMessage}`
                    );
                }
            }

            if (!this.lastQuizState.has(item.id) || this.lastQuizState.get(item.id) !== item.status) {
                this.lastQuizState.set(item.id, item.status);
            }
        }

        for (const id of this.lastQuizState.keys()) {
            if (!currentIds.has(id)) {
                this.lastQuizState.delete(id);
            }
        }
    }
}
