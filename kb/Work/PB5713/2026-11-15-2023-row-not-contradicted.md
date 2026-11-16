**2026-11-15, the registrar landing records-sample-pb5692: the crash is real at --std 2023, but it does not contradict the 2023 row this note names, so that row is not re-recorded or claimed.**

The repro was re-run on that landing's build of main 95e8e476ad: `cobol.exe pos-sr9.cob --std 2023 --source-format fixed --run`
ends `Unhandled exception: System.ArgumentOutOfRangeException ... at WiseOwl.Cobol.CodeGen.SetEmitter.EmitSet(BoundSetConditions set)
in ...SetEmitter.cs:line 396`, rc=1, and the same at `--std 2002` (the line is 396 on this head, 391 when the note was filed).

The row the note names, `SR-13.18.63.3-37`, is the FORMAT 5 rule "The VALID phrase and the INVALID phrase shall not both be specified
for the same conditional variable" (`§2023:13.18.63.3 SR37`, `cite.py --check`: OK). Its DOCUMENTED-NON-SUPPORT record's licence is
the VALIDATE facility's (Annex A.4.14, Not claimed: `docs/conformance/5/A.4.14.md`), for the format 5 VALID/INVALID phrase only; it
says the VALUE clause's formats 1 to 4 are supported. The crash is a level-88 entry written with the table format (FORMAT 2,
`VALUE ... FROM (subscripts) [TO ...]`), which is no format 5 phrase. So the record's own licence does not cover the crash and the
crash does not contradict it: the row stays as recorded.

The 2002 row this note claims, `2002-SR-13.16.61.2-9` ("Formats 1 and 2 shall not be specified in a condition-name entry",
`§2002:13.16.61.2 SR9`, `cite.py --check`: OK), has no word-identical 2023 twin (`spec-lineage-candidates-2002.json` lists it
unpaired), so `SR-13.18.63.3-37` was a nearest pointer, not its successor. The 2023 rule a level-88 entry with the table format
breaks is for this note's fix to name, from the 2023 condition-name entry's own format; it was not identified here and no 2023 row
is claimed.
