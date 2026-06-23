import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ApiService, Developer, TaskTicket } from '../../services/api.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit {
  developers: Developer[] = [];
  tickets: TaskTicket[] = [];
  
  totalTickets = 0;
  assignedTickets = 0;
  pendingTickets = 0;
  loading = true;

  constructor(private apiService: ApiService) {}

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.loading = true;
    
    // ForkJoin or subscribe in sequence for ease
    this.apiService.getDevelopers().subscribe({
      next: (devs) => {
        this.developers = devs;
        
        this.apiService.getTickets().subscribe({
          next: (tix) => {
            this.tickets = tix;
            this.calculateStats();
            this.loading = false;
          },
          error: (err) => {
            console.error('Error fetching tickets', err);
            this.loading = false;
          }
        });
      },
      error: (err) => {
        console.error('Error fetching developers', err);
        this.loading = false;
      }
    });
  }

  calculateStats(): void {
    this.totalTickets = this.tickets.length;
    this.assignedTickets = this.tickets.filter(t => t.status === 'Assigned').length;
    this.pendingTickets = this.tickets.filter(t => t.status === 'Pending').length;
  }

  getWorkloadPercent(workload: number): number {
    // Treat 4 or more tasks as 100% capacity
    return Math.min(100, (workload / 4) * 100);
  }

  getWorkloadColor(workload: number): string {
    if (workload === 0) return 'var(--color-success)'; // Emerald green (no load)
    if (workload === 1) return 'var(--color-info)'; // Cyan
    if (workload === 2) return 'var(--color-warning)'; // Amber/yellow
    return 'var(--color-danger)'; // Red (high load)
  }

  getAvailabilityDotColor(status: string): string {
    switch (status.toLowerCase()) {
      case 'available': return 'var(--color-success)';
      case 'busy': return 'var(--color-warning)';
      case 'on leave': return 'var(--color-danger)';
      default: return 'var(--text-muted)';
    }
  }
}
