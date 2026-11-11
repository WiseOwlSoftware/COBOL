**Addendum (2026-11-10): 1 more rule(s) from the 1985 code read.** The 1985 code read (2026-11-10) found the Data Division format row with this note's mechanism: §1985:IV.6.3.2.2.1 prints a file description entry followed by one or more record description entries, and an FD with a RECORD clause and no record compiles at --std 85 through MaterializeImpliedRecord (the same FD without the RECORD clause draws COBOL1836, which asks for a RECORD clause, the repair 1985 does not allow).

**Found by.** The 1985 code read (2026-11-10; its records are `E:/COBOL-coord/scratch/coderead-85/for-register.json`): each rule below was read against the code sites it names and probed at `--std 85`, and found non-conforming. Verdict, harm, evidence, repro and observed text are the records'; the rows' verdicts are recorded on another branch.

**Inventory.** kb/Work PB5094 still refuses a 1985 rule id in `inventory_rows`, so these rows are listed here, beside the note's own; this note claims them.

**Rules added (1; every citation checked with `scripts/spec/cite.py --check`).**

- `1985-FMT-IV.6.3.2.2.1` - `§1985:IV.6.3.2.2.1` (record verdict: PARTIAL, harm: under-rejects). Checked: §1985:IV.6.3.2.2.1: "WORKING-STORAGE". Code sites read: src/WiseOwl.Cobol.Frontend/Grammar/Core/CobolData.g4#dataDivision; src/WiseOwl.Cobol.Frontend/Grammar/Core/CobolData.g4#fileSection; src/WiseOwl.Cobol.Frontend/Grammar/Core/CobolData.g4#fileDescriptionEntry; src/WiseOwl.Cobol.Frontend/Grammar/Core/CobolData.g4#sortMergeDescriptionEntry; src/WiseOwl.Cobol.Frontend/Grammar/Core/CobolData.g4#workingStorageSection; src/WiseOwl.Cobol.Frontend/Grammar/Core/CobolData.g4#linkageSection; src/WiseOwl.Cobol.Frontend/Grammar/Core/CobolCommunication.g4#communicationSection; src/WiseOwl.Cobol.Frontend/Grammar/Core/CobolCommunication.g4#communicationDescriptionEntry; src/WiseOwl.Cobol.Frontend/Grammar/Core/CobolReportWriter.g4#reportSection; src/WiseOwl.Cobol.Frontend/Grammar/Core/CobolReportWriter.g4#reportDescriptionEntry; src/WiseOwl.Cobol.Compiler/Binding/FileControlBinder.FileSection.cs#BindFileSection; src/WiseOwl.Cobol.Compiler/Binding/FileControlBinder.FileSection.cs#MaterializeImpliedRecord; src/WiseOwl.Cobol.Compiler/Binding/ReportSectionBinder.cs#ScreenReportDescriptionHasGroup; src/WiseOwl.Cobol.Compiler/Validation/LevelNumberPass.cs#VisitLevelNumber; src/WiseOwl.Cobol.Compiler/Validation/VersionConformancePass.cs#VisitLocalStorageSection; src/WiseOwl.Cobol.Compiler/Validation/VersionConformancePass.cs#VisitConstantEntryBody; src/WiseOwl.Cobol.Compiler/Validation/VersionConformancePass.cs#VisitTypedefClause; src/WiseOwl.Cobol.Compiler/Validation/VersionConformancePass.cs#VisitCommunicationSection; src/WiseOwl.Cobol.Compiler/Binding/ScreenFacility.cs#ReportSection. Code read: §1985:IV.6.3.2.2 says the format "defines the order of their presentation in the source program", and §1985:IV.6.3.2.2.1 prints DATA DIVISION., then five optional sections in the order FILE, WORKING-STORAGE, LINKAGE, COMMUNICATION, REPORT: "file-description-entry {record-description-entry} ...", the sort-merge entry likewise, a report-file-description-entry with no record, 77-level and record entries in any order in WORKING-STORAGE and LINKAGE, "communication-description-entry [record-description-entry] ..." and "report-description-entry {report-group-description-entry} ...". The grammar rule dataDivision fixes the order and one occurrence of each section (the union rule also carries LOCAL-STORAGE and SCREEN); at 85 VersionConformancePass.VisitLocalStorageSection, VisitConstantEntryBody and VisitTypedefClause refuse the 2002 additions (COBOL0900), ScreenFacility.ReportSection declines the SCREEN SECTION (COBOL1560), and VisitCommunicationSection admits the COMMUNICATION SECTION. BindFileSection requires a record after an SD (COBOL1837) and refuses one after a report FD (COBOL2578), ScreenReportDescriptionHasGroup requires a report group after an RD (COBOL2708), and LevelNumberPass.VisitLevelNumber refuses level 77 under an FD or CD (COBOL1746). Probed right at --std 85: all five sections in order with every entry kind (h5.cob), five empty sections, 77 and 01 mixed in both orders; refused: every section out of order or written twice, a section with no DATA DIVISION header, LOCAL-STORAGE, SCREEN, 01 CONSTANT, TYPEDEF, an SD or RD with nothing after it, a record after a report FD, a 77 after an FD or a CD, a record with no CD or RD before it. PARTIAL because MaterializeImpliedRecord has no edition arm: an FD with a RECORD clause and no record description entry (the §2023:13.4.5.3 SR3 form) compiles and runs at 85, where the braces require one. The witness is a WORKING-STORAGE SECTION of 01, 77, 01.

**Repro (`1985-FMT-IV.6.3.2.2.1`).** `cobol.exe fdnorec.cob --std 85 --source-format fixed --run`

```cobol
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FDNOREC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "FDNOREC.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  F1 RECORD CONTAINS 80 CHARACTERS.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "RAN".
           STOP RUN.
```

**Expected (from the 1985 text, the record's).** --std 85 refuses the FD at line 9: no record description entry follows it. §1985:IV.6.3.2.2.1 prints "file-description-entry {record-description-entry} ...", and §1985:IV.2.1.4 says "When braces, { } , enclose a portion of a general format, one of the options contained within the braces must be either explicitly specified or implicitly selected." §1985:VII.3.2.3 SR3 restates it: "One or more record description entries must follow the file description entry."

**Observed (the record's).** exit 0, no diagnostic; the program runs and displays RAN. Control at --std 85: the same FD without the RECORD clause (g1.cob) draws COBOL1836, which asks for a RECORD clause (§2023:13.4.5.3 SR3 a), the repair 1985 does not allow.
