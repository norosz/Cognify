import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router'; // Added
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatTabsModule } from '@angular/material/tabs';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { PendingService, ExtractedContentDto, PendingQuizDto } from '../../core/services/pending.service';
import { NotificationService } from '../../core/services/notification.service';
import { HandwritingPreviewDialogComponent } from '../modules/components/handwriting-preview-dialog/handwriting-preview-dialog.component';
import { ConfirmationDialogComponent } from '../../shared/components/confirmation-dialog/confirmation-dialog.component';

@Component({
  selector: 'app-pending',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatTabsModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatDialogModule
  ],
  templateUrl: './pending.component.html',
  styleUrl: './pending.component.scss'
})
export class PendingComponent implements OnInit {
  private pendingService = inject(PendingService);
  private notificationService = inject(NotificationService);
  private dialog = inject(MatDialog);
  private route = inject(ActivatedRoute); // Injected

  extractedContents = this.pendingService.extractedContents;
  pendingQuizzes = this.pendingService.pendingQuizzes;
  isLoading = signal<boolean>(false);
  selectedTabIndex = signal<number>(0); // Added
  savingQuizIds = signal<Set<string>>(new Set());

  ngOnInit(): void {
    // Check matrix params (route.params) for tab selection
    // The link is generated as ['/pending', { tab: 'quizzes' }] which results in /pending;tab=quizzes
    this.route.params.subscribe(params => {
      if (params['tab'] === 'quizzes') {
        this.selectedTabIndex.set(1);
      } else if (params['tab'] === 'extractions') {
        this.selectedTabIndex.set(0);
      }
    });

    this.loadPendingItems();
  }

  loadPendingItems(showLoading: boolean = true): void {
    if (showLoading) this.isLoading.set(true);

    this.pendingService.refreshAll().subscribe({
      next: () => this.isLoading.set(false),
      error: () => this.isLoading.set(false)
    });
  }

  viewExtractedContent(content: ExtractedContentDto): void {
    const dialogRef = this.dialog.open(HandwritingPreviewDialogComponent, {
      width: '800px',
      data: {
        text: content.text,
        moduleId: content.moduleId,
        mode: 'view',
        images: content.images ?? []
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result && result.title) {
        this.saveExtractedAsNote(content, result.title);
      }
    });
  }

  saveExtractedAsNote(content: ExtractedContentDto, title: string): void {
    this.pendingService.saveAsNote(content.id, title).subscribe({
      next: () => {
        this.notificationService.success(
          'Note saved successfully',
          ['/modules', content.moduleId, { tab: 'notes' }],
          'View Note'
        );
        this.loadPendingItems();
      },
      error: () => this.notificationService.error('Failed to save note')
    });
  }

  deleteExtractedContent(id: string): void {
    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      width: '400px',
      data: {
        title: 'Discard Extraction',
        message: 'Are you sure you want to discard this extraction?',
        confirmText: 'Discard',
        isDestructive: true
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.pendingService.deleteExtractedContent(id).subscribe({
          next: () => {
            this.notificationService.success('Extraction discarded');
            this.loadPendingItems();
          }
        });
      }
    });
  }

  saveQuiz(quiz: PendingQuizDto): void {
    if (this.savingQuizIds().has(quiz.id)) {
      return;
    }

    const nextSet = new Set(this.savingQuizIds());
    nextSet.add(quiz.id);
    this.savingQuizIds.set(nextSet);

    this.pendingService.saveQuiz(quiz.id).subscribe({
      next: () => {
        this.notificationService.success(
          'Quiz saved successfully',
          ['/modules', quiz.moduleId, { tab: 'quizzes' }],
          'View Quiz'
        );
        this.loadPendingItems();
      },
      error: () => this.notificationService.error('Failed to save quiz')
    }).add(() => {
      const updatedSet = new Set(this.savingQuizIds());
      updatedSet.delete(quiz.id);
      this.savingQuizIds.set(updatedSet);
    });
  }

  isSavingQuiz(id: string): boolean {
    return this.savingQuizIds().has(id);
  }

  isExamPending(quiz: PendingQuizDto): boolean {
    return !quiz.noteId;
  }

  deleteQuiz(id: string): void {
    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      width: '400px',
      data: {
        title: 'Discard Quiz',
        message: 'Are you sure you want to discard this quiz?',
        confirmText: 'Discard',
        isDestructive: true
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.pendingService.deletePendingQuiz(id).subscribe({
          next: () => {
            this.notificationService.success('Quiz discarded');
            this.loadPendingItems();
          }
        });
      }
    });
  }

  getExtractionIcon(status: string): string {
    switch (status) {
      case 'Ready': return 'history_edu';
      case 'Processing': return 'sync';
      case 'Error': return 'error';
      default: return 'description';
    }
  }

  getQuizStatusIcon(status: string): string {
    switch (status.toLowerCase()) {
      case 'ready': return 'check_circle';
      case 'generating': return 'sync';
      case 'failed': return 'error';
      default: return 'help';
    }
  }
}
