**2026-11-15: four more diagnostics that cite 2023 rules at `--std 85`, one of them citing ANOTHER statement's rule (registrar re-read of the open PARTIAL 1985 rows).**

Re-run on the build of branch `records-1985-reread` (main 2722570af0), `--std 85`
(`E:/COBOL-coord/scratch/op-20261115/registrar-1985-reread/log-fmt.txt`, `log-misc.txt`; sources under
`E:/COBOL-coord/scratch/op-20261115/open-rows-1985/`). The registrar searched kb/Work for a note of this pattern before
filing (the brief's PB5769): this decision note is it, so these are instances here and PB5769 was not used.

1. **COBOL0900, the 85 phrase-order refusal, cites RETURN's rule for DIVIDE, MULTIPLY, SUBTRACT and WRITE.**
   `work-b/d2.cob` (DIVIDE with NOT ON SIZE ERROR first): `error COBOL0900: a conditional phrase written after a phrase
   its general format prints after it requires COBOL-2002 (targeting COBOL-85) — the DIVIDE statement writes ON SIZE
   ERROR after NOT ON SIZE ERROR, and its COBOL-85 general format prints ON SIZE ERROR first (§1985:IV.2.1; §2002:F.2;
   §2023:5.2.6.4; §2023:14.9.34.3 SR4)`; `m2.cob`, `s2.cob` and `w2.cob` (END-OF-PAGE) print the same citation list.
   §2023:14.9.34.3 SR4 is "The AT END phrase and the NOT AT END phrase, when specified, may be written in reversed order"
   (`cite.py --check` OK), the RETURN statement's rule, and governs none of these statements. The refusal is right; the
   citation misleads. It is not only an edition question: the list is the construct's, not the statement's.
   **Sites:** `tests/version-matrix/constructs/conditional-phrases-any-order-2002.json` (the construct's citation, which
   `src/WiseOwl.Cobol.Editions/ConstructRegistry.g.cs` generates), `src/WiseOwl.Cobol.Compiler/Validation/VersionConformancePass.cs`
   (`VersionConformancePass.VisitConditionalPhrases`: where the message could name the statement's own 1985 format,
   §1985:VI.6.11.2, VI.6.20.2, VI.6.26.2, VII.4.7.2).
2. **COBOL1639** cites `§2023:8.4.2.2.3 SR1` (or `§2023:8.4.2.2`, `§2023:8.4.2.1`, `§2023:8.4.6.1`) at 85 for an ambiguous
   or undefined reference where §1985:IV.4.3.8.1 SR3 governs: `work-a/r2-reckey.cob`, `r2-odo.cob`, `r2-unq.cob`,
   `r2-xprog.cob`; `refute-a/s3-clauses.cob`, `s3-cond-bad.cob`, `s3-qual-bad.cob`, `s3-sib.cob`, `s3-down.cob`. The
   lead the PB5566 landing file records ("COBOL1639 cites §2023:8.4.2.2.3 SR1 at --std 85 where §1985:IV.4.3.8.1 SR3
   governs") is already filed as **PB5627** (open), whose sites (`DataBinder.UniqueOrReportAmbiguous`) own it.
3. **COBOL2121** (a non-unique paragraph-name, `refute-a/s3-para-bad.cob`) cites `§2023:8.4.2.1` at 85, where
   §1985:IV.4.3.8.1 SR3 governs. Site: `src/WiseOwl.Cobol.Compiler/Binding/Validation/StatementValidation.cs`
   (`StatementValidation.RejectAmbiguousProcedureName`).
4. **COBOL5997** (an unqualified LINAGE-COUNTER with two LINAGE files, `refute-a/s3-lc-bad.cob`) cites
   `§2023:8.4.3.14 SR3 / §2023:8.4.2.2` at 85, where §1985:IV.4.3.8.1 SR3 ("Reserved words naming the special registers
   require qualification to provide uniqueness of reference", the refuter's `cite.py --check` OK) governs. Site:
   `src/WiseOwl.Cobol.Compiler/Binding/CounterResolver.cs` (`CounterResolver.LinageFileOf`).

Each refusal is right at 85; each citation points a 1985 programmer at 2023 text.
