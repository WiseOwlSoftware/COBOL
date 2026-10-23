*>--- conformance-case
*> id: pb3338_apply_commit_sr1_file_of_another_element
*> rules: [SR-12.4.6.3.3-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3338 - ISO 12.4.6.3.3 SR1: "File-name-1 shall be specified in the FILE-CONTROL paragraph of the source
*> element that contains this clause."
*> The shape: a contained program's APPLY COMMIT clause names RELF, a file of its CONTAINING program's FILE-CONTROL
*> paragraph (not GLOBAL, so not visible to the contained program at all). An APPLY COMMIT operand is looked up only in
*> the element that writes the clause, so the only refusal this program can draw is that the operand resolves to nothing
*> there (COBOLNET1639, "no declaration in this source element"). Fails if the clause is accepted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3338S1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "pb3338s1.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK.
       DATA DIVISION.
       FILE SECTION.
       FD RELF.
       01 RREC PIC X(10).
       WORKING-STORAGE SECTION.
       01 RK PIC 9(4) GLOBAL.
       PROCEDURE DIVISION.
           CALL "PB3338S1I"
           DISPLAY "UNREACHABLE"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3338S1I.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       I-O-CONTROL.
           APPLY COMMIT ON RELF.
       PROCEDURE DIVISION.
           GOBACK.
       END PROGRAM PB3338S1I.
       END PROGRAM PB3338S1.
