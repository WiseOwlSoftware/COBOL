*>--- conformance-case
*> id: pb3557_apply_commit_sr11_qualified_global_record_area
*> rules: [SR-12.4.6.3.3-11]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3557 (review R1-1) - ISO 12.4.6.3.3 SR11: "Where a file or record area is declared with the GLOBAL clause,
*> it may only be specified in the APPLY COMMIT clause of the same source element."
*> The shape: a contained program's APPLY COMMIT clause names RREC IN RELF, the record area of RELF written with its
*> file-report-qualifier (ISO 8.4.2.2.2 Format 1: "data-name-1 [ data-qualifier ] ... [ file-report-qualifier ]"),
*> where RELF is a file its CONTAINING program declares with the GLOBAL clause. The qualified spelling names the same
*> GLOBAL record area as the bare word, so the clause is refused under SR11 (COBOL4709) - not as an undefined data-name.
*> Fails if the clause is accepted or refused otherwise.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3557QA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "pb3557qa.dat"
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
           CALL "PB3557QAI"
           DISPLAY "UNREACHABLE"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3557QAI.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       I-O-CONTROL.
           APPLY COMMIT ON RREC IN RELF.
       PROCEDURE DIVISION.
           GOBACK.
       END PROGRAM PB3557QAI.
       END PROGRAM PB3557QA.
