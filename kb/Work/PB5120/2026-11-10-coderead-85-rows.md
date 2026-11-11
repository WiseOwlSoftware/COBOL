**Addendum (2026-11-10): 1 more rule(s) from the 1985 code read.** The 1985 code read (2026-11-10) found the inspection-cycle rule with this note's mechanism: §1985:VI.6.18.4 GR2 begins inspection at the leftmost position, and CobolInspect.Region establishes the BEFORE boundary of an operand that also has an AFTER phrase by searching from the AFTER-derived start, so the Y between the delimiters is tallied and replaced (T-AB 01, R-AB BXA*BZ); the same with the phrases written BEFORE then AFTER.

**Found by.** The 1985 code read (2026-11-10; its records are `E:/COBOL-coord/scratch/coderead-85/for-register.json`): each rule below was read against the code sites it names and probed at `--std 85`, and found non-conforming. Verdict, harm, evidence, repro and observed text are the records'; the rows' verdicts are recorded on another branch.

**Inventory.** kb/Work PB5094 still refuses a 1985 rule id in `inventory_rows`, so these rows are listed here, beside the note's own; this note claims them.

**Rules added (1; every citation checked with `scripts/spec/cite.py --check`).**

- `1985-GR-VI.6.18.4-2` - `§1985:VI.6.18.4 GR2` (record verdict: PARTIAL, harm: wrong-answer). Checked: §1985:VI.6.18.4 GR2: "which includes the comparison cycle, the establishment of boundaries for the BEFORE or AFTER phrase, and the mechanism for tallying and/or replacing". Code sites read: src/WiseOwl.Cobol.Runtime/Verbs/CobolInspect.cs#RunCycle; src/WiseOwl.Cobol.Runtime/Verbs/CobolInspect.cs#Region; src/WiseOwl.Cobol.Runtime/Verbs/CobolInspect.cs#Tally; src/WiseOwl.Cobol.Runtime/Verbs/CobolInspect.cs#Replace; src/WiseOwl.Cobol.Runtime/Verbs/CobolInspect.cs#Convert; src/WiseOwl.Cobol.Compiler/CodeGen/Verbs/InspectEmitter.cs#Emit; src/WiseOwl.Cobol.Compiler/Validation/VersionConformancePass.cs#VisitInspectStatement. Code read: §1985:VI.6.18.4 GR2 (old GR1, renumbered by Amendment 2): inspection, 'which includes the comparison cycle, the establishment of boundaries for the BEFORE or AFTER phrase, and the mechanism for tallying and/or replacing', 'begins at the leftmost character position of the data item referenced by identifier-1, regardless of its class, and proceeds from left to right to the rightmost character position as described in general rules 6 through 8'. InspectEmitter.Emit reads identifier-1 once as its character image whatever its class (a signed numeric item de-signed and re-signed on the store) and CobolInspect.RunCycle starts at position 0 and advances rightward, past each match or by one position, until the last character; at 85 the direction is fixed because VersionConformancePass.VisitInspectStatement refuses BACKWARD below 2023 (COBOL0845, probed), so Tally, Replace and Convert always pass backward = false. Probed at --std 85 beyond the witness: signed, sign-separate, numeric-edited, alphanumeric-edited and group identifier-1, and the standard's examples 1 to 4 (§1985:VI.6.18.5), all as the rule gives. PARTIAL in one arm: CobolInspect.Region establishes the BEFORE boundary of an operand that also has an AFTER phrase by searching from the AFTER-derived start (`text.IndexOf(before, start)`), not from the leftmost character position of identifier-1, the 2023 wording (§2023:14.9.22.4 GR9 'the first character position that is eligible to participate up to, but not including, the first occurrence encountered of literal-2'); §1985:VI.6.18.4 GR8 b measures BEFORE 'from its leftmost character position up to, but not including, the' 'first occurrence of literal-2 within the content of the data item referenced by identifier-1'. An operand with only BEFORE or only AFTER, and every statement without them, conforms. kb/Work PB5120 (open) owns this defect at the same site and names GR8 only; this row waits on it.

**Repro (`1985-GR-VI.6.18.4-2`).**

```cobol
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  D6  PIC X(6).
       01  C1  PIC 99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "BXAYBZ" TO D6.
           MOVE ZERO TO C1.
           INSPECT D6 TALLYING C1 FOR CHARACTERS
               AFTER INITIAL "A" BEFORE INITIAL "B".
           DISPLAY "T-AB " C1.
           INSPECT D6 REPLACING CHARACTERS BY "*"
               AFTER INITIAL "A" BEFORE INITIAL "B".
           DISPLAY "R-AB " D6.
           STOP RUN.
(cobol n1.cob --std 85 --source-format fixed --run; the probe at E:/COBOL-coord/scratch/coderead-85/out/work-slice-18/n1.cob also runs the phrases in the other order and a control)
```

**Expected (from the 1985 text, the record's).** T-AB 00 and R-AB BXAYBZ. §1985:VI.6.18.4 GR2: the 'establishment of boundaries for the BEFORE or AFTER phrase' 'begins at the leftmost character position of the data item referenced by identifier-1'; §1985:VI.6.18.4 GR8 b: the BEFORE operand participates only in the portion 'from its leftmost character position up to, but not including, the' 'first occurrence of literal-2 within the content of the data item referenced by identifier-1'. The first occurrence of "B" in BXAYBZ is the leftmost character, so the BEFORE portion is empty, no comparison cycle is eligible, nothing is tallied and nothing is replaced.

**Observed (the record's).** Exit 0, no diagnostic; displays 'T-AB 01' and 'R-AB BXA*BZ' at --std 85: the BEFORE delimiter is looked for only to the right of the AFTER boundary (the second B), so the Y between them is tallied and replaced. The same with the phrases written BEFORE then AFTER (T-BA 01).
