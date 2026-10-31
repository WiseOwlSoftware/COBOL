## Entry 2884 — 2026-10-31 15:05 PDT — A qualified paragraph or section header is refused (COBOL5716) instead of declaring a glued name (PB4238)

**The defect.** `procedureName` is `word ((OF | IN) word)?` and `paragraphName`, `sectionName` are that rule, so the
parse admitted `P1 OF S1.` and `S1 OF X SECTION.`. `ProcedureTableBuilder` read the header with `GetText()`, which glues
the children, and declared a procedure named `P1OFS1` / `S1OFX` that `PERFORM P1OFS1` then ran. The re-probe on the
w20261788f build reproduced both review programs (`IN P1`, `IN S1`, exit 0) and showed the same hole in a DECLARATIVES
section header, in the paragraph under it, and in an all-digits name (`12 OF Q.`).

**The rule.** ISO §14.2.1 prints the headers bare (`section-name-1 SECTION.`, `paragraph-name-1.`); §14.4.1 gives
qualification ("a paragraph-name (that may be qualified)") to the procedure-name that refers to a paragraph. A name being
defined is a bare user-defined word (§8.3.2.2). Citations run through `cite.py --check`.

**The fix.** `ProcedureTableBuilder.HeaderName` is the one reader of a header's name: it refuses a qualified header with
COBOL5716, at the header's own position (a paragraph inside a section used to report the section's), quoting the name as
written, and still returns the head word so the one error is not followed by a "not defined" per reference. All six
header reads (nondeclarative paragraph and section, their paragraphs, the declarative section and its paragraphs) go
through it. `ProcedureNameShape` is the one reader of a `procedureName` node's words (head, qualifier); the reference
resolver, the SORT/ALTER/PERFORM message quotes and the two validation-pass messages read through it too, so no consumer
of the node calls `GetText()`. The grammar keeps the shared production, so the refusal names the rule instead of a
generic parse error at `OF`.

**Pinned.** `ProcedureHeaderNameDriftTests` fails if any header slot (`paragraphName()`, `sectionName()`) is read without
`.procedureName()`, and refuses each header slot once per header at 85 to 2023 while qualified references to bare headers
stay legal. Conformance: `pb4238-qualified-paragraph-header` and `pb4238-qualified-section-header` (refused at every
edition) and `pb4238-qualified-reference-bare-headers` (accepted from 85; two sections each carrying `P1`, run through
`PERFORM P1 OF S1` and `PERFORM P1 IN S2`).

**Rows.** PB4238 claimed none and closes none.

