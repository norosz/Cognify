import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { Notification, NotificationType } from '../models/notification.model';

@Injectable({
    providedIn: 'root'
})
export class NotificationService {
    private notifications = new BehaviorSubject<Notification[]>([]);
    notifications$ = this.notifications.asObservable();

    /**
     * Show a notification toast.
     * @returns The notification ID for updating/dismissing later
     */
    show(type: NotificationType, message: string, options?: { autoClose?: boolean; closable?: boolean; link?: any[]; linkText?: string }): string {
        const id = crypto.randomUUID();
        const autoClose = options?.autoClose ?? (type !== 'loading');
        const closable = options?.closable ?? true;

        const notification: Notification = {
            id,
            type,
            message,
            autoClose,
            closable,
            link: options?.link,
            linkText: options?.linkText
        };
        this.notifications.next([...this.notifications.value, notification]);

        return id;
    }

    /**
     * Update an existing notification
     */
    update(id: string, changes: Partial<Pick<Notification, 'type' | 'message' | 'autoClose' | 'link' | 'linkText'>>): void {
        const current = this.notifications.value;
        const index = current.findIndex(n => n.id === id);
        if (index === -1) return;

        const updated = { ...current[index], ...changes };
        const newList = [...current];
        newList[index] = updated;
        this.notifications.next(newList);
    }

    /**
     * Dismiss a notification
     */
    dismiss(id: string): void {
        this.notifications.next(this.notifications.value.filter(n => n.id !== id));
    }

    /**
     * Convenience methods
     */
    success(message: string, link?: any[], linkText?: string): string {
        return this.show('success', message, { link, linkText });
    }

    warning(message: string): string {
        return this.show('warning', message);
    }

    error(message: string): string {
        return this.show('error', message);
    }

    loading(message: string): string {
        return this.show('loading', message, { autoClose: false });
    }

    info(message: string, link?: any[], linkText?: string): string {
        return this.show('success', message, { link, linkText });
    }
}
