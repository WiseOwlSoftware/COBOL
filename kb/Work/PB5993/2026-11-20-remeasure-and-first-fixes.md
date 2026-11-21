---
title: "PB5993 re-measure 13 of the 20 measured codes were never offenders; the 1985-rule arms are fixed; the class beyond the suite's 85 refusals is 336 codes"
kind: analysis
parent: PB5993
filed: 2026-11-20
---
# PB5993 - re-measure and first fixes (wave 20262118, group A)

**Re-measure.** The note's 20 measured codes were counted without the net's own two ways out. On this tree: COBOL0880, 0804, 0877,
0876, 0831, 0803 are edition gates whose message says "(targeting COBOL-85)" (`EditionCitationNet.EditionGate`); COBOL1560, 2495,
4733, 0865, 5434, 5962, 1505, 2496, 1565, 0898, 1507, 0827 carry `No1985Rule` in their descriptor. The suite's 2482 refusals at 85
all pass the net (`WiseOwlRefusal_RefusesTheSourceWithItsDiagnostic`). Real offenders of the named set: COBOL2691, 1641 and the
hand-found 2403, 0854, 2422, 2198, 1858, 2894. COBOL1927 cites §1985:VI.5.14.3 SR2 already; its "BINARY for COMP" is the
documented COMP = BINARY identification (BINARY is a 1985 word, §1985:VI.5.14.3 SR3), so it is not an edition defect.

**Fixed.** COBOL2403 (§1985:VI.5.3.4 GR1), 0854 (§1985:VI.5.8.3 SR1b), 2422 report arm (§1985:XIII.3.17.3 SR1/SR2, "display" alone),
2198 (§1985:XIII.3.21.3 SR3, "DISPLAY" alone, usage word not the enum name), 1858 (`FileModel.OrganizationFace(EditionInfo)`:
§1985:VII.2.6.3 GR3), 1641 (§1985:XII.3.2), 2894 (ANSI X3.23-1985, §1985:I.1.5.2.5.2), 2691 (`No1985Rule`: 1985 has no `>>`
directive). Six goldens `pb5993-*-85`.

**Still open, wider than the note.** Compiling the 3253 suite cases accepted only from 2002 plus the 2613 refusals not refused at 85
at `--std 85` (`E:/COBOL-coord/scratch/fleet-1985-prose$c/w20262118a/cite85.py`) leaves 336 non-exempt codes (1471 diagnostics)
citing only a later edition: OO, exception checking, directives, CONSTANT entries, intrinsic functions, TYPEDEF and the like, plus
some with a 1985 counterpart (COBOL0890 cites §2002 at 85; COBOL1547's 18-digit literal limit is 1985's). Each needs `Cite` or
`No1985Rule`, and the net needs a theory over those programs at 85. Also open: the register rows' `Standard` text
(`NonstandardExtension.cs`) cites §2023 for the standard form at every edition.

**Sites:** `src/WiseOwl.Cobol.Compiler/Binding/DataBinder.ClausePlacement.cs` (`ClausePlacementRules.Rules`),
`src/WiseOwl.Cobol.Compiler/Binding/Passes/OdoResolvePass.cs`, `src/WiseOwl.Cobol.Compiler/Binding/DataBinder.SignClause.cs`
(`DataBinder.SignClauseElementaryDefect`), `src/WiseOwl.Cobol.Compiler/Binding/ReportSectionBinder.cs` (`ScreenReportUsage`),
`src/WiseOwl.Cobol.Compiler/Binding/Model/FileModel.cs` (`FileModel.OrganizationFace`),
`src/WiseOwl.Cobol.Compiler/Binding/EditionContext.cs` (`EditionContext.Extension`),
`src/WiseOwl.Cobol.Frontend/Preprocessor/CopyProcessor.cs`, `src/WiseOwl.Cobol.Editions/Diagnostics/DiagnosticCatalog.cs`
(`DirectiveAfterProgramText`).
