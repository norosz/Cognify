import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { FinalExamDto, FinalExamSaveResultDto, RegenerateFinalExamRequest } from '../models/exam.models';

@Injectable({
    providedIn: 'root'
})
export class FinalExamService {
    private http = inject(HttpClient);

    getFinalExam(moduleId: string): Observable<FinalExamDto> {
        return this.http.get<FinalExamDto>(`/api/modules/${moduleId}/final-exam`);
    }

    regenerateFinalExam(moduleId: string, request: RegenerateFinalExamRequest): Observable<{ pendingQuizId: string }> {
        return this.http.post<{ pendingQuizId: string }>(`/api/modules/${moduleId}/final-exam/regenerate`, request);
    }

    saveFinalExam(moduleId: string, pendingQuizId: string): Observable<FinalExamSaveResultDto> {
        return this.http.post<FinalExamSaveResultDto>(`/api/modules/${moduleId}/final-exam/pending/${pendingQuizId}/save`, {});
    }

    includeAllNotes(moduleId: string): Observable<{ updatedCount: number }> {
        return this.http.post<{ updatedCount: number }>(`/api/modules/${moduleId}/final-exam/include-all-notes`, {});
    }
}
