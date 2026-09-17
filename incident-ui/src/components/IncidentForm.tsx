import { useState } from "react";
import type { IPublicClientApplication, AccountInfo } from "@azure/msal-browser";
import { incidentsApi } from "../api/apiClient";
import type { Severity } from "../types/incidents";

const SEVERITIES: Severity[] = ["Low", "Medium", "High", "Critical"];

export function IncidentForm({
  instance,
  account,
  onCreated,
}: {
  instance: IPublicClientApplication;
  account: AccountInfo;
  onCreated: () => void;
}) {
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [severity, setSeverity] = useState<Severity>("Medium");
  const [category, setCategory] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    setError(null);
    try {
      await incidentsApi.create(instance, account, {
        title,
        description,
        severity,
        category: category || undefined,
      });
      setTitle("");
      setDescription("");
      setSeverity("Medium");
      setCategory("");
      onCreated();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <form onSubmit={submit} style={{ display: "grid", gap: "0.5rem", maxWidth: "480px" }}>
      <h3>New Incident</h3>
      <input
        placeholder="Title"
        value={title}
        onChange={(e) => setTitle(e.target.value)}
        required
        maxLength={200}
      />
      <textarea
        placeholder="Description"
        value={description}
        onChange={(e) => setDescription(e.target.value)}
        required
        maxLength={4000}
        rows={3}
      />
      <select value={severity} onChange={(e) => setSeverity(e.target.value as Severity)}>
        {SEVERITIES.map((s) => (
          <option key={s} value={s}>{s}</option>
        ))}
      </select>
      <input
        placeholder="Category (optional)"
        value={category}
        onChange={(e) => setCategory(e.target.value)}
        maxLength={100}
      />
      <button type="submit" disabled={submitting}>
        {submitting ? "Creating…" : "Create Incident"}
      </button>
      {error && <p style={{ color: "crimson" }}>{error}</p>}
    </form>
  );
}