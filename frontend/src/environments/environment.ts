/**
 * Configuração de ambiente — PRODUÇÃO (padrão).
 * Substituída por environment.development.ts em builds de desenvolvimento
 * via `fileReplacements` no angular.json.
 */
export const environment = {
  production: true,
  /** Base da API REST. Em produção, servida atrás do mesmo host/proxy. */
  apiBaseUrl: '/api',
  /** Hub SignalR. Em produção, servido pelo mesmo nginx do frontend (mesma origem). */
  hubUrl: '/hubs/chamados',
};
