import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
    LearningAnalyticsSummaryDto,
    PerformanceTrendsDto,
    TopicDistributionDto,
    RetentionHeatmapPointDto,
    DecayForecastDto,
    MistakePatternSummaryDto
} from '../models/analytics.models';

@Injectable({
    providedIn: 'root'
})
export class LearningAnalyticsService {
    private http = inject(HttpClient);
    private apiUrl = '/api/learning-analytics';

    getSummary(): Observable<LearningAnalyticsSummaryDto> {
        return this.http.get<LearningAnalyticsSummaryDto>(`${this.apiUrl}/summary`);
    }

    getTrends(params?: { from?: string; to?: string; bucketDays?: number }): Observable<PerformanceTrendsDto> {
        return this.http.get<PerformanceTrendsDto>(`${this.apiUrl}/trends`, { params: params as any });
    }

    getTopics(params?: { maxTopics?: number; maxWeakTopics?: number }): Observable<TopicDistributionDto> {
        return this.http.get<TopicDistributionDto>(`${this.apiUrl}/topics`, { params: params as any });
    }

    getRetentionHeatmap(params?: { maxTopics?: number }): Observable<RetentionHeatmapPointDto[]> {
        return this.http.get<RetentionHeatmapPointDto[]>(`${this.apiUrl}/retention-heatmap`, { params: params as any });
    }

    getDecayForecast(params?: { maxTopics?: number; days?: number; stepDays?: number }): Observable<DecayForecastDto> {
        return this.http.get<DecayForecastDto>(`${this.apiUrl}/decay-forecast`, { params: params as any });
    }

    getMistakePatterns(params?: { maxItems?: number; maxTopics?: number }): Observable<MistakePatternSummaryDto[]> {
        return this.http.get<MistakePatternSummaryDto[]>(`${this.apiUrl}/mistake-patterns`, { params: params as any });
    }
}
