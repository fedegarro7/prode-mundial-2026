import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Rankings } from './rankings.component';

describe('Rankings', () => {
  let component: Rankings;
  let fixture: ComponentFixture<Rankings>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Rankings],
    }).compileComponents();

    fixture = TestBed.createComponent(Rankings);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
