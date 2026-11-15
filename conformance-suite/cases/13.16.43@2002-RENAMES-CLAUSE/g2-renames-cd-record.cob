      *>--- conformance-case
      *> id: g2-renames-cd-record
      *> rules: [2002-GR-13.16.43.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.43.3 GR1: "When the THROUGH phrase is not specified, all
      *> of the data attributes of data-name-2 become the data attributes of
      *> data-name-1 and the storage area occupied by data-name-2 becomes the
      *> storage area occupied by data-name-1." At 2002 a 66 entry may follow a
      *> record under a CD (§2002:13.16.43.2 SR3 names "the associated level
      *> 01, FD, CD, or SD entries"). Expected lines: C2N reads C2's content,
      *> C2N=BBB; FUNCTION LENGTH(C2N) is C2's 3 positions, LEN=03; MOVE "XYZW"
      *> TO C2N is an alphanumeric move into C2's 3 positions, so C2=XYZ, with
      *> the neighbours C1=AA and C3=CC untouched (C2N's storage is exactly
      *> C2's), and the record reads REC=AAXYZCC007; C4N takes C4's numeric
      *> attributes, so MOVE 7 TO C4N then ADD 1 TO C4N leaves C4=008. A leg
      *> fails if the 66 entry gets storage or attributes of its own.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2REN05.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N PIC 99.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 C1 PIC X(2).
           05 C2 PIC X(3).
           05 C3 PIC X(2).
           05 C4 PIC 9(3).
       66  C2N RENAMES C2.
       66  C4N RENAMES C4.
       PROCEDURE DIVISION.
           MOVE "AA" TO C1
           MOVE "BBB" TO C2
           MOVE "CC" TO C3
           MOVE 7 TO C4
           DISPLAY "C2N=" C2N
           MOVE FUNCTION LENGTH(C2N) TO N
           DISPLAY "LEN=" N
           MOVE "XYZW" TO C2N
           DISPLAY "C2=" C2
           DISPLAY "C1=" C1
           DISPLAY "C3=" C3
           DISPLAY "REC=" CDREC
           MOVE 7 TO C4N
           ADD 1 TO C4N
           DISPLAY "C4=" C4
           STOP RUN.
