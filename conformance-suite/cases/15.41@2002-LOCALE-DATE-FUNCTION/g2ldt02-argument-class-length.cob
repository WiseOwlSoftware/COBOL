      *>--- conformance-case
      *> id: g2ldt02-argument-class-length
      *> rules: [2002-AR-15.41.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.41.2 AR1 - "Argument-1 shall be of class alphanumeric or
      *>   national and shall be 8 character positions in length." Every
      *>   argument-1 below is of one of the two classes and 8 character
      *>   positions long, so each is ACCEPTED and formats the same date:
      *>   ALN-ITEM  PIC X(8) (class alphanumeric);
      *>   ALN-LIT   the alphanumeric literal "20260819";
      *>   ALN-GROUP an alphanumeric group of 8 positions (§2002:8.5.2: "an
      *>             alphanumeric group item has class and category
      *>             alphanumeric");
      *>   ALN-NE    PIC Z(7)9, numeric-edited of usage display, class
      *>             alphanumeric by §2002:8.5.2 Table 3 ("Numeric-edited (if
      *>             usage is display)");
      *>   NAT-ITEM  PIC N(8) (class national);
      *>   NAT-LIT   the national literal N"20260819";
      *>   NAT-NE    PIC Z(7)9 USAGE NATIONAL, numeric-edited of usage
      *>             national, class national by Table 3 ("Numeric-edited
      *>             (if usage is national)"); 8 character positions.
      *> Each returns the date in fr-FR's d_fmt (§2002:15.41.3 RV2;
      *>   DESIGN-locale-facility DETERMINATION L10, the culture's short
      *>   date pattern dd/MM/yyyy): "19/08/2026". A compiler that refused
      *>   any of these forms fails to compile the case; one that read the
      *>   argument as other than the 8 characters prints another date.
      *> The refusals beside it (not the closing witness): PIC 9(8) USAGE
      *>   NATIONAL, class numeric in 2002 (§2002:8.5.2.9 item 1; kb/Work
      *>   PB5625 reading B), g2ldt03-argument-numeric-national; and a
      *>   national item of 7 positions, g2ldt04-argument-national-seven.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2LDT02.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE FR IS "fr-FR".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  DX              PIC X(8) VALUE "20260819".
       01  DG.
           05  DG-Y        PIC X(4) VALUE "2026".
           05  DG-MD       PIC X(4) VALUE "0819".
       01  DZ              PIC Z(7)9.
       01  DN              PIC N(8) VALUE N"20260819".
       01  DNZ             PIC Z(7)9 USAGE NATIONAL.
       01  S               PIC X(12).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 20260819 TO DZ
           MOVE 20260819 TO DNZ
           MOVE FUNCTION LOCALE-DATE(DX FR) TO S
           DISPLAY "ALN-ITEM=[" S "]"
           MOVE FUNCTION LOCALE-DATE("20260819" FR) TO S
           DISPLAY "ALN-LIT=[" S "]"
           MOVE FUNCTION LOCALE-DATE(DG FR) TO S
           DISPLAY "ALN-GROUP=[" S "]"
           MOVE FUNCTION LOCALE-DATE(DZ FR) TO S
           DISPLAY "ALN-NE=[" S "]"
           MOVE FUNCTION LOCALE-DATE(DN FR) TO S
           DISPLAY "NAT-ITEM=[" S "]"
           MOVE FUNCTION LOCALE-DATE(N"20260819" FR) TO S
           DISPLAY "NAT-LIT=[" S "]"
           MOVE FUNCTION LOCALE-DATE(DNZ FR) TO S
           DISPLAY "NAT-NE=[" S "]"
           STOP RUN.
