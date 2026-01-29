import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { NgxEchartsModule } from 'ngx-echarts';
import { ModuleService } from '../../core/modules/module.service';
import { ModuleDto } from '../../core/modules/module.models';
import { CreateModuleDialogComponent } from '../modules/create-module-dialog/create-module-dialog.component';
import { KnowledgeService } from '../../core/services/knowledge.service';
import { AdaptiveQuizService } from '../../core/services/adaptive-quiz.service';
import { PendingService } from '../../core/services/pending.service';
import { NotificationService } from '../../core/services/notification.service';
import { ReviewQueueItemDto, UserKnowledgeStateDto } from '../../core/models/knowledge.models';
import { LearningAnalyticsService } from '../../core/services/learning-analytics.service';
import {
  LearningAnalyticsSummaryDto,
  PerformanceTrendsDto,
  TopicDistributionDto,
  RetentionHeatmapPointDto,
  DecayForecastDto
} from '../../core/models/analytics.models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule,
    RouterLink,
    MatMenuModule,
    MatProgressSpinnerModule,
    NgxEchartsModule
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  moduleService = inject(ModuleService);
  knowledgeService = inject(KnowledgeService);
  adaptiveQuizService = inject(AdaptiveQuizService);
  pendingService = inject(PendingService);
  notificationService = inject(NotificationService);
  analyticsService = inject(LearningAnalyticsService);
  dialog = inject(MatDialog);
  modules = signal<ModuleDto[]>([]);
  reviewQueue = signal<ReviewQueueItemDto[]>([]);
  weakTopics = signal<UserKnowledgeStateDto[]>([]);
  analyticsSummary = signal<LearningAnalyticsSummaryDto | null>(null);
  isKnowledgeLoading = signal<boolean>(false);
  isGenerating = signal<boolean>(false);
  isAnalyticsLoading = signal<boolean>(false);
  performanceTrends = signal<PerformanceTrendsDto | null>(null);
  topicDistribution = signal<TopicDistributionDto | null>(null);
  retentionHeatmap = signal<RetentionHeatmapPointDto[]>([]);
  decayForecast = signal<DecayForecastDto | null>(null);

  readinessOptions = signal<any>({});
  velocityOptions = signal<any>({});
  distributionOptions = signal<any>({});
  heatmapOptions = signal<any>({});
  decayOptions = signal<any>({});

  ngOnInit() {
    this.loadModules();
    this.loadKnowledge();
    this.loadAnalytics();
  }

  loadModules() {
    this.moduleService.getModules().subscribe({
      next: (data) => this.modules.set(data),
      error: (err) => console.error('Failed to load modules', err)
    });
  }

  loadKnowledge() {
    this.isKnowledgeLoading.set(true);

    forkJoin({
      reviewQueue: this.knowledgeService.getReviewQueue(5),
      states: this.knowledgeService.getStates()
    }).subscribe({
      next: (data) => {
        this.reviewQueue.set(data.reviewQueue);
        const ordered = [...data.states].sort((a, b) => b.forgettingRisk - a.forgettingRisk);
        this.weakTopics.set(ordered.slice(0, 5));
        this.isKnowledgeLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load knowledge states', err);
        this.isKnowledgeLoading.set(false);
      }
    });
  }

  loadAnalytics() {
    this.isAnalyticsLoading.set(true);

    forkJoin({
      summary: this.analyticsService.getSummary(),
      trends: this.analyticsService.getTrends({ bucketDays: 7 }),
      topics: this.analyticsService.getTopics({ maxTopics: 20, maxWeakTopics: 5 }),
      heatmap: this.analyticsService.getRetentionHeatmap({ maxTopics: 12 }),
      decay: this.analyticsService.getDecayForecast({ maxTopics: 5, days: 14, stepDays: 2 })
    }).subscribe({
      next: (data) => {
        this.analyticsSummary.set(data.summary);
        this.performanceTrends.set(data.trends);
        this.topicDistribution.set(data.topics);
        this.retentionHeatmap.set(data.heatmap);
        this.decayForecast.set(data.decay);

        this.readinessOptions.set(this.buildReadinessGaugeOptions(data.summary));
        this.velocityOptions.set(this.buildVelocitySparklineOptions(data.trends));
        this.distributionOptions.set(this.buildTopicDistributionOptions(data.topics));
        this.heatmapOptions.set(this.buildHeatmapOptions(data.heatmap));
        this.decayOptions.set(this.buildDecayForecastOptions(data.decay));

        this.isAnalyticsLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load analytics', err);
        this.readinessOptions.set({});
        this.velocityOptions.set({});
        this.distributionOptions.set({});
        this.heatmapOptions.set({});
        this.decayOptions.set({});
        this.isAnalyticsLoading.set(false);
      }
    });
  }

  generateReviewQuiz() {
    if (this.isGenerating()) return;

    const loadingId = this.notificationService.loading('Creating review quiz...');
    this.isGenerating.set(true);

    this.adaptiveQuizService.createAdaptiveQuiz({
      mode: 'Review',
      questionCount: 5,
      maxTopics: 5,
      questionType: 'MultipleChoice'
    }).subscribe({
      next: (response) => {
        const topicLabel = response.selectedTopic || response.pendingQuiz.noteName;
        const details = topicLabel ? ` for ${topicLabel}` : '';
        this.notificationService.update(loadingId, {
          type: 'success',
          message: `Review quiz queued${details}. Check Pending for progress.`,
          autoClose: true,
          link: ['/pending', { tab: 'quizzes' }],
          linkText: 'View Pending'
        });
        this.pendingService.refreshPendingCount();
        this.isGenerating.set(false);
      },
      error: () => {
        this.notificationService.update(loadingId, {
          type: 'error',
          message: 'Failed to create review quiz. Please try again.'
        });
        this.isGenerating.set(false);
      }
    });
  }

  generateWeaknessQuiz() {
    if (this.isGenerating()) return;

    const loadingId = this.notificationService.loading('Creating weakness quiz...');
    this.isGenerating.set(true);

    this.adaptiveQuizService.createAdaptiveQuiz({
      mode: 'Weakness',
      questionCount: 5,
      maxTopics: 5,
      questionType: 'MultipleChoice'
    }).subscribe({
      next: (response) => {
        const topicLabel = response.selectedTopic || response.pendingQuiz.noteName;
        const details = topicLabel ? ` for ${topicLabel}` : '';
        this.notificationService.update(loadingId, {
          type: 'success',
          message: `Weakness quiz queued${details}. Check Pending for progress.`,
          autoClose: true,
          link: ['/pending', { tab: 'quizzes' }],
          linkText: 'View Pending'
        });
        this.pendingService.refreshPendingCount();
        this.isGenerating.set(false);
      },
      error: () => {
        this.notificationService.update(loadingId, {
          type: 'error',
          message: 'Failed to create weakness quiz. Please try again.'
        });
        this.isGenerating.set(false);
      }
    });
  }

  openCreateModuleDialog() {
    const dialogRef = this.dialog.open(CreateModuleDialogComponent, {
      width: '500px'
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.loadModules();
      }
    });
  }

  openEditModuleDialog(event: Event, module: ModuleDto) {
    event.stopPropagation();
    event.preventDefault();

    const dialogRef = this.dialog.open(CreateModuleDialogComponent, {
      width: '500px',
      data: { module }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.loadModules();
      }
    });
  }

  deleteModule(event: Event, id: string) {
    event.stopPropagation();
    event.preventDefault();

    if (confirm('Are you sure you want to delete this module? All associated documents and notes will be deleted.')) {
      this.moduleService.deleteModule(id).subscribe({
        next: () => this.loadModules(),
        error: (err) => console.error('Failed to delete module', err)
      });
    }
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
      grid: { left: 120, right: 20, top: 20, bottom: 20 },
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
      grid: { left: 40, right: 20, top: 20, bottom: 40 },
      xAxis: { type: 'category', data: xLabels },
      yAxis: { type: 'value', min: 0, max: 100 },
      series
    };
  }
}
