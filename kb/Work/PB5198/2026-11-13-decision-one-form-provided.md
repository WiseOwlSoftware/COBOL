## 2026-11-13: decision, the RERUN clause provides its integer-1 RECORDS form (kind: decision)

The note's fix direction offered two answers: implement one form, or record a determination that the 1985 null facility is a documented non-support. The second is
not open. §1985:VII.2.12.4 GR2 says "The implementor must provide at least one of the specified forms of the RERUN clause." and the clause is not in the
hardware dependent list (§1985:p.XVII-94), so §1985:I.1.5.2.2 asks the function. A determination that declines every form would record a non-conformance.

**Decision.** Provide form d, `RERUN ON implementor-name-1 EVERY integer-1 RECORDS OF file-name-2` (GR2 d). Reasons: it is the one form that applies to every file
organization and access ("File-name-2 may be either an input or output file with any organization or access"), it needs no reel device WiseOwl COBOL does not claim
(docs/CONFORMANCE.md section 3.4), and it counts what every connector already reports, the completed READ, WRITE, REWRITE and DELETE. Forms a, b and c (END OF REEL or
UNIT), e (CLOCK-UNITS), f and g (condition-name-1) stay accepted with their syntax rules asked, and write nothing; the standard leaves their forms to the implementor.
The determination, with the device, the information, the count's unit and the failure status, is docs/CONFORMANCE.md section 3.11.

**What the re-probe showed (this tree, `--std 85`, the note's program in an empty directory).** Before: exit 0, `RERUN DONE`, and the directory held `r3data.txt`
(20 bytes) and the compiler's outputs, no rerun file. After: the same plus `sysrr.txt` holding `RERUN RERUND::OUTF 1` and `RERUN RERUND::OUTF 2` (the first draft used
the registry key; the landed one writes the SELECT name, `OUTF`).

**Not decided here, and not done.** A diagnostic that says an unprovided form writes nothing. The standard asks none, the sibling MULTIPLE FILE TAPE clause gives none, and
a warning on a legal 85 program is a new diagnostic code with its own population; if the owner wants one, it is a note of its own.
