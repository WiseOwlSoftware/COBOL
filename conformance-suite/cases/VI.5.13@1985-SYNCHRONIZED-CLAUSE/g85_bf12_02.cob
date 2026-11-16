      *>--- conformance-case
      *> id: g85_bf12_02
      *> rules: [1985-GR-VI.5.13.4-10]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.13.4 GR10 - "Any rules for synchronization of the
      *> records of a data file, as this affects the synchronization of
      *> elementary items, will be specified by the implementor."
      *> The rule's observable is the implementor's stated rule, and
      *> this case holds the implementation to it. WiseOwl COBOL's rule
      *> (docs/conformance/7/DOC-A.1-196, with DOC-A.1-195 and
      *> DOC-A.1-205): a record is never aligned or padded as a whole
      *> and a SYNCHRONIZED elementary item in it sits at the offset it
      *> would have without the clause, so each record written is byte
      *> for byte the record's own image: X + S9(4) COMP SYNC + X is
      *> 1 + 2 + 1 = 4 character positions, and two records make a file
      *> of exactly 8 positions.
      *> FS, FP and FX name the SAME file. Two records are written
      *> through FS, whose middle item carries SYNC.
      *>  XREAD: record 1 read back through FP, the same layout WITHOUT
      *>   SYNC; the three fields decode to the values written only if
      *>   no byte was inserted or moved for the SYNC description.
      *>  RAW: record 1 read through FX, one PIC X(4) record; "a" is its
      *>   first and "z" its fourth character position.
      *>  RAW2: record 2 read through FX. Where record 2 begins is what
      *>   GR10 observes: slack after record 1 (a record aligned or
      *>   padded as a whole) moves "b" off position 1 and "y" off
      *>   position 4 of the second X(4) record.
      *>  END: a third READ of FX takes AT END; slack after the last
      *>   record would be read as a further record.
      *> §1985:VI.5.13.4 GR5 keeps the item's own size for every use, so
      *> MOVE -1234 and the comparison are the same with or without
      *> SYNC.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1202.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FS ASSIGN TO "g85bf1202.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT FP ASSIGN TO "g85bf1202.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT FX ASSIGN TO "g85bf1202.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  FS.
       01  RS.
           05  RS-A  PIC X.
           05  RS-N  PIC S9(4) COMP SYNC.
           05  RS-Z  PIC X.
       FD  FP.
       01  RP.
           05  RP-A  PIC X.
           05  RP-N  PIC S9(4) COMP.
           05  RP-Z  PIC X.
       FD  FX.
       01  RX  PIC X(4).
       WORKING-STORAGE SECTION.
       01  FLG  PIC X(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT FS.
           MOVE "a" TO RS-A.
           MOVE -1234 TO RS-N.
           MOVE "z" TO RS-Z.
           WRITE RS.
           MOVE "b" TO RS-A.
           MOVE 1 TO RS-N.
           MOVE "y" TO RS-Z.
           WRITE RS.
           CLOSE FS.
           MOVE "NO" TO FLG.
           OPEN INPUT FP.
           READ FP
               AT END MOVE "EOF" TO FLG
               NOT AT END
                   IF RP-A = "a" AND RP-N = -1234 AND RP-Z = "z"
                       MOVE "YES" TO FLG
                   END-IF
           END-READ.
           CLOSE FP.
           DISPLAY "XREAD=" FLG.
           MOVE "NO" TO FLG.
           OPEN INPUT FX.
           READ FX
               AT END MOVE "EOF" TO FLG
               NOT AT END
                   IF RX(1:1) = "a" AND RX(4:1) = "z"
                       MOVE "YES" TO FLG
                   END-IF
           END-READ.
           DISPLAY "RAW=" FLG.
           MOVE "NO" TO FLG.
           READ FX
               AT END MOVE "EOF" TO FLG
               NOT AT END
                   IF RX(1:1) = "b" AND RX(4:1) = "y"
                       MOVE "YES" TO FLG
                   END-IF
           END-READ.
           DISPLAY "RAW2=" FLG.
           MOVE "NO" TO FLG.
           READ FX
               AT END MOVE "YES" TO FLG
           END-READ.
           DISPLAY "END=" FLG.
           CLOSE FX.
           STOP RUN.
