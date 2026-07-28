import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink, Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-login',
  imports: [FormsModule, RouterLink],
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
    private route: ActivatedRoute
  ) { }

  ngOnInit() {
    this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/dashboard';
  }

  girisYap() {
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
        alert("Giriş başarısız! Bilgilerini kontrol et.");
      }
    });
  }
}
