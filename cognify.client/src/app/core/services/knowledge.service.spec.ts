import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { KnowledgeService } from './knowledge.service';

describe('KnowledgeService', () => {
    let service: KnowledgeService;
    let httpMock: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [HttpClientTestingModule]
        });

        service = TestBed.inject(KnowledgeService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should fetch knowledge states', () => {
        service.getStates().subscribe();

        const req = httpMock.expectOne('/api/knowledge-states');
        expect(req.request.method).toBe('GET');
        req.flush([]);
    });

    it('should fetch review queue with maxItems', () => {
        service.getReviewQueue(5).subscribe();

        const req = httpMock.expectOne(r => r.url === '/api/knowledge-states/review-queue' && r.params.get('maxItems') === '5');
        expect(req.request.method).toBe('GET');
        req.flush([]);
    });
});
