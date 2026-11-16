      *>--- conformance-case
      *> id: g2mrgt4-merge-ties-format
      *> rules: [2002-GR-14.8.23.3-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.23.3 GR4 -- equal-key records are returned
      *> "a) Follows the order of the associated input files as
      *> specified in the MERGE statement. b) Is such that all records
      *> associated with one input file are returned prior to the
      *> return of records from another input file."
      *> One of the three USING files (F2) carries the FORMAT clause
      *> (§2002:13.16.22, variable-length records per §2002:13.3.4.2 SR5);
      *> §2002:14.8.23.2 SR9 admits it as a USING file, so the rule's
      *> "associated input files" include it.
      *> Data (key K ascending, every file already in order, so GR6 is
      *> not triggered):
      *>   F1: A F1-1 / A F1-2 / B F1-3
      *>   F2: A F2-1 / A F2-2 / B F2-3   (FORMAT CHARACTER DATA)
      *>   F3: A F3-1 / C F3-2
      *> MERGE 1 USING F2 F1 F3: the A ties in MERGE order F2, F1, F3,
      *>   each file's ties together (b): F2-1 F2-2 F1-1 F1-2 F3-1;
      *>   B ties F2-3 F1-3; then C F3-2.
      *> MERGE 2 USING F3 F1 F2: A ties F3-1 F1-1 F1-2 F2-1 F2-2;
      *>   B ties F1-3 F2-3; then C F3-2.
      *> A tie order taken from declaration order, from record arrival
      *> or from interleaving the files, or a FORMAT file read out of
      *> its place, prints a different sequence in one of the merges.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2MRGT4.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "g2mrgt4a.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT F2 ASSIGN TO "g2mrgt4b.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT F3 ASSIGN TO "g2mrgt4c.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT SM ASSIGN TO "g2mrgt4s.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD  F1 RECORD VARYING IN SIZE FROM 1 TO 6 CHARACTERS.
       01  F1-REC        PIC X(5).
       FD  F2 FORMAT CHARACTER DATA
              RECORD VARYING IN SIZE FROM 1 TO 6 CHARACTERS.
       01  F2-REC        PIC X(5).
       FD  F3 RECORD VARYING IN SIZE FROM 1 TO 6 CHARACTERS.
       01  F3-REC        PIC X(5).
       SD  SM RECORD VARYING IN SIZE FROM 1 TO 6 CHARACTERS.
       01  SM-REC.
           05  S-K       PIC X.
           05  S-TAG     PIC X(4).
       WORKING-STORAGE SECTION.
       01  EOF-SW        PIC X.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT F1 F2 F3.
           MOVE "AF1-1" TO F1-REC WRITE F1-REC.
           MOVE "AF1-2" TO F1-REC WRITE F1-REC.
           MOVE "BF1-3" TO F1-REC WRITE F1-REC.
           MOVE "AF2-1" TO F2-REC WRITE F2-REC.
           MOVE "AF2-2" TO F2-REC WRITE F2-REC.
           MOVE "BF2-3" TO F2-REC WRITE F2-REC.
           MOVE "AF3-1" TO F3-REC WRITE F3-REC.
           MOVE "CF3-2" TO F3-REC WRITE F3-REC.
           CLOSE F1 F2 F3.
           DISPLAY "M1 F2 F1 F3".
           MERGE SM ON ASCENDING KEY S-K
               USING F2 F1 F3
               OUTPUT PROCEDURE IS OUT-P.
           DISPLAY "M2 F3 F1 F2".
           MERGE SM ON ASCENDING KEY S-K
               USING F3 F1 F2
               OUTPUT PROCEDURE IS OUT-P.
           STOP RUN.
       OUT-P.
           MOVE "N" TO EOF-SW.
           PERFORM UNTIL EOF-SW = "Y"
               RETURN SM
                   AT END MOVE "Y" TO EOF-SW
                   NOT AT END DISPLAY S-K " " S-TAG
               END-RETURN
           END-PERFORM.
