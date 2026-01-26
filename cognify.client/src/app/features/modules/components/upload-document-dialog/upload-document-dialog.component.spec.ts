import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { UploadDocumentDialogComponent } from './upload-document-dialog.component';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { DocumentsService } from '../../services/documents.service';
import { of, throwError, Subject } from 'rxjs';
import { By } from '@angular/platform-browser';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';

describe('UploadDocumentDialogComponent', () => {
    let component: UploadDocumentDialogComponent;
    let fixture: ComponentFixture<UploadDocumentDialogComponent>;
    let mockDialogRef: jasmine.SpyObj<MatDialogRef<UploadDocumentDialogComponent>>;
    let mockDocumentsService: jasmine.SpyObj<DocumentsService>;

    beforeEach(async () => {
        mockDialogRef = jasmine.createSpyObj('MatDialogRef', ['close']);
        mockDocumentsService = jasmine.createSpyObj('DocumentsService', ['uploadDocument']);

        await TestBed.configureTestingModule({
            imports: [UploadDocumentDialogComponent, NoopAnimationsModule],
            providers: [
                { provide: MatDialogRef, useValue: mockDialogRef },
                { provide: MAT_DIALOG_DATA, useValue: { moduleId: 'module-123' } },
                { provide: DocumentsService, useValue: mockDocumentsService }
            ]
        })
            .compileComponents();

        fixture = TestBed.createComponent(UploadDocumentDialogComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should initialize with null selectedFile', () => {
        expect(component.selectedFile).toBeNull();
        expect(component.isUploading).toBeFalse();
    });

    it('should update selectedFile on file selection', () => {
        const file = new File([''], 'test.txt', { type: 'text/plain' });
        const event = { target: { files: [file] } };

        component.onFileSelected(event);

        expect(component.selectedFile).toBe(file);
        expect(component.uploadError).toBeNull();
    });

    it('should call uploadDocument and close dialog on success', fakeAsync(() => {
        const file = new File([''], 'test.txt', { type: 'text/plain' });
        component.selectedFile = file;
        const subject = new Subject<any>();
        mockDocumentsService.uploadDocument.and.returnValue(subject.asObservable());

        component.upload();

        expect(component.isUploading).toBeTrue();

        subject.next({});
        subject.complete();
        tick(); // Simulate async completion

        expect(mockDocumentsService.uploadDocument).toHaveBeenCalledWith('module-123', file);
        expect(component.isUploading).toBeFalse();
        expect(mockDialogRef.close).toHaveBeenCalledWith(true);
    }));

    it('should handle upload error', fakeAsync(() => {
        const file = new File([''], 'test.txt', { type: 'text/plain' });
        component.selectedFile = file;
        mockDocumentsService.uploadDocument.and.returnValue(throwError(() => new Error('Upload failed')));

        component.upload();
        tick();

        expect(component.isUploading).toBeFalse();
        expect(component.uploadError).toContain('Failed to upload file');
        expect(mockDialogRef.close).not.toHaveBeenCalled();
    }));

    it('should close dialog with false on cancel', () => {
        component.cancel();
        expect(mockDialogRef.close).toHaveBeenCalledWith(false);
    });
});
