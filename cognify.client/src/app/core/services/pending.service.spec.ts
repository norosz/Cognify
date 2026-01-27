import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { PendingService, ExtractedContentDto, PendingQuizDto } from './pending.service';

describe('PendingService', () => {
    let service: PendingService;
    let httpMock: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [HttpClientTestingModule],
            providers: [PendingService]
        });
        service = TestBed.inject(PendingService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    describe('getExtractedContents', () => {
        it('should return extracted contents', () => {
            const mockContents: ExtractedContentDto[] = [
                { id: '1', documentId: 'd1', moduleId: 'm1', documentName: 'Doc1', moduleName: 'Mod1', text: 'content', extractedAt: '2024-01-01', status: 'Ready' }
            ];

            service.getExtractedContents().subscribe(contents => {
                expect(contents.length).toBe(1);
                expect(contents[0].documentName).toBe('Doc1');
            });

            const req = httpMock.expectOne('/api/pending/extracted-contents');
            expect(req.request.method).toBe('GET');
            req.flush(mockContents);
        });
    });

    describe('saveAsNote', () => {
        it('should POST to save as note endpoint', () => {
            service.saveAsNote('123', 'My Note').subscribe(result => {
                expect(result.noteId).toBe('note-456');
            });

            const req = httpMock.expectOne('/api/pending/extracted-contents/123/save-as-note');
            expect(req.request.method).toBe('POST');
            expect(req.request.body).toEqual({ title: 'My Note' });
            req.flush({ noteId: 'note-456' });
        });
    });

    describe('deleteExtractedContent', () => {
        it('should DELETE extracted content', () => {
            service.deleteExtractedContent('123').subscribe();

            const req = httpMock.expectOne('/api/pending/extracted-contents/123');
            expect(req.request.method).toBe('DELETE');
            req.flush(null);
        });
    });

    describe('getPendingQuizzes', () => {
        it('should return pending quizzes', () => {
            const mockQuizzes: PendingQuizDto[] = [
                { id: '1', noteId: 'n1', moduleId: 'm1', title: 'Quiz1', noteName: 'Note1', moduleName: 'Mod1', difficulty: 'medium', questionType: 'MCQ', questionCount: 5, status: 'Ready', createdAt: '2024-01-01' }
            ];

            service.getPendingQuizzes().subscribe(quizzes => {
                expect(quizzes.length).toBe(1);
                expect(quizzes[0].title).toBe('Quiz1');
            });

            const req = httpMock.expectOne('/api/pending/quizzes');
            expect(req.request.method).toBe('GET');
            req.flush(mockQuizzes);
        });
    });

    describe('initiateQuiz', () => {
        it('should POST to initiate quiz', () => {
            const request = { noteId: 'n1', title: 'New Quiz', difficulty: 'hard', questionType: 'MCQ', questionCount: 10 };

            service.initiateQuiz(request).subscribe();

            const req = httpMock.expectOne('/api/pending/quizzes');
            expect(req.request.method).toBe('POST');
            expect(req.request.body).toEqual(request);
            req.flush({});
        });
    });

    describe('saveQuiz', () => {
        it('should POST to save quiz', () => {
            service.saveQuiz('quiz-123').subscribe(result => {
                expect(result.questionSetId).toBe('qs-456');
            });

            const req = httpMock.expectOne('/api/pending/quizzes/quiz-123/save');
            expect(req.request.method).toBe('POST');
            req.flush({ questionSetId: 'qs-456' });
        });
    });

    describe('deletePendingQuiz', () => {
        it('should DELETE pending quiz', () => {
            service.deletePendingQuiz('quiz-123').subscribe();

            const req = httpMock.expectOne('/api/pending/quizzes/quiz-123');
            expect(req.request.method).toBe('DELETE');
            req.flush(null);
        });
    });

    describe('refreshPendingCount', () => {
        it('should update pendingCount signal', () => {
            service.refreshPendingCount();

            const req = httpMock.expectOne('/api/pending/count');
            expect(req.request.method).toBe('GET');
            req.flush(5);

            expect(service.pendingCount()).toBe(5);
        });
    });
});
