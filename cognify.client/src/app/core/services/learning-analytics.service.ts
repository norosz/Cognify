import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
    LearningAnalyticsSummaryDto,
    PerformanceTrendsDto,
    TopicDistributionDto,
    RetentionHeatmapPointDto,
    DecayForecastDto,
    MistakePatternSummaryDto,
    CategoryBreakdownDto,
    ExamAnalyticsSummaryDto
} from '../models/analytics.models';

@Injectable({
    providedIn: 'root'
})
export class LearningAnalyticsService {
    private http = inject(HttpClient);
    private apiUrl = '/api/learning-analytics';

    getSummary(includeExams = false): Observable<LearningAnalyticsSummaryDto> {
        return this.http.get<LearningAnalyticsSummaryDto>(`${this.apiUrl}/summary`, {
            params: { includeExams }
        });
    }

    getTrends(params?: { from?: string; to?: string; bucketDays?: number }, includeExams = false): Observable<PerformanceTrendsDto> {
        return this.http.get<PerformanceTrendsDto>(`${this.apiUrl}/trends`, {
            params: { ...(params ?? {}), includeExams } as any
        });
    }

    getTopics(params?: { maxTopics?: number; maxWeakTopics?: number }, includeExams = false): Observable<TopicDistributionDto> {
        return this.http.get<TopicDistributionDto>(`${this.apiUrl}/topics`, {
            params: { ...(params ?? {}), includeExams } as any
        });
    }

    getRetentionHeatmap(params?: { maxTopics?: number }, includeExams = false): Observable<RetentionHeatmapPointDto[]> {
        return this.http.get<RetentionHeatmapPointDto[]>(`${this.apiUrl}/retention-heatmap`, {
            params: { ...(params ?? {}), includeExams } as any
        });
    }

    getDecayForecast(params?: { maxTopics?: number; days?: number; stepDays?: number }, includeExams = false): Observable<DecayForecastDto> {
        return this.http.get<DecayForecastDto>(`${this.apiUrl}/decay-forecast`, {
            params: { ...(params ?? {}), includeExams } as any
        });
    }

    getMistakePatterns(params?: { maxItems?: number; maxTopics?: number }, includeExams = false): Observable<MistakePatternSummaryDto[]> {
        return this.http.get<MistakePatternSummaryDto[]>(`${this.apiUrl}/mistake-patterns`, {
            params: { ...(params ?? {}), includeExams } as any
        });
    }

    getCategoryBreakdown(params?: { includeExams?: boolean; groupBy?: 'moduleCategory' | 'quizCategory'; filterQuizCategories?: string[]; moduleId?: string }): Observable<CategoryBreakdownDto> {
        const query: Record<string, any> = { includeExams: params?.includeExams ?? false };

        if (params?.groupBy) {
            query['groupBy'] = params.groupBy;
        }

        if (params?.filterQuizCategories && params.filterQuizCategories.length > 0) {
            query['filterQuizCategories'] = params.filterQuizCategories.join(',');
        }

        if (params?.moduleId) {
            query['moduleId'] = params.moduleId;
        }

        return this.http.get<CategoryBreakdownDto>(`${this.apiUrl}/category-breakdown`, {
            params: query
        });
    }

    getQuizCategories(): Observable<string[]> {
        return this.http.get<string[]>(`${this.apiUrl}/quiz-categories`);
    }

    getExamSummary(): Observable<ExamAnalyticsSummaryDto> {
        return this.http.get<ExamAnalyticsSummaryDto>(`${this.apiUrl}/exams/summary`);
    }

    getExamCategoryBreakdown(): Observable<CategoryBreakdownDto> {
        return this.http.get<CategoryBreakdownDto>(`${this.apiUrl}/exams/category-breakdown`);
    }
}
