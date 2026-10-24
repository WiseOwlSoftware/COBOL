*>--- conformance-case
*> id: apply-commit-lock-mode
*> rules: [SR-12.4.5.9.3-1]
*> source-format: free
*> portable: unverified
*>---
*> THE WITNESS FOR SR-12.4.5.9.3-1.
*> THE RULE (ISO 12.4.5.9.3 SR1): "This clause shall not be specified for a file that is the subject of an
*> APPLY COMMIT clause for which there is an implicit LOCK MODE IS AUTOMATIC WITH LOCK ON MULTIPLE RECORDS
*> applied automatically, including for sequential files."
*> SINCE kb/Work PB3338 THE ANTECEDENT IS REACHABLE: the APPLY COMMIT clause (Annex A.4.3 item 2) is accepted for
*> a RELATIVE or INDEXED file, so this program, written in the rule's OWN forbidden shape over a RELATIVE file, is
*> refused by the rule itself (COBOLNET2974, DataBinder.ScreenApplyCommitSubject) in the strict lane and under
*> --permissive alike (ApplyCommitSubjectRuleTests). Until PB3338 the row was recorded CONFORMS as vacuously
*> satisfied (kb/Work PB371, owner 2026-09-02) and this fixture pinned the clause's decline; a SEQUENTIAL file
*> under APPLY COMMIT is still refused by name (COBOLNET1709, kb/Work PB3340), which is why the file here is
*> RELATIVE: a sequential file would draw that refusal and witness nothing about this rule.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. APCLOCKM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "apclockm.dat"
               ORGANIZATION IS RELATIVE
               LOCK MODE IS AUTOMATIC.
       I-O-CONTROL.
           APPLY COMMIT ON F.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(10).
       PROCEDURE DIVISION.
           DISPLAY "UNREACHABLE".
           STOP RUN.
