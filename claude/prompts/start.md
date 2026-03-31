# Start Command — CobolSharp Modernization & COBOL‑85 Compliance

Agents, begin the CobolSharp modernization and COBOL‑85 compliance process.

Brent is final authority.

---

## 1. Initialization Mode

If initialization = false:
  - Perform a full-system audit of the CobolSharp compiler and runtime.
  - Reconstruct the architecture (grammar, binder, semantics, runtime, diagnostics, tests).
  - Identify modernization and COBOL‑85 compliance gaps.
  - Generate the modernization ledger, including the COBOL‑85 Compliance Ledger (M300–M399).
  - Write/update `initialization.json` with:
    - commit pointer
    - ledger version
    - audit timestamp

If initialization = true:
  - Load `initialization.json`.
  - Load the modernization ledger (including COBOL‑85 compliance items).
  - Detect code changes since the last commit pointer.
  - Re-audit changed modules and update affected ledger items.
  - Continue closing ledger items in priority order.
  - Update the ledger and commit pointer (as a JSON patch, not applied automatically).

---

## 2. COBOL‑85 Compliance Mode (Mandatory)

You must treat COBOL‑85 compliance as a first-class, ongoing mission.

### 2.1 Canonicalization of M300–M399
- **Delete all existing M300–M399 ledger entries.**
- **Replace them with the new canonical M300–M399 entries provided by Brent.**
- These entries represent the authoritative list of all COBOL‑85 gaps.
- No other source overrides them.

### 2.2 Gap Execution Discipline
- Load the COBOL‑85 Compliance Ledger (M300–M399).
- Identify the highest-priority **open** item.
- Do **not** skip items.
- Do **not** reorder items unless Brent explicitly instructs you to.
- For the selected item:
  - Analyze the gap using only evidence from source, tests, NIST suites, and audit artifacts.
  - Propose concrete code changes (but do not apply them).
  - Propose concrete tests (unit, integration, NIST) needed to close the gap.
  - Identify risks and acceptance criteria.
  - Cross-link to related items (dependencies, e.g., grammar → binder → runtime).

---

## 3. Session Start Ritual

At the beginning of each session:

1. Load `initialization.json` and the modernization ledger.
2. Confirm whether initialization is true or false and follow the corresponding branch.
3. Enumerate all **open** COBOL‑85 compliance items (M300–M399).
4. Select the highest-priority open item.
5. Announce:  
   `resuming COBOL‑85 compliance — working on <ID>: <title>`.

---

## 4. Session End Ritual

Before ending each session:

1. Summarize all progress made on modernization and COBOL‑85 items:
   - items analyzed
   - fixes designed
   - tests added or required
   - remaining gaps
2. For each affected ledger item:
   - Update status (e.g., open → in-progress → ready-for-implementation).
   - Add evidence references (files, tests, NIST cases).
   - Record new or updated test requirements.
3. Produce a JSON patch for `modernization-ledger.json` and, if needed, `initialization.json`.
4. Do **not** apply patches; output them as text only.
5. Announce:  
   `ledger patch ready — awaiting human review`.

---

## 5. Constraints

- Operate in DRY-RUN mode by default:
  - Do not modify files.
  - Do not apply patches.
- Never drop or forget a ledger item; all work must be reflected back into the ledger.
- Never drift away from modernization and COBOL‑85 compliance unless Brent explicitly redirects you.
