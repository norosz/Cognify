import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { NotificationService } from './notification.service';

describe('NotificationService', () => {
    let service: NotificationService;

    beforeEach(() => {
        TestBed.configureTestingModule({});
        service = TestBed.inject(NotificationService);
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    describe('show()', () => {
        it('should add a notification and return an ID', () => {
            const id = service.show('success', 'Test message');

            expect(id).toBeTruthy();
            service.notifications$.subscribe(notifications => {
                expect(notifications.length).toBe(1);
                expect(notifications[0].message).toBe('Test message');
                expect(notifications[0].type).toBe('success');
            });
        });

        it('should support multiple concurrent notifications', () => {
            service.show('success', 'Message 1');
            service.show('error', 'Message 2');
            service.show('warning', 'Message 3');

            service.notifications$.subscribe(notifications => {
                expect(notifications.length).toBe(3);
            });
        });
    });

    describe('convenience methods', () => {
        it('success() should create success notification', () => {
            service.success('Success!');
            service.notifications$.subscribe(notifications => {
                expect(notifications[0].type).toBe('success');
            });
        });

        it('error() should create error notification', () => {
            service.error('Error!');
            service.notifications$.subscribe(notifications => {
                expect(notifications[0].type).toBe('error');
            });
        });

        it('warning() should create warning notification', () => {
            service.warning('Warning!');
            service.notifications$.subscribe(notifications => {
                expect(notifications[0].type).toBe('warning');
            });
        });

        it('loading() should create loading notification with autoClose false', () => {
            service.loading('Loading...');
            service.notifications$.subscribe(notifications => {
                expect(notifications[0].type).toBe('loading');
                expect(notifications[0].autoClose).toBe(false);
            });
        });
    });

    describe('update()', () => {
        it('should update an existing notification', () => {
            const id = service.loading('Loading...');
            service.update(id, { type: 'success', message: 'Done!' });

            service.notifications$.subscribe(notifications => {
                expect(notifications[0].type).toBe('success');
                expect(notifications[0].message).toBe('Done!');
            });
        });

        it('should not throw when updating non-existent notification', () => {
            expect(() => service.update('fake-id', { message: 'test' })).not.toThrow();
        });
    });

    describe('dismiss()', () => {
        it('should remove a notification', () => {
            const id = service.success('Test');
            service.dismiss(id);

            service.notifications$.subscribe(notifications => {
                expect(notifications.length).toBe(0);
            });
        });
    });

    describe('auto-close', () => {
        // Note: Auto-close logic is implemented in the notification-container component, not the service.
        // The service just stores the autoClose flag.
        it('should set autoClose flag correctly on non-loading notifications', () => {
            service.success('Auto close test');

            let notification: any;
            service.notifications$.subscribe(n => notification = n[0]);

            expect(notification.autoClose).toBe(true);
        });

        it('should NOT auto-close loading notifications', fakeAsync(() => {
            service.loading('Loading...');

            tick(5000);

            service.notifications$.subscribe(notifications => {
                expect(notifications.length).toBe(1);
            });
        }));
    });
});
