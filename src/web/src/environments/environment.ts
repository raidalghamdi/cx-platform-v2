// Default (production) environment. The dev server proxies /api → :5001 via
// proxy.conf.json, so apiBase stays as "/api/v1" in both modes.
export const environment = {
  production: true,
  apiBase: '/api/v1',
  showDemoAccounts: true,
};
