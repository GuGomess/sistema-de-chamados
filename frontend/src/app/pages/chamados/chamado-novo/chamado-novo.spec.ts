import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ChamadoNovo } from './chamado-novo';

describe('ChamadoNovo', () => {
  let component: ChamadoNovo;
  let fixture: ComponentFixture<ChamadoNovo>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ChamadoNovo],
    }).compileComponents();

    fixture = TestBed.createComponent(ChamadoNovo);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
