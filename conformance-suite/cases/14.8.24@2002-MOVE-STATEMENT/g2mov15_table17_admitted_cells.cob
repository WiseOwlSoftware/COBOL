      *>--- conformance-case
      *> id: g2mov15_table17_admitted_cells
      *> rules: [2002-SR-14.8.24.2-8]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.24.2 SR8 - a sender of usage binary-char,
      *>   binary-short, binary-long or binary-double moves to a numeric
      *>   or numeric-edited item (line B); "otherwise, table 17,
      *>   Validity of types of MOVE statements, specifies the validity
      *>   of the move". Every Yes cell of Table 17 outside the
      *>   Type-name column is written once below and must be
      *>   ACCEPTED; each line prints the receivers so a cell that is
      *>   refused, or moved by the wrong arm, fails. (The Type-name
      *>   column's only Yes cell, Type-name -> Type-name, is line T;
      *>   its No cells are SR2's refusals.)
      *> Values (§2002:14.8.24.3 GR4; §2002:14.5.8 items 1, 3, 5, 6):
      *>   a character receiver is left-aligned with space fill; a
      *>   boolean receiver left-aligned with zero fill; a numeric one
      *>   aligned by decimal point with zero fill; an alphanumeric or
      *>   national sender into a numeric receiver is an unsigned
      *>   integer (GR4 b) 3.a); a boolean sender moves its boolean
      *>   value (GR4 a)); a numeric-edited sender is de-edited into a
      *>   numeric receiver (GR4 b)). Noninteger numeric receivers are
      *>   shown through an alphanumeric REDEFINES view of their digits.
      *> Cells: line 1 Alphabetic -> Alphabetic, Alphanumeric,
      *>   National; 2 Alphanumeric -> Alphabetic, Alphanumeric,
      *>   National, Numeric; 3 Alphanumeric -> Boolean; 4
      *>   Alphanumeric-edited -> Alphabetic, Alphanumeric, National;
      *>   5 Boolean -> Alphanumeric, Boolean, National; 6 National ->
      *>   National, Numeric, Boolean; 7 National-edited -> National;
      *>   8 Numeric integer -> Alphanumeric, National, Numeric; 9
      *>   Numeric noninteger -> Numeric, Numeric-edited; 10
      *>   Numeric-edited -> Alphanumeric, National, Numeric; 11
      *>   Alphanumeric -> Alphanumeric-edited, Numeric-edited; B the
      *>   binary senders; T Type-name -> Alphabetic, Alphanumeric,
      *>   Boolean, National, Numeric, Numeric-edited, Type-name.
      *>   §2002:8.5.2: the category of a strongly-typed group item is
      *>   its type-name. SG is a group, so each line-T move is not an
      *>   elementary move and is "treated exactly as if it were an
      *>   alphanumeric to alphanumeric elementary move" with no
      *>   conversion and no editing (§2002:14.8.24.3 GR2): every
      *>   receiver gets the 5 characters "10101" of SG. RN is not
      *>   displayed: its content is SG's bytes in a national item, an
      *>   implementor-defined image. The edited senders (lines 4,
      *>   7, 10) are moved to one receiver per statement: a
      *>   several-receiver MOVE of an edited sender is a separate
      *>   GR1 lead.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2MOV15.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TS TYPEDEF STRONG.
          05 TA PIC X(3).
          05 TB PIC 9(2).
       01 SG   TYPE TS.
       01 SG2  TYPE TS.
       01 SA   PIC A(3)      VALUE "ABC".
       01 SX   PIC X(3)      VALUE "123".
       01 SXB  PIC X(3)      VALUE "101".
       01 SXE  PIC XBX       VALUE "A B".
       01 SB   PIC 1(3)      VALUE B"101".
       01 SN   PIC N(3)      VALUE N"456".
       01 SNB  PIC N(3)      VALUE N"110".
       01 SNE  PIC NBN       VALUE N"C D".
       01 SI   PIC 9(3)      VALUE 123.
       01 SV   PIC 9V99      VALUE 1.25.
       01 SE   PIC ZZ9.99    VALUE "  4.75".
       01 BC   USAGE BINARY-CHAR   VALUE 7.
       01 BSH  USAGE BINARY-SHORT  VALUE -5.
       01 BL   USAGE BINARY-LONG   VALUE 1234.
       01 BD   USAGE BINARY-DOUBLE VALUE 99.
       01 RA   PIC A(5).
       01 RX   PIC X(5).
       01 RX6  PIC X(6).
       01 RXE  PIC XXBX.
       01 RB   PIC 1(5).
       01 RN   PIC N(5).
       01 RNE  PIC NNBN.
       01 R9   PIC 9(5).
       01 RV   PIC 99V999.
       01 RVX  REDEFINES RV PIC X(5).
       01 RV2  PIC 9(3)V99.
       01 RV2X REDEFINES RV2 PIC X(5).
       01 RE   PIC ZZ9.99.
       01 RS   PIC S9(3) SIGN LEADING SEPARATE.
       01 R6   PIC 9(6).
       01 RE5  PIC ZZZZ9.
       PROCEDURE DIVISION.
       MAIN.
           MOVE SA TO RA RX RN.
           DISPLAY "1 RA=[" RA "] RX=[" RX "] RN=[" RN "]".
           MOVE SX TO RA RX RN R9.
           DISPLAY "2 RA=[" RA "] RX=[" RX "] RN=[" RN "] R9=" R9.
           MOVE SXB TO RB.
           DISPLAY "3 RB=" RB.
           MOVE SXE TO RA.
           MOVE SXE TO RX.
           MOVE SXE TO RN.
           DISPLAY "4 RA=[" RA "] RX=[" RX "] RN=[" RN "]".
           MOVE SB TO RX RB RN.
           DISPLAY "5 RX=[" RX "] RB=" RB " RN=[" RN "]".
           MOVE SN TO RN R9.
           MOVE SNB TO RB.
           DISPLAY "6 RN=[" RN "] R9=" R9 " RB=" RB.
           MOVE SNE TO RN.
           DISPLAY "7 RN=[" RN "]".
           MOVE SI TO RX RN R9.
           DISPLAY "8 RX=[" RX "] RN=[" RN "] R9=" R9.
           MOVE SV TO RV RE.
           DISPLAY "9 RV=" RVX " RE=[" RE "]".
           MOVE SE TO RX6.
           MOVE SE TO RN.
           MOVE SE TO RV2.
           DISPLAY "10 RX6=[" RX6 "] RN=[" RN "] RV2=" RV2X.
           MOVE SX TO RXE RE.
           DISPLAY "11 RXE=[" RXE "] RE=[" RE "]".
           MOVE BC TO R9.
           MOVE BSH TO RS.
           MOVE BL TO RE.
           MOVE BD TO R6.
           DISPLAY "B R9=" R9 " RS=" RS " RE=[" RE "] R6=" R6.
           MOVE "101" TO TA OF SG.
           MOVE 01 TO TB OF SG.
           MOVE SG TO RA.
           MOVE SG TO RX.
           MOVE SG TO RB.
           MOVE SG TO RN.
           MOVE SG TO R9.
           MOVE SG TO RE5.
           MOVE SG TO SG2.
           DISPLAY "T RA=[" RA "] RX=[" RX "] RB=" RB " R9=" R9
               " RE=[" RE5 "] SG2=" TA OF SG2 TB OF SG2.
           STOP RUN.
