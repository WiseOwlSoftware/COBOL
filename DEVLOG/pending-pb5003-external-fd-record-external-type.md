## PB5003 - a record that is external by its FD and by its type takes one cell, not two

`FD F IS EXTERNAL. 01 R TYPE TG.` with `01 TG IS TYPEDEF STRONG EXTERNAL.` produced C# declaring `_scell_R` and `_redef_R` twice
(CS0102, exit 70). Re-probe on the build reproduced it. `LinkageBinder.CallBindExternalAndGlobal` claimed the record twice: the
by-type arm keyed a cell `R` (the record is external through its type, ISO 2023 13.18.22 type rules) and the EXTERNAL-FD arm keyed
`FD::F` (13.18.22.4 GR4 b, GR5). The by-type arm now skips a record of an EXTERNAL FD, so the FD arm, which keys by the file's
externalized name, is the only claimant. Siblings probed: a non-pointer group record under an EXTERNAL FD (fixed by the same line),
a non-EXTERNAL FD with an EXTERNAL-typed record and a plain 01 of the same type (both unchanged and correct).

Goldens: `pb5003-external-fd-record-external-type` (accept 2023, `WXYZ07 ABCD42`) and its refusal below 2023 (COBOL0900, the
strong-EXTERNAL-type edition gate). Self-review found nothing further; the file's data (a pointer record) reads back NULL after
a file round trip, which is a separate matter and not changed here.
