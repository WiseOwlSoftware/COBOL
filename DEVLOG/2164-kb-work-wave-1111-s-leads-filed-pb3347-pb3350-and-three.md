## Entry 2164 — 2026-10-17 16:32 PDT — kb/Work: wave 1111's leads filed (PB3347-PB3350 and three addenda)

The enqueue unit's leads from the wave-1111 reports (A, C, D, H) are on the register. Each was grepped against kb/Work by
member and symptom first; no code changed.

PB3347 (adjudication) asks whether a SPECIAL-NAMES switch condition-name and a level-88 of the same spelling in one source
element are ambiguous. `IF S-ON` binds to the 88 silently and prints ON for an OFF switch; §8.4.6.1 may make that correct,
so the verdict comes first. The three citations were run through `cite.py --check` and are recorded in the note.

PB3348 records that the traceability inventory's free-text row notes are checked by nothing: the drift test proves an anchor
resolves, never that the prose still describes it. PB3349 lists the 14 stale `Cobol<Type>.<Member>(args)` calls in docs that
`arity.py` finds (11 missing members, 3 wrong arities) with their lines. PB3350 files the O(records) `List<long>` that
`RecordFraming.FrameStarts` (now `RecordFraming.cs:589`) builds for a backward READ and for `CountRecords`.

Addenda: PB1322 gets the measurement (108 distinct CS1574 in `Cobol.Net.Compiler` alone, plus CS0419, CS1570, CS1584);
PB3192 gets its third symptom (a method's TYPE clone registers its subordinate names class-wide, `q1.cob`); PB3260 gets the
second report-header symptom (the `(HEAD <sha> when cut)` parenthetical read as the report's HEAD) and its code sites.
PB3351 was allocated and not used: lead 7 was an addendum to PB3260, whose fix shape already names the mechanism.

