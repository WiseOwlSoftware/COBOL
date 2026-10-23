*>--- conformance-case
*> id: pb3338_apply_commit_no_operand
*> rules: [SR-12.4.6.3.3-4]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3338 (and PB1096) - ISO 12.4.6.3.3 SR4: "At least one file or data-item shall be specified." The
*> general format repeats an all-optional pair, so `APPLY COMMIT ON.` is a spelling of the format and parses; SR4
*> is the syntax rule that refuses it, by name (COBOLNET4709), where the grammar used to fail with an anonymous
*> parse error. Fails if the clause is accepted or refused by a generic syntax error.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3338NO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "pb3338no.dat"
               ORGANIZATION IS RELATIVE
               RELATIVE KEY IS RK.
       I-O-CONTROL.
           APPLY COMMIT ON.
       DATA DIVISION.
       FILE SECTION.
       FD RELF.
       01 RREC PIC X(10).
       WORKING-STORAGE SECTION.
       01 RK PIC 9(4).
       PROCEDURE DIVISION.
           STOP RUN.
