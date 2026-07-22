import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';

import { ChamadoService } from '../../../core/services/chamado.service';
import {
  Categoria,
  Chamado,
  PageMeta,
  Prioridade,
  Status,
  UsuarioResumo,
} from '../../../core/models/chamado.model';

@Component({
  selector: 'app-chamados-lista',
  imports: [ReactiveFormsModule, RouterLink, DatePipe],
  templateUrl: './chamados-lista.html',
  styleUrl: './chamados-lista.scss',
})
export class ChamadosLista implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly chamadoService = inject(ChamadoService);
  private readonly router = inject(Router);

  protected readonly status = signal<Status[]>([]);
  protected readonly categorias = signal<Categoria[]>([]);
  protected readonly prioridades = signal<Prioridade[]>([]);
  protected readonly tecnicos = signal<UsuarioResumo[]>([]);
  protected readonly carregandoOpcoes = signal(true);

  protected readonly chamados = signal<Chamado[]>([]);
  protected readonly meta = signal<PageMeta | null>(null);
  protected readonly carregando = signal(false);
  protected readonly erro = signal<string | null>(null);

  private pagina = 1;

  protected readonly form = this.formBuilder.nonNullable.group({
    q: [''],
    idStatus: [null as number | null],
    idCategoria: [null as number | null],
    idPrioridade: [null as number | null],
    idTecnico: [null as number | null],
    dataInicio: [''],
    dataFim: [''],
  });

  ngOnInit(): void {
    forkJoin({
      status: this.chamadoService.listarStatus(),
      categorias: this.chamadoService.listarCategorias(),
      prioridades: this.chamadoService.listarPrioridades(),
      tecnicos: this.chamadoService.listarTecnicos(),
    }).subscribe({
      next: ({ status, categorias, prioridades, tecnicos }) => {
        this.status.set(status);
        this.categorias.set(categorias);
        this.prioridades.set(prioridades);
        this.tecnicos.set(tecnicos);
        this.carregandoOpcoes.set(false);
        this.buscar();
      },
      error: () => {
        this.erro.set('Não foi possível carregar os filtros. Tente novamente.');
        this.carregandoOpcoes.set(false);
      },
    });
  }

  protected onFiltrar(): void {
    this.pagina = 1;
    this.buscar();
  }

  protected irParaPagina(pagina: number): void {
    this.pagina = pagina;
    this.buscar();
  }

  protected abrirChamado(chamado: Chamado): void {
    this.router.navigate(['/chamados', chamado.id]);
  }

  private buscar(): void {
    this.carregando.set(true);
    this.erro.set(null);

    const { q, idStatus, idCategoria, idPrioridade, idTecnico, dataInicio, dataFim } =
      this.form.getRawValue();

    this.chamadoService
      .listar({
        page: this.pagina,
        pageSize: 20,
        q,
        idStatus,
        idCategoria,
        idPrioridade,
        idTecnico,
        dataInicio: dataInicio || null,
        dataFim: dataFim || null,
      })
      .subscribe({
        next: (pagina) => {
          this.chamados.set(pagina.items);
          this.meta.set(pagina.meta);
          this.carregando.set(false);
        },
        error: (error: HttpErrorResponse) => {
          this.carregando.set(false);
          this.erro.set(
            error.status === 401
              ? 'Sua sessão expirou. Faça login novamente.'
              : 'Não foi possível carregar os chamados. Tente novamente.',
          );
        },
      });
  }
}
