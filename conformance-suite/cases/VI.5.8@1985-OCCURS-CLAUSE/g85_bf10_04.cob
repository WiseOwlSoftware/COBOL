      *>--- conformance-case
      *> id: g85_bf10_04
      *> rules: [1985-SR-VI.5.8.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.8.3 SR2 - "Data-name-1 and data-name-2 may be qualified."
      *> DEPENDING ON CNT OF GRP-A (data-name-1) and ASCENDING KEY IS KY OF ENT
      *> (data-name-2) are both qualified; each is ambiguous unqualified (GRP-B
      *> has a CNT, OTHER-G a KY), so a compiler that ignores the qualifier
      *> either refuses the program or binds the wrong item.
      *>   T=[A1B2C3    ] : CNT OF GRP-A is 3, so only that part of the table
      *>                    area is sent (§1985:VI.5.8.4 GR2b, GR3a): TBL is 6
      *>                    long; CNT OF GRP-B (5) would give 10.
      *>   FOUND C=3      : SEARCH ALL on KY OF ENT finds "C" at occurrence 3.
      *>   NOT FOUND      : "D" is occurrence 4, beyond CNT OF GRP-A.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1004.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  GRP-A.
           05  CNT                 PIC 9 VALUE 5.
       01  GRP-B.
           05  CNT                 PIC 9 VALUE 5.
       01  TBL.
           05  ENT OCCURS 1 TO 5 TIMES
                   DEPENDING ON CNT OF GRP-A
                   ASCENDING KEY IS KY OF ENT
                   INDEXED BY IX.
               10  KY              PIC X.
               10  VL              PIC 9.
       01  OTHER-G.
           05  KY                  PIC X VALUE "Z".
       01  W10                     PIC X(10).
       PROCEDURE DIVISION.
       P1.
           MOVE "A1B2C3D4E5" TO TBL.
           MOVE 3 TO CNT OF GRP-A.
           MOVE TBL TO W10.
           DISPLAY "T=[" W10 "]".
           SEARCH ALL ENT
               AT END DISPLAY "NOT FOUND"
               WHEN KY OF ENT (IX) = "C"
                   DISPLAY "FOUND C=" VL (IX).
           SEARCH ALL ENT
               AT END DISPLAY "NOT FOUND"
               WHEN KY OF ENT (IX) = "D"
                   DISPLAY "FOUND D=" VL (IX).
           STOP RUN.
