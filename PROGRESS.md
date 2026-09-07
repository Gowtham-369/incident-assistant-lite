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
###  Block 2 — EF Core + database
- [x] Done
- **What I did:** Created the `Incident` entity with `Severity` and `IncidentStatus`
  enums, `AppDbContext` with model config + 3 seeded incidents, wired the connection
  string to the `sql2025` container, registered the DbContext in Program.cs, ran the
  first migration, and verified the rows exist via sqlcmd inside the container.
- **Commands that mattered:**
  - `dotnet add package Microsoft.EntityFrameworkCore.SqlServer` (+ `.Design`)
  - `dotnet ef migrations add InitialCreate`
  - `dotnet ef database update`
  - Verify: `docker exec -it sql2025 /opt/mssql-tools18/bin/sqlcmd -S localhost
    -U sa -P '<pw>' -C -Q "SELECT Title, Severity, Status FROM IncidentDb.dbo.Incidents"`
- **Key design choices:**
  - Enums stored as strings via `HasConversion<string>()` — human-readable rows,
    at the cost of a few bytes vs int storage
  - Seed data via `HasData` must be deterministic (hardcoded Guids/dates baked
    into the migration — `Guid.NewGuid()` here would make every migration see
    "changed" data)
  - Connection string lives only in `appsettings.Development.json` (gitignored),
    never in `appsettings.json`
- **Surprises / gotchas:**
- **Time actually taken:**
### Block 3 — CRUD
- [x] Done
- **What I did:**
- **Scoped DI lifetime** — `AddScoped<IIncidentService, IncidentService>()` means
  one instance per HTTP request: everything in one request shares that instance,
  the next request gets a fresh one. Chosen because the service depends on
  AppDbContext, which is itself scoped (a DbContext is a unit of work, not
  thread-safe, must not live across requests). The trio: Transient = new every
  resolve, Scoped = per request, Singleton = one for app lifetime.

- **JsonStringEnumConverter** — added via AddJsonOptions. Without it enums
  serialize as their underlying numbers (`"severity": 2`); with it they become
  strings (`"severity": "High"`), both in responses and when parsing request
  bodies/query params. Pairs with `HasConversion<string>()` on the EF side:
  enums are human-readable in JSON *and* in the database rows.

- **CancellationToken** — a cooperative "stop signal" threaded through async
  calls. ASP.NET Core creates one per request and cancels it if the client
  disconnects (closed tab, timeout, navigation away). By accepting it in the
  controller and passing it down (service → EF → SQL), an abandoned request
  actually cancels the database query instead of burning resources computing a
  response nobody will read. Cooperative = nothing is forcibly killed; each
  layer checks the token and throws TaskCanceledException is a subclass of the OperationCanceledException at a safe point.
  Cost of adding it: one parameter. It matters most for slow/expensive queries —
especially on AI endpoint calls, which are exactly the slow, per-token-billed
  kind of work we want to stop early.
- **Surprises / gotchas:**
- **Time actually taken:**

### Block 4 — Hardening + kick off APIM provisioning
- [x] Done
- **What I did:

- Added Validation Annotations on Request record DTO
- **Surprises / gotchas:**
- **Time actually taken:**

### Block 5 — Entra ID app registration (API)
- [x] Done
- **App registration details:** incident-api, single tenant, no redirect URI
  (APIs don't redirect — that's a client concern). Application ID URI:
  `api://<client-id>`, exposed scope Incidents.ReadWrite.
- **What I did:** Registered the API in Entra ID, exposed the scope, added
  Microsoft.Identity.Web, configured AzureAd section in appsettings.json
  (tenant + client IDs are public identifiers, not secrets — safe to commit),
  added UseAuthentication before UseAuthorization, locked IncidentsController
  with [Authorize].
- **Verified:** anonymous GET /api/incidents → 401 with WWW-Authenticate: Bearer.
- **Surprises / gotchas:** none — but middleware order (authn before authz) is
  a classic failure point worth remembering.
### Block 6 — Prove auth works
- [x] Done
- **How I got a test token:**
- - **OAuth 2.0 auth code + PKCE flow, tested via Postman** — tokens are issued to
  *client* registrations, not resource/API registrations. So the setup takes two
  app registrations playing different roles:
  - `incident-api` (resource): exposes scope `Incidents.ReadWrite`, audience
    `api://<api-client-id>`, validates tokens — never requests them.
  - `incident-spa` (client): granted the delegated permission to that scope
    (+ admin consent), requests tokens on behalf of the signed-in user.

  Postman OAuth 2.0 config that worked:
  - Grant: Authorization Code (With PKCE), Code Challenge: SHA-256
  - Auth URL / Token URL: login.microsoftonline.com/`<spa-tenant-id>`/oauth2/v2.0/{authorize,token}
  - Client ID: the *SPA's* GUID; Client Secret: empty (PKCE replaces it — public
    clients prove themselves with a per-request code challenge, no stored secret)
  - Scope: `api://<api-client-id>/Incidents.ReadWrite` (the API's GUID — two
    different GUIDs playing two different roles). Also, can get this from incident-spa App in registered applications in Entra ID as we already gave delegated to permissions to incident-spa to use incident-api
  - Client Authentication: **send credentials in body** (Microsoft's token
    endpoint expects client_id in the body, not a Basic Auth header)

  Flow: Get New Access Token → browser opens Microsoft login → sign in → auth
  code redirects to Postman's callback → Postman redeems code + PKCE verifier
  for the access token → Bearer token on the request → API validates
  sig/iss/aud/exp offline → 200.

  Verified claims on jwt.ms: `aud` = `api://<api-client-id>`, `iss` = my tenant,
  `scp` = Incidents.ReadWrite, `exp` ≈ issue + 60-90 min.
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
| |  |  |
| AADSTS900144 "request body must contain client_id" (Postman token fetch) | Postman's Client Authentication was set to Basic Auth header; Microsoft's token endpoint expects client_id in the request body for public clients | Changed to "Send client credentials in body" |
| AADSTS9002327 "SPA tokens may only be redeemed via cross-origin requests" | Postman's callback URL was registered under the *Single-page application* platform, whose auth codes can only be redeemed by browser requests carrying an Origin header — Postman is a native app | Added a *Mobile and desktop applications* platform with the Postman callback; kept localhost:5173 under SPA for React. One client, two platforms, different redemption rules |
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


#### -How Microsoft Entra ID authentication and authorization works?
#### What happens at startup?
When the app boots, AddMicrosoftIdentityWebApi registers the JWT bearer authentication handler with your tenant's settings. The first time a token needs validating, the handler fetches your tenant's OpenID Connect discovery document from https://login.microsoftonline.com/<tenant-id>/v2.0/.well-known/openid-configuration — a JSON file describing your tenant's token issuer and, crucially, where its public signing keys live (the JWKS endpoint). It downloads and caches those keys. That's the entire setup: your API never talks to Entra ID again per-request; it now has everything needed to verify tokens locally.

#### What happens per request?

Every request passes through UseAuthentication(), which asks the bearer handler: is there an `Authorization: Bearer <token>` header?

**If No token:** the handler marks the request as anonymous. Then UseAuthorization() sees the [Authorize] attribute on the controller, finds no authenticated identity, and short-circuits with a 401 — your controller code never runs. The WWW-Authenticate: Bearer header is the API formally saying "this endpoint accepts bearer tokens; go get one." That's the standard challenge defined by the OAuth spec.

**With a token:** the handler validates it entirely offline using the cached keys — 
Four checks: 

The signature (was this really signed by your tenant? cryptographically verified against Microsoft's public keys — this is why no secret exists on your API), 

The issuer (your tenant, not someone else's), 

The audience (this token was minted for `api://<your-client-id>`, not for Graph or some other API — a token being valid isn't enough, it must be for you), 

The expiry. All four pass → the token's claims become the request's User identity, [Authorize] is satisfied, and your controller runs.

The elegant part worth saying in an interview: the API never sees credentials and holds no secrets. It's a pure verifier — users authenticate with Entra ID, and the API just checks the cryptographic receipt.
## Interview Talking Points (fill as they emerge)

- Defense in depth: JWT validated at APIM *and* at the API —
- Structured output from the LLM and defensive parsing —
- Why controllers + service layer over minimal APIs here —
-
