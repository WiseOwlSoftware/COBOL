      *> ISO §13.17.3 SR9 — "If the LOCALE phrase of the PICTURE clause
      *> is specified, the SIGN clause shall not be specified."
      *> The screen description entry (§13.17) is Annex A.4.2 item 20
      *> ("Screen description entry, format 1: group; format 2:
      *> elementary (13.17)"), an OPTIONAL element (§4.2.7) this
      *> implementation DECLINES (docs/CONFORMANCE.md §4 item 4), and
      *> A.4.1 makes its associated syntax rules optional with it.
      *> WITNESS: SM-1 writes the pair SR9 forbids. Under the decline no
      *> screen clause rule is enforced, so the violation is NOT
      *> separately diagnosed — but it is not silent either: the SCREEN
      *> SECTION draws the named COBOLNET1560 non-support warning
      *> (pinned by DocumentedNonSupportWitnessTests) and the facility
      *> is inert, so nothing about the entry reaches a device or value.
      *> The PICTURE format-2 spelling is the same one WORKING-STORAGE
      *> uses (§13.18.40.2; golden 2002/pb64t6_picture_locale_smoke).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SCRW02.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE US IS "en-US".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-M PIC 9(3) VALUE 4.
       SCREEN SECTION.
       01 SG-MON.
          05 SM-1 LINE 1 COL 1 PIC $9 LOCALE IS US SIZE IS 2
             SIGN IS LEADING SEPARATE FROM WS-M.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "M=" WS-M.
           DISPLAY "NO SCREEN OUTPUT".
           STOP RUN.
