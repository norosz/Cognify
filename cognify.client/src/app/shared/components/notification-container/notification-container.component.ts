import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterModule, Router } from '@angular/router';
import { NotificationService } from '../../../core/services/notification.service';
import { Notification } from '../../../core/models/notification.model';

import { NotificationToastComponent } from '../notification-toast/notification-toast.component';

@Component({
    selector: 'app-notification-container',
    standalone: true,
    imports: [CommonModule, NotificationToastComponent],
    templateUrl: './notification-container.component.html',
    styleUrl: './notification-container.component.scss'
})
export class NotificationContainerComponent {
    private notificationService = inject(NotificationService);
    notifications$ = this.notificationService.notifications$;

    dismiss(notification: Notification): void {
        this.notificationService.dismiss(notification.id);
    }

    trackById(index: number, item: Notification): string {
        return item.id;
    }
}
