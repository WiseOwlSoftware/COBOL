      *>--- conformance-case
      *> id: g2002-locale-date-function
      *> rules: [2002-AR-15.41.2-3, 2002-RV-15.41.3-1, 2002-RV-15.41.3-2, 2002-RV-15.41.3-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.41.2 AR3 - locale-name-1 is associated with a locale in
      *>   SPECIAL-NAMES (FR, GER, USA, XX); each use below is ACCEPTED
      *>   and runs. A compiler that refused a declared name fails to
      *>   compile this case.
      *> §2002:15.41.3 RV1 - a named locale formats the date (DATE-FR,
      *>   DATE-DE, NAMED-OVER-CUR: the name wins over the current LC_TIME);
      *>   omitted, the current locale is used: CUR-FR after SET LOCALE
      *>   LC_TIME TO FR, CUR-KEEP after switching only LC_COLLATE
      *>   (§2002:14.5.6, items 3 and 8: only LC_TIME of the current locale is
      *>   used, and an unswitched category keeps its locale), CUR-DE after
      *>   SET LOCALE LC_TIME TO GER. XX names an unavailable locale: the
      *>   EC-LOCALE-MISSING declarative runs (HANDLED-LOCALE-MISSING).
      *> §2002:15.41.3 RV2 - the date 2026-08-19 in each locale's d_fmt
      *>   (DESIGN-locale-facility DETERMINATION L10: the culture's short
      *>   date pattern): fr-FR dd/MM/yyyy "19/08/2026", de-DE dd.MM.yyyy
      *>   "19.08.2026", en-US M/d/yyyy "8/19/2026"; a national argument-1
      *>   formats the same date (DATE-NAT-FR).
      *> §2002:15.41.3 RV3 - the length follows the locale's format: 10
      *>   under fr-FR, 9 under en-US (LEN-FR, LEN-US).
      *> No line depends on the host's user default locale: the current
      *> locale is always set by SET LOCALE before an omitted locale-name.
       >>TURN EC-LOCALE-MISSING CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2LDT01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE FR IS "fr-FR"
           LOCALE GER IS "de-DE"
           LOCALE USA IS "en-US"
           LOCALE XX IS "xx-NOWHERE".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  D               PIC X(8) VALUE "20260819".
       01  DN              PIC N(8) VALUE N"20260819".
       01  S               PIC X(12).
       01  N               PIC 99.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-LOCALE-MISSING.
       H-P.
           IF FUNCTION EXCEPTION-STATUS = "EC-LOCALE-MISSING"
               DISPLAY "HANDLED-LOCALE-MISSING"
           ELSE
               DISPLAY "HANDLED-OTHER"
           END-IF
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE FUNCTION LOCALE-DATE(D FR) TO S
           DISPLAY "DATE-FR=[" S "]"
           MOVE FUNCTION LOCALE-DATE(D GER) TO S
           DISPLAY "DATE-DE=[" S "]"
           MOVE FUNCTION LOCALE-DATE(D USA) TO S
           DISPLAY "DATE-US=[" S "]"
           MOVE FUNCTION LOCALE-DATE(DN FR) TO S
           DISPLAY "DATE-NAT-FR=[" S "]"
           MOVE FUNCTION LENGTH(FUNCTION LOCALE-DATE(D FR)) TO N
           DISPLAY "LEN-FR=" N
           MOVE FUNCTION LENGTH(FUNCTION LOCALE-DATE(D USA)) TO N
           DISPLAY "LEN-US=" N
           SET LOCALE LC_TIME TO FR
           MOVE FUNCTION LOCALE-DATE(D) TO S
           DISPLAY "CUR-FR=[" S "]"
           SET LOCALE LC_COLLATE TO GER
           MOVE FUNCTION LOCALE-DATE(D) TO S
           DISPLAY "CUR-KEEP=[" S "]"
           MOVE FUNCTION LOCALE-DATE(D GER) TO S
           DISPLAY "NAMED-OVER-CUR=[" S "]"
           SET LOCALE LC_TIME TO GER
           MOVE FUNCTION LOCALE-DATE(D) TO S
           DISPLAY "CUR-DE=[" S "]"
           MOVE FUNCTION LOCALE-DATE(D XX) TO S
           DISPLAY "AFTER-MISSING"
           STOP RUN.
