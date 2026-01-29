import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { LearningAnalyticsService } from './learning-analytics.service';

describe('LearningAnalyticsService', () => {
    let service: LearningAnalyticsService;
    let httpMock: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [HttpClientTestingModule]
        });

        service = TestBed.inject(LearningAnalyticsService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should fetch summary', () => {
        service.getSummary().subscribe();

        const req = httpMock.expectOne('/api/learning-analytics/summary');
        expect(req.request.method).toBe('GET');
        req.flush({});
    });

    it('should fetch trends with bucketDays', () => {
        service.getTrends({ bucketDays: 7 }).subscribe();

        const req = httpMock.expectOne(r => r.url === '/api/learning-analytics/trends' && r.params.get('bucketDays') === '7');
        expect(req.request.method).toBe('GET');
        req.flush({ points: [] });
    });

    it('should fetch topics', () => {
        service.getTopics({ maxTopics: 10, maxWeakTopics: 3 }).subscribe();

        const req = httpMock.expectOne(r => r.url === '/api/learning-analytics/topics' && r.params.get('maxTopics') === '10');
        expect(req.request.method).toBe('GET');
        req.flush({ topics: [], weakestTopics: [] });
    });

    it('should fetch retention heatmap', () => {
        service.getRetentionHeatmap({ maxTopics: 12 }).subscribe();

        const req = httpMock.expectOne(r => r.url === '/api/learning-analytics/retention-heatmap' && r.params.get('maxTopics') === '12');
        expect(req.request.method).toBe('GET');
        req.flush([]);
    });

    it('should fetch decay forecast', () => {
        service.getDecayForecast({ maxTopics: 4, days: 10, stepDays: 2 }).subscribe();

        const req = httpMock.expectOne(r => r.url === '/api/learning-analytics/decay-forecast' && r.params.get('maxTopics') === '4');
        expect(req.request.method).toBe('GET');
        req.flush({ topics: [] });
    });
});
