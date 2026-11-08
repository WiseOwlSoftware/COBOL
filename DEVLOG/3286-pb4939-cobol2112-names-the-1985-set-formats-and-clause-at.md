## Entry 3286 — 2026-11-07 17:38 PST — PB4939: COBOL2112 names the 1985 SET formats and clause at --std 85

COBOL2112's text (the no-format arm, the not-admitted arm and the not-sole-receiver arm of `SetFormatSelection`) listed
the 2023 formats (Format 14, Format 10) and cited only `§2023:14.9.39.2` at every edition. The verdict was right at 85
(`§1985:VI.6.23.2` Format 2 takes index-name-3 only); the words were the 2023 statement of it.

Re-probe on the branch build: `SET WS-N UP BY 1` and `SET WS-N I1 UP BY 2` at `--std 85` reproduced both 2023-worded
messages. Fix: each `SetFormatSelection.Row` carries an optional `RowAt1985` (Formats 1 and 2, the only receiver-taking
formats the 1985 clause prints; its Formats 3 and 4 are the switch and condition-name statements). Below 2002 the
messages list only those rows and cite `§1985:VI.6.23.2`, `§1985:VI.6.23.3 SR2` and `§1985:VI.6.23.4 GR4` (each run
through `cite.py --check`). 2002 and later keep the 2023 text, as the note specified. A new format row with no 1985 form
needs nothing: it is simply absent from the 85 list.

Goldens: `pb4939-set-no-format-names-1985-clause-at-85` and `pb4939-set-not-admitted-cites-1985-rules-at-85`, each
refused at 85 with the clause substring pinned. No inventory row closed.

