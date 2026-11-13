## PB5198, PB5388: the 1985 RERUN clause writes rerun information in its integer-1 RECORDS form, and its doc comment sits on its own method

**PB5198.** At `--std 85` the RERUN clause was parsed, its five syntax rules were asked, and then `BindIoControl` dropped it: no form wrote any rerun information,
though §1985:VII.2.12.4 GR2 says "The implementor must provide at least one of the specified forms of the RERUN clause." and the clause is not in the hardware
dependent list, so §1985:I.1.5.2.2 asks the function. Re-probe on this tree (the note's program, `--std 85 --run`, empty directory): exit 0, `RERUN DONE`, and the
directory held `r3data.txt` (20 bytes) and the compiler's outputs, no rerun file. The note offered two answers; recording a non-support would record a non-conformance
(GR2 asks at least one form), so the decision is to provide form d, `RERUN ON implementor-name-1 EVERY integer-1 RECORDS OF file-name-2` (GR2 d), the one form that
applies to every organization and access and needs no reel device. The decision note is `kb/Work/PB5198/2026-11-13-decision-one-form-provided.md`; the determination is
`docs/CONFORMANCE.md` section 3.11.

The shape follows RESERVE, the property of a file control entry told to the connector right after registration. `FileControlBinder.RecordRerunRecords` keeps the clause
on file-name-2 (`FileModel.RerunRecords`) when its ON word is an implementor-name and its integer-1 is nonzero; `EmitAreaRegistrations`, the one call site every
registering arm already makes, emits `CobolFile.RegisterRerunRecords`; and `FileConnector.RecordProcessed` is the one count and the one writer: a READ (through
`ReadSucceeded`, the one terminal of a successful read, so the five read entries need no edit), WRITE, WRITE ADVANCING, REWRITE or DELETE that completed (a status of
class 0) is a record processed, the count belongs to the OPEN, and every integer-1 records one line (`RERUN <file-name-2> <count>`, a line feed on every host) is appended
to the host file the implementor-name spells as an ASSIGN target spells one, through the run unit's host medium (`HostFile.OpenRerunDevice`, the seventh `HostFile` role and an
`IHostMedium` member: the first draft opened an `OpenAuxiliary` append handle and then a connector write stream, and the gate's drift tests on append handles, share modes
and `OpenOrCreate` sites refused both, correctly, since the `HostFile` roles are the one place a host-path stream is opened). A device the host refuses is the file's I-O status '30'
(§2023:9.1.13.6 item 1; item 6 b's '37' is stated of OPEN and DELETE FILE only), the record the statement wrote staying written. The six other forms stay accepted with
their syntax rules asked and write nothing.

Goldens: the program test `conformance-suite/wiseowl/cases/VII.2.12@1985-RERUN-CLAUSE/pb5198_rerun_records_device_85` (EVERY 2 over a sequential file written five times and
read three, EVERY 1 over a relative file written twice, rewritten and deleted; it reads each device back) and `RerunRecordsTests` (five facts and a theory: the count from the
OPEN, a failed statement and an end of file not counted, relative and indexed write/rewrite/delete counted with a duplicate key and a missing record not, a refused device
answering '30' for an I-O failure and for an insufficient authority). Rows re-verdicted by `record_verdicts --edition 1985`: `1985-GR-VII.2.12.4-1` and
`1985-GR-VII.2.12.4-2` PARTIAL to CONFORMS (GAP 311 to 309), and the notes of `1985-GR-VIII.2.6.3-1` and `1985-GR-IX.2.8.3-1`, which called the facility null, restated.
No diagnostic code was used (COBOL7090-COBOL7092 returned): a warning for an unprovided form is not asked by the standard and is left to a note if wanted.

**PB5388.** The `<summary>` of `ScreenRerunClauses` ended just before the one of `IsSequentiallyOrganized`, so the RERUN documentation attached to the wrong method.
`IsSequentiallyOrganized` and its summary moved above the RERUN block; the sweep of the file for another `</summary>` followed by a `<summary>` found none.

**Self-review.** Architecture: one count and one writer in the connector base, one registration call site, the model carries the clause and no C# text. Full code: the rule
cited is GR2 d, not a paraphrase, and the sibling arms (sequential, relative, indexed) all reach the one count. Performance: the unregistered case is one null test per
terminal. Duplication: no second copy of the count. The review lenses found the first draft's '37' for a refused permission unsupported by item 6 b and the registry key in
the written line; both fixed.
