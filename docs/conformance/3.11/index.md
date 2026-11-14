---
heading: "### 3.11 The RERUN clause at COBOL-85: one of its seven forms is provided (kb/Work PB5198)"
---
The clause (§1985:VII.2.12) is an obsolete element, deleted from 2002 (COBOL0902, VCR Table 7 row 7.15). It is not in the hardware dependent list (§1985:p.XVII-94), so
§1985:I.1.5.2.2 asks the function of it ("An implementation must accept the syntax and provide the function for all Standard COBOL language elements"), and §1985:VII.2.12.4
GR2 d lets the implementor choose the forms: "The implementor must provide at least one of the specified forms of the RERUN clause." The earlier "null rerun facility" posture
wrote no rerun information for any form; it did not meet GR2. WiseOwl COBOL provides the integer-1 RECORDS form (GR2 d) and accepts the other six.

| Item | Determination |
|---|---|
| The form provided: `RERUN ON implementor-name-1 EVERY integer-1 RECORDS OF file-name-2` (§1985:VII.2.12.4 GR1 b, GR2 d) | The rerun information is written on the device of implementor-name-1 whenever integer-1 records of file-name-2 have been processed, for a file of any organization or access (GR2 d: "File-name-2 may be either an input or output file with any organization or access") |
| The device (GR1 b: "a device specified by the implementor") | The host file the implementor-name spells as an ASSIGN target spells one (`CobolFile.ResolveHostPath`: `SYSRR` is `sysrr.txt` in the working directory). Each rerun point appends to it; the file is never truncated, so a program that wants it empty creates it first |
| The information (GR1: "the implementor specifies where, on the reel or file, the rerun information is to be recorded") | One line per rerun point: the word RERUN, file-name-2 as the SELECT clause spells it, and the count of records processed since the file's OPEN, ended by a line feed on every host. The standard specifies no restart (§1985:C.1 item 5: "the syntax and semantics for restart are not specified"), so nothing reads the line back |
| "Processed" | A READ, WRITE, REWRITE or DELETE statement that completed successfully (an I-O status of class 0). A statement that failed, an end of file and a START are not records processed. The count belongs to the OPEN: a CLOSE and the next OPEN begin it again from zero |
| A device the host refuses | The statement that reached the rerun point answers the file's I-O status '30' (§2023:9.1.13.6 item 1: "A permanent error exists and no further information is available concerning the input-output operation"), as a record the medium refuses does, an insufficient authority included (item 6 b's '37' is stated of OPEN and DELETE FILE only). The record the statement wrote stays written |
| integer-1 of zero | No rerun point: the count after a record is never zero. An integer-1 beyond 2^63-1 is read as 2^63-1, a count no run unit reaches |
| The six forms not provided: END OF REEL or END OF UNIT (GR2 a, b, c), integer-2 CLOCK-UNITS (e), condition-name-1 (f, g) | Accepted, their syntax rules enforced (COBOL6496, COBOL6529-COBOL6531), and no rerun information written. A disk file has no reel or unit to end, WiseOwl COBOL claims no reel device (§3.4), and the standard leaves the interval of CLOCK-UNITS and the interrogation of a switch to the implementor (GR2 e, f) |

Basis, CLAUDE.md rule 1: the 1985 text decides, and it leaves the choice of forms to the implementor. The form chosen is the one that applies to every organization, so it
needs no device the host lacks. The test is `conformance-suite/wiseowl/cases/VII.2.12@1985-RERUN-CLAUSE/pb5198_rerun_records_device_85`.
