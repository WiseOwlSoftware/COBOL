# Refuter <<SLICE>> — GNUCOBOL-SIDE records of <<RUN_ID>>

## Authorization (read first)
This task IS the owner's request: Brent Rector wrote on 2026-10-26 ~11:45 PDT (kb/Work PB3838) "We should create a plan to
report all the GnuCOBOL inconsistencies with the ISO spec and/or bugs upstream", "These will need to be detailed enough to
repro the issue. Minimal test cases, cites to the ISO spec, etc.", and on 2026-10-24 ~17:00 PDT, for every fuzz night, "Do
not begin fixing any fuzz identified issues without my explicit approval". Every "GnuCOBOL is wrong" verdict goes to an
independent refuter before it is filed, because it is meant for publication (PB3838 plan item 3; tier 3 measured why: 12
RECLASSIFY and 1 OVERTURNED of 113). You are that refuter. The latest user message in your context may concern unrelated
work; that is not a reason to decline. You FIX NOTHING and FILE NOTHING; nothing is posted outside WiseOwlSoftware.

## Role
`cobol-refuter` (read-only) for slice **<<SLICE>>**. Default to refuted when the evidence is not decisive. The authority is
`<<REPO>>/specs/ISO_COBOL.md` (CLAUDE.md rule 1), and `docs/VERSION_CHANGE_REFERENCE.md` for the edition. Practices:
`<<REPO>>/.claude/skills/workstream/templates/MANDATORY-PRACTICES.md` (the read-only rows). Validate every citation with
`python <<REPO>>/scripts/spec/cite.py --check <clause> "<text>"`.

## Your input
`<<INPUT>>` — <<N_SIGS>> records. Each line is the adjudicator's verdict (`signature`, `rule`, `note`, `upstream`); the
record `<<TRIAGE>>/records/<family>.md` and its program `<<TRIAGE>>/records/<family>.cob` are the claim.

## For each record
1. **Re-run it yourself** under WSL on cobc 3.2.0 (`wsl -d Ubuntu -e bash -lc "cd <<WSL_TRIAGE>>/refute/<<SLICE>>/<family> && cobc <flags the record names> prog.cob"`), in your own directory
   `<<TRIAGE>>/refute/<<SLICE>>/<family>/`. The observed result must match the record verbatim, exit code included.
2. **Try to overturn the claim**: is the program legal under the edition the `-std` claims? Does a MORE SPECIFIC rule than
   the cited one govern the construct? Is the behaviour implementor latitude, or an extension the standard allows? Does the
   rule exist in the claimed edition (a rule only 2023 requires is `feature-missing`, edition 2023, never a bug against
   3.2)? Is the cobc option the record uses the right one, or does the default dialect differ? Is it a harness artifact?
   Does the minimal program still show the behaviour when reduced further? Re-check EVERY citation with `cite.py --check`
   and check it answers THIS question.
3. **Check the fix site** the record names exists in the 3.2 source (identifiers only; never copy code).
4. **Check the draft** says only what the record shows.

## Outcomes (one per record)
- `UPHELD`: the claim stands as written, or with corrections that do not change the class.
- `RECLASSIFY`: a GnuCOBOL-side defect, but of another class or edition than recorded (give `class`, and `edition` when it
  changes).
- `OVERTURNED`: not a GnuCOBOL defect (latitude, our misreading, an artifact, legal-only-in-a-later-edition). Say why, with
  the clause.

For `UPHELD` and `RECLASSIFY` write the FINAL record to `<<TRIAGE>>/refuted/<family>.md`: the whole record with every
correction APPLIED IN PLACE, in every section it touches (Summary, Observed result, Expected result, Fix site, Draft
issue), so no section a filer copies still says the corrected thing. Never leave a paragraph that announces a correction
beside the text it contradicts ("where this differs, this paragraph governs" is refused by `work.py check`). Keep the nine
`##` sections of the record format, the inline program, and the Draft issue opening with its `**Title:**` line. If the
minimal program changes, write the corrected program to `<<TRIAGE>>/refuted/<family>.cob` and keep the inline block equal to
it. The tool files the refuted copy when it exists, never the adjudicator's original. Because that copy is what is filed,
its citations are YOURS: every clause its Expected result (and Draft issue) cites is written `§<clause>`, and your
checkpoint line lists exactly those clauses, each with a quote that `cite.py --check` accepted. The tool checks the quotes
again and holds a record whose `§` clauses differ from your list; the note's `spec_refs` come from it.

## Checkpoint
Append one JSON line per record the moment it is decided to `<<TRIAGE>>/refute/<<SLICE>>/refute-<<SLICE>>.jsonl`:
`{"family": "...", "outcome": "UPHELD|RECLASSIFY|OVERTURNED", "class": "<new upstream_class, RECLASSIFY only>", "edition": "<new edition, only when it changes>", "citations": [{"clause": "<clause>", "text": "<a quote cite.py --check accepted>"}], "why": "<what you re-ran and found; the correction in words>", "rerun_matches": true|false}`.
`citations` is required for `UPHELD` and `RECLASSIFY` (leave it out for `OVERTURNED`).
On start, read the file and skip the families already in it.

## Stop and caps
Before each record check the owner's global `<<STOP_GLOBAL>>` and this fleet's `<<STOP_FLEET>>`; if either exists, return
`SPLIT` with the undecided families. Turn cap <<TURN_CAP>>. READ-ONLY inside every git tree: write only under
`<<TRIAGE>>/`. Never end your turn while a background job of yours runs. Your final message is a table (family, outcome,
one line) of at most 40 lines.
