      *>--- conformance-case
      *> id: g2-global-cd-constant
      *> rules: [2002-SR-13.16.25.2-1, 2002-GR-13.16.25.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.25.2 SR1 - "The GLOBAL clause may be specified
      *> only in the following entries": a) a constant entry (here
      *> one written in the communication section after a CD, which
      *> the §2002:A.1.1 format admits beside the record entries),
      *> b) a level-1 entry of the file (CHK-REC), working-storage
      *> (WG), local-storage (LG) and linkage (LK-G) sections,
      *> d) a file description entry (GF, RF-F) and e) a
      *> report description entry. Each granted form is ACCEPTED.
      *> §2002:13.16.25.3 GR1 - each name so described is a global
      *> name; "All data-names subordinate to a global name are
      *> global names" (WG-A, WG-B, GF-REC, GDE) and "All
      *> condition-names associated with a global name are global
      *> names" (WG-IS-AAA). GR2: the contained program G2GLB01I
      *> references each without describing it again.
      *> Derivation (KCONST is "GLOBALK" wherever written,
      *> §2002:13.9.3 GR1):
      *>   OUTER GLOBALK LOCALK  - the outer program's own view.
      *>   INNER GLOBALK         - the CD-section constant is global.
      *>   INNER 88 TRUE         - WG-A is "AAA", its 88 is global.
      *>   INNER LG=LLL          - the local-storage level 1 item.
      *>   OUTER WG-A=BBB WG-B=2 - the inner MOVEs reached the one
      *>                           shared item.
      *>   OUTER 88 FALSE        - WG-A is no longer "AAA".
      *>   OUTER GF-REC=FREC     - GF (FD IS GLOBAL) was written by the
      *>                           contained program (SR3 permits its
      *>                           I/O there) and is read back here.
      *>   [RPT1]                - GRPT (RD IS GLOBAL) and its detail
      *>                           GDE were INITIATEd and GENERATEd in
      *>                           the contained program (13.12.3 GR1).
      *> Fails if any granted placement is refused, or if a name is
      *> not visible (compile error) or not shared in the contained
      *> program. COBOL0903 (the communication facility is obsolete)
      *> is a warning, not a refusal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2GLB01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT GF ASSIGN TO "g2glb01f.txt".
           SELECT RF-F ASSIGN TO "g2glb01r.txt".
           SELECT CHK ASSIGN TO "g2glb01r.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  GF IS GLOBAL.
       01  GF-REC PIC X(4).
       FD  RF-F IS GLOBAL REPORT IS GRPT.
       FD  CHK.
       01  CHK-REC PIC X IS GLOBAL.
       WORKING-STORAGE SECTION.
       01  WG IS GLOBAL.
           05 WG-A PIC X(3) VALUE "AAA".
              88 WG-IS-AAA VALUE "AAA".
           05 WG-B PIC 9 VALUE 1.
       01  WS-EOF PIC X VALUE "N".
       01  WS-I   PIC 99 VALUE 0.
       01  WS-LINE PIC X(20) VALUE SPACES.
       LOCAL-STORAGE SECTION.
       01  LG PIC X(3) IS GLOBAL VALUE "LLL".
       LINKAGE SECTION.
       01  LK-G PIC X IS GLOBAL.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  KCONST CONSTANT IS GLOBAL AS "GLOBALK".
       01  KLOCAL CONSTANT AS "LOCALK".
       REPORT SECTION.
       RD  GRPT IS GLOBAL.
       01  GDE TYPE DE.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(4) VALUE "RPT1".
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "OUTER " KCONST " " KLOCAL
           CALL "G2GLB01I"
           DISPLAY "OUTER WG-A=" WG-A " WG-B=" WG-B
           IF WG-IS-AAA
               DISPLAY "OUTER 88 TRUE"
           ELSE
               DISPLAY "OUTER 88 FALSE"
           END-IF
           OPEN INPUT GF
           READ GF
               AT END DISPLAY "OUTER GF EMPTY"
               NOT AT END DISPLAY "OUTER GF-REC=" GF-REC
           END-READ
           CLOSE GF
           OPEN INPUT CHK
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM
           CLOSE CHK
           PERFORM SHOW-LINE
           STOP RUN.
       TAKE-BYTE.
           IF CHK-REC = X"0A"
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC NOT = X"0D" AND CHK-REC NOT = X"0C"
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE (WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           IF WS-I > 0
               DISPLAY "[" WS-LINE (1:WS-I) "]"
               MOVE SPACES TO WS-LINE
               MOVE 0 TO WS-I
           END-IF.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2GLB01I.
       PROCEDURE DIVISION.
       INNER-P.
           DISPLAY "INNER " KCONST
           IF WG-IS-AAA
               DISPLAY "INNER 88 TRUE"
           ELSE
               DISPLAY "INNER 88 FALSE"
           END-IF
           DISPLAY "INNER LG=" LG
           MOVE "BBB" TO WG-A
           MOVE 2 TO WG-B
           OPEN OUTPUT GF
           MOVE "FREC" TO GF-REC
           WRITE GF-REC
           CLOSE GF
           OPEN OUTPUT RF-F
           INITIATE GRPT
           GENERATE GDE
           TERMINATE GRPT
           CLOSE RF-F
           EXIT PROGRAM.
       END PROGRAM G2GLB01I.
       END PROGRAM G2GLB01.
