import { useState } from "react";
import type {
  Confidence,
  DegradedReason,
  IncidentDto,
  TriageResult,
} from "../types/incidents";

const DEGRADED_MESSAGES: Record<Exclude<DegradedReason, "none">, string> = {
  no_evidence: "No similar past incidents were found.",
  weak_evidence: "Similar incidents were found, but none match closely enough to suggest steps.",
  generation_failed: "AI suggestions are unavailable right now. Showing similar past incidents instead.",
  model_abstained: "The assistant didn't find a past incident that clearly matches this one.",
  no_valid_steps: "Suggested steps couldn't be linked to past incidents, so they were withheld.",
  baseline_no_retrieval: "Baseline answer: generated without looking at past incidents.",
};

const CONFIDENCE_STYLE: Record<Confidence, { background: string; color: string; label: string }> = {
  high: { background: "#e6f4ea", color: "#1e6b34", label: "High confidence" },
  medium: { background: "#fff4e0", color: "#8a5300", label: "Medium confidence" },
  low: { background: "#ececec", color: "#4a4a4a", label: "Low confidence" },
  unassessed: { background: "#ececec", color: "#4a4a4a", label: "Not assessed" },
};

const styles = {
  badge: { display: "inline-block", padding: "0.15rem 0.6rem", borderRadius: 999, fontSize: "0.8rem", fontWeight: 600 },
  notice: { background: "#fff", borderLeft: "4px solid #b0b0b0", padding: "0.5rem 0.75rem", margin: "0.75rem 0" },
  chip: {
    marginLeft: "0.35rem", padding: "0 0.4rem", border: "1px solid #7a8fb3", borderRadius: 6,
    background: "#eef3fb", color: "#1f3f73", fontSize: "0.8rem", cursor: "pointer",
  },
  chipActive: { background: "#1f3f73", color: "#fff" },
  card: { background: "#fff", border: "1px solid #d6d6d6", borderRadius: 8, padding: "0.75rem", margin: "0.5rem 0" },
  linkButton: { background: "none", border: "none", color: "#1f3f73", textDecoration: "underline", cursor: "pointer", padding: 0 },
  muted: { color: "#666", fontSize: "0.85rem" },
  actions: { display: "flex", gap: "0.5rem", marginTop: "0.75rem" },
} as const;

interface TriagePanelProps {
  result: TriageResult;
  incidents: IncidentDto[];
  onApply: () => void;
  onDismiss: () => void;
}

export function TriagePanel({ result, incidents, onApply, onDismiss }: TriagePanelProps) {
  const [openIncidentId, setOpenIncidentId] = useState<string | null>(null);
  const [showSimilar, setShowSimilar] = useState(false);

  const degraded = result.degraded ?? "none";
  const confidence = result.confidence ?? "low";
  const sources = result.sources ?? [];
  const steps = result.steps ?? [];
  const similar = result.similarIncidents ?? [];
  const badge = CONFIDENCE_STYLE[confidence] ?? CONFIDENCE_STYLE.low;

  // Chip numbers follow the order of the cited sources list
  const sourceNumber = (id: string) => sources.findIndex((s) => s.incidentId === id) + 1;
  const toggle = (id: string) => setOpenIncidentId((current) => (current === id ? null : id));
  const openIncident = openIncidentId ? incidents.find((i) => i.id === openIncidentId) : undefined;

  // Similar incidents are the main content when no steps were given
  const similarVisible = degraded !== "none" || showSimilar;

  return (
    <div>
      <div style={{ display: "flex", alignItems: "center", gap: "0.75rem", flexWrap: "wrap" }}>
        <span style={{ ...styles.badge, background: badge.background, color: badge.color }}>{badge.label}</span>
        {degraded === "none" && (
          <span>
            Suggests <strong>{result.suggestedSeverity}</strong> severity, category{" "}
            <strong>{result.suggestedCategory}</strong>
          </span>
        )}
      </div>

      {degraded !== "none" && <div style={styles.notice}>{DEGRADED_MESSAGES[degraded]}</div>}

      {result.summary && <p style={{ margin: "0.75rem 0 0.25rem" }}>{result.summary}</p>}

      {result.likelyRootCause && (
        <p style={{ margin: "0.25rem 0" }}>
          <strong>Likely root cause:</strong> {result.likelyRootCause}
        </p>
      )}

      {steps.length > 0 ? (
        <ol style={{ paddingLeft: "1.25rem" }}>
          {steps.map((step, idx) => (
            <li key={idx} style={{ marginBottom: "0.35rem" }}>
              {step.text}
              {step.sourceIncidentIds.map((id) => {
                const n = sourceNumber(id);
                const title = sources.find((s) => s.incidentId === id)?.title ?? "Source";
                return (
                  <button
                    key={id}
                    type="button"
                    title={title}
                    aria-label={`Show source ${n}: ${title}`}
                    aria-expanded={openIncidentId === id}
                    onClick={() => toggle(id)}
                    style={{ ...styles.chip, ...(openIncidentId === id ? styles.chipActive : {}) }}
                  >
                    [{n > 0 ? n : "?"}]
                  </button>
                );
              })}
            </li>
          ))}
        </ol>
      ) : (
        // Day 2 style results (no structured steps) still render as a plain list
        result.troubleshootingSteps.length > 0 && (
          <ul>{result.troubleshootingSteps.map((s, idx) => <li key={idx}>{s}</li>)}</ul>
        )
      )}

      {openIncidentId && <SourceCard incident={openIncident} onClose={() => setOpenIncidentId(null)} />}

      {sources.length > 0 && (
        <div style={{ marginTop: "0.5rem" }}>
          <strong>Sources</strong>
          <ol style={{ paddingLeft: "1.25rem", margin: "0.25rem 0" }}>
            {sources.map((s) => (
              <li key={s.incidentId}>
                <button type="button" style={styles.linkButton} onClick={() => toggle(s.incidentId)}>
                  {s.title}
                </button>{" "}
                <span style={styles.muted}>
                  {s.service}
                  {s.rerankerScore != null && ` · match ${s.rerankerScore.toFixed(2)} / 4`}
                </span>
              </li>
            ))}
          </ol>
        </div>
      )}

      {similar.length > 0 && (
        <div style={{ marginTop: "0.5rem" }}>
          {degraded === "none" ? (
            <button type="button" style={styles.linkButton} onClick={() => setShowSimilar((v) => !v)}>
              {showSimilar ? "Hide" : "Show"} other similar incidents ({similar.length})
            </button>
          ) : (
            <strong>Similar past incidents</strong>
          )}
          {similarVisible && (
            <ul style={{ paddingLeft: "1.25rem", margin: "0.25rem 0" }}>
              {similar.map((s) => (
                <li key={s.incidentId}>
                  <button type="button" style={styles.linkButton} onClick={() => toggle(s.incidentId)}>
                    {s.title}
                  </button>{" "}
                  <span style={styles.muted}>
                    {s.service}
                    {s.crossService && " · other service"}
                    {s.rerankerScore != null && ` · match ${s.rerankerScore.toFixed(2)} / 4`}
                  </span>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}

      {result.diagnostics && (
        <details style={{ marginTop: "0.5rem" }}>
          <summary style={styles.muted}>How this answer was produced</summary>
          <div style={styles.muted}>
            Retrieval: {result.diagnostics.executedMode}
            {result.diagnostics.fallbackToAllServices && " (widened to all services)"} · evidence{" "}
            {result.diagnostics.evidence}
            {result.diagnostics.topRerankerScore != null &&
              ` · top match ${result.diagnostics.topRerankerScore.toFixed(2)} / 4`}{" "}
            · {result.diagnostics.sourcesProvided} sources given to the model
            {result.diagnostics.droppedSteps > 0 && ` · ${result.diagnostics.droppedSteps} uncited steps removed`}
          </div>
        </details>
      )}

      <div style={styles.actions}>
        {degraded === "none" && <button onClick={onApply}>Apply suggestion</button>}
        <button onClick={onDismiss}>Dismiss</button>
      </div>
    </div>
  );
}

function SourceCard({ incident, onClose }: { incident?: IncidentDto; onClose: () => void }) {
  if (!incident) {
    return (
      <div style={styles.card}>
        <span style={styles.muted}>This incident isn't in the loaded list. Refresh to see it.</span>{" "}
        <button type="button" style={styles.linkButton} onClick={onClose}>Close</button>
      </div>
    );
  }

  return (
    <div style={styles.card}>
      <div style={{ display: "flex", justifyContent: "space-between", gap: "1rem" }}>
        <strong>{incident.title}</strong>
        <button type="button" style={styles.linkButton} onClick={onClose}>Close</button>
      </div>
      <div style={styles.muted}>
        {incident.service ?? "unknown service"} · {incident.severity} · {incident.status}
        {incident.resolvedAt && ` · resolved ${new Date(incident.resolvedAt).toLocaleDateString()}`}
      </div>
      <p style={{ margin: "0.5rem 0 0.25rem" }}>{incident.description}</p>
      {incident.rootCause && <p style={{ margin: "0.25rem 0" }}><strong>Root cause:</strong> {incident.rootCause}</p>}
      {incident.resolutionNotes && (
        <p style={{ margin: "0.25rem 0" }}><strong>Resolution:</strong> {incident.resolutionNotes}</p>
      )}
    </div>
  );
}