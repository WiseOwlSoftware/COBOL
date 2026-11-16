      *>--- conformance-case
      *> id: g2-same-as-qualified
      *> rules: [2002-SR-13.16.47.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.47.2 SR1 - "Data-name-1 may be qualified." F is
      *> declared three times, with three different descriptions, so each
      *> SAME AS resolves only through its qualifier: OF, IN, and two
      *> levels. By §2002:13.16.47.3 GR1 each subject is "as though the data
      *> description identified by data-name-1 had been coded in place of
      *> the SAME AS clause" (the VALUE clause included), so:
      *>   S1 SAME AS F OF R2 is PIC 9(5) VALUE 12: S1=00012, after MOVE 7
      *>     S1=00007, LEN=05;
      *>   S2 SAME AS F IN R1 is PIC X(3) VALUE "AAA": S2=AAA, after
      *>     MOVE "XY" S2=XY | (padded to 3), LEN=03;
      *>   S3 SAME AS F OF G OF R3 is PIC X(2) VALUE "GG": S3=GG, LEN=02.
      *> A leg fails if a qualified data-name-1 is refused, or resolves to
      *> another F (each F differs in class, size and value).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SAQ01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N PIC 99.
       01  R1.
           05 F PIC X(3) VALUE "AAA".
       01  R2.
           05 F PIC 9(5) VALUE 12.
       01  R3.
           05 G.
              10 F PIC X(2) VALUE "GG".
       01  S1 SAME AS F OF R2.
       01  S2 SAME AS F IN R1.
       01  S3 SAME AS F OF G OF R3.
       PROCEDURE DIVISION.
           DISPLAY "S1=" S1
           DISPLAY "S2=" S2
           DISPLAY "S3=" S3
           MOVE 7 TO S1
           MOVE "XY" TO S2
           DISPLAY "S1=" S1
           MOVE FUNCTION LENGTH(S1) TO N
           DISPLAY "LEN=" N
           DISPLAY "S2=" S2 "|"
           MOVE FUNCTION LENGTH(S2) TO N
           DISPLAY "LEN=" N
           MOVE FUNCTION LENGTH(S3) TO N
           DISPLAY "LEN=" N
           STOP RUN.
