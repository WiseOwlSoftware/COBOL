      *>--- conformance-case
      *> id: g2002_constant_entries_under_cd
      *> rules: [2002-SR-13.9.2-2, 2002-SR-13.9.2-7, 2002-GR-13.9.3-2,
      *>    2002-GR-13.9.3-4, 2002-GR-13.9.3-5, 2002-GR-13.9.3-6]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> Every constant entry below is written in the COMMUNICATION
      *> SECTION after a CD entry, a position the 2002 data division
      *> grants (§2002:A.1.1 "COMMUNICATION SECTION . constant-entry")
      *> and the 2023 twin rows cannot reach (2023 has no communication
      *> section). The facility is obsolete at 2002 (COBOL0903 warning).
      *>
      *> §2002:13.9.3 GR6 - "If the LENGTH phrase is specified, the
      *> class and category of constant-name-1 is numeric." Value as
      *> the LENGTH function (§2002:15.39.3 RV2/RV3), "when data-name-2
      *> is a variable-length data item, the maximum size of the data
      *> item is used":
      *>   KA  = LENGTH OF WA (X(5))              = 5  -> "KA=0005"
      *>   KNL = LENGTH OF WN (N(3)), national
      *>         character positions (RV2)         = 3  -> "KNL=0003"
      *>   KGL = LENGTH OF G, G = X(2) + X(3) OCCURS 1 TO 4 DEPENDING
      *>         ON GD, GD = 1: the MAXIMUM 2+4*3  = 14 -> "KGL=0014"
      *>         (the current size, 5, would fail the leg)
      *> §2002:13.9.3 GR5 - BYTE-LENGTH, the same maximum exception; one
      *>   byte per alphanumeric position is this compiler's documented
      *>   width (FUNCTION BYTE-LENGTH, INTRINSICS_DESIGN):
      *>   KB  = BYTE-LENGTH OF WA                = 5  -> "KB=0005"
      *>   KGB = BYTE-LENGTH OF G (maximum)        = 14 -> "KGB=0014"
      *> §2002:13.9.3 GR4 - "it is evaluated in accordance with 7.2.5"
      *>   and "Constant-name-1 is an integer"; §2002:7.2.5.2 GR1
      *>   standard arithmetic, GR2 truncation to the integer part:
      *>   KC  = 2 + 3 * 4                         = 14 -> "KC=0014"
      *>   KT  = 7 / 2 = 3.5 -> 3                       -> "KT=0003"
      *>   K32 = 1234567890123456789012345678901 * 1001 / 1001:
      *>     the exact product 1235802458013580245801358024579901 (34
      *>     digits) is "truncated to 32 significant digits"
      *>     (§2002:8.8.1.3.4) to ...4579900; the quotient
      *>     1234567890123456789012345678900.999.. truncates to 32
      *>     digits (§2002:8.8.1.3.5) as ...8900.9, and GR2 of 7.2.5.2
      *>     keeps ...678900. Exact or 34-digit arithmetic gives ...901.
      *>     -> "K32=1234567890123456789012345678900"
      *>   KD  = KC * 2 = 28 (a prior constant as a literal operand,
      *>         §2002:13.9.2 SR2 within SR7's "operands shall be
      *>         literals")                             -> "KD=0028"
      *> §2002:13.9.2 SR7 - each expression above is formed per 7.2.5
      *>   (fixed-point literal operands, no exponentiation, within the
      *>   standard range) and is ACCEPTED; the refused forms are the
      *>   g2002-constant-arith-* cases beside this one.
      *> §2002:13.9.3 GR2 - the class and category of a literal-1
      *>   constant is that of literal-1:
      *>   KS = "LIT" alphanumeric: MOVE KS TO WS X(5) pads right
      *>        -> "KS=[LIT  ]", and WS = KS compares equal (shorter
      *>        operand space-extended)             -> "KS-EQ"
      *>   KN = 12.5 numeric with scale: MOVE to WD 99V99 holds 12.50,
      *>        ADD KN (an ADD operand must be numeric) gives 25.00, and
      *>        MOVE WD to the edited WE PIC 99.99 shows it -> "KN=25.00"
      *>   KNAT = N"AB" national: MOVE to N(3) and compare with N"AB "
      *>        -> "NAT-EQ"
      *>   KCV FROM CV1 (>>DEFINE CV1 AS 42): numeric, ADD to 0 -> 42
      *>        -> "KCV=0042"
      *> §2002:13.9.2 SR2 - constant-names in literal positions (MOVE,
      *>   ADD, relation, arithmetic-expression operand) and, being
      *>   integers, as PICTURE repetitions: B1 PIC X(KA) is X(5) and
      *>   B2 PIC 9(KT) is 9(3), so CDREC after MOVE KS TO B1, MOVE KC
      *>   TO B2 is                                  -> "CDREC=[LIT  014]"
       >>DEFINE CV1 AS 42
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CON07.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WREC.
          05 WA PIC X(5).
          05 WN PIC N(3).
       01 GD PIC 9 VALUE 1.
       01 G.
          05 GH PIC X(2).
          05 GE PIC X(3) OCCURS 1 TO 4 DEPENDING ON GD.
       01 W1   PIC 9(4).
       01 W2   PIC 9(4).
       01 W3   PIC 9(4).
       01 W31  PIC 9(31).
       01 WS   PIC X(5).
       01 WD   PIC 99V99.
       01 WE   PIC 99.99.
       01 WNAT PIC N(3).
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  KA   CONSTANT AS LENGTH OF WA OF WREC.
       01  KNL  CONSTANT AS LENGTH OF WN IN WREC.
       01  KGL  CONSTANT AS LENGTH OF G.
       01  KB   CONSTANT AS BYTE-LENGTH OF WA IN WREC.
       01  KGB  CONSTANT AS BYTE-LENGTH OF G.
       01  KC   CONSTANT AS 2 + 3 * 4.
       01  KT   CONSTANT AS 7 / 2.
       01  K32  CONSTANT AS 1234567890123456789012345678901 * 1001
                             / 1001.
       01  KD   CONSTANT AS KC * 2.
       01  KS   CONSTANT AS "LIT".
       01  KN   CONSTANT AS 12.5.
       01  KNAT CONSTANT AS N"AB".
       01  KCV  CONSTANT FROM CV1.
       01  CDREC.
           05 B1 PIC X(KA).
           05 B2 PIC 9(KT).
       PROCEDURE DIVISION.
           MOVE KA TO W1
           MOVE KNL TO W2
           MOVE KGL TO W3
           DISPLAY "KA=" W1 " KNL=" W2 " KGL=" W3
           MOVE KB TO W1
           MOVE KGB TO W2
           DISPLAY "KB=" W1 " KGB=" W2
           MOVE KC TO W1
           MOVE KT TO W2
           MOVE KD TO W3
           DISPLAY "KC=" W1 " KT=" W2 " KD=" W3
           MOVE K32 TO W31
           DISPLAY "K32=" W31
           MOVE KS TO WS
           DISPLAY "KS=[" WS "]"
           IF WS = KS
               DISPLAY "KS-EQ"
           ELSE
               DISPLAY "KS-NE"
           END-IF
           MOVE KN TO WD
           ADD KN TO WD
           MOVE WD TO WE
           DISPLAY "KN=" WE
           MOVE KNAT TO WNAT
           IF WNAT = N"AB "
               DISPLAY "NAT-EQ"
           ELSE
               DISPLAY "NAT-NE"
           END-IF
           MOVE 0 TO W1
           ADD KCV TO W1
           DISPLAY "KCV=" W1
           MOVE KS TO B1
           MOVE KC TO B2
           DISPLAY "CDREC=[" CDREC "]"
           STOP RUN.
