**2026-11-15: at 85 the RELEASE half of this note waits on a reading of §1985:X.1.3.7; the RETURN half has nothing to fix (registrar re-read of the open PARTIAL 1985 rows).**

Re-run on the build of branch `records-1985-reread` (main 2722570af0), `--std 85`
(`E:/COBOL-coord/scratch/op-20261115/registrar-1985-reread/log-misc.txt`):
`E:/COBOL-coord/scratch/op-20261115/open-rows-1985/work-b/r9.cob` (`SD SORT-A` with `01 REC-A IS GLOBAL`; the input
procedure CALLs the contained program R9N, which RELEASEs REC-A) is refused: `error COBOL1757: RELEASE 'REC-A' — 'REC-A' is
a logical record of 'SORT-A', which a containing program describes without the GLOBAL clause; the record's own GLOBAL
clause (ISO §2023:13.18.27.3 SR1 b)) makes the record-name visible here, not the file. ISO §2023:13.18.27.3 SR3 — "If the
GLOBAL clause is not specified in ...`. The text cites only 2023 rules at 85 (an instance of PB4950 as well).

**Whether r9 is legal at 85 depends on how §1985:X.1.3.7 is read.**
- Reading A (the re-read's adjudicator): no 1985 rule forbids it. REC-A may be GLOBAL (§1985:X.4.6.3 SR1 names 01 entries
  "whose level-number is 01 in the File Section or the Working-Storage Section", `cite.py --check` OK, with no SD
  exception), RELEASE's SR1 and SR2 hold, and §1985:XI.4.4.4 GR7 puts statements "executed as the result of a transfer of
  control by CALL, EXIT, GO TO, and PERFORM statements in the range of the input procedure" in the range (OK). Then
  COBOL1757 rejects legal source.
- Reading B (the refuter's): §1985:X.1.3.7 lists when "Two programs in a run unit may reference common file connectors
  in the following circumstances" (OK), and for a contained program that is "by referring to an associated global
  file-name" (OK). An SD's file-name cannot be global, and a record area is reached through the file connector
  (§1985:II.6.2.2.1.2: file connectors are "used as the linkage between a file-name and a physical file and between a
  file-name and its associated record area", OK). So R9N's RELEASE reaches a connector it may not share, and 2023's
  SR3 is the explicit form of a rule 1985 states only in its introduction.
The words that separate them: whether `RELEASE record-name-1` "references" the sort file's connector in X.1.3.7's sense.
The 1985 text does not say. ⛔ This note must not be fixed for 85 until that is adjudicated; the 2023 and 2002 arms are
not affected by this reading.

**The RETURN half has nothing to fix at 85.** RETURN names file-name-1 (§1985:XI.4.3.2), and the SD format
(§1985:XI.3.2.2, "IS VARYING IN SIZE [ [ FROM integer-2 ] [ TO integer-3 ] CHARACTERS ]" and the DATA RECORDS clause, OK)
prints no GLOBAL clause, so no contained program can name the outer SORT's file. Both ways of trying are refused on this
build: `work-b/RT4.cob` (the contained program names SORT-A) `error COBOL1639: 'SORT-A' is not defined as a file`, and
`work-b/RT5.cob` (`SD SORT-A IS GLOBAL`) `error COBOL1970: unexpected 'IS'`.

The 1985 row `1985-SR-XI.4.2.3-2` stays PARTIAL; its other, decided defect (a declarative RELEASE exempted although no
statement of the range can execute the declarative) is PB5768.
