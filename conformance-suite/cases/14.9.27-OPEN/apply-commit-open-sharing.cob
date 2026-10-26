*>--- conformance-case
*> id: apply-commit-open-sharing
*> rules: [SR-14.9.27.3-7]
*> source-format: free
*> portable: unverified
*>---
*> THE WITNESS FOR SR-14.9.27.3-7.
*> THE RULE (ISO 14.9.27.3 SR7): "The sharing phrase shall not be specified for a file subject to an APPLY
*> COMMIT clause."
*> SINCE kb/Work PB3338 THE ANTECEDENT IS REACHABLE: the APPLY COMMIT clause (Annex A.4.3 item 2) is accepted for
*> a RELATIVE or INDEXED file, so this program, written in the rule's OWN forbidden shape over a RELATIVE file, is
*> refused by the rule itself (COBOL2974, DataBinder.ScreenApplyCommitSubject) in the strict lane and under
*> --permissive alike (ApplyCommitSubjectRuleTests). Until PB3338 the row was recorded CONFORMS as vacuously
*> satisfied (kb/Work PB371, owner 2026-09-02) and this fixture pinned the clause's decline. The file here is
*> RELATIVE; the rule is the same for a SEQUENTIAL file, which APPLY COMMIT admits too (kb/Work PB3340).
*> THE PHRASE ORDER IS THE PRINTED ONE: 14.9.27.2 Format 1 is `OPEN { {mode} [ sharing-phrase ] [ retry-phrase ]
*> { file-name-1 [ WITH NO REWIND ] } ... } ...` - the sharing phrase precedes the FILE LIST. The file control
*> entry writes no LOCK MODE clause, so the only refusal this program can draw is SR7's (12.4.5.9.3 SR1 forbids
*> LOCK MODE for such a file and has its own fixture, apply-commit-lock-mode).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. APCOPENS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "apcopens.dat"
               ORGANIZATION IS RELATIVE.
       I-O-CONTROL.
           APPLY COMMIT ON F.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(10).
       PROCEDURE DIVISION.
           OPEN INPUT SHARING WITH ALL OTHER F.
           CLOSE F.
           STOP RUN.
