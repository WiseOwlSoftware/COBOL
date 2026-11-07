## Entry 3241 — 2026-11-06 23:27 PST — File the night's leads: bare citations left in the tooling, the local backend's first-error stop, two drifted 2023 figures (PB4911-PB4913)

Three notes, no code. **PB4911**: after PB4887 phase 2 (PR 1270) the tooling still carries bare citations of the 2023
standard. The operator lane found `gen_ledger.py` publishing 4.2.16 after a bare section sign. A read-only sweep with the parser's own screen
counts 372 candidate lines under `scripts/`, `tools/`, `.github/` and `.claude/` (an upper bound: the parser's own examples
and planted test data are among them), and the citations audit refuses none. The two screen-reach leads of the phase 2
branch are folded into it. **PB4912**: `transcribe_pdf.py pages --local` stops the whole run on the first HTTP error from
Ollama. The local witness run of the 1985 figure pages stopped at page 55 of 224, and a page-by-page driver with retries
finished it, leaving 221 of 224 pages written. **PB4913**: the 2023 `sweep_figures.py --check` reports two drifted figures
(§2023:14.9.9.2 and §2023:14.9.28.2 Format 3), a lead of wave 20261890.

