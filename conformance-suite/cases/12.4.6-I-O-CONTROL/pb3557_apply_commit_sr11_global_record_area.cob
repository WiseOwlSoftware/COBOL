*>--- conformance-case
*> id: pb3557_apply_commit_sr11_global_record_area
*> rules: [SR-12.4.6.3.3-11]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3557 - ISO 12.4.6.3.3 SR11: "Where a file or record area is declared with the GLOBAL clause, it may only be
*> specified in the APPLY COMMIT clause of the same source element."
*> The shape: a contained program's APPLY COMMIT clause names RREC, the record area of RELF, a file its CONTAINING program
*> declares with the GLOBAL clause (the record-names of a GLOBAL FD are global names, ISO 13.18.27.4 GR1). SR11's second
*> noun is the record area, so the clause is refused under SR11 (COBOL4709) - not as an undefined data-name, for the name
*> IS declared and visible to the contained program. Fails if the clause is accepted or refused otherwise.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3557RA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "pb3557ra.dat"
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
           CALL "PB3557RAI"
           DISPLAY "UNREACHABLE"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3557RAI.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       I-O-CONTROL.
           APPLY COMMIT ON RREC.
       PROCEDURE DIVISION.
           GOBACK.
       END PROGRAM PB3557RAI.
       END PROGRAM PB3557RA.
