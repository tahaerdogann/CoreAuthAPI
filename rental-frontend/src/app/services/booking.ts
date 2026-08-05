import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class BookingService {
  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  createBooking(slotId: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/Bookings/create`, { slotId });
  }

  getMyBookings(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/Bookings/my-bookings`);
  }

  cancelBooking(bookingId: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/Bookings/cancel/${bookingId}`, {});
  }
}
