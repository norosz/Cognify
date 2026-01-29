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

  extractedContents = signal<ExtractedContentDto[]>([]);
  pendingQuizzes = signal<PendingQuizDto[]>([]);
  isLoading = signal<boolean>(false);
  selectedTabIndex = signal<number>(0); // Added
  private refreshInterval: any;

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
    this.startPolling();
  }

  ngOnDestroy(): void {
    if (this.refreshInterval) {
      clearInterval(this.refreshInterval);
    }
  }

  startPolling(): void {
    // Poll every 5 seconds to check for updates
    this.refreshInterval = setInterval(() => {
      // Poll if any quiz OR extraction is processing
      const hasGeneratingQuiz = this.pendingQuizzes().some(q => q.status === 'Generating');
      const hasProcessingExtraction = this.extractedContents().some(e => e.status === 'Processing');

      // console.log('Polling check:', { hasGeneratingQuiz, hasProcessingExtraction });

      if (hasGeneratingQuiz || hasProcessingExtraction) {
        this.loadPendingItems(false); // Silent reload
      }
    }, 5000);
  }

  loadPendingItems(showLoading: boolean = true): void {
    if (showLoading) this.isLoading.set(true);

    const prevQuizzes = this.pendingQuizzes();
    const prevExtractions = this.extractedContents();

    this.pendingService.getExtractedContents().subscribe({
      next: (contents) => {
        // Check for extraction completions
        contents.forEach(current => {
          const prev = prevExtractions.find(p => p.id === current.id);
          if (prev && prev.status === 'Processing' && current.status === 'Ready') {
            this.notificationService.success(
              'Extraction Complete!',
              ['/pending', { tab: 'extractions' }],
              'View Pending Note'
            );
          }
          if (prev && prev.status === 'Processing' && current.status === 'Error') {
            this.notificationService.error(`Extraction failed: ${current.documentName}`);
          }
        });

        this.extractedContents.set(contents);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });

    this.pendingService.getPendingQuizzes().subscribe({
      next: (quizzes) => {
        quizzes.forEach(current => {
          const prev = prevQuizzes.find(p => p.id === current.id);
          if (prev && prev.status === 'Generating' && current.status === 'Ready') {
            this.notificationService.success(
              `Quiz "${current.title}" is ready!`,
              ['/pending', { tab: 'quizzes' }],
              'View Quiz'
            );
          }
          if (prev && prev.status === 'Generating' && current.status === 'Error') {
            this.notificationService.error(`Quiz generation failed for "${current.title}"`);
          }
        });

        this.pendingQuizzes.set(quizzes);
      }
    });

    this.pendingService.refreshPendingCount();
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
    if (confirm('Are you sure you want to discard this extraction?')) {
      this.pendingService.deleteExtractedContent(id).subscribe({
        next: () => {
          this.notificationService.success('Extraction discarded');
          this.loadPendingItems();
        }
      });
    }
  }

  saveQuiz(quiz: PendingQuizDto): void {
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
    });
  }

  deleteQuiz(id: string): void {
    if (confirm('Are you sure you want to discard this quiz?')) {
      this.pendingService.deletePendingQuiz(id).subscribe({
        next: () => {
          this.notificationService.success('Quiz discarded');
          this.loadPendingItems();
        }
      });
    }
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
      case 'error': return 'error';
      default: return 'help';
    }
  }
}
