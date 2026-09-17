import { useCallback, useEffect, useState } from "react";
import { useMsal } from "@azure/msal-react";
import type { AccountInfo } from "@azure/msal-browser";
import { incidentsApi } from "../api/apiClient";
import type { IncidentDto, IncidentStatus } from "../types/incidents";
import { IncidentForm } from "./IncidentForm";

const STATUSES: IncidentStatus[] = ["Open", "InProgress", "Resolved"];

export function IncidentList({ account }: { account: AccountInfo }) {
  const { instance } = useMsal();
  const [incidents, setIncidents] = useState<IncidentDto[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  const refresh = useCallback(() => {
    incidentsApi
      .getAll(instance, account)
      .then(setIncidents)
      .catch((e) => setError(e.message));
  }, [instance, account]);

  useEffect(() => {
    refresh();
  }, [refresh]);

  const changeStatus = async (incident: IncidentDto, status: IncidentStatus) => {
    try {
      await incidentsApi.update(instance, account, incident.id, {
        title: incident.title,
        description: incident.description,
        severity: incident.severity,
        category: incident.category ?? undefined,
        status,
        resolutionNotes: incident.resolutionNotes ?? undefined,
      });
      refresh();
    } catch (e) {
      setError((e as Error).message);
    }
  };

  const remove = async (id: string) => {
    if (!confirm("Delete this incident?")) return;
    try {
      await incidentsApi.remove(instance, account, id);
      refresh();
    } catch (e) {
      setError((e as Error).message);
    }
  };

  if (error) return <p style={{ color: "crimson" }}>Failed to load: {error}</p>;

  return (
    <div>
      <IncidentForm instance={instance} account={account} onCreated={refresh} />

      {!incidents ? (
        <p>Loading incidents…</p>
      ) : (
        <table style={{ width: "100%", borderCollapse: "collapse", marginTop: "1rem" }}>
          <thead>
            <tr style={{ textAlign: "left", borderBottom: "2px solid #ccc" }}>
              <th>Title</th><th>Severity</th><th>Status</th><th>Category</th><th>Created</th><th></th>
            </tr>
          </thead>
          <tbody>
            {incidents.map((i) => (
              <tr key={i.id} style={{ borderBottom: "1px solid #eee" }}>
                <td>{i.title}</td>
                <td>{i.severity}</td>
                <td>
                  <select
                    value={i.status}
                    onChange={(e) => changeStatus(i, e.target.value as IncidentStatus)}
                  >
                    {STATUSES.map((s) => (
                      <option key={s} value={s}>{s}</option>
                    ))}
                  </select>
                </td>
                <td>{i.category ?? "—"}</td>
                <td>{new Date(i.createdAt).toLocaleDateString()}</td>
                <td>
                  <button onClick={() => remove(i.id)}>Delete</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}