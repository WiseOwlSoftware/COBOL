      *>--- conformance-case
      *> id: g2_unstring_receiving_operands
      *> rules: [2002-SR-14.8.44.2-7, 2002-SR-14.8.44.2-9]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.44.2 SR7 - "The DELIMITER IN phrase and the COUNT
      *>   IN phrase may be specified only if the DELIMITED BY phrase is
      *>   specified": the granted form is written here, both phrases
      *>   together (lines U1-U3) and each alone (line U4), always with
      *>   DELIMITED BY, and the program compiles and runs.
      *> §2002:14.8.44.2 SR9 - "The data item referenced by identifier-4
      *>   is the receiving operand for data. The data item referenced
      *>   by identifier-5 is the receiving operand for delimiters": the
      *>   INTO items receive the isolated data and the DELIMITER IN
      *>   items the delimiter, in working storage and in the record of
      *>   an input CD (the CD leg, line C1).
      *> The values follow from §2002:14.8.44.3 GR11c (data moved by the
      *>   rules for MOVE: alphanumeric, left-justified, space-padded),
      *>   GR11d (the delimiter is moved to identifier-5; at the end of
      *>   identifier-1 identifier-5 is space filled) and GR11e (COUNT IN
      *>   receives the characters examined, excluding the delimiter).
      *> Each leg can fail: a refusal of the granted phrases fails the
      *>   compile; a swap of the two receiving roles puts a delimiter in
      *>   R1 or a data character in D1; a delimiter counted in the COUNT
      *>   would give 04 instead of 03; a D3 left at "#" shows the end of
      *>   sender not space filling identifier-5.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2UNS01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SRC  PIC X(10) VALUE "ABC,DE;FGH".
       01 SRC2 PIC X(6)  VALUE "AB,CDE".
       01 R1 PIC X(4) VALUE "####".
       01 R2 PIC X(4) VALUE "####".
       01 R3 PIC X(4) VALUE "####".
       01 R4 PIC X(4) VALUE "####".
       01 R5 PIC X(4) VALUE "####".
       01 R6 PIC X(4) VALUE "####".
       01 D1 PIC X VALUE "#".
       01 D2 PIC X VALUE "#".
       01 D3 PIC X VALUE "#".
       01 D5 PIC X VALUE "#".
       01 C1 PIC 99 VALUE 99.
       01 C2 PIC 99 VALUE 99.
       01 C3 PIC 99 VALUE 99.
       01 C4 PIC 99 VALUE 99.
       COMMUNICATION SECTION.
       CD CIN FOR INPUT
           SYMBOLIC QUEUE IS QN
           MESSAGE COUNT IS MC.
       01 CREC.
          02 CR1 PIC X(4).
          02 CD1 PIC X.
          02 CC1 PIC 99.
          02 CR2 PIC X(4).
          02 CC2 PIC 99.
          02 FILLER PIC X(74).
       PROCEDURE DIVISION.
       MAIN-PARA.
      *> U1-U3: both phrases on every receiver.
           UNSTRING SRC DELIMITED BY "," OR ";"
               INTO R1 DELIMITER IN D1 COUNT IN C1
                    R2 DELIMITER IN D2 COUNT IN C2
                    R3 DELIMITER IN D3 COUNT IN C3
           END-UNSTRING
           DISPLAY "U1 [" R1 "][" D1 "][" C1 "]"
           DISPLAY "U2 [" R2 "][" D2 "][" C2 "]"
           DISPLAY "U3 [" R3 "][" D3 "][" C3 "]"
      *> U4: COUNT IN alone, DELIMITER IN alone, neither.
           UNSTRING SRC DELIMITED BY "," OR ";"
               INTO R4 COUNT IN C4
                    R5 DELIMITER IN D5
                    R6
           END-UNSTRING
           DISPLAY "U4 [" R4 "][" C4 "][" R5 "][" D5 "][" R6 "]"
      *> C1: identifier-4, identifier-5 and identifier-6 in a CD record.
           UNSTRING SRC2 DELIMITED BY ","
               INTO CR1 DELIMITER IN CD1 COUNT IN CC1
                    CR2 COUNT IN CC2
           END-UNSTRING
           DISPLAY "C1 [" CR1 "][" CD1 "][" CC1 "][" CR2 "][" CC2 "]"
           STOP RUN.
