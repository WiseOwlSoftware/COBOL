      *>--- conformance-case
      *> id: pb6008-release-in-unreached-section-after-85
      *> rules: [1985-SR-XI.4.2.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6008 - the RELEASE sibling of
      *> pb6008-return-in-unreached-section-after-85. "A RELEASE
      *> statement may be used only within the range of an input
      *> procedure associated with a SORT statement" for its file
      *> (§1985:XI.4.2.3 SR2). The input procedure IN-PROC leaves its
      *> first paragraph by GO TO I-END, its last paragraph, where the
      *> return mechanism ends it (§1985:XI.4.4.4 GR8). No path of the
      *> range reaches section REL-SEC, written after the procedures,
      *> so its RELEASE is outside every range. Refused COBOL7081.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6008RL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SORT-A ASSIGN TO P6008RL.
       DATA DIVISION.
       FILE SECTION.
       SD  SORT-A.
       01  REC-A.
           05  KEY-A PIC X(3).
       PROCEDURE DIVISION.
       MAIN-CTL SECTION.
       M1.
           SORT SORT-A ON ASCENDING KEY KEY-A
               INPUT PROCEDURE IS IN-PROC
               OUTPUT PROCEDURE IS OUT-PROC.
           STOP RUN.
       IN-PROC SECTION.
       I1.
           MOVE "AAA" TO REC-A.
           RELEASE REC-A.
           GO TO I-END.
       I-END.
           EXIT.
       OUT-PROC SECTION.
       O1.
           RETURN SORT-A AT END GO TO O9.
           DISPLAY KEY-A.
           GO TO O1.
       O9.
           EXIT.
       REL-SEC SECTION.
       R1.
           MOVE "BBB" TO REC-A.
           RELEASE REC-A.
