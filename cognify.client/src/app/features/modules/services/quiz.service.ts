import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CreateQuizDto, QuizDto, AttemptDto, SubmitAttemptDto } from '../../../core/models/quiz.models';

@Injectable({
    providedIn: 'root'
})
export class QuizService {
    private apiUrl = '/api';

    constructor(private http: HttpClient) { }

    createQuiz(dto: CreateQuizDto): Observable<QuizDto> {
        return this.http.post<QuizDto>(`${this.apiUrl}/quizzes`, dto);
    }

    getQuiz(id: string): Observable<QuizDto> {
        return this.http.get<QuizDto>(`${this.apiUrl}/quizzes/${id}`);
    }

    getQuizzesByNote(noteId: string): Observable<QuizDto[]> {
        return this.http.get<QuizDto[]>(`${this.apiUrl}/quizzes?noteId=${noteId}`);
    }

    deleteQuiz(id: string): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/quizzes/${id}`);
    }

    submitAttempt(dto: SubmitAttemptDto): Observable<AttemptDto> {
        return this.http.post<AttemptDto>(`${this.apiUrl}/quizzes/${dto.quizId}/attempts`, dto);
    }

    getAttempts(quizId: string): Observable<AttemptDto[]> {
        return this.http.get<AttemptDto[]>(`${this.apiUrl}/quizzes/${quizId}/attempts/me`);
    }
}
