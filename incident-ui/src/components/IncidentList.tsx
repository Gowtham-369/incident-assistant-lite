import { useEffect, useState } from "react";
import { useMsal } from "@azure/msal-react";
import type { AccountInfo } from "@azure/msal-browser";
import { incidentsApi } from "../api/apiClient";
import type { IncidentDto } from "../types/incidents";

export function IncidentList({ account }: { account: AccountInfo }) {
  const { instance } = useMsal();
  const [incidents, setIncidents] = useState<IncidentDto[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    incidentsApi
      .getAll(instance, account)
      .then(setIncidents)
      .catch((e) => setError(e.message));
  }, [instance, account]);

  if (error) return <p style={{ color: "crimson" }}>Failed to load: {error}</p>;
  if (!incidents) return <p>Loading incidents…</p>;

  return (
    <table style={{ width: "100%", borderCollapse: "collapse", marginTop: "1rem" }}>
      <thead>
        <tr style={{ textAlign: "left", borderBottom: "2px solid #ccc" }}>
          <th>Title</th><th>Severity</th><th>Status</th><th>Category</th><th>Created</th>
        </tr>
      </thead>
      <tbody>
        {incidents.map((i) => (
          <tr key={i.id} style={{ borderBottom: "1px solid #eee" }}>
            <td>{i.title}</td>
            <td>{i.severity}</td>
            <td>{i.status}</td>
            <td>{i.category ?? "—"}</td>
            <td>{new Date(i.createdAt).toLocaleDateString()}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}