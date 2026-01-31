import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatButtonModule } from '@angular/material/button';
import { MatTabsModule } from '@angular/material/tabs';
import { NgxEchartsModule } from 'ngx-echarts';
import { forkJoin } from 'rxjs';
import { LearningAnalyticsService } from '../../core/services/learning-analytics.service';
import {
  LearningAnalyticsSummaryDto,
  ExamAnalyticsSummaryDto,
  PerformanceTrendsDto,
  TopicDistributionDto,
  RetentionHeatmapPointDto,
  DecayForecastDto,
  MistakePatternSummaryDto,
  CategoryBreakdownItemDto
} from '../../core/models/analytics.models';

@Component({
  selector: 'app-statistics',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatButtonModule,
    MatTabsModule,
    NgxEchartsModule
  ],
  templateUrl: './statistics.component.html',
  styleUrl: './statistics.component.scss'
})
export class StatisticsComponent implements OnInit {
  analyticsService = inject(LearningAnalyticsService);
  analyticsSummary = signal<LearningAnalyticsSummaryDto | null>(null);
  isAnalyticsLoading = signal<boolean>(false);
  performanceTrends = signal<PerformanceTrendsDto | null>(null);
  topicDistribution = signal<TopicDistributionDto | null>(null);
  retentionHeatmap = signal<RetentionHeatmapPointDto[]>([]);
  decayForecast = signal<DecayForecastDto | null>(null);
  mistakePatterns = signal<MistakePatternSummaryDto[]>([]);
  categoryBreakdown = signal<CategoryBreakdownItemDto[]>([]);

  examSummary = signal<ExamAnalyticsSummaryDto | null>(null);
  examCategoryBreakdown = signal<CategoryBreakdownItemDto[]>([]);

  readinessOptions = signal<any>({});
  velocityOptions = signal<any>({});
  distributionOptions = signal<any>({});
  heatmapOptions = signal<any>({});
  decayOptions = signal<any>({});
  categoryOptions = signal<any>({});
  categoryMetric = signal<'attempts' | 'average'>('attempts');
  examCategoryOptions = signal<any>({});
  examCategoryMetric = signal<'attempts' | 'average'>('attempts');

  ngOnInit() {
    this.loadPracticeAnalytics();
    this.loadExamAnalytics();
  }

  setExamCategoryMetric(metric: 'attempts' | 'average') {
    this.examCategoryMetric.set(metric);
    this.examCategoryOptions.set(this.buildExamCategoryBreakdownOptions(this.examCategoryBreakdown(), metric));
  }

  setCategoryMetric(metric: 'attempts' | 'average') {
    this.categoryMetric.set(metric);
    this.categoryOptions.set(this.buildCategoryBreakdownOptions(this.categoryBreakdown(), metric));
  }


  loadPracticeAnalytics() {
    this.isAnalyticsLoading.set(true);
    forkJoin({
      summary: this.analyticsService.getSummary(false),
      trends: this.analyticsService.getTrends({ bucketDays: 7 }, false),
      topics: this.analyticsService.getTopics({ maxTopics: 20, maxWeakTopics: 5 }, false),
      heatmap: this.analyticsService.getRetentionHeatmap({ maxTopics: 12 }, false),
      decay: this.analyticsService.getDecayForecast({ maxTopics: 5, days: 14, stepDays: 2 }, false),
      mistakes: this.analyticsService.getMistakePatterns({ maxItems: 6, maxTopics: 3 }, false),
      categories: this.analyticsService.getCategoryBreakdown({
        includeExams: false,
        groupBy: 'moduleCategory'
      })
    }).subscribe({
      next: (data) => {
        this.analyticsSummary.set(data.summary);
        this.performanceTrends.set(data.trends);
        this.topicDistribution.set(data.topics);
        this.retentionHeatmap.set(data.heatmap);
        this.decayForecast.set(data.decay);
        this.mistakePatterns.set(data.mistakes);
        this.categoryBreakdown.set(data.categories.items || []);

        this.readinessOptions.set(this.buildReadinessGaugeOptions(data.summary));
        this.velocityOptions.set(this.buildVelocitySparklineOptions(data.trends));
        this.distributionOptions.set(this.buildTopicDistributionOptions(data.topics));
        this.heatmapOptions.set(this.buildHeatmapOptions(data.heatmap));
        this.decayOptions.set(this.buildDecayForecastOptions(data.decay));
        this.categoryOptions.set(this.buildCategoryBreakdownOptions(data.categories.items || [], this.categoryMetric()));

        this.isAnalyticsLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load analytics', err);
        this.readinessOptions.set({});
        this.velocityOptions.set({});
        this.distributionOptions.set({});
        this.heatmapOptions.set({});
        this.decayOptions.set({});
        this.mistakePatterns.set([]);
        this.categoryBreakdown.set([]);
        this.categoryOptions.set({});
        this.isAnalyticsLoading.set(false);
      }
    });
  }

  loadExamAnalytics() {
    this.analyticsService.getExamSummary().subscribe({
      next: (summary) => this.examSummary.set(summary),
      error: () => this.examSummary.set(null)
    });

    this.analyticsService.getExamCategoryBreakdown().subscribe({
      next: (data) => {
        this.examCategoryBreakdown.set(data.items || []);
        this.examCategoryOptions.set(this.buildExamCategoryBreakdownOptions(data.items || [], this.examCategoryMetric()));
      },
      error: () => {
        this.examCategoryBreakdown.set([]);
        this.examCategoryOptions.set({});
      }
    });
  }

  formatMistakeLabel(label: string): string {
    const trimmed = label?.trim();
    if (!trimmed) {
      return '';
    }

    const overrides: Record<string, string> = {
      IncorrectAnswer: 'Incorrect Answer',
      TooShortAnswer: 'Too Short Answer',
      IrrelevantContent: 'Irrelevant Content'
    };

    if (overrides[trimmed]) {
      return overrides[trimmed];
    }

    return trimmed.replace(/_/g, ' ').replace(/([a-z])([A-Z])/g, '$1 $2');
  }


  private buildExamCategoryBreakdownOptions(items: CategoryBreakdownItemDto[], metric: 'attempts' | 'average') {
    const labels = items.map(i => i.categoryLabel);
    const values = items.map(i => metric === 'attempts'
      ? i.examAttemptCount
      : Math.round(i.examAverageScore));

    return {
      grid: { left: 140, right: 20, top: 10, bottom: 20 },
      xAxis: { type: 'value', min: 0, max: metric === 'attempts' ? undefined : 100 },
      yAxis: { type: 'category', data: labels },
      tooltip: {
        formatter: ({ name, value }: any) => metric === 'attempts'
          ? `${name}: ${value} exams`
          : `${name}: ${value}% avg`
      },
      series: [
        {
          type: 'bar',
          data: values,
          itemStyle: { color: '#4fb6ff' },
          barWidth: 14
        }
      ]
    };
  }

  private buildCategoryBreakdownOptions(items: CategoryBreakdownItemDto[], metric: 'attempts' | 'average') {
    const labels = items.map(i => i.categoryLabel);
    const values = items.map(i => metric === 'attempts'
      ? i.practiceAttemptCount
      : Math.round(i.practiceAverageScore));

    return {
      grid: { left: 140, right: 20, top: 10, bottom: 20 },
      xAxis: { type: 'value', min: 0, max: metric === 'attempts' ? undefined : 100 },
      yAxis: { type: 'category', data: labels },
      tooltip: {
        formatter: ({ name, value }: any) => metric === 'attempts'
          ? `${name}: ${value} attempts`
          : `${name}: ${value}% avg`
      },
      series: [
        {
          type: 'bar',
          data: values,
          itemStyle: { color: '#8f73ff' },
          barWidth: 14
        }
      ]
    };
  }

  private buildReadinessGaugeOptions(summary: LearningAnalyticsSummaryDto) {
    const score = Math.round((summary.examReadinessScore ?? 0) * 100);
    return {
      series: [
        {
          type: 'gauge',
          min: 0,
          max: 100,
          progress: { show: true, width: 12 },
          axisLine: { lineStyle: { width: 12 } },
          splitLine: { show: false },
          axisTick: { show: false },
          axisLabel: { show: false },
          pointer: { show: false },
          detail: { valueAnimation: true, formatter: '{value}%', fontSize: 20 },
          data: [{ value: score }]
        }
      ]
    };
  }

  private buildVelocitySparklineOptions(trends: PerformanceTrendsDto) {
    const points = trends.points ?? [];
    const values = points.map(p => Math.round(p.averageScore));
    return {
      xAxis: { type: 'category', show: false, data: points.map(p => p.periodStart) },
      yAxis: { type: 'value', show: false, min: 0, max: 100 },
      grid: { left: 0, right: 0, top: 10, bottom: 0 },
      series: [
        {
          type: 'line',
          smooth: true,
          data: values,
          showSymbol: false,
          lineStyle: { width: 2 }
        }
      ]
    };
  }

  private buildTopicDistributionOptions(distribution: TopicDistributionDto) {
    const topics = distribution.topics ?? [];
    return {
      tooltip: { trigger: 'item', formatter: '{b}: {c}%' },
      series: [
        {
          type: 'pie',
          radius: ['55%', '80%'],
          avoidLabelOverlap: true,
          label: { show: false },
          data: topics.map(t => ({ name: t.topic, value: Math.round(t.masteryScore * 100) }))
        }
      ]
    };
  }

  private buildHeatmapOptions(points: RetentionHeatmapPointDto[]) {
    const topics = points.map(p => p.topic);
    const data = points.map((p, index) => [0, index, Math.round(p.masteryScore * 100)]);
    return {
      tooltip: { formatter: ({ value }: any) => `${topics[value[1]]}: ${value[2]}%` },
      grid: { left: 120, right: 20, top: 20, bottom: 80 },
      xAxis: { type: 'category', data: ['Mastery'], splitArea: { show: false } },
      yAxis: { type: 'category', data: topics, splitArea: { show: false } },
      visualMap: {
        min: 0,
        max: 100,
        calculable: false,
        orient: 'horizontal',
        left: 'center',
        bottom: 0,
        inRange: { color: ['#2d2f7f', '#764ba2', '#8f73ff', '#d4c2ff'] }
      },
      series: [
        {
          name: 'Mastery',
          type: 'heatmap',
          data,
          label: { show: true, formatter: '{c}%' },
          emphasis: { itemStyle: { shadowBlur: 6, shadowColor: 'rgba(0,0,0,0.3)' } }
        }
      ]
    };
  }

  private buildDecayForecastOptions(decay: DecayForecastDto) {
    const series = decay.topics.map(topic => ({
      name: topic.topic,
      type: 'line',
      smooth: true,
      data: topic.points.map(p => Math.round(p.forgettingRisk * 100))
    }));

    const xLabels = decay.topics[0]?.points.map(p => p.date) ?? [];

    return {
      tooltip: { trigger: 'axis' },
      legend: { show: true, bottom: 0 },
      grid: { left: 40, right: 20, top: 20, bottom: 80 },
      xAxis: { type: 'category', data: xLabels },
      yAxis: { type: 'value', min: 0, max: 100 },
      series
    };
  }
}
