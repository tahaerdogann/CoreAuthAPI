import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BookingService } from '../../../services/booking';
import { Router } from '@angular/router';

@Component({
  selector: 'app-sessions',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './sessions.html',
})
export class SessionsComponent implements OnInit {
  bookings: any[] = [];
  upcomingBookings: any[] = [];
  pastBookings: any[] = [];
  activeTab = 'upcoming'; // default
  
  cancelModalVisible = false;
  bookingToCancel: any = null;

  constructor(private bookingService: BookingService, private router: Router, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.loadBookings();
  }

  loadBookings(): void {
    this.bookingService.getMyBookings().subscribe({
      next: (data) => {
        this.bookings = data;
        const now = new Date().getTime();
        this.upcomingBookings = this.bookings.filter(b => new Date(b.startTime).getTime() >= now && b.status !== 2);
        // Past includes cancelled ones as history or just past ones
        this.pastBookings = this.bookings.filter(b => new Date(b.startTime).getTime() < now || b.status === 2)
                                          .sort((a,b) => new Date(b.startTime).getTime() - new Date(a.startTime).getTime());
        this.cdr.detectChanges();
      },
      error: (err) => console.error(err)
    });
  }

  setTab(tab: string): void {
    this.activeTab = tab;
  }

  rebook(courtId: string): void {
    this.router.navigate(['/court-detail', courtId]);
  }

  openCancelModal(booking: any): void {
    this.bookingToCancel = booking;
    this.cancelModalVisible = true;
  }

  closeCancelModal(): void {
    this.cancelModalVisible = false;
    this.bookingToCancel = null;
  }

  confirmCancel(): void {
    if (!this.bookingToCancel) return;
    this.bookingService.cancelBooking(this.bookingToCancel.bookingId).subscribe({
      next: (res) => {
        this.closeCancelModal();
        this.loadBookings(); // refresh list
      },
      error: (err) => {
        console.error(err);
        this.closeCancelModal();
      }
    });
  }
}
