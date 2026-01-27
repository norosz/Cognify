import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AiService } from './ai.service';
import { QuestionType, GenerateQuestionsRequest, GradeAnswerRequest } from '../models/ai.models';
import { provideHttpClient } from '@angular/common/http';

describe('AiService', () => {
    let service: AiService;
    let httpMock: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [
                AiService,
                provideHttpClient(),
                provideHttpClientTesting()
            ]
        });
        service = TestBed.inject(AiService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should call extract-text endpoint', () => {
        const docId = '123';
        const mockResponse = { text: 'Extracted text', confidence: 0.9 };

        service.extractText(docId).subscribe(res => {
            expect(res.text).toBe('Extracted text');
        });

        const req = httpMock.expectOne(`/api/ai/extract-text/${docId}`);
        expect(req.request.method).toBe('POST');
        req.flush(mockResponse);
    });

    it('should call questions/generate endpoint', () => {
        const request: GenerateQuestionsRequest = {
            noteId: 'note-1',
            type: QuestionType.MultipleChoice,
            difficulty: 2,
            count: 5
        };
        const mockResponse = [{ text: 'Q1', type: 'MultipleChoice', options: ['A', 'B'], correctAnswer: 'A' }];

        service.generateQuestions(request).subscribe(res => {
            expect(res.length).toBe(1);
            expect(res[0].text).toBe('Q1');
        });

        const req = httpMock.expectOne('/api/ai/questions/generate');
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual(request);
        req.flush(mockResponse);
    });

    it('should call grade endpoint', () => {
        const request: GradeAnswerRequest = {
            question: 'What is 2+2?',
            answer: '4',
            context: 'Math'
        };
        const mockResponse = { analysis: 'Score: 100\nFeedback: Correct' };

        service.gradeAnswer(request).subscribe(res => {
            expect(res.analysis).toContain('Score: 100');
        });

        const req = httpMock.expectOne('/api/ai/grade');
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual(request);
        req.flush(mockResponse);
    });
});
