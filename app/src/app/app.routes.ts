import { Routes } from '@angular/router';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { DevelopersComponent } from './components/developers/developers.component';
import { UploadComponent } from './components/upload/upload.component';
import { HistoryComponent } from './components/history/history.component';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  { path: 'dashboard', component: DashboardComponent },
  { path: 'developers', component: DevelopersComponent },
  { path: 'upload', component: UploadComponent },
  { path: 'history', component: HistoryComponent },
  { path: '**', redirectTo: 'dashboard' }
];
