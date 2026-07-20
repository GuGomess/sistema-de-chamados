/**
 * Configuração de ambiente — DESENVOLVIMENTO.
 * As chamadas a `/api` são redirecionadas para o backend pelo proxy
 * configurado em proxy.conf.json (ver angular.json → serve).
 */
export const environment = {
  production: false,
  apiBaseUrl: '/api',
};
