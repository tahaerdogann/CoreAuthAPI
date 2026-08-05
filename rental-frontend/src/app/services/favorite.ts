import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class FavoriteService {
  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  toggleFavorite(courtId: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/Favorites/toggle/${courtId}`, {});
  }

  getMyFavorites(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/Favorites/my-favorites`);
  }

  checkFavorite(courtId: string): Observable<{isFavorite: boolean}> {
    return this.http.get<{isFavorite: boolean}>(`${this.apiUrl}/Favorites/check/${courtId}`);
  }
}
