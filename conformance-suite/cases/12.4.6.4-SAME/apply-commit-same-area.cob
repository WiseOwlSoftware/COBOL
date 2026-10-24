*>--- conformance-case
*> id: apply-commit-same-area
*> rules: [SR-12.4.6.4.3-11]
*> source-format: free
*> portable: unverified
*>---
*> THE WITNESS FOR SR-12.4.6.4.3-11.
*> THE RULE (ISO 12.4.6.4.3 SR11): "A file or record area that is subject to an APPLY COMMIT clause shall not
*> be specified with another file or record area that is not subject to an APPLY COMMIT clause."
*> Hence the shape below: a SAME RECORD AREA over F and G, with only F in the APPLY COMMIT clause.
*> SINCE kb/Work PB3338 THE ANTECEDENT IS REACHABLE: the APPLY COMMIT clause (Annex A.4.3 item 2) is accepted for
*> a RELATIVE or INDEXED file, so this program, written in the rule's OWN forbidden shape over a RELATIVE file, is
*> refused by the rule itself (COBOLNET2974, DataBinder.ScreenApplyCommitSubject) in the strict lane and under
*> --permissive alike (ApplyCommitSubjectRuleTests). Until PB3338 the row was recorded CONFORMS as vacuously
*> satisfied (kb/Work PB371, owner 2026-09-02) and this fixture pinned the clause's decline; a SEQUENTIAL file
*> under APPLY COMMIT is still refused by name (COBOLNET1709, kb/Work PB3340), which is why the file here is
*> RELATIVE: a sequential file would draw that refusal and witness nothing about this rule.
*> GR-12.4.6.4.4-3 is a different shape (BOTH files subject to commit and rollback) and a general rule about
*> the area's use while the clause is active, not this syntax rule.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. APCSAMEA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "apcsame1.dat"
               ORGANIZATION IS RELATIVE.
           SELECT G ASSIGN TO "apcsame2.dat"
               ORGANIZATION IS RELATIVE.
       I-O-CONTROL.
           SAME RECORD AREA FOR F G.
           APPLY COMMIT ON F.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(10).
       FD G.
       01 G-REC PIC X(10).
       PROCEDURE DIVISION.
           DISPLAY "UNREACHABLE".
           STOP RUN.
