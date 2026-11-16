      *>--- conformance-case
      *> id: g2002u-occ-cd-odo-group-length
      *> rules: [2002-GR-13.16.36.3-8]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.36.3 GR8 - a group with a subordinate variable-table
      *> (format 2) OCCURS is a variable-length data item. a) data-name-1
      *> outside the group: the part of the table used is set by its value
      *> at the start of the operation, and a group with no elementary
      *> item before the table is a zero-length item when that value is
      *> zero; b) data-name-1 inside the group: a sending reference uses
      *> its value, a receiving one the maximum length.
      *> G, G2, GN and BG are records under an OUTPUT CD (§2002:A.1.2.2
      *> SR9); SV is a strongly-typed group in working-storage, since an
      *> item of a STRONG type shall not be implicitly redefined
      *> (§2002:13.16.55.2 SR4), as a CD record is.
      *> FUNCTION LENGTH measures as a sending item (§2002:15.39.3 RV4 b),
      *> in alphanumeric, national or boolean positions (RV3, RV2, RV1).
      *> EXPECTED, DERIVED FROM THE 2002 TEXT:
      *> A-RECV  alphanumeric, a): N=5 fills "-----"; N=2 then "ABCDE" is
      *>         received into 2 positions; N=5 shows [AB---].
      *> A-SEND  a): N=2, G sends "AB" to X(8): [AB      ], length 02.
      *> A-ZERO  a): N=0, G is zero-length; a zero-length sender moves as
      *>         SPACE (§2002:14.8.24.3 GR1): [        ], length 00.
      *> B-RECV  b): G2 receives at its maximum length 6, so "3ABCDE"
      *>         sets N2 to 3; DISPLAY sends N2+1 = 4 positions: [3ABC].
      *> B-SEND  b): N2=1, G2 sends 2 positions: [1A      ], length 02.
      *> N-RECV  national group, a): [AB---   ] after the same steps as A.
      *> N-SEND  a): N=3 sends 3 national positions: [AB-     ], length 03.
      *> BIT     bit group, a): N=8 receives B"00000000"; N=3 receives
      *>         B"11111111" into 3 positions; at N=8 BG = B"11100000"
      *>         (EQ); at N=3 its length is 03 boolean positions.
      *> STRONG  strongly-typed group, b): SN=2 gives length 03; SN=4, 05.
      *> Each leg fails if the maximum length is used where the current
      *> one is required or the reverse.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OCODO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N   PIC 9 VALUE 5.
       01  T   PIC X(8).
       01  TN  PIC N(8).
       01  L   PIC 9(2).
       01  ST TYPEDEF STRONG.
           05 SN PIC 9.
           05 SE PIC X OCCURS 1 TO 5 DEPENDING ON SN.
       01  SV TYPE ST.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK9
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  G.
           05 E PIC X OCCURS 0 TO 5 DEPENDING ON N.
       01  G2.
           05 N2 PIC 9.
           05 E2 PIC X OCCURS 1 TO 5 DEPENDING ON N2.
       01  GN GROUP-USAGE NATIONAL.
           05 EN PIC N OCCURS 1 TO 5 DEPENDING ON N.
       01  BG GROUP-USAGE BIT.
           05 BE PIC 1 OCCURS 0 TO 8 DEPENDING ON N.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "-----" TO G
           MOVE 2 TO N
           MOVE "ABCDE" TO G
           MOVE 5 TO N
           DISPLAY "A-RECV [" G "]"
           MOVE 2 TO N
           MOVE G TO T
           MOVE FUNCTION LENGTH(G) TO L
           DISPLAY "A-SEND [" T "] LEN " L
           MOVE 0 TO N
           MOVE "XYZ" TO T
           MOVE G TO T
           MOVE FUNCTION LENGTH(G) TO L
           DISPLAY "A-ZERO [" T "] LEN " L
           MOVE 1 TO N2
           MOVE "3ABCDE" TO G2
           DISPLAY "B-RECV N2=" N2 " [" G2 "]"
           MOVE 1 TO N2
           MOVE G2 TO T
           MOVE FUNCTION LENGTH(G2) TO L
           DISPLAY "B-SEND [" T "] LEN " L
           MOVE 5 TO N
           MOVE N"-----" TO GN
           MOVE 2 TO N
           MOVE N"ABCDE" TO GN
           MOVE 5 TO N
           MOVE GN TO TN
           DISPLAY "N-RECV [" FUNCTION DISPLAY-OF(TN) "]"
           MOVE 3 TO N
           MOVE GN TO TN
           MOVE FUNCTION LENGTH(GN) TO L
           DISPLAY "N-SEND [" FUNCTION DISPLAY-OF(TN) "] LEN " L
           MOVE 8 TO N
           MOVE B"00000000" TO BG
           MOVE 3 TO N
           MOVE B"11111111" TO BG
           MOVE 8 TO N
           IF BG = B"11100000"
               DISPLAY "BIT-RECV EQ"
           ELSE
               DISPLAY "BIT-RECV NE"
           END-IF
           MOVE 3 TO N
           MOVE FUNCTION LENGTH(BG) TO L
           DISPLAY "BIT-SEND LEN " L
           MOVE 2 TO SN OF SV
           MOVE FUNCTION LENGTH(SV) TO L
           DISPLAY "STRONG SN=2 LEN " L
           MOVE 4 TO SN OF SV
           MOVE FUNCTION LENGTH(SV) TO L
           DISPLAY "STRONG SN=4 LEN " L
           STOP RUN.
