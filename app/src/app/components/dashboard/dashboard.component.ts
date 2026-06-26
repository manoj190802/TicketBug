import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { ApiService, Developer, TaskTicket } from '../../services/api.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, ReactiveFormsModule, FormsModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit {
  developers: Developer[] = [];
  tickets: TaskTicket[] = [];
  
  // Filtered statistics
  filteredTotalTickets = 0;
  filteredAssignedTickets = 0;
  filteredPendingTickets = 0;
  filteredDevsCount = 0;

  loading = true;
  timeFilter: 'all' | 'week' | 'month' | 'year' = 'all';

  // Modal display states
  showManageTicketsModal = false;
  showManageDevsModal = false;
  showTicketFormModal = false;
  showDevFormModal = false;

  // Form states
  ticketForm!: FormGroup;
  devForm!: FormGroup;
  editingTicket: TaskTicket | null = null;
  editingDev: Developer | null = null;
  submittingTicket = false;
  submittingDev = false;

  constructor(
    private apiService: ApiService,
    private fb: FormBuilder
  ) {}

  ngOnInit(): void {
    this.loadData();
    this.initForms();
  }

  initForms(): void {
    this.ticketForm = this.fb.group({
      title: ['', Validators.required],
      description: ['', Validators.required],
      category: ['Full Stack', Validators.required],
      requiredSkills: ['', Validators.required],
      assignedDeveloperId: [''],
      status: ['Pending', Validators.required]
    });

    this.devForm = this.fb.group({
      name: ['', Validators.required],
      skills: ['', Validators.required],
      experience: [0, [Validators.required, Validators.min(0)]],
      availabilityStatus: ['Available', Validators.required]
    });
  }

  loadData(): void {
    this.loading = true;
    
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
    const now = new Date();
    let filteredTickets = [...this.tickets];
    let filteredDevs = [...this.developers];

    if (this.timeFilter !== 'all') {
      const limitDate = new Date();
      if (this.timeFilter === 'week') {
        limitDate.setDate(now.getDate() - 7);
      } else if (this.timeFilter === 'month') {
        limitDate.setMonth(now.getMonth() - 1);
      } else if (this.timeFilter === 'year') {
        limitDate.setFullYear(now.getFullYear() - 1);
      }

      filteredTickets = this.tickets.filter(t => t.createdAt && new Date(t.createdAt) >= limitDate);
      filteredDevs = this.developers.filter(d => d.createdAt && new Date(d.createdAt) >= limitDate);
    }

    this.filteredTotalTickets = filteredTickets.length;
    this.filteredAssignedTickets = filteredTickets.filter(t => t.status === 'Assigned').length;
    this.filteredPendingTickets = filteredTickets.filter(t => t.status === 'Pending').length;
    this.filteredDevsCount = filteredDevs.length;
  }

  setTimeFilter(filter: 'all' | 'week' | 'month' | 'year'): void {
    this.timeFilter = filter;
    this.calculateStats();
  }

  // --- TICKET CRUD ---
  openTicketForm(ticket?: TaskTicket): void {
    this.editingTicket = ticket || null;
    this.ticketForm = this.fb.group({
      title: [ticket?.title || '', Validators.required],
      description: [ticket?.description || '', Validators.required],
      category: [ticket?.category || 'Full Stack', Validators.required],
      requiredSkills: [ticket?.requiredSkills?.join(', ') || '', Validators.required],
      assignedDeveloperId: [ticket?.assignedDeveloperId || ''],
      status: [ticket?.status || 'Pending', Validators.required]
    });
    this.showTicketFormModal = true;
  }

  saveTicket(): void {
    if (this.ticketForm.invalid) return;
    this.submittingTicket = true;
    const val = this.ticketForm.value;

    const skills = val.requiredSkills
      .split(',')
      .map((s: string) => s.trim().toLowerCase())
      .filter((s: string) => s.length > 0);

    const ticketData: TaskTicket = {
      ...this.editingTicket,
      title: val.title,
      description: val.description,
      category: val.category,
      requiredSkills: skills,
      assignedDeveloperId: val.assignedDeveloperId || undefined,
      status: val.status
    };

    const request = this.editingTicket?.id 
      ? this.apiService.updateTicket(this.editingTicket.id, ticketData)
      : this.apiService.createTicket(ticketData);

    request.subscribe({
      next: () => {
        this.submittingTicket = false;
        this.showTicketFormModal = false;
        this.editingTicket = null;
        this.loadData();
      },
      error: (err) => {
        console.error('Error saving ticket', err);
        this.submittingTicket = false;
      }
    });
  }

  deleteTicket(id: string | undefined): void {
    if (!id) return;
    if (confirm('Are you sure you want to delete this ticket?')) {
      this.apiService.deleteTicket(id).subscribe({
        next: () => {
          this.loadData();
        },
        error: (err) => {
          console.error('Error deleting ticket', err);
        }
      });
    }
  }

  // --- DEVELOPER CRUD ---
  openDevForm(dev?: Developer): void {
    this.editingDev = dev || null;
    this.devForm = this.fb.group({
      name: [dev?.name || '', Validators.required],
      skills: [dev?.skills?.join(', ') || '', Validators.required],
      experience: [dev?.experience || 0, [Validators.required, Validators.min(0)]],
      availabilityStatus: [dev?.availabilityStatus || 'Available', Validators.required]
    });
    this.showDevFormModal = true;
  }

  saveDev(): void {
    if (this.devForm.invalid) return;
    this.submittingDev = true;
    const val = this.devForm.value;

    const skills = val.skills
      .split(',')
      .map((s: string) => s.trim().toLowerCase())
      .filter((s: string) => s.length > 0);

    const devData: Developer = {
      id: this.editingDev?.id || undefined,
      name: val.name,
      skills: skills,
      experience: val.experience,
      workload: this.editingDev?.workload || 0,
      availabilityStatus: val.availabilityStatus
    };

    const request = this.editingDev?.id
      ? this.apiService.updateDeveloper(this.editingDev.id, devData)
      : this.apiService.createDeveloper(devData);

    request.subscribe({
      next: () => {
        this.submittingDev = false;
        this.showDevFormModal = false;
        this.editingDev = null;
        this.loadData();
      },
      error: (err) => {
        console.error('Error saving developer', err);
        this.submittingDev = false;
      }
    });
  }

  deleteDev(id: string | undefined): void {
    if (!id) return;
    if (confirm('Are you sure you want to remove this developer?')) {
      this.apiService.deleteDeveloper(id).subscribe({
        next: () => {
          this.loadData();
        },
        error: (err) => {
          console.error('Error deleting developer', err);
        }
      });
    }
  }

  getWorkloadPercent(workload: number): number {
    return Math.min(100, (workload / 4) * 100);
  }

  getWorkloadColor(workload: number): string {
    if (workload === 0) return 'var(--color-success)';
    if (workload === 1) return 'var(--color-info)';
    if (workload === 2) return 'var(--color-warning)';
    return 'var(--color-danger)';
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
