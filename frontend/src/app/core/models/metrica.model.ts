export interface ChamadoPorStatus {
  statusId: number;
  statusNome: string;
  quantidade: number;
}

export interface ProdutividadeTecnico {
  tecnicoId: number;
  tecnicoNome: string;
  chamadosAtribuidos: number;
  chamadosResolvidos: number;
  tempoMedioResolucaoHoras: number | null;
}
