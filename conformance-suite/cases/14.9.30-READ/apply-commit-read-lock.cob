*>--- conformance-case
*> id: apply-commit-read-lock
*> rules: [SR-14.9.30.3-5]
*> source-format: free
*> portable: unverified
*>---
*> THE WITNESS FOR SR-14.9.30.3-5.
*> THE RULE (ISO 14.9.30.3 SR5, inside the format-1 sublist): "If file-name-1 is subject to an APPLY COMMIT
*> clause, none of the phrases IGNORING LOCK, WITH LOCK, or WITH NO LOCK shall be specified."
*> SINCE kb/Work PB3338 THE ANTECEDENT IS REACHABLE: the APPLY COMMIT clause (Annex A.4.3 item 2) is accepted for
*> a RELATIVE or INDEXED file, so this program, written in the rule's OWN forbidden shape over a RELATIVE file, is
*> refused by the rule itself (COBOL2974, DataBinder.ScreenApplyCommitSubject) in the strict lane and under
*> --permissive alike (ApplyCommitSubjectRuleTests). Until PB3338 the row was recorded CONFORMS as vacuously
*> satisfied (kb/Work PB371, owner 2026-09-02) and this fixture pinned the clause's decline. The file here is
*> RELATIVE; the rule is the same for a SEQUENTIAL file, which APPLY COMMIT admits too (kb/Work PB3340).
*> NOT to be confused with the neighbouring SR-14.9.30.3-4, whose antecedent is "If automatic locking has
*> been specified for file-name-1" - a LOCK MODE condition with live content of its own.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. APCREADL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "apcreadl.dat"
               ORGANIZATION IS RELATIVE.
       I-O-CONTROL.
           APPLY COMMIT ON F.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(10).
       PROCEDURE DIVISION.
           OPEN INPUT F.
           READ F WITH LOCK
               AT END CONTINUE
           END-READ.
           CLOSE F.
           STOP RUN.
