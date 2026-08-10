import { Component, OnInit, ChangeDetectorRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { NgApexchartsModule, ChartComponent, ApexAxisChartSeries, ApexChart, ApexXAxis, ApexDataLabels, ApexStroke, ApexTooltip, ApexTitleSubtitle } from "ng-apexcharts";

export type ChartOptions = {
  series: ApexAxisChartSeries;
  chart: ApexChart;
  xaxis: ApexXAxis;
  dataLabels: ApexDataLabels;
  stroke: ApexStroke;
  title: ApexTitleSubtitle;
  tooltip: ApexTooltip;
};

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, NgApexchartsModule],
  templateUrl: './dashboard.html',
  styleUrls: ['./dashboard.css']
})
export class DashboardComponent implements OnInit {
  @ViewChild("chart") chart!: ChartComponent;
  public chartOptions: Partial<ChartOptions> | any;

  stats = {
    totalCourts: 0,
    totalBookings: 0,
    activeRevenue: 0
  };
  isLoading = true;
  timeframe: 'week' | 'month' | 'year' = 'week';
  allBookings: any[] = [];

  constructor(private http: HttpClient, private cdr: ChangeDetectorRef) {
    this.initChart();
  }

  ngOnInit() {
    this.loadStats();
  }

  initChart() {
    this.chartOptions = {
      series: [
        { name: "Geçmiş Gelir (₺)", data: [] },
        { name: "Beklenen Gelir (₺)", data: [] }
      ],
      chart: { type: "area", height: 350, toolbar: { show: false }, fontFamily: 'Inter, sans-serif' },
      dataLabels: { enabled: false },
      stroke: { curve: "smooth", width: 2 },
      xaxis: { categories: [] },
      tooltip: { x: { format: "dd/MM/yy" } },
      colors: ['#3b82f6', '#10b981'],
      fill: {
        type: 'gradient',
        gradient: { shadeIntensity: 1, opacityFrom: 0.4, opacityTo: 0.05, stops: [0, 90, 100] }
      }
    };
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
            this.allBookings = activeBookings;
            
            this.stats.totalBookings = activeBookings.length;
            this.stats.activeRevenue = activeBookings.reduce((sum: number, current: any) => sum + (current.price || 0), 0);
            
            this.updateChartData();
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

  changeTimeframe(tf: 'week' | 'month' | 'year') {
    this.timeframe = tf;
    this.updateChartData();
  }

  updateChartData() {
    const now = new Date();
    let startDate = new Date();
    
    if (this.timeframe === 'week') startDate.setDate(now.getDate() - 7);
    else if (this.timeframe === 'month') startDate.setMonth(now.getMonth() - 1);
    else if (this.timeframe === 'year') startDate.setFullYear(now.getFullYear() - 1);

    // Gruplama için günleri oluştur
    const days: string[] = [];
    const pastRevenue: number[] = [];
    const futureRevenue: number[] = [];

    // İleriye dönük olarak da 7 gün / 1 ay gösterelim
    let endDate = new Date(now);
    if (this.timeframe === 'week') endDate.setDate(now.getDate() + 7);
    else if (this.timeframe === 'month') endDate.setMonth(now.getMonth() + 1);
    else if (this.timeframe === 'year') endDate.setFullYear(now.getFullYear() + 1);

    // Date range iteration
    let currentDate = new Date(startDate);
    while (currentDate <= endDate) {
      let dateStr = currentDate.toISOString().split('T')[0];
      
      // Aylık/Yıllık formatı ayarla
      let displayFormat = dateStr;
      if (this.timeframe === 'year') {
          displayFormat = `${currentDate.getFullYear()}-${(currentDate.getMonth()+1).toString().padStart(2, '0')}`;
          if (!days.includes(displayFormat)) {
             days.push(displayFormat);
             pastRevenue.push(0);
             futureRevenue.push(0);
          }
      } else {
         days.push(displayFormat);
         pastRevenue.push(0);
         futureRevenue.push(0);
      }
      currentDate.setDate(currentDate.getDate() + 1);
    }

    this.allBookings.forEach(b => {
      let bDate = new Date(b.startTime);
      if (bDate >= startDate && bDate <= endDate) {
         let dateStr = bDate.toISOString().split('T')[0];
         let idx = days.indexOf(dateStr);
         if (this.timeframe === 'year') {
             dateStr = `${bDate.getFullYear()}-${(bDate.getMonth()+1).toString().padStart(2, '0')}`;
             idx = days.indexOf(dateStr);
         }
         
         if (idx !== -1) {
            if (bDate < now) {
                pastRevenue[idx] += (b.price || 0);
            } else {
                futureRevenue[idx] += (b.price || 0);
            }
         }
      }
    });

    this.chartOptions.xaxis = { categories: days };
    this.chartOptions.series = [
      { name: "Geçmiş Gelir (₺)", data: pastRevenue },
      { name: "Beklenen Gelir (₺)", data: futureRevenue }
    ];
    this.cdr.detectChanges();
  }

}
