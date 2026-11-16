      *>--- conformance-case
      *> id: g2str01-operand-usage-class
      *> rules: [2002-SR-14.8.39.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.39.2 SR1 - "All literals shall be described as
      *>   alphanumeric, boolean, or national literals, and all identifiers,
      *>   except identifier-4, shall be described implicitly or explicitly
      *>   as usage display or national. If any one of literal-1, literal-2,
      *>   identifier-1, identifier-2, or identifier-3 is of class national,
      *>   then all shall be of class national." Each STRING below obeys it
      *>   and is ACCEPTED:
      *>   ALN  alphanumeric literals and usage-display identifiers (PIC X,
      *>        an integer PIC 9(3), and IN-Q, a record item of a 2002
      *>        communication description, obsolete: COBOL0903 warning)
      *>        into PIC X; the delimiter is the alphanumeric literal "*";
      *>   BOOL a boolean literal B"101" beside "AB" into PIC X;
      *>   NAT  every operand of class national: N"..", PIC N(3), and PIC
      *>        Z9 USAGE NATIONAL (numeric-edited of usage national, class
      *>        national by §2002:8.5.2 Table 3), delimiter N"*", into PIC N;
      *>   NUMN PIC 9(3) USAGE NATIONAL beside alphanumeric literals into
      *>        PIC X: the item is usage national, which the first sentence
      *>        admits, and class numeric (§2002:8.5.2.9 item 1; kb/Work
      *>        PB5625 reading B), so no operand is of class national and
      *>        the second sentence does not apply.
      *> Values by §2002:14.8.39.3 GR1, GR3 and GR7 (characters moved left
      *>   to right without space filling; positions not reached keep their
      *>   prior content, "-" or "*"); the national-to-alphanumeric
      *>   correspondence of NUMN's digits is WiseOwl's documented identity
      *>   (docs/conformance/7/DOC-A.1-33.md):
      *>   ALN  "AB" (S1 up to "*") + "DEF" + "042" + "QQ" (IN-Q up to "*")
      *>        -> "ABDEF042QQ--"; BOOL "101" + "AB" -> "101AB-";
      *>   NAT  "XYZ" + " 7" + "AB" (N"AB*C" up to "*") -> "XYZ 7AB-";
      *>   NUMN "A" + "125" + "B" -> "A125B*".
      *> Refused beside it (not the closing witness): a COMP-3 identifier
      *>   (g2str02), a numeric literal (g2str03), an alphanumeric literal
      *>   beside national operands (g2str04) and PIC 9(3) USAGE NATIONAL
      *>   among national operands, class numeric beside class national
      *>   (g2str05).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2STR01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  S1              PIC X(4) VALUE "AB*C".
       01  S2              PIC X(3) VALUE "DEF".
       01  SN              PIC 9(3) VALUE 42.
       01  R               PIC X(12) VALUE ALL "-".
       01  RB              PIC X(6) VALUE ALL "-".
       01  NS              PIC N(3) VALUE N"XYZ".
       01  NZ              PIC Z9 USAGE NATIONAL.
       01  NR              PIC N(8) VALUE ALL N"-".
       01  N9              PIC 9(3) USAGE NATIONAL VALUE 125.
       01  RN              PIC X(6) VALUE ALL "*".
       COMMUNICATION SECTION.
       CD  IN-CD FOR INPUT.
       01  IN-REC.
           05  IN-Q        PIC X(4).
           05  IN-R        PIC X(83).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "QQ*Z" TO IN-Q
           MOVE 7 TO NZ
           STRING S1 DELIMITED BY "*" S2 SN DELIMITED BY SIZE
                  IN-Q DELIMITED BY "*" INTO R
           END-STRING
           DISPLAY "ALN=[" R "]"
           STRING B"101" "AB" DELIMITED BY SIZE INTO RB
           END-STRING
           DISPLAY "BOOL=[" RB "]"
           STRING NS NZ DELIMITED BY SIZE N"AB*C" DELIMITED BY N"*"
                  INTO NR
           END-STRING
           DISPLAY "NAT=[" FUNCTION DISPLAY-OF(NR) "]"
           STRING "A" N9 "B" DELIMITED BY SIZE INTO RN
           END-STRING
           DISPLAY "NUMN=[" RN "]"
           STOP RUN.
