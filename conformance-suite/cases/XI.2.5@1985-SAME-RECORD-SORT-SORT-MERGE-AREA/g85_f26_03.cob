      *>--- conformance-case
      *> id: g85_f26_03
      *> rules: [1985-GR-XI.2.5.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.2.5.4 GR2 - "If the SAME SORT AREA or SAME SORT-MERGE
      *>   AREA clause is used, at least one of the file-names must
      *>   represent a sort or merge file." Items a-d: sort storage is
      *>   reused among the named sort files (a), non-sort files' areas
      *>   may serve the sorts (b, extent implementor-defined), "Files
      *>   other than sort or merge files do not share the same storage
      *>   area with each other" (c), and the non-sort files are not
      *>   open during a SORT on a named sort file (d).
      *>   cite.py: OK  §1985:XI.2.5.4 GR2
      *> Both spellings are used, each naming sort files and the
      *> non-sort files F3 and F4; F3/F4 are closed during every SORT
      *> (item d). Derivation, by line:
      *>  1 C F3-ONE F4-ONE - F3 and F4 are both open OUTPUT and both
      *>    named in one SAME SORT AREA clause; by item c their record
      *>    areas are distinct, so each keeps its own MOVE (a shared
      *>    area would print F4-ONE twice).
      *>  2-4 S1 A, S1 B, S1 C - S1 ascending over C,A,B
      *>    (§1985:XI.4.3.4 GR2, cite.py: OK: records returned "as
      *>    determined by the keys listed in the SORT").
      *>  5-7 S2 3, S2 2, S2 1 - S2 descending over 1,3,2; S2 reuses
      *>    S1's sort storage (item a), invisibly.
      *>  8-10 S3 X, S3 Y, S3 Z - S3 named in a SAME SORT-MERGE AREA
      *>    clause sorts likewise.
      *>  11 R F3-ONE F4-ONE - item b may lend F3/F4 storage to the
      *>    sorts, never their files: both read back their own record,
      *>    both open at once (item c).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2603.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT S1 ASSIGN TO "G85F2603.S1".
           SELECT S2 ASSIGN TO "G85F2603.S2".
           SELECT S3 ASSIGN TO "G85F2603.S3".
           SELECT F3 ASSIGN TO "G85F2603.F3"
               ORGANIZATION IS SEQUENTIAL.
           SELECT F4 ASSIGN TO "G85F2603.F4"
               ORGANIZATION IS SEQUENTIAL.
       I-O-CONTROL.
           SAME SORT AREA FOR S1 S2 F3 F4
           SAME SORT-MERGE AREA FOR S3 F3.
       DATA DIVISION.
       FILE SECTION.
       SD  S1.
       01  S1-REC PIC X.
       SD  S2.
       01  S2-REC.
           05  S2-K PIC 9.
       SD  S3.
       01  S3-REC PIC X.
       FD  F3.
       01  F3-REC PIC X(6).
       FD  F4.
       01  F4-REC PIC X(6).
       WORKING-STORAGE SECTION.
       01  WS-EOF PIC 9.
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       M-1.
           OPEN OUTPUT F3 F4.
           MOVE "F3-ONE" TO F3-REC.
           MOVE "F4-ONE" TO F4-REC.
           DISPLAY "C " F3-REC " " F4-REC.
           WRITE F3-REC.
           WRITE F4-REC.
           CLOSE F3 F4.
           SORT S1 ON ASCENDING KEY S1-REC
               INPUT PROCEDURE IS IN-1
               OUTPUT PROCEDURE IS OUT-1.
           SORT S2 ON DESCENDING KEY S2-K
               INPUT PROCEDURE IS IN-2
               OUTPUT PROCEDURE IS OUT-2.
           SORT S3 ON ASCENDING KEY S3-REC
               INPUT PROCEDURE IS IN-3
               OUTPUT PROCEDURE IS OUT-3.
           OPEN INPUT F3 F4.
           READ F3 AT END MOVE SPACES TO F3-REC.
           READ F4 AT END MOVE SPACES TO F4-REC.
           DISPLAY "R " F3-REC " " F4-REC.
           CLOSE F3 F4.
           STOP RUN.
       IN-1 SECTION.
       I1-1.
           MOVE "C" TO S1-REC.
           RELEASE S1-REC.
           MOVE "A" TO S1-REC.
           RELEASE S1-REC.
           MOVE "B" TO S1-REC.
           RELEASE S1-REC.
       OUT-1 SECTION.
       O1-1.
           MOVE 0 TO WS-EOF.
           PERFORM UNTIL WS-EOF = 1
               RETURN S1
                   AT END MOVE 1 TO WS-EOF
                   NOT AT END DISPLAY "S1 " S1-REC
               END-RETURN
           END-PERFORM.
       IN-2 SECTION.
       I2-1.
           MOVE 1 TO S2-K.
           RELEASE S2-REC.
           MOVE 3 TO S2-K.
           RELEASE S2-REC.
           MOVE 2 TO S2-K.
           RELEASE S2-REC.
       OUT-2 SECTION.
       O2-1.
           MOVE 0 TO WS-EOF.
           PERFORM UNTIL WS-EOF = 1
               RETURN S2
                   AT END MOVE 1 TO WS-EOF
                   NOT AT END DISPLAY "S2 " S2-K
               END-RETURN
           END-PERFORM.
       IN-3 SECTION.
       I3-1.
           MOVE "Z" TO S3-REC.
           RELEASE S3-REC.
           MOVE "X" TO S3-REC.
           RELEASE S3-REC.
           MOVE "Y" TO S3-REC.
           RELEASE S3-REC.
       OUT-3 SECTION.
       O3-1.
           MOVE 0 TO WS-EOF.
           PERFORM UNTIL WS-EOF = 1
               RETURN S3
                   AT END MOVE 1 TO WS-EOF
                   NOT AT END DISPLAY "S3 " S3-REC
               END-RETURN
           END-PERFORM.
