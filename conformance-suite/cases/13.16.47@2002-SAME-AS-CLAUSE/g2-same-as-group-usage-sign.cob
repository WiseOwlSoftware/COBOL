      *>--- conformance-case
      *> id: g2-same-as-group-usage-sign
      *> rules: [2002-GR-13.16.47.3-3, 2002-GR-13.16.47.3-5]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.47.3 GR3 - "If an alphanumeric group item or
      *> strongly-typed group item to which data-name-1 is subordinate
      *> contains a USAGE clause, the effect is as though that USAGE clause
      *> had been specified for the subject of the entry."
      *> §2002:13.16.47.3 GR5 - the same for "a SIGN clause" of "an
      *> alphanumeric group item, national group item, or strongly-typed
      *> group item".
      *> The subjects sit in working storage (X1-X3, P1, P2) and in a
      *> record description entry under a CD (C1-C2): §2002:A.1.1.2 "A
      *> record description consists of a set of data description
      *> entries", and §2002:13.16.47.2 SR7 restricts only data-name-1 to
      *> the file, working-storage, local-storage or linkage section.
      *> The national-group leg is in working storage only (X3): the
      *> communications facility omits national data (§2002:A NOTE
      *> "Support for data of classes boolean and national and exception
      *> handling are intentionally omitted from the obsolete
      *> communications facility.").
      *> SR9 holds: WS-REC, CDREC and REC3 carry no USAGE, SIGN or
      *> GROUP-USAGE clause.
      *> USAGE legs (GR3): WA is under W1 USAGE BINARY (alphanumeric
      *> group), SE OF V1 under V1 TYPE ST, ST TYPEDEF STRONG USAGE BINARY
      *> (§2002:13.16.56.3 GR3: the clauses are assumed by data defined
      *> using the type-name). So X1, C1 and P1 are as though PIC 9(4)
      *> USAGE BINARY: each BYTE-LENGTH (§2002:15.12.3 RV1) equals that
      *> of BCTL PIC 9(4) USAGE BINARY. The leg can fail: were the USAGE
      *> dropped, the subject would be PIC 9(4) display, the size of DCTL;
      *> line CTL-DIFFER Y shows BCTL and DCTL differ in size here (the
      *> implementor's binary sizing), so the equality discriminates.
      *> SIGN legs (GR5): WB is under W2 SIGN IS LEADING SEPARATE
      *> (alphanumeric group), SS OF V2 under a strong type with SIGN IS
      *> LEADING SEPARATE, WC under W3 GROUP-USAGE NATIONAL SIGN IS
      *> TRAILING SEPARATE (national group). By §2002:13.16.50.3 GR6 a)
      *> the separate sign is a character position that is not a digit
      *> position, so each S9(3) subject is 4 positions long:
      *> FUNCTION LENGTH (§2002:15.39.3 RV3 alphanumeric, RV2 national)
      *> gives 4 for X2, C2, P2, X3; without the inherited clause it
      *> would be 3. By GR6 a) and b) -12 in a LEADING SEPARATE item is
      *> "-012" (WS-REC(1:4), CDREC(1:4)) and in the TRAILING SEPARATE
      *> national item is N"012-" (X3N redefines X3).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SAU01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W1 USAGE BINARY.
           05 WA PIC 9(4).
       01  W2 SIGN IS LEADING SEPARATE.
           05 WB PIC S9(3).
       01  W3 GROUP-USAGE NATIONAL SIGN IS TRAILING SEPARATE.
           05 WC PIC S9(3) USAGE NATIONAL.
       01  ST TYPEDEF STRONG USAGE BINARY.
           05 SE PIC 9(4).
       01  SU TYPEDEF STRONG SIGN IS LEADING SEPARATE.
           05 SS PIC S9(3).
       01  V1 TYPE ST.
       01  V2 TYPE SU.
       01  BCTL PIC 9(4) USAGE BINARY.
       01  DCTL PIC 9(4).
       01  WS-REC.
           05 X2 SAME AS WB.
           05 X1 SAME AS WA.
       01  REC3.
           05 X3 SAME AS WC.
       01  REC3N REDEFINES REC3.
           05 X3N PIC N(4).
       01  P1 SAME AS SE OF V1.
       01  P2 SAME AS SS OF V2.
       01  N1 PIC 9.
       01  N2 PIC 9.
       01  N3 PIC 9.
       01  N4 PIC 9.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 C2 SAME AS WB.
           05 C1 SAME AS WA.
       PROCEDURE DIVISION.
           MOVE 1234 TO X1 C1 P1
           MOVE -12 TO X2 C2 P2 X3
           IF FUNCTION BYTE-LENGTH(BCTL) NOT =
              FUNCTION BYTE-LENGTH(DCTL)
              DISPLAY "CTL-DIFFER Y"
           ELSE
              DISPLAY "CTL-DIFFER N"
           END-IF
           IF FUNCTION BYTE-LENGTH(X1) = FUNCTION BYTE-LENGTH(BCTL)
              DISPLAY "X1-BINARY Y"
           ELSE
              DISPLAY "X1-BINARY N"
           END-IF
           IF FUNCTION BYTE-LENGTH(C1) = FUNCTION BYTE-LENGTH(BCTL)
              DISPLAY "C1-BINARY Y"
           ELSE
              DISPLAY "C1-BINARY N"
           END-IF
           IF FUNCTION BYTE-LENGTH(P1) = FUNCTION BYTE-LENGTH(BCTL)
              DISPLAY "P1-BINARY Y"
           ELSE
              DISPLAY "P1-BINARY N"
           END-IF
           IF X1 = 1234 AND C1 = 1234 AND P1 = 1234
              DISPLAY "VALUES Y"
           ELSE
              DISPLAY "VALUES N"
           END-IF
           MOVE FUNCTION LENGTH(X2) TO N1
           MOVE FUNCTION LENGTH(C2) TO N2
           MOVE FUNCTION LENGTH(P2) TO N3
           MOVE FUNCTION LENGTH(X3) TO N4
           DISPLAY "LEN " N1 " " N2 " " N3 " " N4
           DISPLAY "IMG [" WS-REC(1:4) "][" CDREC(1:4) "]"
           IF X3N = N"012-"
              DISPLAY "X3-TRAILING Y"
           ELSE
              DISPLAY "X3-TRAILING N"
           END-IF
           STOP RUN.
