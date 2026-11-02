*>--- conformance-case
*> id: pb4219_apply_commit_nearer_global_file_beats_farther_index_name
*> rules: [SR-12.4.6.3.3-11, GR-8.4.6.2.1-3]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4219 - ISO 8.4.6.2.1 3) b): when the name is not declared in source element B, the referenced item is the
*> one of the NEAREST containing element that declares it. PB4219A declares the GLOBAL table T1 whose INDEXED BY phrase
*> names RELF; PB4219B, contained in it, declares the file RELF IS GLOBAL; PB4219C, contained in PB4219B, writes
*> APPLY COMMIT ON RELF. PB4219B's file is the nearer declaration, so the clause names a file of a containing element
*> declared with the GLOBAL clause, which ISO 12.4.6.3.3 SR11 refuses: "Where a file or record area is declared with the
*> GLOBAL clause, it may only be specified in the APPLY COMMIT clause of the same source element" (COBOL4709). Fails if
*> the clause is accepted, or refused as an index-name (the farther declaration).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4219A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T1 IS GLOBAL.
          05 E PIC X OCCURS 3 INDEXED BY RELF.
       PROCEDURE DIVISION.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4219B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "pb4219b.dat".
       DATA DIVISION.
       FILE SECTION.
       FD RELF IS GLOBAL.
       01 RR PIC X(4).
       PROCEDURE DIVISION.
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4219C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       I-O-CONTROL.
           APPLY COMMIT ON RELF.
       PROCEDURE DIVISION.
           GOBACK.
       END PROGRAM PB4219C.
       END PROGRAM PB4219B.
       END PROGRAM PB4219A.
