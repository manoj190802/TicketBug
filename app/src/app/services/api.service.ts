import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface Developer {
  id?: string;
  name: string;
  skills: string[];
  experience: number;
  workload: number;
  availabilityStatus: string;
}

export interface TaskTicket {
  id?: string;
  title: string;
  description: string;
  category: string;
  requiredSkills: string[];
  assignedDeveloperId?: string;
  assignedDeveloperName?: string;
  status: string;
  createdAt?: string;
}

export interface AssignmentHistory {
  id?: string;
  ticketId: string;
  ticketTitle: string;
  developerId: string;
  developerName: string;
  assignedAt: string;
  matchScore: number;
}

export interface RecommendationResult {
  developer: Developer;
  matchScore: number;
  skillsMatched: string[];
  explanation: string;
}

export interface UploadResponse {
  ticket: TaskTicket;
  recommendations: RecommendationResult[];
}

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  // Developers
  getDevelopers(): Observable<Developer[]> {
    return this.http.get<Developer[]>(`${this.apiUrl}/developers`);
  }

  getDeveloper(id: string): Observable<Developer> {
    return this.http.get<Developer>(`${this.apiUrl}/developers/${id}`);
  }

  createDeveloper(developer: Developer): Observable<Developer> {
    return this.http.post<Developer>(`${this.apiUrl}/developers`, developer);
  }

  updateDeveloper(id: string, developer: Developer): Observable<Developer> {
    return this.http.put<Developer>(`${this.apiUrl}/developers/${id}`, developer);
  }

  deleteDeveloper(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/developers/${id}`);
  }

  // Tickets
  getTickets(): Observable<TaskTicket[]> {
    return this.http.get<TaskTicket[]>(`${this.apiUrl}/tickets`);
  }

  getHistory(): Observable<AssignmentHistory[]> {
    return this.http.get<AssignmentHistory[]>(`${this.apiUrl}/tickets/history`);
  }

  uploadTicket(file: File, title: string, description: string): Observable<UploadResponse> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('title', title);
    formData.append('description', description);
    return this.http.post<UploadResponse>(`${this.apiUrl}/tickets/upload`, formData);
  }

  assignTicket(ticketId: string, developerId: string, matchScore: number): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/tickets/assign`, {
      ticketId,
      developerId,
      matchScore
    });
  }
}
