      *> reject-at: 85
      *> kb/Work PB1021 - an address-identifier as a relation operand
      *> (ISO 8.8.4.2.2 Format 3; 8.4.3.1.2 identifier Format 9). The
      *> address-identifier arrived in COBOL-2002 with the pointer classes,
      *> so at COBOL-85 the operand draws the introduction gate COBOLNET0900
      *> (VersionConformancePass.VisitAddressIdentifier) - the SAME gate the
      *> CALL argument draws, because both spell the one addressIdentifier
      *> rule.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1021N85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(4) VALUE "ABCD".
       01 Y PIC X(4) VALUE "WXYZ".
       PROCEDURE DIVISION.
           IF ADDRESS OF X = ADDRESS OF Y DISPLAY "EQ".
           STOP RUN.
