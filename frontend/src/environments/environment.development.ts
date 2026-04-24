export const environment = {
  production: false,
  apiBaseUrl: 'https://192.168.1.15:7250',
  msalConfig: {
    auth: {
      clientId: '293ed388-e95b-429b-aba7-4b23b8972967',
      authority: 'https://demoveilleboisee.ciamlogin.com/demoveilleboisee.onmicrosoft.com/',
      knownAuthorities: ['demoveilleboisee.ciamlogin.com'],
      redirectUri: 'https://localhost:4200/auth-callback',
      postLogoutRedirectUri: '/',
    },
    apiScopes: ['api://5c009e04-a7cf-49b7-919f-864d40b44329/demo-veille-boisee.read'],
  },
};
