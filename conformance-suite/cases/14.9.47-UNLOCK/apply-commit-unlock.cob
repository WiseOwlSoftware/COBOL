*>--- conformance-case
*> id: apply-commit-unlock
*> rules: [SR-14.9.47.3-2]
*> source-format: free
*> portable: unverified
*>---
*> THE WITNESS FOR SR-14.9.47.3-2.
*> THE RULE (ISO 14.9.47.3 SR2): "File-name-1 shall not be a file specified in an APPLY COMMIT clause."
*> SINCE kb/Work PB3338 THE ANTECEDENT IS REACHABLE: the APPLY COMMIT clause (Annex A.4.3 item 2) is accepted for
*> a RELATIVE or INDEXED file, so this program, written in the rule's OWN forbidden shape over a RELATIVE file, is
*> refused by the rule itself (COBOLNET2974, DataBinder.ScreenApplyCommitSubject) in the strict lane and under
*> --permissive alike (ApplyCommitSubjectRuleTests). Until PB3338 the row was recorded CONFORMS as vacuously
*> satisfied (kb/Work PB371, owner 2026-09-02) and this fixture pinned the clause's decline; a SEQUENTIAL file
*> under APPLY COMMIT is still refused by name (COBOLNET1709, kb/Work PB3340), which is why the file here is
*> RELATIVE: a sequential file would draw that refusal and witness nothing about this rule.
*> THE FORMAT IS `UNLOCK file-name-1 [ RECORD | RECORDS ]` - square brackets, NO `ALL` (`UNLOCK F ALL RECORDS`
*> is a GnuCOBOL extension, not ISO).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. APCUNLCK.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "apcunlck.dat"
               ORGANIZATION IS RELATIVE.
       I-O-CONTROL.
           APPLY COMMIT ON F.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(10).
       PROCEDURE DIVISION.
           OPEN INPUT F.
           UNLOCK F RECORDS.
           CLOSE F.
           STOP RUN.
