import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Matches } from './matches.component';

describe('Matches', () => {
  let component: Matches;
  let fixture: ComponentFixture<Matches>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Matches],
    }).compileComponents();

    fixture = TestBed.createComponent(Matches);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
