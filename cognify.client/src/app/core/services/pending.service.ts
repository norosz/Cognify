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
    status: 'Processing' | 'Ready' | 'Error' | 'Completed' | 'Failed';
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
    noteId?: string | null;
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
    private readonly extractedStateStorageKey = 'cognify.pending.extractions.lastState.v1';
    private readonly quizStateStorageKey = 'cognify.pending.quizzes.lastState.v1';

    pendingCount = signal<number>(0);
    extractedContents = signal<ExtractedContentDto[]>([]);
    pendingQuizzes = signal<PendingQuizDto[]>([]);

    private pollingSubscription: Subscription | null = null;
    private lastExtractedState = new Map<string, string>();
    private lastQuizState = new Map<string, string>();
    private hasHydratedState = false;

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

    refreshAll(): Observable<{ extracted: ExtractedContentDto[]; quizzes: PendingQuizDto[]; count: number }> {
        return forkJoin({
            extracted: this.getExtractedContents(),
            quizzes: this.getPendingQuizzes(),
            count: this.http.get<number>(`${this.apiUrl}/count`)
        }).pipe(
            tap(results => {
                this.extractedContents.set(results.extracted);
                this.pendingQuizzes.set(results.quizzes);
                this.pendingCount.set(results.count);
            })
        );
    }

    startPolling(): void {
        if (this.pollingSubscription) return;

        if (!this.hasHydratedState) {
            this.loadStateFromStorage(this.extractedStateStorageKey, this.lastExtractedState);
            this.loadStateFromStorage(this.quizStateStorageKey, this.lastQuizState);
            this.hasHydratedState = true;
        }

        // Poll every 5 seconds
        this.pollingSubscription = timer(0, 5000).pipe(
            switchMap(() => forkJoin({
                extracted: this.getExtractedContents(),
                quizzes: this.getPendingQuizzes(),
                count: this.http.get<number>(`${this.apiUrl}/count`)
            }))
        ).subscribe({
            next: (results) => {
                this.pendingCount.set(results.count);
                this.extractedContents.set(results.extracted);
                this.pendingQuizzes.set(results.quizzes);
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
            const currentStatus = this.normalizeExtractionStatus(item.status);

            // Notify on transition from Processing to terminal
            if (prevState === 'Processing') {
                if (currentStatus === 'Completed') {
                    this.notificationService.success(
                        `Document "${item.documentName}" is ready!`,
                        ['/pending', { tab: 'extractions' }],
                        'View Details'
                    );
                } else if (currentStatus === 'Failed') {
                    this.notificationService.error(
                        `Failed to process "${item.documentName}": ${item.errorMessage}`
                    );
                }
            }

            // If we first see it already terminal, notify once
            if (!prevState && this.isExtractionTerminal(currentStatus)) {
                if (currentStatus === 'Completed') {
                    this.notificationService.success(
                        `Document "${item.documentName}" is ready!`,
                        ['/pending', { tab: 'extractions' }],
                        'View Details'
                    );
                } else if (currentStatus === 'Failed') {
                    this.notificationService.error(
                        `Failed to process "${item.documentName}": ${item.errorMessage}`
                    );
                }
            }

            // Initialize state if new (detected as Processing first time)
            if (!this.lastExtractedState.has(item.id) || this.lastExtractedState.get(item.id) !== currentStatus) {
                this.lastExtractedState.set(item.id, currentStatus);
            }
        }

        // Cleanup old IDs
        for (const id of this.lastExtractedState.keys()) {
            if (!currentIds.has(id)) {
                this.lastExtractedState.delete(id);
            }
        }

        this.persistState(this.extractedStateStorageKey, this.lastExtractedState);
    }

    private checkQuizStatus(items: PendingQuizDto[]): void {
        const currentIds = new Set(items.map(i => i.id));

        for (const item of items) {
            const prevState = this.lastQuizState.get(item.id);
            const currentStatus = this.normalizeQuizStatus(item.status);
            const wasInProgress = prevState === 'Pending' || prevState === 'Processing' || prevState === 'Generating';

            if (wasInProgress) {
                if (currentStatus === 'Completed') {
                    this.notificationService.success(
                        `Quiz "${item.title}" is ready!`,
                        ['/pending', { tab: 'quizzes' }],
                        'View Quiz'
                    );
                } else if (currentStatus === 'Failed') {
                    this.notificationService.error(
                        `Failed to generate quiz "${item.title}": ${item.errorMessage}`
                    );
                }
            }

            if (!prevState && this.isQuizTerminal(currentStatus)) {
                if (currentStatus === 'Completed') {
                    this.notificationService.success(
                        `Quiz "${item.title}" is ready!`,
                        ['/pending', { tab: 'quizzes' }],
                        'View Quiz'
                    );
                } else if (currentStatus === 'Failed') {
                    this.notificationService.error(
                        `Failed to generate quiz "${item.title}": ${item.errorMessage}`
                    );
                }
            }

            if (!this.lastQuizState.has(item.id) || this.lastQuizState.get(item.id) !== currentStatus) {
                this.lastQuizState.set(item.id, currentStatus);
            }
        }

        for (const id of this.lastQuizState.keys()) {
            if (!currentIds.has(id)) {
                this.lastQuizState.delete(id);
            }
        }

        this.persistState(this.quizStateStorageKey, this.lastQuizState);
    }

    private normalizeExtractionStatus(status: string): string {
        if (status === 'Ready') return 'Completed';
        if (status === 'Error') return 'Failed';
        return status;
    }

    private normalizeQuizStatus(status: string): string {
        if (status === 'Ready') return 'Completed';
        if (status === 'Error') return 'Failed';
        return status;
    }

    private isExtractionTerminal(status: string): boolean {
        return status === 'Completed' || status === 'Failed';
    }

    private isQuizTerminal(status: string): boolean {
        return status === 'Completed' || status === 'Failed';
    }

    private loadStateFromStorage(storageKey: string, target: Map<string, string>): void {
        try {
            const raw = localStorage.getItem(storageKey);
            if (!raw) return;
            const parsed = JSON.parse(raw) as Record<string, string> | null;
            if (!parsed || typeof parsed !== 'object') return;
            for (const [id, status] of Object.entries(parsed)) {
                if (typeof status === 'string') {
                    target.set(id, status);
                }
            }
        } catch {
            // Ignore storage parse errors
        }
    }

    private persistState(storageKey: string, source: Map<string, string>): void {
        const payload: Record<string, string> = {};
        for (const [id, status] of source.entries()) {
            payload[id] = status;
        }
        try {
            localStorage.setItem(storageKey, JSON.stringify(payload));
        } catch {
            // Ignore storage write errors
        }
    }
}
