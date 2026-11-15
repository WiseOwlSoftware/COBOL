      *>--- conformance-case
      *> id: g2-group-usage-national-cd
      *> rules: [2002-GR-13.16.27.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> A GROUP-USAGE NATIONAL record under an output CD (record
      *> entries there are data description entries, §2002:A.1.1.2).
      *> §2002:13.16.27.3 GR2 - CDREC and its subordinate group G1
      *> (implicitly GROUP-USAGE NATIONAL, 13.16.27.2 SR3) are
      *> national groups, "treated as though it were an elementary
      *> data item of usage national ... described with PICTURE
      *> N(m)": G1 as N(4), CDREC as N(5). MOVE treats national
      *> groups as elementary items (§2002:14.8.24.3 GR2), so the
      *> 14.5.8 rule 5 alignment of a national receiver applies:
      *>   MOVE N"ABCDEF" TO G1 - truncated on the right: N1 = N"AB",
      *>     N2 = N"CD"                              -> TRUNC OK
      *>   MOVE N"AB" TO G1 - national space fill:   -> PAD OK
      *>     N2 = N"  "; an alphanumeric group move would put
      *>     alphanumeric spaces into the national positions.
      *>   G1 = N"AB" - a national comparison of the group as an
      *>     N(4) item (N"AB" padded with national spaces) -> CMP OK
      *>   FUNCTION LENGTH counts national character positions for a
      *>   national group (§2002:15.39.3 RV2): G1 4, CDREC 5
      *>                                             -> LEN 04 05
      *> Fails if a national group moves or compares as an
      *> alphanumeric group. COBOL0903 (the communication facility
      *> is obsolete) is a warning, not a refusal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2GRU01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  L1 PIC 99.
       01  L2 PIC 99.
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
           05 N3 PIC N(1).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE N"ABCDEF" TO G1
           IF N1 = N"AB" AND N2 = N"CD"
               DISPLAY "TRUNC OK"
           ELSE
               DISPLAY "TRUNC BAD"
           END-IF
           MOVE N"AB" TO G1
           IF N1 = N"AB" AND N2 = N"  "
               DISPLAY "PAD OK"
           ELSE
               DISPLAY "PAD BAD"
           END-IF
           IF G1 = N"AB"
               DISPLAY "CMP OK"
           ELSE
               DISPLAY "CMP BAD"
           END-IF
           MOVE FUNCTION LENGTH (G1) TO L1
           MOVE FUNCTION LENGTH (CDREC) TO L2
           DISPLAY "LEN " L1 " " L2
           STOP RUN.
