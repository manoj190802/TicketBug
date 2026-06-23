import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService, AssignmentHistory } from '../../services/api.service';

@Component({
  selector: 'app-history',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './history.component.html',
  styleUrl: './history.component.css'
})
export class HistoryComponent implements OnInit {
  historyLogs: AssignmentHistory[] = [];
  loading = true;

  constructor(private apiService: ApiService) {}

  ngOnInit(): void {
    this.loadHistory();
  }

  loadHistory(): void {
    this.loading = true;
    this.apiService.getHistory().subscribe({
      next: (logs) => {
        this.historyLogs = logs;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error fetching assignment history', err);
        this.loading = false;
      }
    });
  }

  getScoreColor(score: number): string {
    if (score >= 80) return 'var(--color-success)';
    if (score >= 50) return 'var(--color-warning)';
    return 'var(--color-danger)';
  }
}
