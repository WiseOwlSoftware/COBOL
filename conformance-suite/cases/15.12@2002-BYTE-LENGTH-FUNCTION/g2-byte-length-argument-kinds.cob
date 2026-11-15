      *>--- conformance-case
      *> id: g2-byte-length-argument-kinds
      *> rules: [2002-AR-15.12.2-1, 2002-RV-15.12.3-1, 2002-RV-15.12.3-3,
      *>    2002-RV-15.12.3-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.12.2 AR1 - "Argument-1 shall be an alphanumeric or
      *> national literal, a based entry, a type-name, or a data item of
      *> any class or category." Every leg writes one of those kinds and
      *> must be ACCEPTED; §2002:15.12.3 RV1 - "the length of argument-1
      *> in number of bytes" - gives each value. A byte is one
      *> alphanumeric character position here (8-bit bytes, the
      *> implementor's determination DOC-A.1-12 in docs/conformance),
      *> and a national character position is one UTF-16 code unit,
      *> two bytes (the native national repertoire, DOC-A.1-188).
      *>   LIT    "ABCDE"           5 positions                  -> 5
      *>   NLIT   N"AB"             2 national positions         -> 4
      *>   TYPE   T-REC TYPEDEF     X(3) + 9(4) display          -> 7
      *>   BASED  B-REC BASED       X(7) + N(2) = 7 + 4          -> 11
      *>   ALNUM  X(9)                                           -> 9
      *>   ALPHA  A(4)                                           -> 4
      *>   NUM    S9(5), no SIGN clause: 'S' is not counted in the
      *>          size (§2002:13.16.38.3 GR13d; no SIGN clause, so
      *>          §2002:13.16.50.3 GR4)                          -> 5
      *>   NUMSEP S9(5) SIGN LEADING SEPARATE: 'S' is counted
      *>          (§2002:13.16.38.3 GR13d) as its own character
      *>          position (§2002:13.16.50.3 GR6a)               -> 6
      *>   NE     ZZ9.99                                         -> 6
      *>   NAT    N(3)              3 national positions         -> 6
      *>   BOOL   PIC 1(4) (usage display)                       -> 4
      *>   CDREC  the record under an input CD: "a record area of 87
      *>          contiguous alphanumeric character positions"
      *>          (§2002:A.1.2.3 GR2)                            -> 87
      *>   CDQ    its first field X(12)                          -> 12
      *>   CDDATE MESSAGE DATE, positions 49-54 (GR2e)           -> 6
      *>   CDLEN  TEXT LENGTH, positions 75-78 (GR2h)            -> 4
      *>   CDCNT  MESSAGE COUNT, positions 82-87 (GR2k)          -> 6
      *> The pointer, index, object-reference and FLOAT-LONG sizes are
      *> the implementor's. Each leg checks that the item's length
      *> equals that of a group holding only that item, and is
      *> positive (PTR, IDX, OBJ, FLT -> Y). RV1 alone does not fix
      *> that: an object reference's storage "is not necessarily the
      *> same for every object reference" (§2002:13.16.58.3 GR14a),
      *> and a plain group's alignment relative to its first item is
      *> implementor-defined unless that item is of class pointer or
      *> object (§2002:8.5.1.5.1). The IDX, FLT and OBJ legs rest on
      *> WiseOwl's determinations in docs/conformance: DOC-A.1-214 (8
      *> bytes for every object reference) and DOC-A.1-205 (alignment:
      *> none, so a group carries no implicit filler).
      *> A pointer or object reference may stand below level 1 only
      *> in a type declaration
      *> with the STRONG phrase (§2002:13.16.58.2 SR13), so those two
      *> groups are strong type-names - the type-name kind of AR1.
      *> §2002:15.12.3 RV4 - "When argument-1 does not occupy an integral
      *> number of bytes, the returned value is rounded to the next
      *> larger integer value." A level-1 elementary bit item gets no
      *> trailing filler (§2002:8.5.1.5.3 NOTE), so it occupies exactly
      *> its bits:
      *>   BIT12  PIC 1(12) BIT     12 bits = 1.5 bytes          -> 2
      *>   BIT5   PIC 1(5) BIT      5 bits                       -> 1
      *>   BIT8   PIC 1(8) BIT      8 bits, integral, the control -> 1
      *> §2002:15.12.3 RV3 - "The returned value shall include the number
      *> of implicit filler positions, if any, in argument-1." Filler is
      *> generated "Following a bit data item within an alphanumeric
      *> group item ... as needed to advance alignment to a required
      *> natural boundary for the next item" and after a last bit item
      *> "to fill an integral number of characters" (§2002:8.5.1.5.3):
      *>   FILL   F-B1 1 bit | filler 7 | F-A1 8 bits | F-B2 1 bit |
      *>          filler 7 = 24 bits                             -> 3
      *>          (without the interior filler, 10 bits -> 2)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2BYL01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T-REC IS TYPEDEF.
           05 T-A PIC X(3).
           05 T-B PIC 9(4).
       01  B-REC BASED.
           05 B-A PIC X(7).
           05 B-N PIC N(2).
       01  W-X      PIC X(9).
       01  W-ALPHA  PIC A(4).
       01  W-NUM    PIC S9(5).
       01  W-NUMSEP PIC S9(5) SIGN LEADING SEPARATE.
       01  W-NE     PIC ZZ9.99.
       01  W-N      PIC N(3).
       01  W-BOOL   PIC 1(4).
       01  W-BIT12  PIC 1(12) USAGE BIT.
       01  W-BIT5   PIC 1(5) USAGE BIT.
       01  W-BIT8   PIC 1(8) USAGE BIT.
       01  F-G.
           05 F-B1  PIC 1 USAGE BIT.
           05 F-A1  PIC X.
           05 F-B2  PIC 1 USAGE BIT.
       01  W-PTR    USAGE POINTER.
       01  G-PTR IS TYPEDEF STRONG.
           05 G-PTR1 USAGE POINTER.
       01  W-IDX    USAGE INDEX.
       01  G-IDX.
           05 G-IDX1 USAGE INDEX.
       01  W-OBJ    USAGE OBJECT REFERENCE.
       01  G-OBJ IS TYPEDEF STRONG.
           05 G-OBJ1 USAGE OBJECT REFERENCE.
       01  W-FLT    USAGE FLOAT-LONG.
       01  G-FLT.
           05 G-FLT1 USAGE FLOAT-LONG.
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT.
       01  IN-REC.
           05 IN-Q  PIC X(12).
           05 IN-R  PIC X(75).
       CD  CDIN2 FOR INPUT
           MESSAGE DATE IS IN-MDATE
           TEXT LENGTH IS IN-LEN
           MESSAGE COUNT IS IN-COUNT.
       PROCEDURE DIVISION.
       P-MAIN.
           DISPLAY "LIT=" FUNCTION BYTE-LENGTH("ABCDE")
           DISPLAY "NLIT=" FUNCTION BYTE-LENGTH(N"AB")
           DISPLAY "TYPE=" FUNCTION BYTE-LENGTH(T-REC)
           DISPLAY "BASED=" FUNCTION BYTE-LENGTH(B-REC)
           DISPLAY "ALNUM=" FUNCTION BYTE-LENGTH(W-X)
           DISPLAY "ALPHA=" FUNCTION BYTE-LENGTH(W-ALPHA)
           DISPLAY "NUM=" FUNCTION BYTE-LENGTH(W-NUM)
           DISPLAY "NUMSEP=" FUNCTION BYTE-LENGTH(W-NUMSEP)
           DISPLAY "NE=" FUNCTION BYTE-LENGTH(W-NE)
           DISPLAY "NAT=" FUNCTION BYTE-LENGTH(W-N)
           DISPLAY "BOOL=" FUNCTION BYTE-LENGTH(W-BOOL)
           DISPLAY "CDREC=" FUNCTION BYTE-LENGTH(IN-REC)
           DISPLAY "CDQ=" FUNCTION BYTE-LENGTH(IN-Q)
           DISPLAY "CDDATE=" FUNCTION BYTE-LENGTH(IN-MDATE)
           DISPLAY "CDLEN=" FUNCTION BYTE-LENGTH(IN-LEN)
           DISPLAY "CDCNT=" FUNCTION BYTE-LENGTH(IN-COUNT)
           IF FUNCTION BYTE-LENGTH(W-PTR) = FUNCTION BYTE-LENGTH(G-PTR)
              AND FUNCTION BYTE-LENGTH(W-PTR) > 0
               DISPLAY "PTR=Y" ELSE DISPLAY "PTR=N" END-IF
           IF FUNCTION BYTE-LENGTH(W-IDX) = FUNCTION BYTE-LENGTH(G-IDX)
              AND FUNCTION BYTE-LENGTH(W-IDX) > 0
               DISPLAY "IDX=Y" ELSE DISPLAY "IDX=N" END-IF
           IF FUNCTION BYTE-LENGTH(W-OBJ) = FUNCTION BYTE-LENGTH(G-OBJ)
              AND FUNCTION BYTE-LENGTH(W-OBJ) > 0
               DISPLAY "OBJ=Y" ELSE DISPLAY "OBJ=N" END-IF
           IF FUNCTION BYTE-LENGTH(W-FLT) = FUNCTION BYTE-LENGTH(G-FLT)
              AND FUNCTION BYTE-LENGTH(W-FLT) > 0
               DISPLAY "FLT=Y" ELSE DISPLAY "FLT=N" END-IF
           DISPLAY "BIT12=" FUNCTION BYTE-LENGTH(W-BIT12)
           DISPLAY "BIT5=" FUNCTION BYTE-LENGTH(W-BIT5)
           DISPLAY "BIT8=" FUNCTION BYTE-LENGTH(W-BIT8)
           DISPLAY "FILL=" FUNCTION BYTE-LENGTH(F-G)
           STOP RUN.
