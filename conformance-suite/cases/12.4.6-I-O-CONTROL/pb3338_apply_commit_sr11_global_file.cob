*>--- conformance-case
*> id: pb3338_apply_commit_sr11_global_file
*> rules: [SR-12.4.6.3.3-11]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3338 - ISO 12.4.6.3.3 SR11: "Where a file or record area is declared with the GLOBAL clause, it may only be
*> specified in the APPLY COMMIT clause of the same source element."
*> The shape: a contained program's APPLY COMMIT clause names RELF, a file its CONTAINING program declares with the
*> GLOBAL clause (FD RELF IS GLOBAL). The name is visible in the contained program, and SR11 still keeps it out of that
*> program's clause (as SR1 does: RELF is not in the contained program's FILE-CONTROL paragraph). The name IS visible in
*> the contained program (ISO 13.18.27.4 GR1: the file-name of a GLOBAL FD is a global name), so it is refused under its
*> own rule, COBOL4709, never as an undefined data-name (kb/Work PB3557: COBOL1639 said "no declaration in this source
*> element gives the name", false for a visible GLOBAL file). Fails if the clause is accepted or refused otherwise.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3338SB.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "pb3338sb.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK.
       DATA DIVISION.
       FILE SECTION.
       FD RELF IS GLOBAL.
       01 RREC PIC X(10).
       WORKING-STORAGE SECTION.
       01 RK PIC 9(4) GLOBAL.
       PROCEDURE DIVISION.
           CALL "PB3338SBI"
           DISPLAY "UNREACHABLE"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3338SBI.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       I-O-CONTROL.
           APPLY COMMIT ON RELF.
       PROCEDURE DIVISION.
           GOBACK.
       END PROGRAM PB3338SBI.
       END PROGRAM PB3338SB.
