import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink, Router } from '@angular/router';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-login',
  imports: [FormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.css'
})
export class Login {
  email = '';
  password = '';

  constructor(private authService: AuthService, private router: Router) { }

  girisYap() {
    const loginData = {
      email: this.email,
      password: this.password
    };

    this.authService.login(loginData).subscribe({
      next: (response: any) => {
        console.log("Backend'den Gelen Cevap:", response);

        // Bilet (Token) cebe atılıyor
        localStorage.setItem('token', response.token);

        // Dashboard'a yönlendirme yapılıyor
        this.router.navigate(['/dashboard']);
      },
       error: (err: any) => {
        console.error("Hata Oluştu:", err);
        alert("Giriş başarısız! Bilgilerini kontrol et.");
      }
    });
  }
}
