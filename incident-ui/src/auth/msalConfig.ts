import { type Configuration, LogLevel } from "@azure/msal-browser";

export const msalConfig: Configuration = {
  auth: {
    clientId: "fee3592c-dc2b-4429-9b6e-c5fd1c4147b2",
    authority: "https://login.microsoftonline.com/510b3a83-d381-4cf6-9f11-0a8bbb15aa14",
    redirectUri: "http://localhost:5173",
    // onRedirectNavigate: () => false, // not helpful and is suppressing login redirect
  },
  cache: {
    cacheLocation: "sessionStorage", // tokens die with the tab; localStorage = survives restarts, bigger XSS blast radius
  },
  system: {
    loggerOptions: {
      loggerCallback: (level, message) => {
        if (level === LogLevel.Error) console.error(message);
      },
    },
  },
};

// The one scope that matters — your API's, from Day 1 Block 5
export const apiRequest = {
  scopes: ["api://e40a1eaa-09ea-4fca-b1b9-04de3f65e1cb/Incidents.ReadWrite"],
};