export type NotificationType = 'success' | 'warning' | 'error' | 'loading';

export interface Notification {
    id: string;
    type: NotificationType;
    message: string;
    autoClose: boolean;
    closable: boolean;
    link?: any[];
    linkText?: string;
    duration?: number;
    startTime?: number;
    remainingTime?: number;
    paused?: boolean;
}
