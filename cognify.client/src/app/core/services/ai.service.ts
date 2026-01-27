import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { GenerateQuestionsRequest, GeneratedQuestion, GradeAnswerRequest, GradingResult, TextExtractionResult } from '../models/ai.models';

@Injectable({
    providedIn: 'root'
})
export class AiService {
    private apiUrl = '/api/ai';

    constructor(private http: HttpClient) { }

    extractText(documentId: string): Observable<TextExtractionResult> {
        return this.http.post<TextExtractionResult>(`${this.apiUrl}/extract-text/${documentId}`, {});
    }

    generateQuestions(req: GenerateQuestionsRequest): Observable<GeneratedQuestion[]> {
        return this.http.post<GeneratedQuestion[]>(`${this.apiUrl}/questions/generate`, req);
    }

    gradeAnswer(req: GradeAnswerRequest): Observable<GradingResult> {
        return this.http.post<GradingResult>(`${this.apiUrl}/grade`, req);
    }
}
