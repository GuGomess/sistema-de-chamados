import { PerfilCodigo } from './auth.model';

export interface Categoria {
  id: number;
  nome: string;
  descricao: string | null;
  ativa: boolean;
}

export interface Prioridade {
  id: number;
  nome: string;
  nivel: number;
}

export interface Status {
  id: number;
  nome: string;
  ordem: number;
  final: boolean;
}

export interface UsuarioResumo {
  id: number;
  nome: string;
  email: string;
  perfil: PerfilCodigo;
}

export interface ChamadoCreateRequest {
  titulo: string;
  descricao: string;
  idCategoria: number;
  idPrioridade: number;
}

export interface ChamadoUpdateRequest {
  idStatus?: number;
  idCategoria?: number;
  idPrioridade?: number;
}

export interface AtribuirTecnicoRequest {
  idTecnico: number;
}

export interface PrazoResolucaoUpdateRequest {
  prazoResolucao: string;
  justificativa: string;
}

export interface Chamado {
  id: number;
  titulo: string;
  descricao: string;
  solicitante: UsuarioResumo;
  tecnico: UsuarioResumo | null;
  status: Status;
  categoria: Categoria;
  prioridade: Prioridade;
  criadoEm: string;
  atualizadoEm: string;
  prazoResposta: string | null;
  prazoResolucao: string | null;
  resolvidoEm: string | null;
  fechadoEm: string | null;
}

export interface PageMeta {
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface ChamadoPage {
  items: Chamado[];
  meta: PageMeta;
}

export interface ChamadoFiltros {
  page?: number;
  pageSize?: number;
  sort?: string;
  q?: string;
  idStatus?: number | null;
  idCategoria?: number | null;
  idPrioridade?: number | null;
  idTecnico?: number | null;
  dataInicio?: string | null;
  dataFim?: string | null;
}
