export interface Departamento {
  id: number;
  nome: string;
  descricao: string | null;
  ativo: boolean;
  criadoEm: string;
}

export interface DepartamentoCreateRequest {
  nome: string;
  descricao?: string;
}

export interface DepartamentoUpdateRequest {
  nome?: string;
  descricao?: string;
}
