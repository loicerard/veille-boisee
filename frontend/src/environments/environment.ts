export const environment = {
  production: true,
  apiBaseUrl: 'https://ca-demo-veille-boisee-api.wonderfulrock-c585a80b.francecentral.azurecontainerapps.io',
  msalConfig: {
    auth: {
      clientId: '293ed388-e95b-429b-aba7-4b23b8972967',
      authority: 'https://demoveilleboisee.ciamlogin.com/demoveilleboisee.onmicrosoft.com/',
      knownAuthorities: ['demoveilleboisee.ciamlogin.com'],
      redirectUri: 'https://black-moss-0334f9d03.7.azurestaticapps.net/auth-callback',
      postLogoutRedirectUri: '/',
    },
    apiScopes: ['api://5c009e04-a7cf-49b7-919f-864d40b44329/demo-veille-boisee.read'],
  },
};
