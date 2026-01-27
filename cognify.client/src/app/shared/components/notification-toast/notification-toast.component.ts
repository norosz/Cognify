import { Component, Input, Output, EventEmitter, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Router } from '@angular/router';
import { Notification } from '../../../core/models/notification.model';

@Component({
    selector: 'app-notification-toast',
    standalone: true,
    imports: [CommonModule, MatIconModule, MatButtonModule, MatProgressSpinnerModule],
    styles: [`
    :host {
        display: block;
    }
    .notification-toast {
        display: flex;
        align-items: center;
        gap: 12px;
        padding: 12px 16px;
        border-radius: 8px;
        background: rgba(40, 44, 52, 0.95);
        backdrop-filter: blur(8px);
        border: 1px solid rgba(255, 255, 255, 0.1);
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
        color: #e0e0e0;
        animation: slideIn 0.3s ease-out;
        position: relative;
        overflow: hidden;
    }

    .notification-toast.success { border-left: 4px solid #4caf50; }
    .notification-toast.warning { border-left: 4px solid #ff9800; }
    .notification-toast.error   { border-left: 4px solid #f44336; }
    .notification-toast.loading { border-left: 4px solid #90caf9; }

    .notification-toast.success .icon { color: #4caf50; }
    .notification-toast.warning .icon { color: #ff9800; }
    .notification-toast.error   .icon { color: #f44336; }
    .notification-toast.loading .spinner { color: #90caf9; }

    .content { flex: 1; display: flex; flex-direction: column; gap: 4px; }
    .message { font-size: 14px; line-height: 1.4; }
    
    .action-link {
        font-size: 12px;
        font-weight: 600;
        color: #90caf9;
        cursor: pointer;
        display: flex;
        align-items: center;
        gap: 4px;
        margin-top: 2px;
    }
    .action-link:hover { color: #bbdefb; text-decoration: underline; }

    .close-btn {
        margin-left: 8px;
        opacity: 0.7;
    }
    .close-btn:hover { opacity: 1; }

    .progress-bar {
        position: absolute;
        bottom: 0;
        left: 0;
        height: 4px;
        background: #ffffff;
        width: 100%;
        transform-origin: left;
        transition: width linear;
        z-index: 10;
    }

    @keyframes slideIn {
        from { transform: translateX(100%); opacity: 0; }
        to { transform: translateX(0); opacity: 1; }
    }
  `],
    template: `
    <div class="notification-toast" 
        [ngClass]="notification.type"
        (mouseenter)="onMouseEnter()" 
        (mouseleave)="onMouseLeave()">

        <mat-spinner *ngIf="notification.type === 'loading'" mode="indeterminate" diameter="20" class="spinner"></mat-spinner>
        <mat-icon *ngIf="notification.type !== 'loading'" class="icon">{{ getIcon(notification.type) }}</mat-icon>

        <div class="content">
            <span class="message">{{ notification.message }}</span>
            <span *ngIf="notification.link" class="action-link" (click)="handleAction()">
                {{ notification.linkText || 'View' }} <mat-icon inline>arrow_forward</mat-icon>
            </span>
        </div>

        <button *ngIf="notification.closable" mat-icon-button class="close-btn" (click)="dismiss()">
            <mat-icon>close</mat-icon>
        </button>

        <div *ngIf="notification.autoClose" class="progress-bar"
             [style.width.%]="width"
             [style.transition-duration.ms]="transitionDuration">
        </div>
    </div>
  `
})
export class NotificationToastComponent implements OnInit, OnDestroy {
    @Input({ required: true }) notification!: Notification;
    @Output() dismissEvent = new EventEmitter<void>();

    private router = inject(Router);

    width = 100;
    transitionDuration = 0;
    private timer: any;
    private isHovered = false;
    private readonly DEFAULT_DURATION = 5000;

    ngOnInit() {
        if (this.notification.autoClose) {
            this.startCountdown();
        }
    }

    ngOnDestroy() {
        if (this.timer) clearTimeout(this.timer);
    }

    startCountdown() {
        if (this.timer) clearTimeout(this.timer);

        // 1. Reset to full (Instant)
        this.width = 100;
        this.transitionDuration = 0;

        // 2. Start animation in next tick
        setTimeout(() => {
            if (this.isHovered) return; // Guard against hover happening during timeout

            const duration = this.notification.duration || this.DEFAULT_DURATION;
            this.width = 0;
            this.transitionDuration = duration;

            this.timer = setTimeout(() => {
                this.dismiss();
            }, duration);
        }, 100);
    }

    onMouseEnter() {
        if (!this.notification.autoClose) return;
        this.isHovered = true;

        if (this.timer) clearTimeout(this.timer);

        // Jump to full immediately on hover
        this.width = 100;
        this.transitionDuration = 0;
    }

    onMouseLeave() {
        if (!this.notification.autoClose) return;
        this.isHovered = false;

        // Restart full countdown
        this.startCountdown();
    }

    dismiss() {
        this.dismissEvent.emit();
    }

    handleAction() {
        if (this.notification.link) {
            this.router.navigate(this.notification.link);
            this.dismiss();
        }
    }

    getIcon(type: string): string {
        switch (type) {
            case 'success': return 'check_circle';
            case 'warning': return 'warning';
            case 'error': return 'error';
            case 'loading': return 'sync';
            default: return 'info';
        }
    }
}
