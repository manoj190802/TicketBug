import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService, Developer } from '../../services/api.service';

@Component({
  selector: 'app-developers',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './developers.component.html',
  styleUrl: './developers.component.css'
})
export class DevelopersComponent implements OnInit {
  developers: Developer[] = [];
  developerForm!: FormGroup;
  loading = true;
  submitting = false;

  constructor(
    private apiService: ApiService,
    private fb: FormBuilder
  ) {}

  ngOnInit(): void {
    this.loadDevelopers();
    this.initForm();
  }

  initForm(): void {
    this.developerForm = this.fb.group({
      name: ['', [Validators.required]],
      skills: ['', [Validators.required]],
      experience: [0, [Validators.required, Validators.min(0)]],
      availabilityStatus: ['Available', [Validators.required]]
    });
  }

  loadDevelopers(): void {
    this.loading = true;
    this.apiService.getDevelopers().subscribe({
      next: (devs) => {
        this.developers = devs;
        this.loading = false;
      },
      error: (err) => {
        console.error('Error fetching developers', err);
        this.loading = false;
      }
    });
  }

  onSubmit(): void {
    if (this.developerForm.invalid) return;

    this.submitting = true;
    const formVal = this.developerForm.value;
    
    // Parse skills from comma separated string
    const skillsList = formVal.skills
      .split(',')
      .map((s: string) => s.trim().toLowerCase())
      .filter((s: string) => s.length > 0);

    const newDev: Developer = {
      name: formVal.name,
      skills: skillsList,
      experience: formVal.experience,
      workload: 0,
      availabilityStatus: formVal.availabilityStatus
    };

    this.apiService.createDeveloper(newDev).subscribe({
      next: () => {
        this.submitting = false;
        this.developerForm.reset({ availabilityStatus: 'Available', experience: 0 });
        this.loadDevelopers();
      },
      error: (err) => {
        console.error('Error creating developer', err);
        this.submitting = false;
      }
    });
  }

  deleteDeveloper(id: string | undefined): void {
    if (!id) return;
    if (confirm('Are you sure you want to remove this developer from the roster?')) {
      this.apiService.deleteDeveloper(id).subscribe({
        next: () => {
          this.loadDevelopers();
        },
        error: (err) => {
          console.error('Error deleting developer', err);
        }
      });
    }
  }

  getInitials(name: string): string {
    if (!name) return '??';
    const parts = name.split(' ');
    if (parts.length >= 2) {
      return (parts[0][0] + parts[1][0]).toUpperCase();
    }
    return name.substring(0, 2).toUpperCase();
  }

  getStatusBadgeColor(status: string): string {
    switch (status.toLowerCase()) {
      case 'available': return 'rgba(16, 185, 129, 0.15)';
      case 'busy': return 'rgba(245, 158, 11, 0.15)';
      case 'on leave': return 'rgba(239, 68, 68, 0.15)';
      default: return 'rgba(255, 255, 255, 0.1)';
    }
  }

  getStatusTextColor(status: string): string {
    switch (status.toLowerCase()) {
      case 'available': return 'var(--color-success)';
      case 'busy': return 'var(--color-warning)';
      case 'on leave': return 'var(--color-danger)';
      default: return 'var(--text-secondary)';
    }
  }
}
