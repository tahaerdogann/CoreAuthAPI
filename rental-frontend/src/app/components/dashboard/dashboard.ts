import { Component, OnInit } from '@angular/core';
import { CourtService } from '../../services/court';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class Dashboard implements OnInit {
  sahaListesi: any[] = [];

  constructor(private courtService: CourtService) { }

  ngOnInit() {
    this.sahalarıYukle();
  }

  sahalarıYukle() {
    this.courtService.getSahalar().subscribe({
      next: (data: any) => {
        this.sahaListesi = data;
      },
      error: (err: any) => console.error("Sahalar yüklenirken hata:", err)
    });
  }
}
