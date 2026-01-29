import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { LearningAnalyticsSummaryDto } from '../models/analytics.models';

@Injectable({
  providedIn: 'root'
})
export class AnalyticsService {
  private apiUrl = '/api/learning-analytics';

  constructor(private http: HttpClient) {}

  getSummary(days = 30, trendDays = 14, maxTopics = 10): Observable<LearningAnalyticsSummaryDto> {
    return this.http.get<LearningAnalyticsSummaryDto>(
      `${this.apiUrl}/summary?days=${days}&trendDays=${trendDays}&maxTopics=${maxTopics}`
    );
  }
}
