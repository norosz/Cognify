import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CreateQuestionSetDto, QuestionDto, QuestionSetDto, AttemptDto, SubmitAttemptDto } from '../../../core/models/quiz.models';

@Injectable({
    providedIn: 'root'
})
export class QuizService {
    private apiUrl = '/api';

    constructor(private http: HttpClient) { }

    generateQuestions(noteId: string): Observable<QuestionDto[]> {
        return this.http.post<QuestionDto[]>(`${this.apiUrl}/ai/questions/from-note/${noteId}`, {});
    }

    createQuestionSet(dto: CreateQuestionSetDto): Observable<QuestionSetDto> {
        return this.http.post<QuestionSetDto>(`${this.apiUrl}/question-sets`, dto);
    }

    getQuestionSet(id: string): Observable<QuestionSetDto> {
        return this.http.get<QuestionSetDto>(`${this.apiUrl}/question-sets/${id}`);
    }

    getQuestionSetsByNote(noteId: string): Observable<QuestionSetDto[]> {
        return this.http.get<QuestionSetDto[]>(`${this.apiUrl}/question-sets?noteId=${noteId}`);
    }

    deleteQuestionSet(id: string): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/question-sets/${id}`);
    }

    submitAttempt(dto: SubmitAttemptDto): Observable<AttemptDto> {
        return this.http.post<AttemptDto>(`${this.apiUrl}/question-sets/${dto.questionSetId}/attempts`, dto);
    }

    getAttempts(questionSetId: string): Observable<AttemptDto[]> {
        return this.http.get<AttemptDto[]>(`${this.apiUrl}/question-sets/${questionSetId}/attempts/me`);
    }
}
