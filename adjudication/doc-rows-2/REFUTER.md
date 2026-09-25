# doc-rows-2 — REFUTER brief

⛔ FIRST read `E:\CobolSharp\.claude\skills\workstream\templates\MANDATORY-PRACTICES.md` (P10: `spec-compliance-audit`,
`spec-oracle`). The repo is READ-ONLY; you may run `cite.py --check`, `where.py`, and the built compiler
`{PIN}\src\Cobol.Net.Cli\bin\Debug\net10.0\cobol.exe` from `{OUT}\run-refute\`.

Input: the writer's `{OUT}\rows-{SLUG}.jsonl` and the input `{IN}`. For EACH WRITE item, try to break it:
(a) does the row's determination satisfy the A.1 item and its clause (re-run every cite)? (b) is it the spec-conforming
choice — and where it is implementor latitude, did the writer apply ISO → GnuCOBOL → IBM/MF precedence (or an existing
owner decision) correctly? (c) does it contradict any other §7 / §3 row of docs/CONFORMANCE.md? (d) is the verdict right
— re-probe the compiler: CONFORMS only if the code does what the row says; DIVERGES must name a real divergence and an
owning note; (e) is `code-location` anchored at `docs/CONFORMANCE.md#DOC-A.1-N` first? (f) is the row one table line
with exactly four cells (no raw `|` inside a cell unless escaped `\|`)? For each OWNER item: is it genuinely an owner
choice, or does precedence settle it (then give the determination)?

Write `{OUT}\refute-{SLUG}.jsonl`, one line per item as decided: `{"item": N, "upheld": true|false, "kind":
"none|determination|citation|verdict|contradiction|shape|owner", "correction": "exact corrected row / verdict / reason"}`.
Default to overturned when uncertain; say what you tried. Before each item check `{STOP}`. Cap 120 turns. Return ≤10 lines.
