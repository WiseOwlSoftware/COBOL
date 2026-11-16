      *>--- conformance-case
      *> id: g2-dde-cd-implied-picture
      *> rules: [2002-SR-13.14.2-9]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.14.2 SR9 - "The PICTURE clause may be omitted for an
      *> elementary item when an alphanumeric, boolean, or national
      *> literal is specified in the data-item format of the VALUE
      *> clause", implying PICTURE X(length), 1(length) or N(length).
      *> Four entries in a record after a CD omit PICTURE. Refusing any
      *> fails the case. X and H are alphanumeric: LENGTH counts
      *> alphanumeric positions (§2002:15.39.3 RV3), 5 for "HELLO" and
      *> 3 for X"414243" (three characters). N is national, counted in
      *> national positions (RV2): 2. B is boolean, counted in boolean
      *> positions (RV1): 3. The IF lines pin each VALUE taking effect
      *> (§2002:13.16.61.3 GR4); a wrong implied length or class
      *> changes a LEN or flips a Y to N.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DDP04.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  L PIC 99.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 X VALUE "HELLO".
           05 H VALUE X"414243".
           05 N VALUE N"HI".
           05 B VALUE B"101".
       PROCEDURE DIVISION.
           MOVE FUNCTION LENGTH(X) TO L
           DISPLAY "X=[" X "] LEN=" L
           MOVE FUNCTION LENGTH(H) TO L
           IF H = X"414243" DISPLAY "H Y LEN=" L
           ELSE DISPLAY "H N LEN=" L END-IF
           MOVE FUNCTION LENGTH(N) TO L
           IF N = N"HI" DISPLAY "N Y LEN=" L
           ELSE DISPLAY "N N LEN=" L END-IF
           MOVE FUNCTION LENGTH(B) TO L
           IF B = B"101" DISPLAY "B Y LEN=" L
           ELSE DISPLAY "B N LEN=" L END-IF
           STOP RUN.
