import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../../services/auth';
import { Router } from '@angular/router';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './profile.html',
})
export class ProfileComponent implements OnInit {
  profileData = {
    name: '',
    surname: '',
    phoneNumber: '',
    email: ''
  };

  passwordData = {
    currentPassword: '',
    newPassword: '',
    confirmPassword: ''
  };

  profileMessage = '';
  passwordMessage = '';
  profileError = '';
  passwordError = '';

  constructor(private authService: AuthService, private router: Router, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    const user = this.authService.getUserInfo();
    if (user) {
      const nameParts = user.name.split(' ');
      this.profileData.surname = nameParts.pop() || '';
      this.profileData.name = nameParts.join(' ');
      this.profileData.email = user.email || '';
      this.profileData.phoneNumber = user.phoneNumber || '';
    }
  }

  updateProfile() {
    this.profileMessage = '';
    this.profileError = '';

    // Frontend basic validations
    if (this.profileData.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(this.profileData.email)) {
      this.profileError = 'Lütfen geçerli bir e-posta adresi girin.';
      return;
    }
    if (this.profileData.phoneNumber && !/^05\d{9}$/.test(this.profileData.phoneNumber)) {
      this.profileError = 'Telefon numarası 05 ile başlamalı ve 11 haneli olmalıdır.';
      return;
    }

    this.authService.updateProfile(this.profileData).subscribe({
      next: (res) => {
        console.log("Update response:", res); // Debug için ekledim
        this.profileMessage = res.message || 'Profil güncellendi.';
        
        const newToken = res.token || res.Token;
        if (newToken) {
          console.log("Yeni token kaydediliyor...");
          localStorage.setItem('token', newToken);
        }
        
        // Form gönderildikten sonra güncel bilgileri header'a yansıtmak için verify yapabiliriz.
        this.authService.verifyToken().subscribe();
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error("Update Profile Error:", err);
        if (typeof err.error === 'string') {
          this.profileError = err.error;
        } else if (err.error && err.error.title) {
          if (err.error.errors) {
            const firstKey = Object.keys(err.error.errors)[0];
            this.profileError = err.error.errors[firstKey][0];
          } else {
            this.profileError = err.error.title;
          }
        } else {
          this.profileError = 'Profil güncellenirken bir hata oluştu.';
        }
        this.cdr.detectChanges();
      }
    });
  }

  changePassword() {
    this.passwordMessage = '';
    this.passwordError = '';
    
    if (this.passwordData.newPassword !== this.passwordData.confirmPassword) {
      this.passwordError = 'Yeni şifreler eşleşmiyor.';
      return;
    }

    this.authService.changePassword({
      currentPassword: this.passwordData.currentPassword,
      newPassword: this.passwordData.newPassword
    }).subscribe({
      next: (res) => {
        this.passwordMessage = res.message || 'Şifre güncellendi.';
        this.passwordData = { currentPassword: '', newPassword: '', confirmPassword: '' };
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.passwordError = err.error || 'Şifre güncellenirken bir hata oluştu.';
        this.cdr.detectChanges();
      }
    });
  }
}
