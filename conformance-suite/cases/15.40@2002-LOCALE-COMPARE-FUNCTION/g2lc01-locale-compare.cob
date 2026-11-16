      *>--- conformance-case
      *> id: g2lc01-locale-compare
      *> rules: [2002-AR-15.40.2-3, 2002-AR-15.40.2-4, 2002-RV-15.40.3-1, 2002-RV-15.40.3-2, 2002-RV-15.40.3-3, 2002-RV-15.40.3-4, 2002-RV-15.40.3-5, 2002-RV-15.40.3-6]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.40 LOCALE-COMPARE, judged on the 2002 text at --std 2002.
      *> §2002:15.40.2 AR4 - USA, DAN and XX are locale-names associated
      *>   with locales by SPECIAL-NAMES LOCALE clauses (§2002:12.2.6.3 GR5)
      *>   and are accepted as locale-name-1; the program compiles and runs.
      *> §2002:15.40.3 RV5 - EQ/LT/GT: "abc" = "abc", "abc" < "abd",
      *>   "abd" > "abc" give '=', '<', '>'.
      *> §2002:15.40.2 AR3 - "Arguments may be of different classes":
      *>   LT/GT pair an alphanumeric with an alphabetic item; NAT-ALN,
      *>   ALN-NAT and NAT-ALPHA pair a national item with an alphanumeric
      *>   or alphabetic one; each is accepted and compared.
      *> §2002:15.40.3 RV1 - the non-national operand is converted to
      *>   national, so N"abc " and "abc   " compare '=' and N"abc " to
      *>   "abd " compares '<'.
      *> §2002:15.40.3 RV2 - TRAIL/TRAIL-LIT: trailing spaces are truncated,
      *>   so "abc   " = "abc" and "ab   " = "ab" (untruncated, the longer
      *>   operand would be '>'); SPACES: two all-space operands of
      *>   lengths 5 and 1 are each truncated to a single space: '='.
      *>   SPACES-ONE: the all-space SP5 becomes ONE space, not nothing:
      *>   against LV (X"01", completely ignorable in en-US, RV4) a single
      *>   space is greater, '>'; an operand truncated to nothing would
      *>   compare '='.
      *> §2002:15.40.3 RV4 - cultural ordering, not character by character:
      *>   CULT-aB: en-US orders a before B, '<' (the code-point order
      *>   would give '>'); DA-aa-z: Danish orders "aa" (aa = a-ring) after
      *>   z, '>', where US-aa-z gives '<' for the same operands.
      *> §2002:15.40.3 RV3 - with locale-name-1 the named locale is used
      *>   (US-aa-z '<', DA-aa-z '>'); without it the current locale is
      *>   used (§2002:14.5.6 rule 7): CUR-aa-z under the user default
      *>   locale (the harness's root; any non-Danish locale gives '<'),
      *>   and after SET LOCALE LC_COLLATE TO DAN (§2002:14.8.35.3 GR20,
      *>   §2002:14.5.6 rule 3: the new locale becomes the current locale)
      *>   SET-aa-z gives '>' while SET-US-aa-z, naming USA, stays '<'.
      *>   XX's locale is not available: EC-LOCALE-MISSING is set to
      *>   exist; checking is on, so the declarative runs (§2002:14.5.12.1.2
      *>   item 2) and RESUME AT NEXT STATEMENT continues after the MOVE
      *>   (§2002:14.8.31.3 GR2): HANDLED= then AFTER-MISSING. R is not
      *>   displayed: the interrupted statement's receiving item is not
      *>   specified by the rule.
      *> §2002:15.40.3 RV6 - LEN: FUNCTION LENGTH of the result is 1.
       >>TURN EC-LOCALE-MISSING CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2LC01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE USA IS "en-US"
           LOCALE DAN IS "da-DK"
           LOCALE XX IS "xx-NOWHERE".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X6              PIC X(6) VALUE "abc".
       01  X3              PIC X(3) VALUE "abc".
       01  A4              PIC A(4) VALUE "abd".
       01  N4              PIC N(4) VALUE N"abc".
       01  SP5             PIC X(5) VALUE SPACES.
       01  SP1             PIC X(1) VALUE SPACE.
       01  LV              PIC X VALUE X"01".
       01  C               PIC X VALUE "?".
       01  L               PIC 9.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-LOCALE-MISSING.
       H-P.
           DISPLAY "HANDLED=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE FUNCTION LOCALE-COMPARE(X3 X3 USA) TO C
           DISPLAY "EQ=" C
           MOVE FUNCTION LOCALE-COMPARE(X3 A4 USA) TO C
           DISPLAY "LT=" C
           MOVE FUNCTION LOCALE-COMPARE(A4 X3 USA) TO C
           DISPLAY "GT=" C
           MOVE FUNCTION LOCALE-COMPARE(N4 X6 USA) TO C
           DISPLAY "NAT-ALN=" C
           MOVE FUNCTION LOCALE-COMPARE(X6 N4 USA) TO C
           DISPLAY "ALN-NAT=" C
           MOVE FUNCTION LOCALE-COMPARE(N4 A4 USA) TO C
           DISPLAY "NAT-ALPHA=" C
           MOVE FUNCTION LOCALE-COMPARE(X6 X3 USA) TO C
           DISPLAY "TRAIL=" C
           MOVE FUNCTION LOCALE-COMPARE("ab   " "ab" USA) TO C
           DISPLAY "TRAIL-LIT=" C
           MOVE FUNCTION LOCALE-COMPARE(SP5 SP1 USA) TO C
           DISPLAY "SPACES=" C
           MOVE FUNCTION LOCALE-COMPARE(SP5 LV USA) TO C
           DISPLAY "SPACES-ONE=" C
           MOVE FUNCTION LOCALE-COMPARE("a" "B" USA) TO C
           DISPLAY "CULT-aB=" C
           MOVE FUNCTION LOCALE-COMPARE("aa" "z" USA) TO C
           DISPLAY "US-aa-z=" C
           MOVE FUNCTION LOCALE-COMPARE("aa" "z" DAN) TO C
           DISPLAY "DA-aa-z=" C
           MOVE FUNCTION LOCALE-COMPARE("aa" "z") TO C
           DISPLAY "CUR-aa-z=" C
           SET LOCALE LC_COLLATE TO DAN
           MOVE FUNCTION LOCALE-COMPARE("aa" "z") TO C
           DISPLAY "SET-aa-z=" C
           MOVE FUNCTION LOCALE-COMPARE("aa" "z" USA) TO C
           DISPLAY "SET-US-aa-z=" C
           MOVE FUNCTION LENGTH(FUNCTION LOCALE-COMPARE(X6 X3)) TO L
           DISPLAY "LEN=" L
           DISPLAY "BEFORE-MISSING"
           MOVE FUNCTION LOCALE-COMPARE(X3 X3 XX) TO C
           DISPLAY "AFTER-MISSING"
           STOP RUN.
