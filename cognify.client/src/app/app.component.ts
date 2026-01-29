import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NotificationContainerComponent } from './shared/components/notification-container/notification-container.component';
import { PendingService } from './core/services/pending.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, NotificationContainerComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  title = 'cognify.client';

  constructor(private pendingService: PendingService) {
    this.pendingService.startPolling();
  }
}
