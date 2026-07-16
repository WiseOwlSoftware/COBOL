# COBOL.NET diagnostics

> **Generated** from `src/Cobol.Net.Editions/Diagnostics/DiagnosticCatalog.cs` — do not edit by hand.
> Regenerate: `pwsh scripts/gen-diagnostics-doc.ps1` (or run `DiagnosticRegistryDriftTests` with
> `COBOLNET_WRITE_DIAGNOSTICS_DOC=1`). `DiagnosticRegistryDriftTests` fails CI if this file drifts.

This catalogue is the first-class registry that replaced bare `COBOLNETnnnn` string literals
(rearch PHASE 02, P2.10). A **Code** may repeat across rows — the emitted number is byte-stable, the
stable **Id** is the identity. The `recognized-not-implemented` suppress key groups every
legal-but-deferred feature so it can be muted as a group. Scope: the edition band + the `COBOLNET0899`
split + the reused `COBOLNET1533`; the broader every-code→descriptor migration is the P7 follow-on.

| Code | Id | Severity | ISO § | Suppress key | Title |
| --- | --- | --- | --- | --- | --- |
| COBOLNET0801 | digit-capacity-over-31 | Error | ISO §8.3.1.2 | COBOLNET0801 | A fixed-point item/literal exceeds the 31-digit ISO limit. |
| COBOLNET0802 | digit-capacity-over-18-pre-2002 | Error | ISO §8.3.1.2 | COBOLNET0802 | A fixed-point item/literal exceeds the 18-digit COBOL-85 limit (19–31 need --std 2002+). |
| COBOLNET0899 | any-length-returning | Error | ISO §13.18.2.3 SR3b / §13.18.2.4 GR1 | recognized-not-implemented | ANY LENGTH on a RETURNING item (legal per §13.18.2.3 SR3b) is recognized but not yet implemented — the return crossing cannot carry the activator's receiver length yet (the ANY LENGTH formal-parameter leg is fully implemented). |
| COBOLNET0899 | constant-byte-length | Error | ISO §13.10.4 GR5 / §15.14 | recognized-not-implemented | CONSTANT … AS BYTE-LENGTH OF (§13.10.4 GR5 — defined by the §15.14 BYTE-LENGTH intrinsic) is recognized but not yet implemented: the §15.14 intrinsic itself is a Deferred catalog row, and the byte-width authority lands ONCE, with it (the singular-pattern rule). |
| COBOLNET0899 | constant-from-compilation-variable | Error | ISO §13.10 (FROM phrase) | recognized-not-implemented | CONSTANT … FROM compilation-variable-name (§13.10.4 GR1 — the >>DEFINE tie-in) is recognized but not yet implemented: the preprocessor's compilation-variable store (ConditionalCompilationProcessor) is local to the text stage and not reachable at bind time; the position-correct (SR8 'currently true') capture across COPY expansion is the recorded residue. |
| COBOLNET0899 | construct-staged-not-implemented | Error | COMPLETION_ROADMAP_COUNCIL | recognized-not-implemented | A registry-recognized construct is available at this edition but not yet implemented (staged loud). |
| COBOLNET0899 | debug-register-facility | Error | VCR Table 7 row 7.17 | recognized-not-implemented | The X3.23-1985 debug facility (DEBUG-ITEM registers, debugging-section invocation) is not implemented. |
| COBOLNET0899 | external-record-not-cell-backed | Error | ISO §13.18.24 | recognized-not-implemented | An EXTERNAL record cannot be cell-backed (a restriction of the current EXTERNAL model). |
| COBOLNET0899 | national-data | Error | ISO §8.5 / §13.18.60 | recognized-not-implemented | National-category data (PIC N / USAGE NATIONAL, national numeric/boolean, national keys) is recognized but not yet implemented. |
| COBOLNET0899 | national-through-range | Error | ISO §13.18.63 SR31 | recognized-not-implemented | A condition-name THROUGH range over a national conditional variable is not yet implemented. |
| COBOLNET0899 | oo-based-in-class | Error | ISO §13.18.60 | recognized-not-implemented | BASED data / ADDRESS OF in a class definition's data division is not yet implemented. |
| COBOLNET0899 | oo-external-method-working-storage | Error | ISO §14.5 | recognized-not-implemented | EXTERNAL on a method WORKING-STORAGE item is not yet implemented. |
| COBOLNET0899 | oo-factory-object-reference | Error | ISO §13.18.60 | recognized-not-implemented | USAGE OBJECT REFERENCE FACTORY OF is recognized but not yet implemented. |
| COBOLNET0899 | oo-group-valued-property | Error | ISO §8.4.3.9.4 | recognized-not-implemented | A group-valued object-property reference is not yet implemented. |
| COBOLNET0899 | oo-interface-property-prototype | Error | ISO §10.6.2 | recognized-not-implemented | A GET/SET PROPERTY prototype in an interface is not yet implemented. |
| COBOLNET0899 | oo-method-declaratives | Error | ISO §14.2.1 | recognized-not-implemented | DECLARATIVES inside a method are recognized but not yet implemented. |
| COBOLNET0899 | oo-method-raising-last | Error | ISO §14.9.18.3 SR5 | recognized-not-implemented | RAISING LAST EXCEPTION inside a method is not yet implemented. |
| COBOLNET0899 | program-pointer-restricted | Error | ISO §13.18.60 GR25 / SR22 | recognized-not-implemented | USAGE PROGRAM-POINTER TO program-prototype-name (§13.18.60 GR25 — a RESTRICTED program-pointer, confined to NULL or a same-signature program's address) is recognized but not yet implemented: signature matching needs the program-prototype registry (P13); the unrestricted form is live. |
| COBOLNET0899 | report-code-clause | Error | ISO §13.18.12 | recognized-not-implemented | The CODE clause on a report description is not yet implemented. |
| COBOLNET0899 | report-column-without-line | Error | ISO §13.18.14 | COBOLNET0899 | A COLUMN clause has no LINE clause in effect. |
| COBOLNET0899 | report-control-operand-unresolved | Error | ISO §13.18.16.3 SR3 | COBOLNET0899 | A CONTROL operand does not resolve to a data item. |
| COBOLNET0899 | report-control-type-operand | Error | ISO §13.18.57.3 SR10/SR11 | COBOLNET0899 | A TYPE CH/CF operand is not an operand of the CONTROL clause. |
| COBOLNET0899 | report-counter-no-report | Error | ISO §8.4.3.15 | COBOLNET0899 | A LINE/PAGE-COUNTER reference has no report, or is ambiguous across reports. |
| COBOLNET0899 | report-counter-qualifier-not-report | Error | ISO §8.4.3.15 SR2 / §8.4.2.2 | COBOLNET0899 | A LINE/PAGE-COUNTER qualifier shall name a report description entry. |
| COBOLNET0899 | report-generate-needs-control | Error | ISO §14.9.16.3 SR2 | COBOLNET0899 | GENERATE report-name requires a CONTROL clause in the report description. |
| COBOLNET0899 | report-generate-not-detail | Error | ISO §14.9.16.3 SR1 | COBOLNET0899 | GENERATE names a report group that is not a DETAIL group. |
| COBOLNET0899 | report-global-clause | Error | ISO §13.18.27 | recognized-not-implemented | The GLOBAL clause on a report description is not yet implemented. |
| COBOLNET0899 | report-group-before-01 | Error | ISO §13.15 | COBOLNET0899 | A report group entry appears before any 01-level entry. |
| COBOLNET0899 | report-item-missing-picture | Error | ISO §13.16 | COBOLNET0899 | A printable report item has no PICTURE clause. |
| COBOLNET0899 | report-line-counter-receiving | Error | ISO §8.4.3.15.3 SR3 | COBOLNET0899 | LINE-COUNTER shall not be referenced as a receiving operand. |
| COBOLNET0899 | report-line-next-page | Error | ISO §13.18.35 | recognized-not-implemented | LINE … NEXT PAGE is not yet implemented. |
| COBOLNET0899 | report-multiple-on-file | Error | ISO §13.18.46 | recognized-not-implemented | Multiple reports on one file (REPORTS ARE …) are not yet implemented. |
| COBOLNET0899 | report-next-group-clause | Error | ISO §13.18.37 | recognized-not-implemented | The NEXT GROUP clause is not yet implemented. |
| COBOLNET0899 | report-non-display-item | Error | ISO §13.15 | recognized-not-implemented | A non-DISPLAY printable report item is not supported. |
| COBOLNET0899 | report-not-in-file | Error | ISO §13.18.46 / §13.14 | COBOLNET0899 | A report is not named in any file description entry's REPORT clause. |
| COBOLNET0899 | report-occurs-in-group | Error | ISO §13.18.38 | recognized-not-implemented | OCCURS (repeating entries) in a report group description is not yet implemented. |
| COBOLNET0899 | report-page-counter-receiving | Error | ISO §8.4.3.15 | recognized-not-implemented | PAGE-COUNTER as a receiving operand (legal) is not yet implemented. |
| COBOLNET0899 | report-page-type-requires-page | Error | ISO §13.18.57.3 SR12 | COBOLNET0899 | A PAGE HEADING/FOOTING group requires a PAGE clause defining the page limit. |
| COBOLNET0899 | report-reset-not-control-operand | Error | ISO §13.18.54.3 SR8 | COBOLNET0899 | A RESET ON operand is not an operand of the CONTROL clause. |
| COBOLNET0899 | report-source-operand-unresolved | Error | ISO §13.18.53.3 SR4 | COBOLNET0899 | A SOURCE operand does not resolve to a data item. |
| COBOLNET0899 | report-source-other-report-counter | Error | ISO §8.4.3.15 SR2 | recognized-not-implemented | A SOURCE referencing another report's counter is not yet implemented. |
| COBOLNET0899 | report-source-subscripted | Error | ISO §13.18.53 | recognized-not-implemented | A subscripted or reference-modified SOURCE operand is not yet implemented. |
| COBOLNET0899 | report-sum-addend-unresolved | Error | ISO §13.18.54.3 SR5 | COBOLNET0899 | A SUM addend does not resolve to a data item outside the report section. |
| COBOLNET0899 | report-sum-cross-report | Error | ISO §13.18.54.3 SR4g | recognized-not-implemented | SUM … OF report-name (a cross-report sum) is not yet implemented. |
| COBOLNET0899 | report-sum-rolled-total | Error | ISO §13.18.54.4 GR6 | recognized-not-implemented | A SUM addend naming another sum counter (rolled totals) is not yet implemented. |
| COBOLNET0899 | usage-function-pointer | Error | ISO §13.18.60 (FUNCTION-POINTER phrase) | recognized-not-implemented | USAGE FUNCTION-POINTER (§13.18.60 — a function-pointer data item) is recognized but not yet implemented: its target identities are FUNCTION PROTOTYPES (§11.5 Format 2 / the repository function-specifier), which are the P13 repository work — the pointer lands with them. |
| COBOLNET0899 | usage-keyword-unmapped-internal | Error | ISO §13.18.60 | recognized-not-implemented | Internal: a grammar-accepted USAGE keyword has no ParseUsage mapping (a compiler defect). |
| COBOLNET0900 | edition-introduction | Error | ISO §ann. per construct | COBOLNET0900 | A construct is used below the edition that introduced it (requires a newer --std). |
| COBOLNET0901 | edition-reserved-word | Error | ISO §8.9 | COBOLNET0901 | A word reserved in the targeted edition is used as a user-defined word. |
| COBOLNET0902 | edition-removed-construct | Error | ISO ann. E.2 | COBOLNET0902 | A construct removed by the targeted edition is used (error strict / warning permissive). |
| COBOLNET0903 | edition-obsolete-flag | Warning | ISO §4.2.12/§4.2.13, ann. F.2 | COBOLNET0903 | An obsolete/archaic element is used (still conforming; flagged). |
| COBOLNET1533 | strong-class-condition | Error | ISO §8.8.4.4.3 SR1 | COBOLNET1533 | A strongly-typed group item may not appear in a class condition. |
| COBOLNET1533 | strong-compare-mismatch | Error | ISO §8.8.4.2.3 SR1 | COBOLNET1533 | A strongly-typed group may be compared only with a group of the same type. |
| COBOLNET1533 | strong-move-mismatch | Error | ISO §14.9.25.3 SR2 | COBOLNET1533 | MOVE to/from a strongly-typed group requires a group of the same type. |
| COBOLNET1540 | concat-class-mismatch | Error | ISO §8.8.3.2 SR1 | COBOLNET1540 | Both operands of a concatenation expression shall be of the same class — alphanumeric, boolean, or national (a figurative constant takes the other operand's class). |
| COBOLNET1541 | concat-all-figurative | Error | ISO §8.8.3.2 SR1 | COBOLNET1541 | Neither operand of a concatenation expression shall be a figurative constant that begins with the word ALL. |
| COBOLNET1545 | concat-result-too-long | Error | ISO §8.8.3.2 SR2–SR4 | COBOLNET1545 | The value resulting from concatenation shall be at most 8,191 character positions (alphanumeric, boolean, or national). |
| COBOLNET1547 | constant-entry-rule | Error | ISO §13.10.3 / §7.3.6.2 | COBOLNET1547 | A constant entry violates a §13.10 syntax rule (figurative operand SR6; non-literal / exponentiation / division-by-zero in the compile-time expression §7.3.6; duplicate constant-name SR9; ANY-LENGTH / dynamic-length LENGTH operand SR10/SR12; non-integer constant where an integer is required SR2). |
| COBOLNET1548 | constant-as-receiver | Error | ISO §13.10.3 SR2 / §13.18.15.3 SR2 | COBOLNET1548 | A constant-name or a data item of a CONSTANT RECORD shall not be specified as a receiving operand — a constant substitutes a literal, and a structured constant's content cannot be modified. |
| COBOLNET1549 | constant-record-rule | Error | ISO §13.18.15.3 / §13.16.3 | COBOLNET1549 | A CONSTANT RECORD clause violates a structural rule: WS/LS sections only (SR1); level-01 only, no REDEFINES, and no ANY LENGTH / BASED / BLANK WHEN ZERO / SYNCHRONIZED / TYPEDEF on the record or any subordinate (§13.16.3 SR3/SR6/SR13). |
