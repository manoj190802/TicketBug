import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService, TaskTicket, RecommendationResult } from '../../services/api.service';

@Component({
  selector: 'app-upload',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './upload.component.html',
  styleUrl: './upload.component.css'
})
export class UploadComponent {
  selectedFile: File | null = null;
  title = '';
  description = '';
  
  loading = false;
  submitting = false;
  dragover = false;
  assignmentSuccess = false;
  errorMessage = '';

  analyzedTicket: TaskTicket | null = null;
  recommendations: RecommendationResult[] = [];

  constructor(
    private apiService: ApiService,
    private router: Router
  ) {}

  onFileSelected(event: any): void {
    const file = event.target.files[0];
    if (file) {
      this.selectedFile = file;
      this.errorMessage = '';
      if (!this.title) {
        // Default title to filename without extension
        this.title = file.name.replace(/\.[^/.]+$/, "");
      }
    }
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.dragover = true;
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.dragover = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.dragover = false;

    const file = event.dataTransfer?.files[0];
    if (file) {
      const ext = file.name.split('.').pop()?.toLowerCase();
      if (ext === 'pdf' || ext === 'docx' || ext === 'txt') {
        this.selectedFile = file;
        this.errorMessage = '';
        if (!this.title) {
          this.title = file.name.replace(/\.[^/.]+$/, "");
        }
      } else {
        this.errorMessage = 'Unsupported file format. Please upload a PDF, Word document, or text file.';
      }
    }
  }

  clearFile(): void {
    this.selectedFile = null;
    this.title = '';
    this.description = '';
    this.errorMessage = '';
  }

  onSubmit(): void {
    if (!this.selectedFile) return;

    this.loading = true;
    this.errorMessage = '';
    this.analyzedTicket = null;
    this.recommendations = [];

    this.apiService.uploadTicket(this.selectedFile, this.title, this.description).subscribe({
      next: (res) => {
        this.analyzedTicket = res.ticket;
        this.recommendations = res.recommendations;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error uploading requirement document', err);
        this.errorMessage = err.error?.detail || err.message || 'Failed to analyze document. Make sure the FastAPI service is running.';
        this.loading = false;
      }
    });
  }

  assignTicket(developerId: string | undefined, matchScore: number): void {
    if (!this.analyzedTicket || !this.analyzedTicket.id || !developerId) return;

    this.submitting = true;
    this.apiService.assignTicket(this.analyzedTicket.id, developerId, matchScore).subscribe({
      next: () => {
        this.submitting = false;
        this.assignmentSuccess = true;
        
        // Wait 2 seconds and redirect to dashboard
        setTimeout(() => {
          this.router.navigate(['/dashboard']);
        }, 2000);
      },
      error: (err) => {
        console.error('Error assigning ticket', err);
        this.errorMessage = 'Failed to assign ticket. Please try again.';
        this.submitting = false;
      }
    });
  }

  getScoreClass(score: number): string {
    if (score >= 80) return 'high-score';
    if (score >= 50) return 'mid-score';
    return '';
  }
}
