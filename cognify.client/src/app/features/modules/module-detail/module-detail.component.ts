import { Component, inject, signal, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatTabsModule } from '@angular/material/tabs';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { NgxEchartsModule } from 'ngx-echarts';
import { ModuleService } from '../../../core/modules/module.service';
import { ModuleDto, ModuleStatsDto } from '../../../core/modules/module.models';
import { LearningAnalyticsService } from '../../../core/services/learning-analytics.service';
import { CategoryBreakdownItemDto } from '../../../core/models/analytics.models';
import { FinalExamService } from '../../../core/services/final-exam.service';
import { ExamAttemptService } from '../../../core/services/exam-attempt.service';
import { PendingQuizDto, PendingService } from '../../../core/services/pending.service';
import { NotificationService } from '../../../core/services/notification.service';
import { FinalExamDto, ExamAttemptDto } from '../../../core/models/exam.models';
import { DocumentListComponent } from '../components/document-list/document-list.component';
import { NotesListComponent } from '../../notes/components/notes-list/notes-list.component';
import { UploadDocumentDialogComponent } from '../components/upload-document-dialog/upload-document-dialog.component';
import { DocumentsService } from '../services/documents.service';
import { QuizListComponent } from '../components/quiz-list/quiz-list.component';
import { ExamTakingComponent } from '../../exams/exam-taking/exam-taking.component';
import { CategoryHistoryBatchDto } from '../../../core/models/category.models';
import { FinalExamNoteDialogComponent } from '../components/final-exam-note-dialog/final-exam-note-dialog.component';
import { FinalExamSelectedNotesDialogComponent } from '../components/final-exam-selected-notes-dialog/final-exam-selected-notes-dialog.component';
import { NoteService } from '../../../core/services/note.service';
import { Note } from '../../../core/models/note.model';

@Component({
  selector: 'app-module-detail',
  standalone: true,
  imports: [
    CommonModule,
    MatTabsModule,
    MatIconModule,
    MatButtonModule,
    MatCardModule,
    RouterLink,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatAutocompleteModule,
    NgxEchartsModule,
    DocumentListComponent,
    NotesListComponent,
    QuizListComponent
  ],
  templateUrl: './module-detail.component.html',
  styleUrl: './module-detail.component.scss'
})
export class ModuleDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private moduleService = inject(ModuleService);
  private finalExamService = inject(FinalExamService);
  private examAttemptService = inject(ExamAttemptService);
  private pendingService = inject(PendingService);
  private notificationService = inject(NotificationService);
  private dialog = inject(MatDialog);
  private documentsService = inject(DocumentsService);
  private noteService = inject(NoteService);
  private analyticsService = inject(LearningAnalyticsService);

  module = signal<ModuleDto | null>(null);
  moduleStats = signal<ModuleStatsDto | null>(null);
  statsLoading = signal<boolean>(false);
  statsError = signal<string | null>(null);
  selectedTabIndex = signal<number>(0);
  finalExam = signal<FinalExamDto | null>(null);
  finalExamLoading = signal<boolean>(false);
  finalExamError = signal<string | null>(null);
  examAttempts = signal<ExamAttemptDto[]>([]);
  examAttemptsLoading = signal<boolean>(false);
  pendingFinalExam = signal<PendingQuizDto | null>(null);
  pendingFinalExamId = signal<string | null>(null);
  regeneratingFinalExam = signal<boolean>(false);
  savingFinalExam = signal<boolean>(false);
  moduleId = signal<string | null>(null);
  categoryInput = signal<string>('');
  categoryOptions = signal<CategoryOption[]>([]);
  categoryHistoryLoading = signal<boolean>(false);
  categoryHistoryCursor = signal<string | null>(null);
  categoryHistoryHasMore = signal<boolean>(false);
  suggestingCategory = signal<boolean>(false);
  moduleCategoryBreakdown = signal<CategoryBreakdownItemDto[]>([]);
  moduleCategoryOptions = signal<any>({});
  moduleCategoryMetric = signal<'attempts' | 'average'>('attempts');

  @ViewChild(DocumentListComponent) documentList!: DocumentListComponent;
  @ViewChild(NotesListComponent) notesList!: NotesListComponent;
  @ViewChild(QuizListComponent) quizList!: QuizListComponent;

  ngOnInit() {
    const id = this.normalizeModuleId(this.route.snapshot.paramMap.get('id'));
    if (id) {
      this.moduleId.set(id);
      this.moduleService.getModule(id).subscribe({
        next: (data) => {
          this.module.set(data);
          this.categoryInput.set(data.categoryLabel ?? '');
        },
        error: (err) => console.error('Failed to load module', err)
      });

      this.statsLoading.set(true);
      this.moduleService.getModuleStats(id).subscribe({
        next: (stats) => {
          this.moduleStats.set(stats);
          this.statsLoading.set(false);
        },
        error: (err) => {
          console.error('Failed to load module stats', err);
          this.statsError.set('Failed to load module statistics.');
          this.statsLoading.set(false);
        }
      });

      this.loadFinalExam(id);
      this.loadExamAttempts(id);
      this.loadPendingFinalExam(id);
      this.loadModuleCategoryBreakdown(id);
    }

    this.route.queryParamMap.subscribe(params => {
      const tab = params.get('tab') ?? this.route.snapshot.params['tab'];
      if (tab === 'notes') this.selectedTabIndex.set(1);
      else if (tab === 'quizzes') this.selectedTabIndex.set(2);
      else if (tab === 'documents') this.selectedTabIndex.set(0);
    });
  }

  onQuizGenerated() {
    if (this.quizList) {
      this.quizList.loadQuizzes();
    }
  }

  onNoteCreated() {
    if (this.notesList) {
      this.notesList.loadNotes();
    }
  }

  loadFinalExam(moduleId: string) {
    this.finalExamLoading.set(true);
    this.finalExamService.getFinalExam(moduleId).subscribe({
      next: (exam) => {
        this.finalExam.set(exam);
        this.finalExamLoading.set(false);
      },
      error: () => {
        this.finalExamError.set('Failed to load final exam.');
        this.finalExamLoading.set(false);
      }
    });
  }

  loadModuleCategoryBreakdown(moduleId: string) {
    this.analyticsService.getCategoryBreakdown({
      includeExams: false,
      groupBy: 'quizCategory',
      moduleId
    }).subscribe({
      next: (data) => {
        this.moduleCategoryBreakdown.set(data.items || []);
        this.moduleCategoryOptions.set(this.buildModuleCategoryOptions(data.items || [], this.moduleCategoryMetric()));
      },
      error: () => {
        this.moduleCategoryBreakdown.set([]);
        this.moduleCategoryOptions.set({});
      }
    });
  }

  setModuleCategoryMetric(metric: 'attempts' | 'average') {
    this.moduleCategoryMetric.set(metric);
    this.moduleCategoryOptions.set(this.buildModuleCategoryOptions(this.moduleCategoryBreakdown(), metric));
  }

  loadExamAttempts(moduleId: string) {
    this.examAttemptsLoading.set(true);
    this.examAttemptService.getExamAttempts(moduleId).subscribe({
      next: (attempts) => {
        this.examAttempts.set(attempts);
        this.examAttemptsLoading.set(false);
      },
      error: () => {
        this.examAttempts.set([]);
        this.examAttemptsLoading.set(false);
      }
    });
  }

  loadPendingFinalExam(moduleId: string) {
    const pendingId = localStorage.getItem(this.getPendingFinalExamKey(moduleId));
    if (!pendingId) {
      this.pendingFinalExamId.set(null);
      this.pendingFinalExam.set(null);
      return;
    }

    this.pendingFinalExamId.set(pendingId);
    this.pendingService.getPendingQuizzes().subscribe({
      next: (items) => {
        const match = items.find(item => item.id === pendingId) ?? null;
        this.pendingFinalExam.set(match);
        if (!match) {
          this.pendingFinalExamId.set(null);
          localStorage.removeItem(this.getPendingFinalExamKey(moduleId));
        }
      },
      error: () => {
        this.pendingFinalExam.set(null);
      }
    });
  }

  startFinalExam() {
    const moduleId = this.moduleId();
    const currentExam = this.finalExam();
    if (!moduleId || !currentExam?.currentQuizId) return;

    const dialogRef = this.dialog.open(ExamTakingComponent, {
      width: '700px',
      data: { moduleId, quizId: currentExam.currentQuizId }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result?.examAttemptId) {
        this.router.navigate(['/modules', moduleId, 'exams', result.examAttemptId, 'results']);
        this.loadExamAttempts(moduleId);
        this.moduleService.getModuleStats(moduleId).subscribe({
          next: (stats) => this.moduleStats.set(stats),
          error: () => null
        });
      }
    });
  }

  regenerateFinalExam() {
    const moduleId = this.moduleId();
    if (!moduleId || this.regeneratingFinalExam()) return;
    this.loadSelectedNotesAndConfirm(moduleId);
  }

  private isNoNotesSelectedError(err: any): boolean {
    const code = err?.error?.code ?? err?.error?.extensions?.code;
    return code === 'FinalExam.NoNotesSelected';
  }

  private openNoNotesDialog(moduleId: string) {
    const dialogRef = this.dialog.open(FinalExamNoteDialogComponent, {
      width: '420px'
    });

    dialogRef.afterClosed().subscribe(result => {
      if (!result) return;

      this.finalExamService.includeAllNotes(moduleId).subscribe({
        next: () => this.regenerateFinalExam(),
        error: () => this.notificationService.error('Failed to include notes for final exam.')
      });
    });
  }

  private loadSelectedNotesAndConfirm(moduleId: string) {
    this.regeneratingFinalExam.set(true);
    this.noteService.getNotesByModuleId(moduleId).subscribe({
      next: (notes) => {
        const selected = notes.filter(note => note.includeInFinalExam && !this.isFinalExamMarkerNote(note));
        this.regeneratingFinalExam.set(false);

        if (selected.length === 0) {
          this.openNoNotesDialog(moduleId);
          return;
        }

        const dialogRef = this.dialog.open(FinalExamSelectedNotesDialogComponent, {
          width: '420px',
          data: { notes: selected }
        });

        dialogRef.afterClosed().subscribe(result => {
          if (result) {
            this.executeRegenerateFinalExam(moduleId);
          }
        });
      },
      error: () => {
        this.regeneratingFinalExam.set(false);
        this.notificationService.error('Failed to load notes for final exam.');
      }
    });
  }

  private executeRegenerateFinalExam(moduleId: string) {
    this.regeneratingFinalExam.set(true);
    this.finalExamService.regenerateFinalExam(moduleId, {
      questionCount: 20,
      difficulty: 'Intermediate',
      questionType: 'Mixed',
      title: 'Final Exam'
    }).subscribe({
      next: (result) => {
        localStorage.setItem(this.getPendingFinalExamKey(moduleId), result.pendingQuizId);
        this.pendingFinalExamId.set(result.pendingQuizId);
        this.notificationService.success(
          'Final exam generation queued',
          ['/pending', { tab: 'quizzes' }],
          'View Pending'
        );
        this.loadPendingFinalExam(moduleId);
        this.regeneratingFinalExam.set(false);
      },
      error: (err) => {
        this.regeneratingFinalExam.set(false);

        if (this.isNoNotesSelectedError(err)) {
          this.openNoNotesDialog(moduleId);
          return;
        }

        this.notificationService.error('Failed to regenerate final exam.');
      }
    });
  }

  private isFinalExamMarkerNote(note: Note): boolean {
    return note.title === 'Final Exam' && (note.content ?? '').includes('<!-- cognify:final-exam -->');
  }

  private normalizeModuleId(value: string | null): string | null {
    if (!value) return null;
    const decoded = decodeURIComponent(value);
    return decoded.split('?')[0].split(';')[0];
  }

  private buildModuleCategoryOptions(items: CategoryBreakdownItemDto[], metric: 'attempts' | 'average') {
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

  saveFinalExam() {
    const moduleId = this.moduleId();
    const pendingId = this.pendingFinalExamId();
    if (!moduleId || !pendingId || this.savingFinalExam()) return;

    this.savingFinalExam.set(true);
    this.finalExamService.saveFinalExam(moduleId, pendingId).subscribe({
      next: () => {
        localStorage.removeItem(this.getPendingFinalExamKey(moduleId));
        this.pendingFinalExamId.set(null);
        this.pendingFinalExam.set(null);
        this.loadFinalExam(moduleId);
        this.notificationService.success('Final exam saved.');
        this.savingFinalExam.set(false);
      },
      error: () => {
        this.notificationService.error('Failed to save final exam.');
        this.savingFinalExam.set(false);
      }
    });
  }

  canSaveFinalExam(): boolean {
    const pending = this.pendingFinalExam();
    if (!pending) return false;
    const status = pending.status?.toLowerCase();
    return status === 'ready' || status === 'completed';
  }

  canRegenerateFinalExam(): boolean {
    const stats = this.moduleStats();
    if (!stats) return false;
    return (stats.totalDocuments + stats.totalNotes + stats.totalQuizzes) > 0;
  }

  onCategoryFocus() {
    this.loadCategoryHistory(true);
  }

  selectCategory(label: string) {
    this.categoryInput.set(label);
  }

  canSuggestCategory(): boolean {
    const stats = this.moduleStats();
    if (!stats) return false;
    return (stats.totalNotes + stats.totalQuizzes) >= 1;
  }

  suggestCategory() {
    const moduleId = this.moduleId();
    if (!moduleId || this.suggestingCategory() || !this.canSuggestCategory()) return;

    this.suggestingCategory.set(true);
    this.moduleService.suggestCategories(moduleId, 5).subscribe({
      next: () => {
        this.loadCategoryHistory(true);
        this.suggestingCategory.set(false);
      },
      error: (err) => {
        this.notificationService.error(err?.error?.detail || 'Category suggestion unavailable.');
        this.suggestingCategory.set(false);
      }
    });
  }

  applyCategory() {
    const moduleId = this.moduleId();
    const label = this.categoryInput().trim();
    if (!moduleId || !label) return;

    this.moduleService.setCategory(moduleId, label).subscribe({
      next: () => {
        this.notificationService.success(`Module category set to ${label}.`);
        this.moduleService.getModule(moduleId).subscribe({
          next: (data) => this.module.set(data)
        });
        this.loadCategoryHistory(true);
      },
      error: () => this.notificationService.error('Failed to update category.')
    });
  }

  loadCategoryHistory(reset: boolean) {
    const moduleId = this.moduleId();
    if (!moduleId || this.categoryHistoryLoading()) return;

    const cursor = reset ? null : this.categoryHistoryCursor();
    if (!reset && !cursor) return;

    this.categoryHistoryLoading.set(true);
    this.moduleService.getCategoryHistory(moduleId, 10, cursor ?? undefined).subscribe({
      next: (response) => {
        const batches = reset ? response.items : [...this.categoryBatches(), ...response.items];
        this.categoryBatches.set(batches);
        this.categoryHistoryCursor.set(response.nextCursor ?? null);
        this.categoryHistoryHasMore.set(!!response.nextCursor);
        this.categoryOptions.set(this.buildCategoryOptions(batches));
        this.categoryHistoryLoading.set(false);
      },
      error: () => {
        this.categoryHistoryLoading.set(false);
      }
    });
  }

  private categoryBatches = signal<CategoryHistoryBatchDto[]>([]);

  private buildCategoryOptions(batches: CategoryHistoryBatchDto[]): CategoryOption[] {
    const map = new Map<string, CategoryOption>();

    for (const batch of batches) {
      for (const item of batch.items) {
        const existing = map.get(item.label);
        const createdAt = new Date(batch.createdAt).getTime();
        if (!existing) {
          map.set(item.label, {
            label: item.label,
            count: 1,
            source: batch.source,
            lastCreatedAt: createdAt
          });
        } else {
          existing.count += 1;
          if (createdAt >= existing.lastCreatedAt) {
            existing.lastCreatedAt = createdAt;
          }
          if (batch.source === 'Applied') {
            existing.source = 'Applied';
          }
        }
      }
    }

    return Array.from(map.values()).sort((a, b) => b.lastCreatedAt - a.lastCreatedAt);
  }

  private getPendingFinalExamKey(moduleId: string) {
    return `cognify.finalExam.pending.${moduleId}`;
  }


  openUploadDialog() {
    console.log('Opening upload dialog...');
    const currentModule = this.module();
    if (!currentModule) {
      console.error('No module data found!');
      return;
    }

    console.log('Module found:', currentModule.id);
    const dialogRef = this.dialog.open(UploadDocumentDialogComponent, {
      width: '400px',
      data: { moduleId: currentModule.id }
    });

    dialogRef.afterClosed().subscribe(result => {
      console.log('Dialog closed with result:', result);
      if (result) {
        if (this.documentList) {
          this.documentList.loadDocuments();
        } else {
          console.warn('DocumentList component not found!');
        }
      }
    });
  }
}

interface CategoryOption {
  label: string;
  count: number;
  source: 'AI' | 'Applied';
  lastCreatedAt: number;
}
