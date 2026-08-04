---
name: land-a-fix
description: Use when landing a conformance fix-queue item or any spec-derived fix - the complete loop from spec citation through golden, manifest registration, gate, DEVLOG, commit and push. Invoke it when starting a CA/V queue item or any "fix this bug" task.
---

# Land a fix

> ⛔ **ONE WORK REGISTER: `kb/Work/`.** `python scripts/spec/work.py next` answers "what now"; `kb/Work.base` is the view. **Keep it current in the SAME change set as the work**, and **never start a list, table, tracker or "remaining work" section anywhere else** — five registers accumulated by 2026-08-04 and three each claimed to be canonical (CLAUDE.md rule 8).


The work register is **`kb/Work/`** — one note per item, `kb/Work.base` is the view, and
`python scripts/spec/work.py next` prints the ranked list (session-probe shows it every session).

⛔ **WORK THE `Fix next` VIEW TOP-DOWN, NOT SEVERITY TOP-DOWN.** It ranks on what a defect DOES to a user's
program — `not landed AND (wrong-answer OR crashes) AND not blocked` — because severity cannot separate the
cases that matter: PB24 (`FUNCTION LENGTH` silently wrong) and PB39 (rule-id numbering, zero wrong answers) are
BOTH `[MAJOR]`, and a session picked PB39. Each item's forensic prose is in its own note body.

## 1. Derive before you read

Use the `spec-lookup` skill. Produce the §/GR, the expected behavior, and the edition applicability BEFORE reading
the implementation. Work items carry a candidate fix — **verify it against the spec text; some are false
positives.**

## 2. Scope it completely

The spec plus the subsystem deep-dive (`docs/COBOLNET_DESIGN.md` §0.5 indexes them) define the scope. Tests
verify; they never scope. Implement every SR and GR of the rule, not the leg the failing test happens to hit.

**Deferral is debt.** "Document as a known non-conformance", "stage loud", "schedule for a dedicated pass", or
"reject the legal construct" are all debt, and only an explicit owner decision. If the correct fix is genuinely
large, surface the effort as a bare question — do not pre-decide the deferral.

## 3. Fix the root cause, then sweep

No workaround, no shim, no relabeling a defect a "quirk". Never edit valid COBOL to dodge a compiler bug — if the
source is valid, the compiler is broken.

**Every bug is a pattern.** Grep for the sibling instances and fix them in the same pass. Paste the fresh grep;
never write "swept" without one.

## 4. Cite it in the code

The exact section AND rule number (`§14.9.24 GR4d`), not "per the spec".

## 5. Write the golden AND register it in the same commit

- Expected value **computed from the spec**, never copied from the legacy oracle or from observed output.
- Positive golden in `tests/conformance/<edition>/` → add its name to that directory's `manifest.json` `enabled`.
- Negative golden in `tests/conformance/negative/` (`.cob` + `.err`) → add it to
  `tests/conformance/negative/manifest.json`, which is a SEPARATE manifest.
- Do **not** add a `GreenfieldOnly` entry — the legacy differential is opt-in now.

An unregistered golden never runs AND fails the manifest-integrity test — but only at the comprehensive gate, never
at the wave-local run. Register it before running even the wave-local gate.

If the fix gates a construct's `introducedIn`, run the edition-gate sweep — see the `new-construct` skill.

## 6. Gate, then commit

Use the `gate` skill. Read the verdict line, then commit as a separate call.

## 7. DEVLOG, commit, push

- `DEVLOG.md`: insert a new entry directly beneath the ordering note (DESCENDING, newest first). Header
  `## Entry NNN — YYYY-MM-DD HH:MM TZ — Title`, stamped from `date "+%Y-%m-%d %H:%M %Z"`. Write narratively —
  what changed, why, what broke, what was learned. Log the failures too.
- Commit message: write it to a scratchpad file and use `git commit -F <file>`. Do NOT inline a PowerShell
  here-string in the Bash tool — it is POSIX sh and the markers leak into the message.
- Update the fix-queue LANDED header, and plan §0 if the worklist moved.
- **Push.** Every checkpoint.
