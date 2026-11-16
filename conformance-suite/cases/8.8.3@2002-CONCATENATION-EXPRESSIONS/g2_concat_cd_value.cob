      *>--- conformance-case
      *> id: g2_concat_cd_value
      *> rules: [2002-GR-8.8.3.3-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.8.3.3 GR3 - "A concatenation expression shall be
      *> equivalent to a literal of the same class and value, and may
      *> be used anywhere a literal of that class may be used."
      *> At 2002 one such place is the VALUE clause of a record under a
      *> CD in the communication section: §2002:13.16.61.3 GR4 has
      *> "VALUE clauses in the working-storage, local-storage, and
      *> communication sections take effect" in the initial state, and
      *> §2002:A.1.2.2 SR5 lets the first redefinition of an input CD's
      *> 87-position record hold VALUE clauses. 2023 has no CD, so this
      *> position is 2002's alone. The facility is obsolete at 2002
      *> (COBOL0903 warning), which does not fail the case.
      *> cite.py --check: OK §2002:8.8.3.3 GR2, GR3;
      *>   OK §2002:13.16.61.3 GR4, GR7; OK §2002:A.1.2.2 SR5.
      *> Expected, from the rules (GR2: the value is the operands'
      *> values joined; GR7: an alphanumeric literal is aligned left
      *> and space-filled, 14.5.8):
      *>   CDQ  PIC X(12) VALUE "QU" & "EUE1"  -> [QUEUE1      ]
      *>   CDS  PIC X(12) VALUE "S-" & "Q2" & "X" -> [S-Q2X       ]
      *>   CDQ = "QUEUE1" (the equivalent plain literal)     -> EQ=T
      *>   88 CDQ-ONE VALUE "QUE" & "UE1" (a level-88 VALUE literal
      *>   position under the CD record)                    -> 88=T
      *>   MOVE "AB" & "C" TO CDS (a procedure literal position over a
      *>   CD record item)                       -> [ABC         ]
      *> A fold that dropped or misplaced an operand, or a CD record
      *> whose VALUE did not take effect, changes a bracketed line; a
      *> concatenation refused in the CD position fails the compile.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CAT01.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD G2CDIN FOR INPUT.
       01  G2CDREC.
           05  CDQ                 PIC X(12) VALUE "QU" & "EUE1".
               88  CDQ-ONE         VALUE "QUE" & "UE1".
           05  CDS                 PIC X(12) VALUE "S-" & "Q2" & "X".
           05  FILLER              PIC X(63).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "CDQ=[" CDQ "]".
           DISPLAY "CDS=[" CDS "]".
           IF CDQ = "QUEUE1" DISPLAY "EQ=T" ELSE DISPLAY "EQ=F".
           IF CDQ-ONE DISPLAY "88=T" ELSE DISPLAY "88=F".
           MOVE "AB" & "C" TO CDS.
           DISPLAY "MOV=[" CDS "]".
           STOP RUN.
