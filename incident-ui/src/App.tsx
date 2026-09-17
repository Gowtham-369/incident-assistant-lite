import './App.css'
import { apiRequest } from './auth/msalConfig';
import { useState } from 'react';
import { useMsal} from '@azure/msal-react';
import type { AccountInfo } from "@azure/msal-browser";
import { IncidentList } from './components/IncidentList';

function App() {
  const {instance} = useMsal();
  const [account, setAccount] = useState<AccountInfo | null>(() => {
    const active = instance.getActiveAccount() ?? instance.getAllAccounts()[0] ?? null;
    if (active) {
      instance.setActiveAccount(active);
    }
    return active;
  });

  const login = () => {instance.loginRedirect(apiRequest)};
  const logout = async () => {
    if(!account) return;
    await instance.clearCache({account});
    setAccount(null);
  }

  return (
    <div style={{ padding: "2rem", fontFamily: "system-ui" }}>
      <h1>Incident Assistant</h1>

      {account ?
      ( 
        <>
          <p>Signed in as <strong>{account.name}</strong> ({account.username})</p>
          <button onClick={logout}>Sign out</button>
          {account && <IncidentList account={account} />}
        </>
        
      ) : (
        <>
          <p>Sign in to view your incidents.</p>
          <button onClick={login}>Sign in with Microsoft</button>
        </>
      )}
    </div>
  )
}

export default App;
