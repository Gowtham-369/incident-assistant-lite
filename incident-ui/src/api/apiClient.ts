import type { IPublicClientApplication, AccountInfo } from "@azure/msal-browser";
import { InteractionRequiredAuthError } from "@azure/msal-browser";
import { apiRequest } from "../auth/msalConfig";
import type {
  IncidentDto,
  CreateIncidentRequest,
  UpdateIncidentRequest,
  TriageResult,
} from "../types/incidents";

const API_BASE = import.meta.env.VITE_API_BASE;
const APIM_KEY = import.meta.env.VITE_APIM_KEY;

async function getToken(
  instance: IPublicClientApplication,
  account: AccountInfo
): Promise<string> {
  try {
    const result = await instance.acquireTokenSilent({ ...apiRequest, account });
    return result.accessToken;
  } catch (error) {
    // Silent fails when a token needs fresh interaction (expired session,
    // revoked consent, etc.) — rare mid-session, but must be handled.
    if (error instanceof InteractionRequiredAuthError) {
      const result = await instance.acquireTokenPopup(apiRequest);
      return result.accessToken;
    }
    throw error;
  }
}

async function apiFetch<T>(
  instance: IPublicClientApplication,
  account: AccountInfo,
  path: string,
  options: RequestInit = {}
): Promise<T> {
  const token = await getToken(instance, account);

  const response = await fetch(`${API_BASE}${path}`, {
    ...options,
    headers: {
      ...options.headers,
      Authorization: `Bearer ${token}`,
      "Ocp-Apim-Subscription-Key": APIM_KEY,
      "Content-Type": "application/json",
    },
  });

  if (!response.ok) {
    const body = await response.text().catch(() => "");
    throw new Error(`API ${response.status}: ${body || response.statusText}`);
  }

  if (response.status === 204) return undefined as T;
  return response.json() as Promise<T>;
}

export const incidentsApi = {
  getAll: (instance: IPublicClientApplication, account: AccountInfo) =>
    apiFetch<IncidentDto[]>(instance, account, "/api/incidents"),

  create: (instance: IPublicClientApplication, account: AccountInfo, body: CreateIncidentRequest) =>
    apiFetch<IncidentDto>(instance, account, "/api/incidents", {
      method: "POST",
      body: JSON.stringify(body),
    }),

  update: (instance: IPublicClientApplication, account: AccountInfo, id: string, body: UpdateIncidentRequest) =>
    apiFetch<IncidentDto>(instance, account, `/api/incidents/${id}`, {
      method: "PUT",
      body: JSON.stringify(body),
    }),

  remove: (instance: IPublicClientApplication, account: AccountInfo, id: string) =>
    apiFetch<void>(instance, account, `/api/incidents/${id}`, { method: "DELETE" }),

  triage: (instance: IPublicClientApplication, account: AccountInfo, id: string) =>
    apiFetch<TriageResult>(instance, account, `/api/incidents/${id}/triage`, {
      method: "POST",
    }),
};