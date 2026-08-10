import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.html',
  styleUrls: ['./dashboard.css']
})
export class DashboardComponent implements OnInit {
  stats = {
    totalCourts: 0,
    totalBookings: 0,
    activeRevenue: 0
  };
  isLoading = true;

  constructor(private http: HttpClient, private cdr: ChangeDetectorRef) {}

  ngOnInit() {
    this.loadStats();
  }

  loadStats() {
    const token = localStorage.getItem('token');
    const headers = { 'Authorization': `Bearer ${token}` };
    
    // Geçici olarak frontend'de hesaplıyoruz.
    // courts listesini ve booked-slots listesini çekip toplayalım.
    
    this.http.get(`${environment.apiUrl}/Courts/my-courts`, { headers }).subscribe({
      next: (courtsRes: any) => {
        let courts = [];
        if (courtsRes && courtsRes.$values) courts = courtsRes.$values;
        else if (Array.isArray(courtsRes)) courts = courtsRes;
        
        this.stats.totalCourts = courts.length;

        this.http.get(`${environment.apiUrl}/Bookings/owner-booked-slots`, { headers }).subscribe({
          next: (slotsRes: any) => {
            let slots = [];
            if (slotsRes && slotsRes.$values) slots = slotsRes.$values;
            else if (Array.isArray(slotsRes)) slots = slotsRes;
            
            // Sadece Approved (status === 1) olanları al
            const activeBookings = slots.filter((s: any) => !s.isManualClose && s.status === 1);
            this.stats.totalBookings = activeBookings.length;
            this.stats.activeRevenue = activeBookings.reduce((sum: number, current: any) => sum + (current.price || 0), 0);
            
            this.isLoading = false;
            this.cdr.detectChanges();
          },
          error: (err) => {
            console.error(err);
            this.isLoading = false;
            this.cdr.detectChanges();
          }
        });
      },
      error: (err) => {
        console.error(err);
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }
}
