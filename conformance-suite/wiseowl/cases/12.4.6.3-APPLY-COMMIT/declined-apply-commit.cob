*>--- conformance-case
*> id: declined-apply-commit
*> rules: [GR-12.4.6.3.4-1]
*> source-format: free
*> portable: unverified
*>---
*> ISO 12.4.6.3 APPLY COMMIT clause over a SEQUENTIAL file - the one operand Annex A.4.3's PARTIAL claim declines.
*> ISO 9.1.18.2: "Apart from sort-merge files and report writer files, any type of file on a mass storage device may be
*> specified." Commit and rollback is provided for RELATIVE and INDEXED files (kb/Work PB3338; the keyed store engine
*> keeps the transaction); the sequential organization's transaction journal is kb/Work PB3340, so the operand is
*> refused by name, COBOLNET1709 (Annex A.4.1; docs/CONFORMANCE.md section 5, A.4.3 Partial, and section 4 item 2).
*> The id keeps its historical name so the evidence that names it still resolves.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DCLAPPLY.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "dclapply.dat"
               ORGANIZATION IS SEQUENTIAL.
       I-O-CONTROL.
           APPLY COMMIT ON F.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(10).
       PROCEDURE DIVISION.
           DISPLAY "UNREACHABLE".
           STOP RUN.
