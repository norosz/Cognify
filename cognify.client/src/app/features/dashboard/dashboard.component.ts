import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { RouterLink } from '@angular/router';
import { ModuleService } from '../../core/modules/module.service';
import { ModuleDto } from '../../core/modules/module.models';
import { CreateModuleDialogComponent } from '../modules/create-module-dialog/create-module-dialog.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule,
    RouterLink
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  moduleService = inject(ModuleService);
  dialog = inject(MatDialog);
  modules = signal<ModuleDto[]>([]);

  ngOnInit() {
    this.loadModules();
  }

  loadModules() {
    this.moduleService.getModules().subscribe({
      next: (data) => this.modules.set(data),
      error: (err) => console.error('Failed to load modules', err)
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
}
