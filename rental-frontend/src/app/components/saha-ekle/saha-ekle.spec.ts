import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SahaEkle } from './saha-ekle';

describe('SahaEkle', () => {
  let component: SahaEkle;
  let fixture: ComponentFixture<SahaEkle>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SahaEkle],
    }).compileComponents();

    fixture = TestBed.createComponent(SahaEkle);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
