      *>--- conformance-case
      *> id: g2002_group_usage_not_the_group
      *> rules: [2002-GR-13.16.58.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.58.3 GR1 - "If the USAGE clause is specified or
      *> implied at a group level, it applies only to each elementary
      *> item in the group and not to the group itself." A group with a
      *> USAGE clause and no GROUP-USAGE clause therefore stays an
      *> alphanumeric group, and FUNCTION LENGTH measures it in
      *> alphanumeric character positions (§2002:15.39.3 RV3), while a
      *> GROUP-USAGE group is a national group (§2002:13.16.27.3 GR2a,
      *> RV2) or a bit group (§2002:13.16.27.3 GR1a, RV1). The same
      *> groups are written in working storage and in the record of an
      *> output CD (§2002:A.1.2.2 SR9: an ordinary record description).
      *>  GP USAGE PACKED-DECIMAL over 9(3) and S9(5): each leaf is
      *>    packed, digits/2+1 bytes (DOC-A.1-215) = 2 + 3; the group is
      *>    alphanumeric: LENGTH 05 (a packed group would be no group).
      *>  GN USAGE NATIONAL over N(3): the leaf is national, 2 bytes per
      *>    character (DOC-A.1-213); GN is an alphanumeric group of 6
      *>    positions: LENGTH 06. GU GROUP-USAGE NATIONAL over the same
      *>    leaf is a national group: LENGTH 03.
      *>  GB USAGE BIT over 1(4) 1(4): the two bit leaves are adjacent
      *>    (§2002:8.5.1.5.3) and fill 8 bits; GB is an alphanumeric
      *>    group, 1 position: LENGTH 01. GG GROUP-USAGE BIT over the
      *>    same leaves is a bit group of 8 boolean positions: LENGTH 08.
      *> Each USAGE line fails if the usage is applied to the group
      *> itself (GN 03, GB 08) or not to its leaves (GP 08, GN 03).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2UA04.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  L   PIC 99.
       01  WR.
           05 WGP USAGE PACKED-DECIMAL.
              10 WPA PIC 9(3).
              10 WPB PIC S9(5).
           05 WGN USAGE NATIONAL.
              10 WN1 PIC N(3).
           05 WGU GROUP-USAGE NATIONAL.
              10 WN2 PIC N(3).
           05 WGB USAGE BIT.
              10 WB1 PIC 1(4).
              10 WB2 PIC 1(4).
           05 WGG GROUP-USAGE BIT.
              10 WB3 PIC 1(4).
              10 WB4 PIC 1(4).
       COMMUNICATION SECTION.
       CD  CO FOR OUTPUT.
       01  CR.
           05 CGP USAGE PACKED-DECIMAL.
              10 CPA PIC 9(3).
              10 CPB PIC S9(5).
           05 CGN USAGE NATIONAL.
              10 CN1 PIC N(3).
           05 CGU GROUP-USAGE NATIONAL.
              10 CN2 PIC N(3).
           05 CGB USAGE BIT.
              10 CB1 PIC 1(4).
              10 CB2 PIC 1(4).
           05 CGG GROUP-USAGE BIT.
              10 CB3 PIC 1(4).
              10 CB4 PIC 1(4).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION LENGTH(WGP) TO L
           DISPLAY "WS GP " L
           MOVE FUNCTION LENGTH(WGN) TO L
           DISPLAY "WS GN " L
           MOVE FUNCTION LENGTH(WGU) TO L
           DISPLAY "WS GU " L
           MOVE FUNCTION LENGTH(WGB) TO L
           DISPLAY "WS GB " L
           MOVE FUNCTION LENGTH(WGG) TO L
           DISPLAY "WS GG " L
           MOVE FUNCTION LENGTH(CGP) TO L
           DISPLAY "CD GP " L
           MOVE FUNCTION LENGTH(CGN) TO L
           DISPLAY "CD GN " L
           MOVE FUNCTION LENGTH(CGU) TO L
           DISPLAY "CD GU " L
           MOVE FUNCTION LENGTH(CGB) TO L
           DISPLAY "CD GB " L
           MOVE FUNCTION LENGTH(CGG) TO L
           DISPLAY "CD GG " L
           STOP RUN.
