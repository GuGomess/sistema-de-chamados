export type TipoNotificacao =
  | 'SlaEmRisco'
  | 'SlaVencido'
  | 'PrazoAjustado'
  | 'ChamadoReaberto'
  | 'FechadoPorCliente'
  | 'NovaAvaliacao';

export interface Notificacao {
  id: number;
  idChamado: number;
  tipo: TipoNotificacao;
  mensagem: string;
  lida: boolean;
  criadoEm: string;
}

export interface NotificacaoContagem {
  naoLidas: number;
}
