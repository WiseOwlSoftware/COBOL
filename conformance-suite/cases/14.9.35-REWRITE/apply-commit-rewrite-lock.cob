*>--- conformance-case
*> id: apply-commit-rewrite-lock
*> rules: [SR-14.9.35.3-5]
*> source-format: free
*> portable: unverified
*>---
*> THE WITNESS FOR SR-14.9.35.3-5.
*> THE RULE (ISO 14.9.35.3 SR5): "If the rewrite file is subject to an APPLY COMMIT clause, neither the WITH
*> LOCK phrase nor the WITH NO LOCK phrase shall be specified."
*> SINCE kb/Work PB3338 THE ANTECEDENT IS REACHABLE: the APPLY COMMIT clause (Annex A.4.3 item 2) is accepted for
*> a RELATIVE or INDEXED file, so this program, written in the rule's OWN forbidden shape over a RELATIVE file, is
*> refused by the rule itself (COBOLNET2974, DataBinder.ScreenApplyCommitSubject) in the strict lane and under
*> --permissive alike (ApplyCommitSubjectRuleTests). Until PB3338 the row was recorded CONFORMS as vacuously
*> satisfied (kb/Work PB371, owner 2026-09-02) and this fixture pinned the clause's decline. The file here is
*> RELATIVE; the rule is the same for a SEQUENTIAL file, which APPLY COMMIT admits too (kb/Work PB3340).
*> REWRITE's FILE phrase is a different decline (Annex A.4.13 item 2); this fixture writes `REWRITE record-name`,
*> the mandatory alternative, so the only refusal it can draw is SR5's.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. APCREWRL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "apcrewrl.dat"
               ORGANIZATION IS RELATIVE.
       I-O-CONTROL.
           APPLY COMMIT ON F.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(10).
       PROCEDURE DIVISION.
           OPEN I-O F.
           READ F
               AT END CONTINUE
           END-READ.
           REWRITE F-REC WITH LOCK.
           CLOSE F.
           STOP RUN.
