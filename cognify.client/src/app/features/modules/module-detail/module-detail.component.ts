import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatTabsModule } from '@angular/material/tabs';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { ModuleService } from '../../../core/modules/module.service';
import { ModuleDto } from '../../../core/modules/module.models';

@Component({
  selector: 'app-module-detail',
  standalone: true,
  imports: [
    CommonModule,
    MatTabsModule,
    MatIconModule,
    MatButtonModule,
    RouterLink
  ],
  templateUrl: './module-detail.component.html',
  styleUrl: './module-detail.component.scss'
})
export class ModuleDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private moduleService = inject(ModuleService);

  module = signal<ModuleDto | null>(null);

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.moduleService.getModule(id).subscribe({
        next: (data) => this.module.set(data),
        error: (err) => console.error('Failed to load module', err)
      });
    }
  }
}
