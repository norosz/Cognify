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
import { DeleteModuleConfirmationDialogComponent } from '../modules/components/delete-module-confirmation-dialog/delete-module-confirmation-dialog.component';

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
  dialog = inject(MatDialog);
  modules = signal<ModuleDto[]>([]);
  reviewQueue = signal<ReviewQueueItemDto[]>([]);
  weakTopics = signal<UserKnowledgeStateDto[]>([]);
  isKnowledgeLoading = signal<boolean>(false);
  isGenerating = signal<boolean>(false);
  includeExams = signal<boolean>(false);

  private includeExamsKey = 'cognify.analytics.includeExams';

  ngOnInit() {
    const saved = localStorage.getItem(this.includeExamsKey);
    if (saved !== null) {
      this.includeExams.set(saved === 'true');
    }
    this.loadModules();
    this.loadKnowledge();
  }

  loadModules() {
    this.moduleService.getModules().subscribe({
      next: (data) => this.modules.set(data),
      error: (err) => console.error('Failed to load modules', err)
    });
  }

  loadKnowledge() {
    this.isKnowledgeLoading.set(true);
    const includeExams = this.includeExams();

    forkJoin({
      reviewQueue: this.knowledgeService.getReviewQueue(5, includeExams),
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


  generateReviewQuiz() {
    if (this.isGenerating()) return;
    if (!this.hasNotesForAdaptiveQuizzes()) {
      this.notificationService.error('Add at least one note before generating review quizzes.');
      return;
    }

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
    if (!this.hasNotesForAdaptiveQuizzes()) {
      this.notificationService.error('Add at least one note before generating weakness quizzes.');
      return;
    }

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

  deletingModuleId = signal<string | null>(null);
  deleteStatusMessage = signal<string>('');
  private statusMessages = [
    'Removing documents...',
    'Deleting quizzes...',
    'Cleaning up exam history...',
    'Removing notes...',
    'Finalizing deletion...'
  ];

  deleteModule(event: Event, module: ModuleDto) {
    event.stopPropagation();
    event.preventDefault();

    const dialogRef = this.dialog.open(DeleteModuleConfirmationDialogComponent, {
      width: '400px',
      data: { moduleId: module.id, moduleTitle: module.title }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.startModuleDeletion(module.id);
      }
    });
  }

  startModuleDeletion(id: string) {
    this.deletingModuleId.set(id);
    this.cycleStatusMessages();

    this.moduleService.deleteModule(id).subscribe({
      next: () => {
        this.deletingModuleId.set(null);
        this.loadModules();
        this.notificationService.success('Module deleted successfully');
      },
      error: (err) => {
        console.error('Failed to delete module', err);
        this.deletingModuleId.set(null);
        this.notificationService.error('Failed to delete module');
      }
    });
  }

  private cycleStatusMessages() {
    let index = 0;
    this.deleteStatusMessage.set(this.statusMessages[0]);

    const interval = setInterval(() => {
      if (!this.deletingModuleId()) {
        clearInterval(interval);
        return;
      }
      index = (index + 1) % this.statusMessages.length;
      this.deleteStatusMessage.set(this.statusMessages[index]);
    }, 800);
  }

  hasNotesForAdaptiveQuizzes(): boolean {
    return this.modules().some(module => (module.notesCount ?? 0) > 0);
  }

}
