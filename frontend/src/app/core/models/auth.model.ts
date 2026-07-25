export interface LoginRequest {
  email: string;
  senha: string;
}

export type PerfilCodigo = 'ADMINISTRADOR' | 'TECNICO' | 'CLIENTE';

export interface Usuario {
  id: number;
  nome: string;
  email: string;
  perfil: PerfilCodigo;
  ativo: boolean;
  criadoEm: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  usuario: Usuario;
}

export interface UsuarioCreateRequest {
  nome: string;
  email: string;
  senha: string;
  perfil: PerfilCodigo;
}

export interface RegistrarClienteRequest {
  nome: string;
  email: string;
  senha: string;
}
