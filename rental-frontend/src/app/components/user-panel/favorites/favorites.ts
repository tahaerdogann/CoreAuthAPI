import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FavoriteService } from '../../../services/favorite';
import { Router } from '@angular/router';

@Component({
  selector: 'app-favorites',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './favorites.html',
})
export class FavoritesComponent implements OnInit {
  favorites: any[] = [];

  constructor(
    private favoriteService: FavoriteService, 
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadFavorites();
  }

  loadFavorites(): void {
    console.log('loadFavorites tetiklendi...');
    this.favoriteService.getMyFavorites().subscribe({
      next: (data) => {
        console.log('loadFavorites Yanıtı:', data);
        this.favorites = (data as any)?.$values || data || [];
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('loadFavorites Hatası:', err);
        this.cdr.detectChanges();
      }
    });
  }

  goToCourt(courtId: string): void {
    this.router.navigate(['/court-detail', courtId]);
  }

  removeFavorite(courtId: string, event: Event): void {
    event.stopPropagation();
    this.favoriteService.toggleFavorite(courtId).subscribe({
      next: (res) => {
        this.loadFavorites();
      },
      error: (err) => console.error(err)
    });
  }

  getCoverPhoto(court: any): string | null {
    if (!court || !court.photos) return null;
    
    let photos = [];
    if (court.photos.$values) {
      photos = court.photos.$values;
    } else if (Array.isArray(court.photos)) {
      photos = court.photos;
    }

    if (photos.length === 0) return null;

    const cover = photos.find((p: any) => p.isCover);
    if (cover) return cover.url;

    return photos[0].url;
  }
}
