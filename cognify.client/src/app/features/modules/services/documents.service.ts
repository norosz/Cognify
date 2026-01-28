import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpBackend } from '@angular/common/http';
import { Observable, from, of, switchMap, catchError, tap } from 'rxjs';

export interface DocumentDto {
    id: string;
    moduleId: string;
    blobPath: string;
    fileName: string;
    status: number;
    createdAt: string;
    fileSize: number;
    downloadUrl?: string;
}

export interface UploadInitiateRequest {
    fileName: string;
    contentType: string;
    fileSize: number;
}

export interface UploadInitiateResponse {
    documentId: string;
    sasUrl: string;
    blobName: string;
}

@Injectable({
    providedIn: 'root'
})
export class DocumentsService {
    private apiUrl = '/api';
    private directHttp: HttpClient;

    constructor(private http: HttpClient, handler: HttpBackend) {
        // Create a new HttpClient that bypasses all interceptors
        this.directHttp = new HttpClient(handler);
    }

    getDocuments(moduleId: string): Observable<DocumentDto[]> {
        return this.http.get<DocumentDto[]>(`${this.apiUrl}/modules/${moduleId}/documents`);
    }

    // Orchestrates the full SAS Upload Flow
    uploadDocument(moduleId: string, file: File): Observable<DocumentDto> {
        return this.initiateUpload(moduleId, file).pipe(
            switchMap(initResponse => {
                // Step 2: Upload to Blob (Directly)
                return this.uploadToBlob(initResponse.sasUrl, file).pipe(
                    // Step 3: Complete
                    switchMap(() => this.completeUpload(initResponse.documentId))
                );
            })
        );
    }

    public initiateUpload(moduleId: string, file: File): Observable<UploadInitiateResponse> {
        const initReq: UploadInitiateRequest = {
            fileName: file.name,
            contentType: file.type,
            fileSize: file.size
        };

        const url = `${this.apiUrl}/modules/${moduleId}/documents/initiate`;

        // Manually add Auth Header since we bypassed the interceptor
        const token = localStorage.getItem('cognify_token');
        const headers = new HttpHeaders({
            'Authorization': `Bearer ${token}`
        });

        return this.directHttp.post<UploadInitiateResponse>(url, initReq, { headers }).pipe(
            tap({
                next: (res) => console.log('HTTP Success:', res),
                error: (err) => console.error('HTTP Error in Service:', err)
            }),
            catchError((err) => {
                console.error('Caught error in service pipe:', err);
                throw err;
            })
        );
    }

    private uploadToBlob(sasUrl: string, file: File): Observable<void> {
        console.log('Uploading blob to:', sasUrl);
        const headers = new HttpHeaders({
            'x-ms-blob-type': 'BlockBlob',
            'Content-Type': file.type
        });

        // Use directHttp to avoid adding Auth Headers to the Azure Blob Request (which would fail)
        return this.directHttp.put<void>(sasUrl, file, { headers });
    }

    private completeUpload(documentId: string): Observable<DocumentDto> {
        const url = `${this.apiUrl}/documents/${documentId}/complete`;

        // Manual Auth Header
        const token = localStorage.getItem('cognify_token');
        const headers = new HttpHeaders({
            'Authorization': `Bearer ${token}`
        });

        return this.directHttp.post<DocumentDto>(url, {}, { headers });
    }

    deleteDocument(documentId: string): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/documents/${documentId}`);
    }
}
