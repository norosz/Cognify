import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AdaptiveQuizRequest, AdaptiveQuizResponse } from '../models/knowledge.models';

@Injectable({
    providedIn: 'root'
})
export class AdaptiveQuizService {
    private http = inject(HttpClient);
    private apiUrl = '/api/adaptive-quizzes';

    createAdaptiveQuiz(request: AdaptiveQuizRequest): Observable<AdaptiveQuizResponse> {
        return this.http.post<AdaptiveQuizResponse>(this.apiUrl, request);
    }
}
