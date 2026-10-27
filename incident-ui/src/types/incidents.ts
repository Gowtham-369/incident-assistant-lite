export type Severity = "Low" | "Medium" | "High" | "Critical";
export type IncidentStatus = "Open" | "InProgress" | "Resolved";

export interface IncidentDto {
  id: string;
  title: string;
  description: string;
  severity: Severity;
  status: IncidentStatus;
  category: string | null;
  createdAt: string;
  resolvedAt: string | null;
  resolutionNotes: string | null;
  // Day 3A RAG fields
  service?: string;
  rootCause?: string | null;
  errorSignatures?: string[];
}

export interface CreateIncidentRequest {
  title: string;
  description: string;
  severity: Severity;
  category?: string;
}

export interface UpdateIncidentRequest extends CreateIncidentRequest {
  status: IncidentStatus;
  resolutionNotes?: string;
}

export type Confidence = "high" | "medium" | "low" | "unassessed";

export type DegradedReason =
  | "none"
  | "no_evidence"
  | "weak_evidence"
  | "generation_failed"
  | "model_abstained"
  | "no_valid_steps"
  | "baseline_no_retrieval";

export interface TriageStep {
  text: string;
  sourceIncidentIds: string[];
}

export interface CitedIncident {
  incidentId: string;
  title: string;
  service: string;
  rerankerScore: number | null;
}

export interface SimilarIncident {
  incidentId: string;
  title: string;
  service: string;
  matchedSection: string;
  score: number;
  rerankerScore: number | null;
  crossService: boolean;
  matchedContent: string;
}

export interface TriageDiagnostics {
  executedMode: string;
  evidence: string;
  topRerankerScore: number | null;
  fallbackToAllServices: boolean;
  sourcesProvided: number;
  invalidCitations: number;
  droppedSteps: number;
}

export interface TriageResult {
  // Day 2 fields
  suggestedSeverity: Severity;
  suggestedCategory: string;
  troubleshootingSteps: string[];
  // Day 3A RAG fields
  summary?: string | null;
  likelyRootCause?: string | null;
  steps?: TriageStep[] | null;
  confidence?: Confidence;
  degraded?: DegradedReason;
  sources?: CitedIncident[] | null;
  similarIncidents?: SimilarIncident[] | null;
  diagnostics?: TriageDiagnostics | null;
}