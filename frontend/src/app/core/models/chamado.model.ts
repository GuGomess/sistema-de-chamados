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
