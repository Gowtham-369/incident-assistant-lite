import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import { MsalProvider } from '@azure/msal-react'
import { PublicClientApplication } from "@azure/msal-browser";
import { msalConfig } from './auth/msalConfig.ts'

const msalInstance = new PublicClientApplication(msalConfig);

await msalInstance.initialize();
await msalInstance.handleRedirectPromise(); // claims any #code in the URL, exchanges it, caches tokens

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <MsalProvider instance={msalInstance}>
      <App />
    </MsalProvider>
  </StrictMode>,
)
