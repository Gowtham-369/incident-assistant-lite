# AI Incident Assistant Lite — Build Journal

> How to use this: after each 30-min block (or whenever something notable happens), add a short entry. Write commands as you actually ran them, not as the docs say. Future-you reading this before an interview is the audience.

---

## Environment

| Tool | Version | Notes |
|---|---|---|
| .NET SDK | 10.0.301 | |
| dotnet-ef | 10.0.11 | installed as global tool |
| Node | 22.4.1 | |
| Docker | 27.5.1 | |
| Azure CLI | 2.90.0 | via Homebrew |
| SQL Server | 2025 (container `sql2025`, port 1433) | official mcr image, runs natively fine |
| Editor | VS Code + C# Dev Kit | |

---

## Decisions Log

Record *why*, not just *what*. This is interview gold.

| Date | Decision | Why | Alternatives considered |
|---|---|---|---|
| 2026-09-06 | SQL Server 2025 in Docker over Postgres/Azure SQL | Matches resume + target roles; container ran clean on my machine | PostgreSQL (ARM-native, smoother locally), Azure SQL free tier (fallback) |
| 2026-09-06 | CLI scaffolding over VS Code GUI | Precision (--use-controllers), repeatable, scriptable | C# Dev Kit New Project command |
| 2026-09-06 | Plain model deployment over Foundry Agent | API calls model directly via SDK; simpler, cleaner story | Foundry Agents (unneeded orchestration) |
|  |  |  |  |

---

## Day 1 — API, Auth, APIM

### 
### Block 1 — Scaffold
- [x] Done
- **What I did:** Created solution + webapi project with controllers, added Scalar
  as the API reference UI, set up Models/DTOs/Services/Data folders, verified the
  sample endpoint end to end in the browser.
- **Commands that mattered:**
  - `dotnet new webapi -n IncidentAssistant.Api --use-controllers` (flag needed on
    .NET 10, otherwise you get the minimal API template)
  - `dotnet add package Scalar.AspNetCore` + `app.MapScalarApiReference()` in Program.cs
  - `dotnet new gitignore` (covers bin/, obj/, dev appsettings)
- **Surprises / gotchas:** .NET 10 templates no longer ship Swagger UI (dropped in
  .NET 9) — only the OpenAPI document is generated. Scalar at `/scalar` is the
  replacement for browser-based endpoint testing.
- **Time actually taken:** ~30 min
### Block 2 — EF Core + database
- [ ] Done
- **What I did:**
- **Commands that mattered:**
- **Surprises / gotchas:**
- **Time actually taken:**

### Block 3 — CRUD
- [ ] Done
- **What I did:**
- **Surprises / gotchas:**
- **Time actually taken:**

### Block 4 — Hardening + kick off APIM provisioning
- [ ] Done
- **What I did:**
- **Surprises / gotchas:**
- **Time actually taken:**

### Block 5 — Entra ID app registration (API)
- [ ] Done
- **App registration details:** (tenant id, client id — ok to note here; never secrets)
- **What I did:**
- **Surprises / gotchas:**

### Block 6 — Prove auth works
- [ ] Done
- **How I got a test token:**
- **Surprises / gotchas:**

### Block 7 — Host the API
- [ ] Done
- **Where it's hosted / URL:**
- **Surprises / gotchas:**

### Block 8 — APIM
- [ ] Done
- **Gateway URL:**
- **Policies applied:**
- **Surprises / gotchas:**

---

## Day 2 — Frontend + AI

### Block 1 — React scaffold
- [ ] Done
- **What I did:**

### Block 2 — MSAL login
- [ ] Done
- **What I did:**
- **Surprises / gotchas:**

### Block 3 — API client + incident list (expect CORS fight)
- [ ] Done
- **The CORS fix that finally worked:**

### Block 4 — Create/update UI
- [ ] Done
- **What I did:**

### Block 5 — Azure OpenAI resource + deployment
- [ ] Done
- **Deployment name / model:**
- **What I did:**

### Block 6 — Triage endpoint
- [ ] Done
- **Prompt that worked:**
- **Surprises / gotchas:**

### Block 7 — Triage UI
- [ ] Done
- **What I did:**

### Block 8 — Polish + README
- [ ] Done
- **Repo URL:**

---

## Bugs & Fixes

Every bug that cost >10 minutes gets a line. These become your "tell me about a problem you debugged" answers.

| Symptom | Root cause | Fix |
|---|---|---|
|  |  |  |

---

## Things I Learned

Free-form. New concepts, aha moments, stuff to read more about later.

- `git check-ignore -v <path>` — asks git *why* a path is ignored. Prints the
  gitignore file, line number, and the exact rule that matched, e.g.:
  `.gitignore:33:[Bb]in/  IncidentAssistant.Api/bin/`
  Great for debugging "why is/isn't this file being tracked?" — much faster than
  reading the gitignore by eye. (Related: patterns without a leading `/` like
  `[Bb]in/` match at any depth in the repo, which is why one root gitignore
  covers nested project folders.)

- `git commit --amend --no-edit` — rewrites the most recent commit instead of
  creating a new one: whatever is staged gets folded into it. `--no-edit` keeps
  the existing message; use `-m "..."` instead to fix the message itself.
  Everyday uses: forgot a file, or typo in the commit message.
  Rule: amend freely *before* pushing, never after (it replaces the commit with
  a new hash, so amending already-pushed commits breaks shared history).-
## Interview Talking Points (fill as they emerge)

- Defense in depth: JWT validated at APIM *and* at the API —
- Structured output from the LLM and defensive parsing —
- Why controllers + service layer over minimal APIs here —
-
