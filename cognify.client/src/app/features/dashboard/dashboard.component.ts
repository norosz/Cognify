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
import { ModuleService } from '../../core/modules/module.service';
import { ModuleDto } from '../../core/modules/module.models';
import { CreateModuleDialogComponent } from '../modules/create-module-dialog/create-module-dialog.component';
import { KnowledgeService } from '../../core/services/knowledge.service';
import { AdaptiveQuizService } from '../../core/services/adaptive-quiz.service';
import { PendingService } from '../../core/services/pending.service';
import { NotificationService } from '../../core/services/notification.service';
import { ReviewQueueItemDto, UserKnowledgeStateDto } from '../../core/models/knowledge.models';
import { AnalyticsService } from '../../core/services/analytics.service';
import { LearningAnalyticsSummaryDto } from '../../core/models/analytics.models';

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
    MatProgressSpinnerModule
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
  analyticsService = inject(AnalyticsService);
  dialog = inject(MatDialog);
  modules = signal<ModuleDto[]>([]);
  reviewQueue = signal<ReviewQueueItemDto[]>([]);
  weakTopics = signal<UserKnowledgeStateDto[]>([]);
  analyticsSummary = signal<LearningAnalyticsSummaryDto | null>(null);
  isKnowledgeLoading = signal<boolean>(false);
  isAnalyticsLoading = signal<boolean>(false);
  isGenerating = signal<boolean>(false);

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
    this.analyticsService.getSummary().subscribe({
      next: (summary) => {
        this.analyticsSummary.set(summary);
        this.isAnalyticsLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load analytics summary', err);
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
      next: () => {
        this.notificationService.update(loadingId, {
          type: 'success',
          message: 'Review quiz queued. Check Pending for progress.',
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
      next: () => {
        this.notificationService.update(loadingId, {
          type: 'success',
          message: 'Weakness quiz queued. Check Pending for progress.',
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
}
