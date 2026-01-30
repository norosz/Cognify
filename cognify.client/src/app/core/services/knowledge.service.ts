import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ReviewQueueItemDto, UserKnowledgeStateDto } from '../models/knowledge.models';

@Injectable({
    providedIn: 'root'
})
export class KnowledgeService {
    private http = inject(HttpClient);
    private apiUrl = '/api/knowledge-states';

    getStates(): Observable<UserKnowledgeStateDto[]> {
        return this.http.get<UserKnowledgeStateDto[]>(this.apiUrl);
    }

    getReviewQueue(maxItems = 10, includeExams = false): Observable<ReviewQueueItemDto[]> {
        return this.http.get<ReviewQueueItemDto[]>(`${this.apiUrl}/review-queue`, {
            params: { maxItems, includeExams }
        });
    }
}
