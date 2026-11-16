**2026-11-15, the golden-2002-7 integrator (gl2002 wave u, slug g2002u-11-8-5-1-convention): COBOL2385 is a sibling of this note's sweep.**

**Sites:** `src/WiseOwl.Cobol.Compiler/Binding/OptionsBinder.cs:118` (the COBOL2385 message);
`src/WiseOwl.Cobol.Editions/Diagnostics/DiagnosticCatalog.cs#EntryConventionViolation` (the descriptor text and its
`ISO §2023:11.9.7.3 SR1; §2023:11.9.7.4 GR3` citation); `docs/conformance/7/DOC-A.1-64.md` (states only 2023).

**Repro, re-run on the golden-2002-7 build (main 5e49d46678).** `E:/COBOL-coord/scratch/gl2002/integrate-7/repro/ecn/ecn.cob`
(from `lane1/run/g2002u-11-8-5-1-convention/ecname/`), command `cobol.exe ecn.cob --std 2002`:

```cobol
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ECN.
       OPTIONS.
           ENTRY-CONVENTION IS STDCALL.
       PROCEDURE DIVISION.
           DISPLAY "UNREACHED".
           STOP RUN.
```

**Observed.** `ecn.cob(4,12): error COBOL2385: ENTRY-CONVENTION IS STDCALL: this implementation defines no
entry-convention-name — the meaning of one is implementor-defined (ISO §2023:11.9.7.4 GR3) and the only convention
provided is COBOL (Annex A.1 item 64)`, rc 65.

**Expected (from the 2002 text).** The refusal stands (no names are provided), but at `--std 2002` it cites the 2002
rule, `§2002:11.8.5.3 GR2` ("The meaning of the entry convention specified by entry-convention-name-1 is
implementor-defined", cite.py OK), and does not offer COBOL: the 2002 format is `ENTRY-CONVENTION IS
entry-convention-name-1` (`§2002:11.8.5.1`, cite.py OK), and `ENTRY-CONVENTION IS COBOL` is refused at 2002.

Not a wrong answer and not an inventory blocker: 2002-FMT-11.8.5.1 and 2002-GR-11.8.5.3-1 stay open for a
documented-non-support decision at 2002 (§2002:B.1 item 44), not for this message.
