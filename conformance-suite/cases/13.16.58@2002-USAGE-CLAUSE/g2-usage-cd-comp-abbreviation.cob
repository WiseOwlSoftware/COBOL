      *>--- conformance-case
      *> id: g2-usage-cd-comp-abbreviation
      *> rules: [2002-SR-13.16.58.2-5]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.58.2 SR5 - "COMP is an abbreviation for
      *> COMPUTATIONAL." C1 (USAGE COMP) and C2 (USAGE COMPUTATIONAL)
      *> are otherwise identical, PIC S9(4) VALUE -12, and sit in a
      *> record description entry under an output CD (§2002:A.1.1.2: "A
      *> record description consists of a set of data description
      *> entries"); the record is the CD's only record, so it may hold
      *> VALUE clauses (§2002:A.1.2.2 SR9). Being one usage, the two
      *> items have one representation, whatever the implementor chose:
      *> equal BYTE-LENGTH ("LEN SAME") and, in the groups G1 and G2
      *> that each hold one of them, equal bytes ("IMAGE SAME"), both at
      *> the initial value and after ADD 7 to each. -12 + 7 is -5, which
      *> the edited item PIC -9(4) shows as -0005 for each. Fails if
      *> either spelling is refused, or the two spellings give items of
      *> a different width, image or value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2USG04.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  E   PIC -9(4).
       01  L1  PIC 99.
       01  L2  PIC 99.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           TEXT LENGTH IS TL
           STATUS KEY IS SK.
       01  CR.
           05 G1.
              10 C1 PIC S9(4) COMP VALUE -12.
           05 G2.
              10 C2 PIC S9(4) COMPUTATIONAL VALUE -12.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE FUNCTION BYTE-LENGTH(C1) TO L1
           MOVE FUNCTION BYTE-LENGTH(C2) TO L2
           IF L1 = L2
               DISPLAY "LEN SAME"
           ELSE
               DISPLAY "LEN DIFF"
           END-IF
           PERFORM SHOW-IMAGE
           ADD 7 TO C1 C2
           MOVE C1 TO E
           DISPLAY "C1=" E
           MOVE C2 TO E
           DISPLAY "C2=" E
           PERFORM SHOW-IMAGE
           STOP RUN.
       SHOW-IMAGE.
           IF G1 = G2
               DISPLAY "IMAGE SAME"
           ELSE
               DISPLAY "IMAGE DIFF"
           END-IF.
