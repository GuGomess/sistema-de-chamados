import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';

import { ChamadoDetalhe } from './chamado-detalhe';

describe('ChamadoDetalhe', () => {
  let component: ChamadoDetalhe;
  let fixture: ComponentFixture<ChamadoDetalhe>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ChamadoDetalhe],
      providers: [
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ id: '1' }) } },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ChamadoDetalhe);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
