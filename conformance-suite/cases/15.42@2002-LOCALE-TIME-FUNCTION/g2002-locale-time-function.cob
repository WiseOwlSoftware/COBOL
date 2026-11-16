      *>--- conformance-case
      *> id: g2002-locale-time-function
      *> rules: [2002-AR-15.42.2-2, 2002-AR-15.42.2-3, 2002-AR-15.42.2-4, 2002-RV-15.42.3-1, 2002-RV-15.42.3-2, 2002-RV-15.42.3-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.42.2 AR4 - locale-name-1 is associated with a locale in
      *>   SPECIAL-NAMES (FR, GER, USA, XX); each use is ACCEPTED and runs.
      *> §2002:15.42.2 AR2 - argument-1 is HHMMSS as in CURRENT-DATE
      *>   positions 9-14 (§2002:15.16.2 RV1: minutes 00 through 59) and
      *>   must be valid: minutes 60 ("236000") and non-digits ("12AB00")
      *>   are incorrect values, so EC-ARGUMENT-FUNCTION is set to exist
      *>   (§2002:15.2) and its declarative runs (HANDLED-ARGUMENT-FUNCTION).
      *> §2002:15.42.2 AR3 - this function's own exceptions to the
      *>   CURRENT-DATE ranges: hours 00 through 24 ("240000" formats as
      *>   24:00:00) and seconds 00 through 99 ("235999" as 23:59:99);
      *>   hours 25 ("250000") is outside them: EC-ARGUMENT-FUNCTION.
      *> §2002:15.42.3 RV1 - a named locale formats the time (TIME-DE,
      *>   TIME-US, NAMED-OVER-CUR); omitted, the current LC_TIME is used
      *>   (CUR-US after SET LOCALE LC_TIME TO USA; CUR-KEEP after only
      *>   LC_COLLATE is switched, §2002:14.5.6, items 3 and 8); XX names an
      *>   unavailable locale: HANDLED-LOCALE-MISSING.
      *> §2002:15.42.3 RV2 - 13:05:09 in each locale's t_fmt
      *>   (DESIGN-locale-facility DETERMINATION L10: the culture's long
      *>   time pattern): de-DE and fr-FR HH:mm:ss "13:05:09", en-US
      *>   h:mm:ss tt "1:05:09 PM"; a national argument-1 (TIME-NAT-DE).
      *> §2002:15.42.3 RV3 - the length follows the locale's format: 8
      *>   under de-DE, 10 under en-US (LEN-DE, LEN-US).
      *> No line depends on the host's user default locale.
       >>TURN EC-LOCALE-MISSING EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2LTM01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE FR IS "fr-FR"
           LOCALE GER IS "de-DE"
           LOCALE USA IS "en-US"
           LOCALE XX IS "xx-NOWHERE".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T               PIC X(6) VALUE "130509".
       01  TN              PIC N(6) VALUE N"130509".
       01  S               PIC X(12).
       01  N               PIC 99.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-LOCALE-MISSING
                                         EC-ARGUMENT-FUNCTION.
       H-P.
           EVALUATE FUNCTION EXCEPTION-STATUS
             WHEN "EC-LOCALE-MISSING"
               DISPLAY "HANDLED-LOCALE-MISSING"
             WHEN "EC-ARGUMENT-FUNCTION"
               DISPLAY "HANDLED-ARGUMENT-FUNCTION"
             WHEN OTHER
               DISPLAY "HANDLED-OTHER"
           END-EVALUATE
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE FUNCTION LOCALE-TIME(T GER) TO S
           DISPLAY "TIME-DE=[" S "]"
           MOVE FUNCTION LOCALE-TIME(T FR) TO S
           DISPLAY "TIME-FR=[" S "]"
           MOVE FUNCTION LOCALE-TIME(T USA) TO S
           DISPLAY "TIME-US=[" S "]"
           MOVE FUNCTION LOCALE-TIME(TN GER) TO S
           DISPLAY "TIME-NAT-DE=[" S "]"
           MOVE FUNCTION LENGTH(FUNCTION LOCALE-TIME(T GER)) TO N
           DISPLAY "LEN-DE=" N
           MOVE FUNCTION LENGTH(FUNCTION LOCALE-TIME(T USA)) TO N
           DISPLAY "LEN-US=" N
           SET LOCALE LC_TIME TO USA
           MOVE FUNCTION LOCALE-TIME(T) TO S
           DISPLAY "CUR-US=[" S "]"
           SET LOCALE LC_COLLATE TO GER
           MOVE FUNCTION LOCALE-TIME(T) TO S
           DISPLAY "CUR-KEEP=[" S "]"
           MOVE FUNCTION LOCALE-TIME(T GER) TO S
           DISPLAY "NAMED-OVER-CUR=[" S "]"
           MOVE FUNCTION LOCALE-TIME("240000" GER) TO S
           DISPLAY "TIME-24=[" S "]"
           MOVE FUNCTION LOCALE-TIME("235999" GER) TO S
           DISPLAY "TIME-99=[" S "]"
           MOVE FUNCTION LOCALE-TIME("250000" GER) TO S
           DISPLAY "AFTER-HOUR-25"
           MOVE FUNCTION LOCALE-TIME("236000" GER) TO S
           DISPLAY "AFTER-MINUTE-60"
           MOVE FUNCTION LOCALE-TIME("12AB00" GER) TO S
           DISPLAY "AFTER-NON-DIGIT"
           MOVE FUNCTION LOCALE-TIME(T XX) TO S
           DISPLAY "AFTER-MISSING"
           STOP RUN.
