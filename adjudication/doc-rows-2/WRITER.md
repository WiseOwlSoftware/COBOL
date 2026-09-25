# doc-rows-2 — WRITER brief (Annex A.1 items held back as OMIT / SKIP by doc-rows-1)

⛔ FIRST read `E:\CobolSharp\.claude\skills\workstream\templates\MANDATORY-PRACTICES.md` (all-roles rows + P10: read
`E:\claude-skills\skills\spec-oracle\SKILL.md` and `E:\claude-skills\skills\spec-compliance-audit\SKILL.md`). Then
`E:\CobolSharp\CLAUDE.md` (rules 1, 3, 4, 8). The repo is READ-ONLY to you: never edit, never run git or dotnet build.
You may run `python scripts/spec/cite.py --check …`, `python scripts/spec/where.py …`, read any file, and run the BUILT
compiler `{PIN}\src\Cobol.Net.Cli\bin\Debug\net10.0\cobol.exe <f.cob> --run --std <85|2002|2014|2023>` from your own run
directory `{OUT}\run\` (never inside the repo).

## Why these items are still unadjudicated

Owner decision `kb/Work/PB1535` (option (c)): an Annex A.1 row is recorded only WITH its `docs/CONFORMANCE.md` §7
determination row, and **wrong behaviour is never documented**. doc-rows-1 therefore OMITTED every item whose code
behaviour is wrong against the spec (and SKIPPED items waiting on the owner). Each input item carries doc-rows-1's staged
record (`staged_record`, with the evidence in its notes) and the owning defect note.

## Your job, per item (input: `{IN}`)

1. **Re-derive the obligation** from the A.1 item text and the clause it cites (`cite.py --check` every clause).
2. **Re-probe the code's CURRENT behaviour** (main has moved: trains 60–64 landed since doc-rows-1 — PB1033/1058,
   PB1041, PB1053, PB1066, PB1036, PB1556, PB1549, PB1569, R43 selectors, PB1557/1558, PB1565/1566, PB1548 BASE,
   PB1573/1574). Read the owning note; if its defect has been FIXED, say so with the probe.
3. **Choose the determination** the implementation makes or SHALL make — the spec-conforming choice. Where the standard
   leaves the choice to the implementor, apply CLAUDE.md rule 1's precedence: ISO if it controls → GnuCOBOL →
   IBM Enterprise COBOL / Micro Focus. Where an existing owner decision or determination settles it (R-notes, §3 of
   CONFORMANCE.md, DESIGN docs), follow it and cite it. You never ask the owner; if a genuine owner choice remains
   (an option set with significant consequences no precedence settles), decision = `OWNER` and write the bare question.
4. **Write the §7 row** — ONE markdown table line with exactly four cells, the same shape as the existing §7 rows:
   `| DOC-A.1-N | **<element>**, §<clause>, <required/optional> + documented | **<the determination, stated as what
   COBOL.NET does or shall do>** … | <witness test-refs, or "—"> |`. It documents the INTENDED (spec-conforming) choice.
5. **Choose the verdict** for the inventory record:
   - `CONFORMS` — the code already does exactly what the row documents (the defect was fixed, or never real); needs a
     witness test-ref (existing test, or name what is needed and leave test-ref empty → the row stays GAP until then).
   - `DIVERGES` — the row documents the intended choice and the compiler does something else. `notes` must name the
     owning kb/Work defect note (it will claim the row) and the probe showing the divergence.
   - `PARTIAL` — part of the determination holds.
   - `DOCUMENTED-NON-SUPPORT` — only if the element lies wholly inside a declined facility (owner decision R43) —
     cite the §5 row.
   `code-location` MUST start with `docs/CONFORMANCE.md#DOC-A.1-N` (the anchor your row creates), then the code sites.
6. **Decision** per item: `WRITE` (row + record), `OWNER` (bare question, no row), or `NOT-DOCUMENTABLE` (explain).

## Hard rules
- A determination is never "whatever the code happens to do" when the code is wrong — that is exactly what PB1535 forbids.
- Never invent a determination the spec forbids; never leave a determination vague ("implementation-dependent").
- Every clause you write goes through `cite.py --check`; paste the OK lines into `notes`.
- A row must not contradict another §7 or §3 row; if it must change another row, say so (`also_changes`).
- Owning notes: use the input's `owning_note`; if none fits, name the note that should own it (or say "NEW:" with a
  one-paragraph note text — never create kb/Work files yourself).

## Output — `{OUT}\rows-{SLUG}.jsonl`, one JSON line per item, written AS SOON AS each item is decided (checkpoint)
`{"item": N, "rule-id": "DOC-A.1-N", "decision": "WRITE|OWNER|NOT-DOCUMENTABLE", "row": "| DOC-A.1-N | … |",
 "record": {"rule-id", "verdict", "code-location", "test-ref", "editions", "notes"}, "owning_note": "PBnnnn",
 "defect_fixed": true|false, "probe": "what you ran and saw", "also_changes": "", "owner_question": ""}`
On start, read-and-skip items already in the file. Before each item check for `{STOP}`; if present, return.
Cap 150 turns. Return a ≤15-line summary.
