*>--- conformance-case
*> id: pb3338_apply_commit_below_2023
*> rules: [GR-12.4.6.3.4-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3338 - the APPLY COMMIT clause is a COBOL-2023 addition (Annex E.3.2 item 2): the I-O-CONTROL
*> paragraph of ISO/IEC 1989:2002 and 2014 has no APPLY clause, so below 2023 the clause is refused (the closed
*> I-O-CONTROL format, COBOLNET1970). Its 2023 twin, which runs, is pb3338_apply_commit_rollback.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3338BE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "pb3338be.dat"
               ORGANIZATION IS RELATIVE
               RELATIVE KEY IS RK.
       I-O-CONTROL.
           APPLY COMMIT ON RELF.
       DATA DIVISION.
       FILE SECTION.
       FD RELF.
       01 RREC PIC X(10).
       WORKING-STORAGE SECTION.
       01 RK PIC 9(4).
       PROCEDURE DIVISION.
           STOP RUN.
