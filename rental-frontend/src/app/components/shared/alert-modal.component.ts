import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-alert-modal',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="modal-overlay" *ngIf="isOpen">
      <div class="modal-content alert-modal">
        <div class="modal-header">
          <h2 [ngClass]="type">{{ title }}</h2>
          <button (click)="closeModal()" class="close-btn">&times;</button>
        </div>
        <div class="modal-body">
          <p>{{ message }}</p>
        </div>
        <div class="modal-footer" *ngIf="isConfirm">
          <button class="btn btn-outline" (click)="closeModal()">İptal</button>
          <button class="btn btn-primary" [ngClass]="{'btn-danger': type === 'error' || type === 'warning'}" (click)="confirmAction()">Onayla</button>
        </div>
        <div class="modal-footer" *ngIf="!isConfirm">
          <button class="btn btn-primary" (click)="closeModal()">Tamam</button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .modal-overlay {
      position: fixed;
      top: 0; left: 0;
      width: 100vw; height: 100vh;
      background: rgba(0, 0, 0, 0.5);
      z-index: 2000;
      display: flex;
      align-items: center;
      justify-content: center;
      backdrop-filter: blur(4px);
    }
    .modal-content.alert-modal {
      background: white;
      width: 100%;
      max-width: 400px;
      border-radius: 16px;
      overflow: hidden;
      box-shadow: 0 10px 25px rgba(0,0,0,0.1);
      font-family: 'Inter', sans-serif;
      animation: popIn 0.3s cubic-bezier(0.175, 0.885, 0.32, 1.275);
    }
    @keyframes popIn {
      from { transform: scale(0.9); opacity: 0; }
      to { transform: scale(1); opacity: 1; }
    }
    .modal-header {
      padding: 1.25rem 1.5rem;
      border-bottom: 1px solid #e2e8f0;
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
    .modal-header h2 {
      margin: 0;
      font-size: 1.2rem;
      font-weight: 700;
    }
    .modal-header h2.success { color: #10b981; }
    .modal-header h2.error { color: #ef4444; }
    .modal-header h2.warning { color: #f59e0b; }
    .close-btn {
      background: none;
      border: none;
      font-size: 1.5rem;
      cursor: pointer;
      color: #64748b;
    }
    .modal-body {
      padding: 1.5rem;
      color: #334155;
      font-size: 1rem;
      line-height: 1.5;
    }
    .modal-footer {
      padding: 1rem 1.5rem;
      background: #f8fafc;
      border-top: 1px solid #e2e8f0;
      display: flex;
      justify-content: flex-end;
      gap: 1rem;
    }
    .btn {
      padding: 0.5rem 1.25rem;
      border-radius: 8px;
      border: none;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.2s;
    }
    .btn-primary { background: linear-gradient(135deg, #3b82f6, #2563eb); color: white; }
    .btn-primary:hover { box-shadow: 0 4px 12px rgba(59,130,246,0.3); transform: translateY(-1px); }
    .btn-danger { background: #ef4444 !important; color: white; }
    .btn-danger:hover { background: #dc2626 !important; }
    .btn-outline { background: white; border: 1px solid #cbd5e1; color: #334155; }
    .btn-outline:hover { background: #f1f5f9; }
  `]
})
export class AlertModalComponent {
  @Input() isOpen = false;
  @Input() title = 'Uyarı';
  @Input() message = '';
  @Input() type: 'success' | 'error' | 'warning' | 'info' = 'info';
  @Input() isConfirm = false;

  @Output() onConfirm = new EventEmitter<void>();
  @Output() onClose = new EventEmitter<void>();

  closeModal() {
    this.isOpen = false;
    this.onClose.emit();
  }

  confirmAction() {
    this.isOpen = false;
    this.onConfirm.emit();
  }
}
