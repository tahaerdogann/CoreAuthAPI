import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MapPickerComponent, MapAddressResult } from './map-picker.component';
import { CdkDragDrop, moveItemInArray, CdkDropList, CdkDrag } from '@angular/cdk/drag-drop';

export interface DisplayPhoto {
  file?: File;
  existingData?: any;
  url: string;
}

@Component({
  selector: 'app-court-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MapPickerComponent, CdkDropList, CdkDrag],
  template: `
    <form [formGroup]="sahaForm" (ngSubmit)="onSubmit()" class="form-container">
      
      <!-- TEMEL BİLGİLER -->
      <div class="section-card">
        <h3 class="section-title">Temel Bilgiler</h3>
        <div class="form-grid">
          <div class="form-group">
            <label>Saha Adı</label>
            <input type="text" formControlName="name" placeholder="Örn: Merkez Spor Tesisleri">
          </div>
          
          <div class="form-group">
            <label>Spor Türü</label>
            <select formControlName="sportType" (change)="onSportTypeChange()">
              <option value="Futbol">Futbol</option>
              <option value="Basketbol">Basketbol</option>
              <option value="Tenis">Tenis</option>
              <option value="Voleybol">Voleybol</option>
            </select>
          </div>
          
          <div class="form-group">
            <label>Zemin Türü</label>
            <select formControlName="surfaceType">
              <option value="" disabled selected>-- Zemin Seçin --</option>
              <option *ngFor="let st of availableSurfaceTypes" [value]="st">{{ st }}</option>
            </select>
          </div>
        </div>
      </div>

      <!-- LOKASYON BİLGİLERİ -->
      <div class="section-card">
        <h3 class="section-title">Lokasyon Bilgileri</h3>
        
        <app-map-picker
          [mode]="'picker'"
          [latitude]="sahaForm.get('latitude')?.value"
          [longitude]="sahaForm.get('longitude')?.value"
          (addressSelected)="onAddressSelected($event)">
        </app-map-picker>

        <div class="form-grid" style="margin-top: 15px;">
          <div class="form-group">
            <label>İl</label>
            <input type="text" formControlName="city" readonly class="readonly-input" placeholder="">
          </div>
          <div class="form-group">
            <label>İlçe</label>
            <input type="text" formControlName="district" readonly class="readonly-input" placeholder="">
          </div>
          <div class="form-group">
            <label>Mahalle</label>
            <input type="text" formControlName="neighborhood" readonly class="readonly-input" placeholder="">
          </div>
          <div class="form-group full-width">
            <label>Açık Adres</label>
            <textarea formControlName="addressDetail" rows="2" placeholder="cadde, sokak, numara"></textarea>
          </div>
          <div class="form-group full-width">
            <label>Saha Açıklaması</label>
            <textarea formControlName="description" rows="3" placeholder="Saha hakkında ek bilgiler, kurallar, ulaşım detayları..."></textarea>
          </div>
        </div>
      </div>

      <!-- TESİS OLANAKLARI -->
      <div class="section-card" formGroupName="amenities">
        <h3 class="section-title">Tesis Olanakları</h3>
        <div class="checkbox-grid">
          <label class="checkbox-label"><input type="checkbox" formControlName="restroom"> WC / Lavabo</label>
          <label class="checkbox-label"><input type="checkbox" formControlName="cafeteria"> Kafe / Büfe</label>
          <label class="checkbox-label"><input type="checkbox" formControlName="disabledAccess"> Engelli Erişimi</label>
          <label class="checkbox-label"><input type="checkbox" formControlName="changingRoom"> Soyunma Odası</label>
          <label class="checkbox-label"><input type="checkbox" formControlName="wifi"> Ücretsiz Wi-Fi</label>
          <label class="checkbox-label"><input type="checkbox" formControlName="shower"> Duş</label>
          <label class="checkbox-label"><input type="checkbox" formControlName="locker"> Kilitli Dolap</label>
          <label class="checkbox-label"><input type="checkbox" formControlName="grandstand"> Tribün</label>
          <label class="checkbox-label"><input type="checkbox" formControlName="airConditioning"> Klima</label>
          <label class="checkbox-label"><input type="checkbox" formControlName="prayerRoom"> Mescit</label>
          <label class="checkbox-label"><input type="checkbox" formControlName="lighting"> Gece Aydınlatması</label>
        </div>
      </div>

      <!-- EKSTRA KİRALAMA SEÇENEKLERİ -->
      <div class="section-card" formGroupName="rentalOptions">
        <h3 class="section-title">Ekstra Kiralama Seçenekleri</h3>
        <p class="section-subtitle">Bu spor türü için sunabileceğiniz ekstraları seçip fiyatını belirleyebilirsiniz.</p>
        
        <div class="rental-grid">
          <div class="rental-item" *ngFor="let item of activeRentalOptionsKeys" [formGroupName]="item">
            <label class="checkbox-label rental-toggle">
              <input type="checkbox" formControlName="isActive"> 
              <span class="rental-name">{{ getRentalName(item) }}</span>
            </label>
            <div class="rental-inputs" *ngIf="sahaForm.get('rentalOptions.' + item + '.isActive')?.value">
              <div class="input-wrap">
                <label>Stok/Sayı</label>
                <input type="number" formControlName="availableCount" placeholder="Örn: 10">
              </div>
              <div class="input-wrap">
                <label>Birim Fiyat (₺)</label>
                <input type="number" formControlName="unitPrice" placeholder="Örn: 150">
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- FOTOĞRAFLAR -->
      <div class="section-card">
        <h3 class="section-title">Saha Fotoğrafları</h3>
        <p class="section-subtitle">En fazla 16 adet fotoğraf ekleyebilirsiniz. İlk fotoğraf kapak olarak kullanılacaktır.</p>
        
        <div class="file-upload-container" 
             (dragover)="onDragOver($event)" 
             (dragleave)="onDragLeave($event)" 
             (drop)="onFileDrop($event)"
             [class.drag-over]="isDraggingOver">
          <input type="file" id="photos" multiple accept="image/*" (change)="onFileSelected($event)" class="file-input" [disabled]="displayPhotos.length >= 16">
          <label for="photos" class="file-label" [class.disabled]="displayPhotos.length >= 16">
            <span class="upload-icon">📸</span>
            <span>Fotoğraf Seç veya Sürükle</span>
          </label>
        </div>

        <div class="photo-preview-grid" *ngIf="displayPhotos.length > 0" cdkDropList cdkDropListOrientation="mixed" (cdkDropListDropped)="dropPhoto($event)">
          <div class="photo-thumbnail" *ngFor="let photo of displayPhotos; let i = index" cdkDrag [class.new-photo]="photo.file">
            <img [src]="photo.url" alt="Saha Fotoğrafı">
            
            <div class="photo-overlay">
              <button type="button" class="btn-cover" *ngIf="i !== 0" (click)="setAsCover(i)">Kapak Yap</button>
            </div>
            
            <div class="cover-badge" *ngIf="i === 0">Kapak</div>
            <button type="button" class="btn-remove" (click)="removePhoto(i)">×</button>
          </div>
        </div>
      </div>

      <!-- BUTONLAR -->
      <div class="button-group">
        <button type="button" class="btn-cancel" *ngIf="showCancel" (click)="onCancel()">İptal Et</button>
        <button type="submit" class="btn-submit" [disabled]="isSubmitting">{{ isSubmitting ? 'Kaydediliyor...' : submitLabel }}</button>
      </div>

    </form>
  `,
  styles: [`
    .form-container { display: flex; flex-direction: column; gap: 24px; font-family: 'Inter', sans-serif; }
    .section-card { background: #ffffff; padding: 25px; border-radius: 12px; border: 1px solid #e2e8f0; box-shadow: 0 2px 10px rgba(0,0,0,0.02); }
    .section-title { margin: 0 0 15px 0; color: #1e293b; font-size: 18px; font-weight: 700; border-bottom: 1px solid #e2e8f0; padding-bottom: 12px; }
    .section-subtitle { margin: -5px 0 15px 0; color: #64748b; font-size: 13px; font-weight: 500; }
    
    .form-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 15px; }
    .full-width { grid-column: 1 / -1; }
    .form-group label { display: block; font-size: 13px; font-weight: 600; color: #475569; margin-bottom: 6px; }
    .form-group input, .form-group select, .form-group textarea { width: 100%; padding: 10px; border: 1px solid #cbd5e1; border-radius: 8px; font-size: 14px; outline: none; transition: border-color 0.2s; box-sizing: border-box; }
    .form-group input:focus, .form-group select:focus, .form-group textarea:focus { border-color: #3b82f6; }
    
    .checkbox-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(150px, 1fr)); gap: 12px; }
    .checkbox-label { display: flex; align-items: center; gap: 8px; font-size: 14px; color: #334155; font-weight: 500; cursor: pointer; }
    .checkbox-label input { width: 16px; height: 16px; cursor: pointer; }
    
    .rental-grid { display: flex; flex-direction: column; gap: 15px; }
    .rental-item { background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; padding: 15px; display: flex; flex-direction: column; gap: 12px; transition: background 0.2s; }
    .rental-item:hover { background: #f1f5f9; }
    .rental-toggle { font-size: 15px; font-weight: 600; color: #0f172a; }
    .rental-name { text-transform: capitalize; }
    .rental-inputs { display: flex; gap: 15px; flex-wrap: wrap; margin-left: 24px; padding-top: 10px; border-top: 1px dashed #cbd5e1; }
    .input-wrap { flex: 1; min-width: 120px; }
    .input-wrap label { display: block; font-size: 12px; font-weight: 600; color: #64748b; margin-bottom: 4px; }
    .input-wrap input { width: 100%; padding: 8px; border: 1px solid #cbd5e1; border-radius: 6px; font-size: 14px; outline: none; box-sizing: border-box; }
    
    .button-group { display: flex; justify-content: flex-end; gap: 15px; margin-top: 10px; }
    .btn-submit { background: linear-gradient(135deg, #3b82f6, #2563eb); color: white; border: none; padding: 12px 24px; border-radius: 8px; font-weight: 600; font-size: 15px; cursor: pointer; transition: all 0.2s; box-shadow: 0 4px 12px rgba(59, 130, 246, 0.3); }
    .btn-submit:hover:not([disabled]) { transform: translateY(-1px); box-shadow: 0 6px 16px rgba(59, 130, 246, 0.4); }
    .btn-submit[disabled] { opacity: 0.6; cursor: not-allowed; }
    .btn-cancel { background: transparent; color: #64748b; border: 1px solid #cbd5e1; padding: 12px 24px; border-radius: 8px; font-weight: 600; font-size: 15px; cursor: pointer; transition: all 0.2s; }
    .btn-cancel:hover { background: #f1f5f9; color: #334155; }

    /* FOTOĞRAF YÜKLEME ALANI */
    .file-upload-container { margin-bottom: 20px; position: relative; }
    .file-upload-container.drag-over .file-label { border-color: #10b981; background: #ecfdf5; color: #047857; }
    .file-input { width: 0.1px; height: 0.1px; opacity: 0; overflow: hidden; position: absolute; z-index: -1; }
    .file-label { display: flex; flex-direction: column; align-items: center; justify-content: center; padding: 30px; border: 2px dashed #94a3b8; border-radius: 12px; background: #f8fafc; cursor: pointer; transition: all 0.2s; color: #475569; font-weight: 600; }
    .file-label:hover:not(.disabled) { border-color: #3b82f6; background: #eff6ff; color: #1d4ed8; }
    .file-label.disabled { opacity: 0.5; cursor: not-allowed; }
    .upload-icon { font-size: 32px; margin-bottom: 8px; }
    
    .photo-preview-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(120px, 1fr)); gap: 15px; margin-top: 15px; }
    .photo-thumbnail { position: relative; width: 100%; aspect-ratio: 1; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 5px rgba(0,0,0,0.1); cursor: move; }
    .photo-thumbnail img { width: 100%; height: 100%; object-fit: cover; }
    .photo-thumbnail.new-photo img { filter: sepia(0.2); }
    
    .photo-overlay { position: absolute; top: 0; left: 0; right: 0; bottom: 0; background: rgba(0,0,0,0.5); opacity: 0; transition: opacity 0.2s; display: flex; align-items: center; justify-content: center; pointer-events: none; }
    .photo-thumbnail:hover .photo-overlay { opacity: 1; pointer-events: auto; }
    .btn-cover { background: #10b981; color: white; border: none; padding: 6px 12px; border-radius: 6px; font-weight: 600; font-size: 12px; cursor: pointer; transition: background 0.2s; }
    .btn-cover:hover { background: #059669; }
    
    .btn-remove { position: absolute; top: 5px; right: 5px; background: rgba(220, 38, 38, 0.9); color: white; border: none; width: 24px; height: 24px; border-radius: 50%; font-size: 16px; line-height: 1; cursor: pointer; display: flex; align-items: center; justify-content: center; transition: background 0.2s; z-index: 2; }
    .btn-remove:hover { background: #b91c1c; }
    .cover-badge { position: absolute; bottom: 5px; left: 5px; background: #10b981; color: white; font-size: 10px; font-weight: 700; padding: 3px 6px; border-radius: 4px; text-transform: uppercase; z-index: 2;}
    .readonly-input { background: #f8fafc !important; color: #64748b; cursor: not-allowed; }
    
    .cdk-drag-preview { box-sizing: border-box; border-radius: 8px; box-shadow: 0 5px 15px -3px rgba(0, 0, 0, 0.4); overflow: hidden; }
    .cdk-drag-preview img { width: 100%; height: 100%; object-fit: cover; }
    .cdk-drag-placeholder { opacity: 0; }
    .cdk-drag-animating { transition: transform 250ms cubic-bezier(0, 0, 0.2, 1); }
    .photo-preview-grid.cdk-drop-list-dragging .photo-thumbnail:not(.cdk-drag-placeholder) { transition: transform 250ms cubic-bezier(0, 0, 0.2, 1); }
  `]
})
export class CourtFormComponent implements OnInit {
  @Input() initialData: any = null;
  @Input() isSubmitting = false;
  @Input() submitLabel = 'Kaydet';
  @Input() showCancel = false;
  
  @Output() formSubmit = new EventEmitter<any>();
  @Output() formCancel = new EventEmitter<void>();

  sahaForm: FormGroup;
  

  availableSurfaceTypes: string[] = [];
  activeRentalOptionsKeys: string[] = [];
  
  displayPhotos: DisplayPhoto[] = [];
  deletedPhotoIds: string[] = []; // ID of photos to delete on server
  isDraggingOver = false;

  constructor(private fb: FormBuilder) {
    this.sahaForm = this.fb.group({
      name: ['', Validators.required],
      sportType: ['Futbol', Validators.required],
      surfaceType: ['', Validators.required],
      city: ['', Validators.required],
      district: ['', Validators.required],
      neighborhood: [''],
      addressDetail: [''],
      description: [''],
      latitude: [null as number | null],
      longitude: [null as number | null],
      amenities: this.fb.group({
        restroom: [false], cafeteria: [false], disabledAccess: [false],
        changingRoom: [false], wifi: [false], shower: [false],
        locker: [false], grandstand: [false], airConditioning: [false],
        prayerRoom: [false], lighting: [false]
      }),
      rentalOptions: this.fb.group({
        krampon: this.fb.group({ isActive: [false], availableCount: [null], unitPrice: [null] }),
        kaleci_eldiveni: this.fb.group({ isActive: [false], availableCount: [null], unitPrice: [null] }),
        top: this.fb.group({ isActive: [false], availableCount: [null], unitPrice: [null] }),
        yelek: this.fb.group({ isActive: [false], availableCount: [null], unitPrice: [null] }),
        hakem: this.fb.group({ isActive: [false], availableCount: [null], unitPrice: [null] }),
        ayakkabi: this.fb.group({ isActive: [false], availableCount: [null], unitPrice: [null] }),
        raket: this.fb.group({ isActive: [false], availableCount: [null], unitPrice: [null] }),
        top_kutu: this.fb.group({ isActive: [false], availableCount: [null], unitPrice: [null] }),
      })
    });
  }

  ngOnInit() {
    this.onSportTypeChange(); // Initialize defaults
    
    if (this.initialData) {
      this.populateForm(this.initialData);
    }
  }

  populateForm(data: any) {
    // Set basic fields
    this.sahaForm.patchValue({
      name: data.name || '',
      sportType: data.sportType || 'Futbol',
      city: data.city || '',
      district: data.district || '',
      neighborhood: data.neighborhood || '',
      addressDetail: data.addressDetail || '',
      description: data.description || '',
      latitude: data.latitude || null,
      longitude: data.longitude || null
    });

    this.onSportTypeChange(); // Load surface and rental options for sport
    
    // Set surface type after options are loaded
    if (data.surfaceType) {
      this.sahaForm.patchValue({ surfaceType: data.surfaceType });
    }

    // Set amenities
    if (data.amenities !== undefined) {
      const flags = data.amenities;
      this.sahaForm.get('amenities')?.patchValue({
        restroom: !!(flags & 1), cafeteria: !!(flags & 2), disabledAccess: !!(flags & 4),
        changingRoom: !!(flags & 8), wifi: !!(flags & 16), shower: !!(flags & 32),
        locker: !!(flags & 64), grandstand: !!(flags & 128), airConditioning: !!(flags & 256),
        prayerRoom: !!(flags & 512), lighting: !!(flags & 1024)
      });
    }

    // Set rental options
    if (data.rentalOptionsJson) {
      try {
        const parsed = typeof data.rentalOptionsJson === 'string' ? JSON.parse(data.rentalOptionsJson) : data.rentalOptionsJson;
        this.sahaForm.get('rentalOptions')?.patchValue(parsed);
      } catch(e) {}
    }

    // Set photos
    if (data.photos && Array.isArray(data.photos)) {
      const sortedPhotos = [...data.photos].sort((a, b) => a.displayOrder - b.displayOrder);
      this.displayPhotos = sortedPhotos.map(p => ({
        existingData: p,
        url: p.url
      }));
    }
  }

  onAddressSelected(result: MapAddressResult) {
    this.sahaForm.patchValue({
      city: result.city,
      district: result.district,
      neighborhood: result.neighborhood,
      addressDetail: result.fullAddress,
      latitude: result.latitude,
      longitude: result.longitude
    });
  }

  onSportTypeChange() {
    const st = this.sahaForm.get('sportType')?.value;
    
    // Update Surface Types
    if (st === 'Futbol') this.availableSurfaceTypes = ['Suni çim', 'Doğal çim', 'Akrilik', 'Kum'];
    else if (st === 'Basketbol') this.availableSurfaceTypes = ['Parke', 'PVC spor zemini', 'Beton'];
    else if (st === 'Tenis') this.availableSurfaceTypes = ['Toprak kort', 'Sert kort'];
    else if (st === 'Voleybol') this.availableSurfaceTypes = ['Parke', 'PVC spor zemini', 'Kum'];
    else this.availableSurfaceTypes = [];

    // Reset surface type if current value is not in new list
    const currentSurface = this.sahaForm.get('surfaceType')?.value;
    if (!this.availableSurfaceTypes.includes(currentSurface)) {
      this.sahaForm.patchValue({ surfaceType: '' });
    }

    // Update Rental Options
    if (st === 'Futbol') this.activeRentalOptionsKeys = ['krampon', 'kaleci_eldiveni', 'top', 'yelek', 'hakem'];
    else if (st === 'Basketbol') this.activeRentalOptionsKeys = ['top', 'ayakkabi', 'yelek'];
    else if (st === 'Tenis') this.activeRentalOptionsKeys = ['raket', 'top_kutu'];
    else if (st === 'Voleybol') this.activeRentalOptionsKeys = ['ayakkabi', 'top'];
    else this.activeRentalOptionsKeys = [];
  }

  getRentalName(key: string): string {
    const map: any = {
      'krampon': 'Krampon', 'kaleci_eldiveni': 'Kaleci Eldiveni', 'top': 'Top',
      'yelek': 'Yelek', 'hakem': 'Hakem', 'ayakkabi': 'Ayakkabı',
      'raket': 'Raket', 'top_kutu': 'Top (Kutu)'
    };
    return map[key] || key;
  }

  // --- DRAG & DROP FOTO YÜKLEME ---
  onDragOver(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.isDraggingOver = true;
  }

  onDragLeave(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.isDraggingOver = false;
  }

  onFileDrop(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.isDraggingOver = false;
    
    if (event.dataTransfer && event.dataTransfer.files) {
      this.handleFiles(event.dataTransfer.files);
    }
  }

  onFileSelected(event: any) {
    const files: FileList = event.target.files;
    this.handleFiles(files);
    event.target.value = '';
  }

  handleFiles(files: FileList) {
    if (!files) return;
    
    const remainingSlots = 16 - this.displayPhotos.length;
    const limit = Math.min(files.length, remainingSlots);
    
    for (let i = 0; i < limit; i++) {
      const file = files[i];
      if (!file.type.startsWith('image/')) continue; // Sadece resimler
      
      const url = URL.createObjectURL(file);
      this.displayPhotos.push({
        file: file,
        url: url
      });
    }
  }

  removePhoto(index: number) {
    const photo = this.displayPhotos[index];
    if (photo.existingData) {
      this.deletedPhotoIds.push(photo.existingData.id);
    } else if (photo.file) {
      URL.revokeObjectURL(photo.url);
    }
    this.displayPhotos.splice(index, 1);
  }

  // --- CDK DRAG DROP SIRALAMA ---
  dropPhoto(event: CdkDragDrop<DisplayPhoto[]>) {
    moveItemInArray(this.displayPhotos, event.previousIndex, event.currentIndex);
  }

  setAsCover(index: number) {
    if (index === 0) return;
    const item = this.displayPhotos.splice(index, 1)[0];
    this.displayPhotos.unshift(item);
  }

  onSubmit() {
    if (this.sahaForm.invalid) {
      this.sahaForm.markAllAsTouched();
      return;
    }

    const rawValue = this.sahaForm.value;
    
    // Clean up rental options (only send active ones for current sport)
    const activeRentals: any = {};
    this.activeRentalOptionsKeys.forEach(key => {
      const rental = rawValue.rentalOptions[key];

      if (rental && rental.isActive) {
        // Sadece gelişmiş obje formatını kullanıyoruz. Eski düz "Top": 50 satırını sildik!
        // Ekranda rahat göstermek için 'name' alanını da objenin içine ekliyoruz.
        activeRentals[key] = {
          name: this.getRentalName(key), // "Top", "Yelek" gibi büyük harfli görünür isim
          isActive: true,
          availableCount: rental.availableCount || 1,
          unitPrice: rental.unitPrice || 0
        };
      }
    });

    const payload = {
      name: rawValue.name,
      sportType: rawValue.sportType,
      surfaceType: rawValue.surfaceType,
      city: rawValue.city,
      district: rawValue.district,
      neighborhood: rawValue.neighborhood,
      addressDetail: rawValue.addressDetail,
      description: rawValue.description,
      latitude: rawValue.latitude,
      longitude: rawValue.longitude,
      hourlyPrice: 0,
      amenities: this.calculateBitwiseAmenities(rawValue.amenities),
      rentalOptionsJson: JSON.stringify(activeRentals),
      
      // Fotoğraf yükleme işlemleri için eklenen alanlar (Parent Component kullanacak)
      // Fotoğraf yükleme işlemleri için eklenen alanlar (Parent Component kullanacak)
      displayPhotos: this.displayPhotos,
      deletedPhotoIds: this.deletedPhotoIds
    };

    this.formSubmit.emit(payload);
  }

  onCancel() {
    this.formCancel.emit();
  }

  calculateBitwiseAmenities(ag: any): number {
    let total = 0;
    if (ag.restroom) total += 1;
    if (ag.cafeteria) total += 2;
    if (ag.disabledAccess) total += 4;
    if (ag.changingRoom) total += 8;
    if (ag.wifi) total += 16;
    if (ag.shower) total += 32;
    if (ag.locker) total += 64;
    if (ag.grandstand) total += 128;
    if (ag.airConditioning) total += 256;
    if (ag.prayerRoom) total += 512;
    if (ag.lighting) total += 1024;
    return total;
  }
}
