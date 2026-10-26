*>--- conformance-case
*> id: pb3338_apply_commit_sr5_recursive_program
*> rules: [SR-12.4.6.3.3-5]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3338 - ISO 12.4.6.3.3 SR5: "This clause shall not be specified in a recursive source element."
*> The shape: the clause in a program with the RECURSIVE attribute. The clause is accepted for a RELATIVE file, so the only refusal this program can
*> draw is SR5's own, by name (COBOL4709). Fails if the clause is accepted or refused for another reason.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3338S5 RECURSIVE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "pb3338s5.dat"
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
           DISPLAY "UNREACHABLE".
           STOP RUN.
