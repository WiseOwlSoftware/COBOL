      *>--- conformance-case
      *> id: g2-type-page-groups-page-limit
      *> rules: [2002-SR-13.16.55.2-12]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.55.2 SR12 - "PAGE HEADING and PAGE FOOTING and the
      *> OR PAGE phrase are allowed only if a PAGE clause that defines
      *> the page limit is specified in the report description entry."
      *> The granted form: R has PAGE LIMIT 6, so a PH, a PF and a
      *> CH ... OR PAGE are all written, accepted and printed.
      *> Layout: HEADING 1, FIRST DETAIL 2, LAST DETAIL 4, FOOTING 5.
      *>  PH LINE 1; CH ON CTL OR PAGE LINE PLUS 1; DE LINE 3 (absolute,
      *>  at the upper limit of a detail, the line after the OR PAGE
      *>  heading, §2002:13.16.55.3 GR7); PF LINE 6 (FOOTING + 1).
      *> Each group's USE BEFORE REPORTING procedure displays its name
      *> and PAGE-COUNTER ("invoked just before the named report group
      *> is produced", §2002:14.8.45.3 GR8); LINE-COUNTER is shown after
      *> each GENERATE ("the line number within the page of the most
      *> recent line to have been printed", §2002:13.16.33.3 GR1).
      *> DERIVATION:
      *>  INITIATE: PAGE-COUNTER 1 (§2002:8.4.2.14.3 GR2).
      *>  GENERATE 1 (first body group, no page fit test): the page
      *>   heading "is printed immediately before, and on the same page
      *>   as, the chronologically first body group" (§2002:13.16.55.3
      *>   GR6 b) -> "PH 1"; CH on the first GENERATE (GR6 c 1.) ->
      *>   "CH 1"; detail -> "DE 1" at line 3 (§2002:13.16.33.3 GR5 a)
      *>   -> "LC 3".
      *>  GENERATE 2: the detail's USE procedure is performed "Before
      *>   any page fit processing, if the associated report group is a
      *>   body group" (§2002:14.8.45.3 GR9 c) -> "DE 1". Absolute first
      *>   LINE 3 is not greater than LINE-COUNTER 3, so the page fit is
      *>   declared unsuccessful (§2002:13.16.33.3 GR4 b): page advance
      *>   (§2002:14.8.15.3 GR6): "If a page footing is defined it is
      *>   printed" -> "PF 1"; PAGE-COUNTER becomes 2; PH -> "PH 2";
      *>   "The OR PAGE phrase causes the associated control heading to
      *>   be printed in addition after each page advance"
      *>   (§2002:13.16.55.3 GR6 c) -> "CH 2"; detail at line 3 -> "LC 3".
      *>  TERMINATE: no control footing; the page footing ends the last
      *>   page (GR6 f) -> "PF 2".
      *> Fails if PH, PF or OR PAGE is refused with a page limit
      *> present, or if any of them is not printed where GR6 puts it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2TYP12.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "G2TYP12.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R.
       WORKING-STORAGE SECTION.
       01  CTL     PIC X     VALUE "A".
       01  WS-PC   PIC 9.
       01  WS-LC   PIC 9.
       REPORT SECTION.
       RD  R CONTROL IS CTL
           PAGE LIMIT IS 6 LINES HEADING 1 FIRST DETAIL 2
           LAST DETAIL 4 FOOTING 5.
       01  PH-G TYPE PAGE HEADING LINE 1.
           03  COLUMN 1 PIC XX VALUE "PH".
       01  CH-G TYPE CONTROL HEADING ON CTL OR PAGE LINE PLUS 1.
           03  COLUMN 1 PIC XX VALUE "CH".
       01  DE-G TYPE DETAIL LINE 3.
           03  COLUMN 1 PIC XX VALUE "DE".
       01  PF-G TYPE PAGE FOOTING LINE 6.
           03  COLUMN 1 PIC XX VALUE "PF".
       PROCEDURE DIVISION.
       DECLARATIVES.
       U-PH SECTION.
           USE BEFORE REPORTING PH-G.
       U-PH-1.
           MOVE PAGE-COUNTER TO WS-PC.
           DISPLAY "PH " WS-PC.
       U-CH SECTION.
           USE BEFORE REPORTING CH-G.
       U-CH-1.
           MOVE PAGE-COUNTER TO WS-PC.
           DISPLAY "CH " WS-PC.
       U-DE SECTION.
           USE BEFORE REPORTING DE-G.
       U-DE-1.
           MOVE PAGE-COUNTER TO WS-PC.
           DISPLAY "DE " WS-PC.
       U-PF SECTION.
           USE BEFORE REPORTING PF-G.
       U-PF-1.
           MOVE PAGE-COUNTER TO WS-PC.
           DISPLAY "PF " WS-PC.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-1.
           OPEN OUTPUT PRT.
           INITIATE R.
           GENERATE DE-G.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "LC " WS-LC.
           GENERATE DE-G.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "LC " WS-LC.
           TERMINATE R.
           CLOSE PRT.
           STOP RUN.
