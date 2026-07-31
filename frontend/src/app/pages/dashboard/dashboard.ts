import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ChartConfiguration, ChartData } from 'chart.js';
import { BaseChartDirective } from 'ng2-charts';

import { ResumoSla } from '../../core/models/chamado.model';
import { ChamadoPorStatus, ProdutividadeTecnico } from '../../core/models/metrica.model';
import { ChamadoService } from '../../core/services/chamado.service';
import { MetricaService } from '../../core/services/metrica.service';
import { RealtimeService } from '../../core/services/realtime.service';

@Component({
  selector: 'app-dashboard',
  imports: [BaseChartDirective],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard implements OnInit {
  private readonly chamadoService = inject(ChamadoService);
  private readonly metricaService = inject(MetricaService);
  private readonly realtimeService = inject(RealtimeService);
  private readonly router = inject(Router);

  protected readonly resumo = signal<ResumoSla | null>(null);
  protected readonly carregando = signal(true);
  protected readonly erro = signal<string | null>(null);

  protected readonly chamadosPorStatus = signal<ChamadoPorStatus[]>([]);
  protected readonly produtividadeTecnicos = signal<ProdutividadeTecnico[]>([]);
  protected readonly carregandoChamadosPorStatus = signal(true);
  protected readonly erroChamadosPorStatus = signal<string | null>(null);
  protected readonly carregandoProdutividade = signal(true);
  protected readonly erroProdutividade = signal<string | null>(null);

  protected readonly statusChartType = 'doughnut' as const;
  protected readonly statusChartOptions: ChartConfiguration<'doughnut'>['options'] = {
    responsive: true,
  };

  protected readonly statusChartData = computed<ChartData<'doughnut'>>(() => ({
    labels: this.chamadosPorStatus().map((item) => item.statusNome),
    datasets: [
      {
        data: this.chamadosPorStatus().map((item) => item.quantidade),
      },
    ],
  }));

  ngOnInit(): void {
    this.carregarResumo();
    this.carregarMetricas();
    this.realtimeService.on('ChamadoAtualizado', () => {
      this.carregarResumo();
      this.carregarMetricas();
    });
  }

  private carregarResumo(): void {
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

  private carregarMetricas(): void {
    this.carregandoChamadosPorStatus.set(true);
    this.erroChamadosPorStatus.set(null);
    this.carregandoProdutividade.set(true);
    this.erroProdutividade.set(null);

    this.metricaService.chamadosPorStatus().subscribe({
      next: (dados) => {
        this.chamadosPorStatus.set(dados);
        this.carregandoChamadosPorStatus.set(false);
      },
      error: () => {
        this.erroChamadosPorStatus.set(
          'Não foi possível carregar os indicadores de chamados por status.',
        );
        this.carregandoChamadosPorStatus.set(false);
      },
    });

    this.metricaService.produtividadeTecnicos().subscribe({
      next: (dados) => {
        this.produtividadeTecnicos.set(dados);
        this.carregandoProdutividade.set(false);
      },
      error: () => {
        this.erroProdutividade.set('Não foi possível carregar a produtividade dos técnicos.');
        this.carregandoProdutividade.set(false);
      },
    });
  }

  protected verVencidos(): void {
    this.router.navigate(['/chamados'], { queryParams: { situacaoSla: 'Vencido' } });
  }

  protected verEmRisco(): void {
    this.router.navigate(['/chamados'], { queryParams: { situacaoSla: 'EmRisco' } });
  }

  protected formatarHoras(horas: number | null): string {
    return horas === null ? '—' : horas.toFixed(1);
  }
}
