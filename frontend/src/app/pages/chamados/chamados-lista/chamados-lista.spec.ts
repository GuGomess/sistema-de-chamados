import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ChamadosLista } from './chamados-lista';

describe('ChamadosLista', () => {
  let component: ChamadosLista;
  let fixture: ComponentFixture<ChamadosLista>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ChamadosLista],
    }).compileComponents();

    fixture = TestBed.createComponent(ChamadosLista);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
