import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface DocumentDto {
    id: string;
    moduleId: string;
    blobPath: string;
    fileName: string;
    status: number; // 0=Processing, 1=Ready, 2=Error
    createdAt: string;
}

@Injectable({
    providedIn: 'root'
})
export class DocumentsService {
    // Using relative path for proxy
    private apiUrl = '/api';

    constructor(private http: HttpClient) { }

    getDocuments(moduleId: string): Observable<DocumentDto[]> {
        return this.http.get<DocumentDto[]>(`${this.apiUrl}/modules/${moduleId}/documents`);
    }

    uploadDocument(moduleId: string, file: File): Observable<DocumentDto> {
        const formData = new FormData();
        formData.append('file', file);

        return this.http.post<DocumentDto>(`${this.apiUrl}/modules/${moduleId}/documents`, formData);
    }

    deleteDocument(documentId: string): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/documents/${documentId}`);
    }
}
