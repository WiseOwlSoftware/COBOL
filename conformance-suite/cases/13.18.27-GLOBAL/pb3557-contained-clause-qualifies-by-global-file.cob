      *>--- conformance-case
      *> id: pb3557-contained-clause-qualifies-by-global-file
      *> rules: [GR-13.18.27.4-1, GR-13.18.27.4-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3557, review finding R2-1's sibling. A container's GLOBAL file-name qualifies its record's
      *> data-names in a contained program's own DATA DIVISION clauses, as it does in the PROCEDURE DIVISION.
      *>
      *> THE RULE. ISO/IEC 1989:2023 section 13.18.27.4 GR2: "A statement in a program contained directly or
      *> indirectly within a program that describes a global name may reference that name without describing it
      *> again." The FD RELF of the container carries the GLOBAL clause, so RELF, its record RREC and RREC's
      *> subordinate CNT are global names (GR1), and 8.4.2.2.2 Format 1 lets a data-name carry its
      *> file-report-qualifier: "data-name-1 [ data-qualifier ] ... [ file-report-qualifier ]". The OCCURS ...
      *> DEPENDING ON clause of the contained program's own table T names CNT IN RREC IN RELF. The compiler
      *> resolved that clause before the container's files joined the contained program's file table, so the
      *> file-name qualifier matched nothing and the legal program was refused: COBOL0851 "data-name-1 is not
      *> defined under the given qualifiers".
      *>
      *> DERIVATION OF THE EXPECTED OUTPUT. The container opens RELF for output, so its record area is available
      *> (9.1.2: "For files open in the extend or output mode, the logical record is available until execution of
      *> a successful write or rewrite"). The contained program moves 3 to CNT, so 13.18.38.4 GR7 ("The value of
      *> the data item referenced by data-name-1 represents the current number of occurrences of the subject of
      *> the entry") gives T three occurrences of E, and DISPLAY of the group T shows its three current
      *> characters: AAA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3557Q.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "pb3557q.dat".
       DATA DIVISION.
       FILE SECTION.
       FD RELF IS GLOBAL.
       01 RREC.
          05 CNT PIC 9.
          05 FILLER PIC X(9).
       PROCEDURE DIVISION.
           OPEN OUTPUT RELF
           CALL "PB3557QI"
           CLOSE RELF
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3557QI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E PIC X OCCURS 1 TO 5 DEPENDING ON CNT IN RREC IN RELF.
       PROCEDURE DIVISION.
           MOVE 3 TO CNT IN RREC IN RELF
           MOVE ALL "A" TO T
           DISPLAY "T=" T
           GOBACK.
       END PROGRAM PB3557QI.
       END PROGRAM PB3557Q.
