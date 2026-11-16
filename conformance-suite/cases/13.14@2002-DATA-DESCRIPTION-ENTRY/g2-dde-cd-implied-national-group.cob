      *>--- conformance-case
      *> id: g2-dde-cd-implied-national-group
      *> rules: [2002-GR-13.14.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.14.3 GR2 - a group subordinate to a national group
      *> with no GROUP-USAGE clause has GROUP-USAGE NATIONAL implied.
      *> G1 sits under CDREC GROUP-USAGE NATIONAL in a record after a
      *> CD. As a national group (§2002:13.16.27.3 GR2) G1 is moved as
      *> an elementary national item (§2002:14.8.24.3 GR2), so "AB"
      *> pads N2 with national spaces; FUNCTION LENGTH counts it in
      *> national character positions (§2002:15.39.3 RV2): 4. Were G1
      *> an alphanumeric group, the move would pad with alphanumeric
      *> spaces and LENGTH would count alphanumeric positions.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DDE11.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  LEN-G1 PIC 99.
       01  LEN-N1 PIC 99.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC GROUP-USAGE NATIONAL.
           05 G1.
              10 N1 PIC N(2).
              10 N2 PIC N(2).
       PROCEDURE DIVISION.
           MOVE "AB" TO G1
           IF N2 = SPACES DISPLAY "N2 SPACES"
           ELSE DISPLAY "N2 NOT SPACES" END-IF
           MOVE FUNCTION LENGTH(G1) TO LEN-G1
           MOVE FUNCTION LENGTH(N1) TO LEN-N1
           DISPLAY "LEN-G1=" LEN-G1 " LEN-N1=" LEN-N1
           STOP RUN.
