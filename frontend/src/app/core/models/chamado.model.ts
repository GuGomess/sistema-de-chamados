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

export type SituacaoSla = 'EmDia' | 'EmRisco' | 'Vencido';

export interface UsuarioResumo {
  id: number;
  nome: string;
  email: string;
  perfil: PerfilCodigo;
}

export interface ChamadoCreateRequest {
  titulo: string;
  descricao: string;
  // Opcionais: cliente não escolhe categoria/prioridade ao abrir um chamado
  // (o servidor aplica os defaults de triagem); técnico/administrador informam.
  idCategoria?: number;
  idPrioridade?: number;
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

export interface PrazoRespostaUpdateRequest {
  prazoResposta: string;
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
  primeiraRespostaEm: string | null;
  resolvidoEm: string | null;
  fechadoEm: string | null;
  situacaoSlaResposta: SituacaoSla;
  situacaoSlaResolucao: SituacaoSla;
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
  situacaoSla?: SituacaoSla | null;
  // Aba "Meus chamados" (técnico/administrador): assumidos por mim ou abertos por mim.
  meus?: boolean;
  ocultarFinalizados?: boolean;
}

export interface FecharClienteRequest {
  motivo?: string;
}

export interface Avaliacao {
  id: number;
  idChamado: number;
  autor: UsuarioResumo;
  nota: number;
  comentario: string | null;
  publica: boolean;
  oculta: boolean;
  editado: boolean;
  criadoEm: string;
}

export interface AvaliacaoCreateRequest {
  nota: number;
  comentario?: string;
  publica: boolean;
}

export interface AvaliacaoUpdateRequest {
  nota: number;
  comentario?: string;
  publica: boolean;
}

export interface ResumoSla {
  emRisco: number;
  vencidos: number;
}

export interface Comentario {
  id: number;
  idChamado: number;
  autor: UsuarioResumo;
  mensagem: string;
  interno: boolean;
  criadoEm: string;
  anexos: Anexo[];
}

export interface ComentarioCreateRequest {
  mensagem: string;
  interno: boolean;
  arquivos?: File[];
}

export interface Historico {
  id: number;
  idChamado: number;
  autor: UsuarioResumo;
  statusAnterior: Status | null;
  statusNovo: Status | null;
  acao: string;
  detalhe: string | null;
  criadoEm: string;
}

export interface Anexo {
  id: number;
  idChamado: number;
  idComentario: number | null;
  autor: UsuarioResumo;
  nomeArquivo: string;
  tipoMime: string;
  tamanhoBytes: number;
  url: string;
  criadoEm: string;
}
