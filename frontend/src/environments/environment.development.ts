/**
 * Configuração de ambiente — DESENVOLVIMENTO.
 * As chamadas a `/api` são redirecionadas para o backend pelo proxy
 * configurado em proxy.conf.json (ver angular.json → serve).
 */
export const environment = {
  production: false,
  apiBaseUrl: '/api',
  /** Hub SignalR. Em desenvolvimento precisa da URL absoluta do backend — o
   * proxy do dev-server (proxy.conf.json) não é usado aqui. */
  hubUrl: 'http://localhost:5000/hubs/chamados',
};
