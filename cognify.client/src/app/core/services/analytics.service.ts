import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { LearningAnalyticsSummaryDto } from '../models/analytics.models';
import { LearningAnalyticsService } from './learning-analytics.service';

@Injectable({
  providedIn: 'root'
})
/**
 * @deprecated Use LearningAnalyticsService instead. This wrapper exists to avoid contract drift.
 */
export class AnalyticsService {
  constructor(private analytics: LearningAnalyticsService) {}

  getSummary(_days = 30, _trendDays = 14, _maxTopics = 10): Observable<LearningAnalyticsSummaryDto> {
    return this.analytics.getSummary();
  }
}
