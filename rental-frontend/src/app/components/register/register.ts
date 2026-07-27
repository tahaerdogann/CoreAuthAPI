import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators, AbstractControl } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './register.html',
  styleUrls: ['./register.css']
})
export class RegisterComponent implements OnInit {

  registerForm!: FormGroup;
  currentStep: number = 1;
  contactMethod: 'email' | 'phone' = 'email';

  mesaj: string = '';
  hata: string = '';
  emailAlinmisMi: boolean = false;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) { }

  ngOnInit() {
    this.registerForm = this.fb.group({
      name: ['', Validators.required],
      surname: ['', Validators.required],
      email: ['', [Validators.email]],
      phoneNumber: [''],
      password: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', Validators.required]
    }, { validators: this.passwordMatchValidator });
  }

  passwordMatchValidator(control: AbstractControl) {
    const password = control.get('password')?.value;
    const confirmPassword = control.get('confirmPassword')?.value;
    if (password !== confirmPassword) {
      control.get('confirmPassword')?.setErrors({ passwordMismatch: true });
      return { passwordMismatch: true };
    }
    return null;
  }

  nextStep() {
    if (this.currentStep === 1) {
      if (this.registerForm.get('name')?.valid && this.registerForm.get('surname')?.valid) {
        this.currentStep++;
      } else {
        this.hata = "Lütfen ad ve soyad alanlarını doldurun.";
      }
    }
    else if (this.currentStep === 2) {
      this.checkContactAndProceed();
    }
  }

  previousStep() {
    this.currentStep--;
    this.hata = '';
  }

  toggleContactMethod() {
    this.contactMethod = this.contactMethod === 'email' ? 'phone' : 'email';
    this.hata = '';
    this.registerForm.patchValue({ email: '', phoneNumber: '' });
  }

  checkContactAndProceed() {
    this.hata = '';

    if (this.contactMethod === 'email') {
      const emailControl = this.registerForm.get('email');
      if (emailControl?.invalid || !emailControl?.value) {
        this.hata = "Lütfen geçerli bir e-posta adresi girin.";
        return;
      }

      this.authService.checkEmail(emailControl.value).subscribe(res => {
        if (res.exists) {
          this.hata = "Bu e-posta adresi sistemde zaten kayıtlı!";
          this.emailAlinmisMi = true;
        } else {
          this.emailAlinmisMi = false;
          this.currentStep++;
        }
      });
    }
    else {
      const phoneControl = this.registerForm.get('phoneNumber');
      if (!phoneControl?.value || phoneControl.value.length < 10) {
        this.hata = "Lütfen geçerli bir telefon numarası girin.";
        return;
      }
      this.currentStep++;
    }
  }

  kayitOl() {
    if (this.registerForm.invalid) {
      this.hata = "Lütfen şifrelerinizi kontrol edin (En az 6 karakter ve eşleşmeli).";
      return;
    }

    this.mesaj = 'Kaydınız oluşturuluyor...';
    this.hata = '';

    const payload = {
      name: this.registerForm.value.name,
      surname: this.registerForm.value.surname,
      email: this.registerForm.value.email,
      phoneNumber: this.registerForm.value.phoneNumber,
      password: this.registerForm.value.password
    };

    this.authService.register(payload).subscribe({
      next: (response: any) => {
        localStorage.setItem('token', response.token);
        this.mesaj = 'Kayıt başarılı! Yönlendiriliyorsunuz...';

        setTimeout(() => {
          this.router.navigate(['/dashboard']);
        }, 1500);
      },
      error: (err) => {
        this.mesaj = '';
        this.hata = typeof err.error === 'string' ? err.error : 'Kayıt olurken bir hata oluştu.';
        console.error("Detaylı Hata:", err);
        // Backend'den gelen hatayı akıllıca yakalıyoruz
        if (typeof err.error === 'string') {
          this.hata = err.error;
        } else if (err.error && err.error.title) {
          this.hata = err.error.title; // .NET validasyon hataları
        } else if (err.error && typeof err.error === 'object') {
          // Eğer nesne döndüyse ilk hatayı ekrana bas
          const firstKey = Object.keys(err.error)[0];
          this.hata = err.error[firstKey] || 'Kayıt olurken bir hata oluştu.';
        } else {
          this.hata = 'Sunucuya bağlanırken bir hata oluştu.';
        }

      }
    });
  }
}
