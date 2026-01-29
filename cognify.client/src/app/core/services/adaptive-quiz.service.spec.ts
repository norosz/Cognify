import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { AdaptiveQuizService } from './adaptive-quiz.service';

describe('AdaptiveQuizService', () => {
    let service: AdaptiveQuizService;
    let httpMock: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [HttpClientTestingModule]
        });

        service = TestBed.inject(AdaptiveQuizService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should post adaptive quiz request', () => {
        service.createAdaptiveQuiz({
            mode: 'Review',
            questionCount: 5,
            maxTopics: 5
        }).subscribe();

        const req = httpMock.expectOne('/api/adaptive-quizzes');
        expect(req.request.method).toBe('POST');
        req.flush({ pendingQuiz: { id: '1' } });
    });
});
