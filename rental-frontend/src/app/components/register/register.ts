import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms'; // ngModel hatasını çözer
import { RouterLink, Router } from '@angular/router';
import { Auth } from '../../services/auth';

@Component({
  selector: 'app-register',
  imports: [FormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.css'
})
export class Register {
  // HTML'in aradığı ve bulamadığı değişkenler
  email = '';
  password = '';

  constructor(private authService: Auth, private router: Router) { }

  // HTML'in aradığı ve bulamadığı fonksiyon
  kayitOl() {
    const registerData = {
      email: this.email,
      password: this.password
    };

    this.authService.register(registerData).subscribe({
      next: (response: any) => {
        console.log("Kayıt Başarılı:", response);
        alert("Harika! Kayıt başarılı. Şimdi giriş yapabilirsin.");
        this.router.navigate(['/login']);
      },
      error: (err) => {
        console.error("Kayıt Hatası:", err);
        alert("Kayıt başarısız! Konsoldaki hatayı incele.");
      }
    });
  }
}
