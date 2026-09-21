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

export interface TriageResult {
  suggestedSeverity: Severity;
  suggestedCategory: string;
  troubleshootingSteps: string[];
}