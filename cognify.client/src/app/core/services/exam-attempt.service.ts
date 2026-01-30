import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AttemptReviewDto } from '../models/quiz.models';
import { ExamAttemptDto, SubmitExamAttemptDto } from '../models/exam.models';

@Injectable({
    providedIn: 'root'
})
export class ExamAttemptService {
    private http = inject(HttpClient);

    submitExamAttempt(moduleId: string, dto: SubmitExamAttemptDto): Observable<ExamAttemptDto> {
        return this.http.post<ExamAttemptDto>(`/api/modules/${moduleId}/exam-attempts`, dto);
    }

    getExamAttempts(moduleId: string): Observable<ExamAttemptDto[]> {
        return this.http.get<ExamAttemptDto[]>(`/api/modules/${moduleId}/exam-attempts/me`);
    }

    getExamAttempt(moduleId: string, examAttemptId: string): Observable<ExamAttemptDto> {
        return this.http.get<ExamAttemptDto>(`/api/modules/${moduleId}/exam-attempts/${examAttemptId}`);
    }

    getExamAttemptReview(moduleId: string, examAttemptId: string): Observable<AttemptReviewDto> {
        return this.http.get<AttemptReviewDto>(`/api/modules/${moduleId}/exam-attempts/${examAttemptId}/review`);
    }
}
