## Entry 3713 — 2026-11-15 18:14 PST — golden-2002-7: 97 own-edition 2002 records from 16 wave-u slugs of the 2002 witness lane (GAP 1899 to 1805)

The seventh landing of the 2002 golden lane (gl2002, kb/Work PB5597). Its 16 input files are "wave u": 2002 rows that
the 2002 read left needs-own-witness, undecided or uninterpretable. The files cover §2002:11.6 to §2002:13.16.27 and
§2002:A.1.2.

**Shape.** The writers' reports, with a fixer's `REPORT.json` replacing the report where one exists, hold 106 rows:
- 84 new-golden and 13 existing-golden;
- 8 not-closable and 1 defect-suspected.

The dispatch counted 10 existing-golden and 11 not-closable. The reports differ from that count.

**Landed.** 110 cases:
- 57 accepted at [2002]. Of these, 55 are new. The other two are existing 2002 cases whose rules lists widen:
  g2-aligned-cd-record-bit-items and g2-bwz-cd-record.
- 53 refusals with `refuse-at: ["2002"]`.

97 records are CONFORMS, written by `record_verdicts.py --edition 2002`: `GAP : 1899 → 1805 (+94 closed of 3298)`.
- Three of the rows were already CONFORMS: 2002-GR-12.3.4.6.3-5, 2002-SR-14.8.37.2-6 and 2002-GR-14.8.37.3-13. They
  keep their stored witnesses and notes beside the new ones.
- Six records rest on a refusal alone. Each is for an SR or FMT rule whose text grants no form.

No validate stage ran, so the integrator validated every draft on its own build at `--std 2002`:
- run.py passed all 57 accepted cases (PASS 57, FAIL 0).
- All 53 refusals refused with their expected codes.

Mechanical repairs, with no expectation changed:
- 30 PROGRAM-ID renames.
- The draft g2-global-cd-constant reused the id and path of a case that golden-2002-4 landed. It lands as
  g2-global-cd-constant-nested.
- CRLF in 4 drafts was converted to LF.

The docs/conformance register rows DOC-A.1-73, -129, -160 and -183 now name the 2002 goldens that pin them.

**Refuter overturns.** The refuters overturned 15 verdicts, and the fixers applied all 15:
- does-not-exercise-rule: 7
- expected-value: 3
- citation: 2
- convention: 1
- not-spec-derived: 1
- evidence-kind: 1

Two of the expected-value overturns removed `RECORD DELIMITER IS STANDARD-1` from g2fc01, because SR1 restricts the
clause to variable-length records.

**Defects.** Each was re-run on this build at `--std 2002`. No new note was filed.
- kb/Work PB5615 (extended; 2002-SR-12.3.4.2-7 joins its `inventory_rows`): `ASSIGN USING` of a PIC A item is refused
  with COBOL1810. §2002:8.5.2.1 admits an alphabetic operand wherever an elementary alphanumeric one is permitted. The
  row's drafts g2asg02 to g2asg05 are held back.
- kb/Work PB5670 (extended): at 2002, COBOL2385 cites §2023:11.9.7.4 GR3 and offers the COBOL convention, but 2002
  refuses `ENTRY-CONVENTION IS COBOL`.
- Not filed: the refuter's lead that RECORD DELIMITER STANDARD-1 on a fixed-length file is accepted with COBOL1778.
  §2002:3.1.5 does not require syntax errors inside unsupported processor-dependent syntax to be diagnosed, and
  STANDARD-1 is §2002:B.3 item 10.
- Not filed: the sequential READ record-lock race (2002-GR-12.3.4.8.3-8). It is a code read with no runnable repro.

**Not closable (8 rows):**
- 2002-FMT-11.8.5.1 and 2002-GR-11.8.5.3-1: they need a documented-non-support decision for entry-convention names at
  2002 (§2002:B.1 item 44).
- 2002-GR-12.2.6.3-17: it needs a determination that a table built on the ISO/IEC 14651:2020 template satisfies 14651:2001.
- 2002-GR-12.3.4.8.3-8: atomicity of the record lock across run units.
- 2002-GR-12.3.4.10.3-4 and -5: vacuous under GR2's not-applicable arm. They close on the docs/conformance 3.6
  determination, if at all.
- 2002-GR-13.16.10.3-2 and -4: they need a decision on whether BLOCK CONTAINS may be accepted with no effect under
  §2002:9.1.1.

The writers deferred nothing.

