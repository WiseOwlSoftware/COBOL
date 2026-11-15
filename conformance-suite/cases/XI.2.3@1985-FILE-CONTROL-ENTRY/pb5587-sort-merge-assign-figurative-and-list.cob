      *>--- conformance-case
      *> id: pb5587-sort-merge-assign-figurative-and-list
      *> rules: [1985-FMT-XI.2.3.2]
      *> accept: [85]
      *> source-format: fixed
      *>---
      *> kb/Work PB5587. §1985:XI.2.3.2 prints SELECT file-name-1
      *> ASSIGN TO {implementor-name-1 | literal-1} ... with the
      *> ellipsis, and §1985:XI.2.3.3 SR2 restricts only what may
      *> follow the ASSIGN clause: no rule says literal-1 is not a
      *> figurative constant or that the list has one operand. So
      *> "A figurative constant may be used whenever 'literal' appears
      *> in a format" (§1985:IV.4.2.2.2.3 GRL2.3), and a numeric
      *> literal is no more refused: the generic literal-1 admits it
      *> where VII.2.3.3 SR3 says "nonnumeric". The three sort
      *> files below are legal at 85. The sorts return AAA BBB CCC
      *> (the ascending key order, §1985:XI.4.4.4 GR2a); a
      *> compiler that refuses either entry fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5587SM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF1 ASSIGN TO SPACE.
           SELECT SF2 ASSIGN TO "W1" "W2".
           SELECT SF3 ASSIGN TO 123.
       DATA DIVISION.
       FILE SECTION.
       SD  SF1.
       01  SREC1 PIC X(3).
       SD  SF2.
       01  SREC2 PIC X(3).
       SD  SF3.
       01  SREC3 PIC X(3).
       WORKING-STORAGE SECTION.
       01  EOF1 PIC X VALUE "N".
       01  EOF2 PIC X VALUE "N".
       01  EOF3 PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       M-1.
           SORT SF1 ON ASCENDING KEY SREC1
               INPUT PROCEDURE IS IN1
               OUTPUT PROCEDURE IS OUT1.
           SORT SF2 ON ASCENDING KEY SREC2
               INPUT PROCEDURE IS IN2
               OUTPUT PROCEDURE IS OUT2.
           SORT SF3 ON ASCENDING KEY SREC3
               INPUT PROCEDURE IS IN3
               OUTPUT PROCEDURE IS OUT3.
           STOP RUN.
       IN1 SECTION.
       IN1-1.
           MOVE "CCC" TO SREC1.
           RELEASE SREC1.
           MOVE "AAA" TO SREC1.
           RELEASE SREC1.
           MOVE "BBB" TO SREC1.
           RELEASE SREC1.
       OUT1 SECTION.
       OUT1-1.
           RETURN SF1 AT END MOVE "Y" TO EOF1.
           IF EOF1 = "N"
               DISPLAY "1 " SREC1
               GO TO OUT1-1.
       IN2 SECTION.
       IN2-1.
           MOVE "CCC" TO SREC2.
           RELEASE SREC2.
           MOVE "AAA" TO SREC2.
           RELEASE SREC2.
           MOVE "BBB" TO SREC2.
           RELEASE SREC2.
       OUT2 SECTION.
       OUT2-1.
           RETURN SF2 AT END MOVE "Y" TO EOF2.
           IF EOF2 = "N"
               DISPLAY "2 " SREC2
               GO TO OUT2-1.
       IN3 SECTION.
       IN3-1.
           MOVE "CCC" TO SREC3.
           RELEASE SREC3.
           MOVE "AAA" TO SREC3.
           RELEASE SREC3.
           MOVE "BBB" TO SREC3.
           RELEASE SREC3.
       OUT3 SECTION.
       OUT3-1.
           RETURN SF3 AT END MOVE "Y" TO EOF3.
           IF EOF3 = "N"
               DISPLAY "3 " SREC3
               GO TO OUT3-1.
