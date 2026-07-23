import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';

import { ResumoSla } from '../../core/models/chamado.model';
import { ChamadoService } from '../../core/services/chamado.service';

@Component({
  selector: 'app-dashboard',
  imports: [],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard implements OnInit {
  private readonly chamadoService = inject(ChamadoService);
  private readonly router = inject(Router);

  protected readonly resumo = signal<ResumoSla | null>(null);
  protected readonly carregando = signal(true);
  protected readonly erro = signal<string | null>(null);

  ngOnInit(): void {
    this.chamadoService.resumoSla().subscribe({
      next: (resumo) => {
        this.resumo.set(resumo);
        this.carregando.set(false);
      },
      error: () => {
        this.erro.set('Não foi possível carregar os indicadores de SLA.');
        this.carregando.set(false);
      },
    });
  }

  protected verVencidos(): void {
    this.router.navigate(['/chamados'], { queryParams: { situacaoSla: 'Vencido' } });
  }

  protected verEmRisco(): void {
    this.router.navigate(['/chamados'], { queryParams: { situacaoSla: 'EmRisco' } });
  }
}
