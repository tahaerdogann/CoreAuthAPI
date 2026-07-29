import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-login',
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.css'
})
export class Login implements OnInit {
  email = '';
  password = '';
  returnUrl: string = '/dashboard';

  constructor(
    private authService: AuthService, 
    private router: Router,
    private route: ActivatedRoute,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit() {
    this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/dashboard';
  }

  hataMesaji: string = '';

  girisYap() {
    this.hataMesaji = '';
    const loginData = {
      email: this.email,
      password: this.password
    };

    this.authService.login(loginData).subscribe({
      next: (response: any) => {
        console.log("Backend'den Gelen Cevap:", response);
        localStorage.setItem('token', response.token);
        this.router.navigateByUrl(this.returnUrl);
      },
       error: (err: any) => {
        console.error("Hata Oluştu:", err);
        this.hataMesaji = "Giriş başarısız! E-posta veya şifre hatalı.";
        this.cdr.detectChanges();
      }
    });
  }
}
