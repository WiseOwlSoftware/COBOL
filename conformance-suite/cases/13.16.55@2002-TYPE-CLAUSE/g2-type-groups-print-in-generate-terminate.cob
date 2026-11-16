      *>--- conformance-case
      *> id: g2-type-groups-print-in-generate-terminate
      *> rules: [2002-GR-13.16.55.3-5]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.55.3 GR5 - "Report groups are printed only during
      *> the execution of a GENERATE or a TERMINATE statement. Detail
      *> report groups are printed when referenced explicitly in a
      *> GENERATE statement. All other report groups are printed
      *> implicitly according the general rules that follow."
      *> Each group's USE BEFORE REPORTING declarative DISPLAYs its name
      *> ("The declarative is invoked just before the named report group
      *> is produced", §2002:14.8.45.3 GR8), and the main line DISPLAYs a
      *> marker after each statement, so the output places every
      *> printing inside the statement that caused it.
      *> DERIVATION
      *>  INITIATE R prints nothing         -> "AFTER INITIATE" first
      *>  first GENERATE DA-G: RH as the first group (§2002:13.16.55.3
      *>  GR6a), PH before the first body group (GR6b), CH FINAL at the
      *>  first GENERATE after INITIATE (GR6c 1.), then DA (GR6d)
      *>                                    -> "  RH" "  PH" "  CH" "  DA"
      *>                                    -> "AFTER GENERATE 1"
      *>  second GENERATE DA-G: FINAL never breaks, so only the detail
      *>                                    -> "  DA" "AFTER GENERATE 2"
      *>  DB-G is never named in a GENERATE, so it is never printed
      *>  (no "  DB" anywhere)
      *>  TERMINATE R: CF FINAL (GR6e 2.), PF last on the page (GR6f),
      *>  "the page footing on the last page is immediately followed by
      *>  the report footing" (GR6)         -> "  CF" "  PF" "  RF"
      *>                                    -> "AFTER TERMINATE"
      *>  R2 is INITIATEd and TERMINATEd with no GENERATE: its RH is
      *>  printed only at a first GENERATE (GR6a) and its RF only if a
      *>  GENERATE was executed since INITIATE (GR6g), so nothing
      *>                                    -> "R2 DONE" alone
      *> A leg fails if a group prints at INITIATE, outside a GENERATE or
      *> TERMINATE, or a detail prints without its own GENERATE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2TYP13.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "G2TYP13.TXT".
           SELECT PRT2 ASSIGN TO "G2TYP13B.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R.
       FD  PRT2 REPORT IS R2.
       REPORT SECTION.
       RD  R CONTROL IS FINAL
           PAGE LIMIT IS 20 LINES HEADING 1 FIRST DETAIL 3
           LAST DETAIL 15 FOOTING 18.
       01  RH-G TYPE REPORT HEADING LINE 1.
           03  COLUMN 1 PIC XX VALUE "RH".
       01  PH-G TYPE PAGE HEADING LINE 2.
           03  COLUMN 1 PIC XX VALUE "PH".
       01  CH-G TYPE CONTROL HEADING FINAL LINE PLUS 1.
           03  COLUMN 1 PIC XX VALUE "CH".
       01  DA-G TYPE DETAIL LINE PLUS 1.
           03  COLUMN 1 PIC XX VALUE "DA".
       01  DB-G TYPE DETAIL LINE PLUS 1.
           03  COLUMN 1 PIC XX VALUE "DB".
       01  CF-G TYPE CONTROL FOOTING FINAL LINE PLUS 1.
           03  COLUMN 1 PIC XX VALUE "CF".
       01  PF-G TYPE PAGE FOOTING LINE 19.
           03  COLUMN 1 PIC XX VALUE "PF".
       01  RF-G TYPE REPORT FOOTING LINE PLUS 1.
           03  COLUMN 1 PIC XX VALUE "RF".
       RD  R2 PAGE LIMIT IS 10 LINES HEADING 1 FIRST DETAIL 3
           LAST DETAIL 6 FOOTING 8.
       01  RH2-G TYPE RH LINE 1.
           03  COLUMN 1 PIC XXX VALUE "RH2".
       01  DE2-G TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC XXX VALUE "DE2".
       01  RF2-G TYPE RF LINE PLUS 1.
           03  COLUMN 1 PIC XXX VALUE "RF2".
       PROCEDURE DIVISION.
       DECLARATIVES.
       U-RH SECTION.
           USE BEFORE REPORTING RH-G.
       U-RH-1.
           DISPLAY "  RH".
       U-PH SECTION.
           USE BEFORE REPORTING PH-G.
       U-PH-1.
           DISPLAY "  PH".
       U-CH SECTION.
           USE BEFORE REPORTING CH-G.
       U-CH-1.
           DISPLAY "  CH".
       U-DA SECTION.
           USE BEFORE REPORTING DA-G.
       U-DA-1.
           DISPLAY "  DA".
       U-DB SECTION.
           USE BEFORE REPORTING DB-G.
       U-DB-1.
           DISPLAY "  DB".
       U-CF SECTION.
           USE BEFORE REPORTING CF-G.
       U-CF-1.
           DISPLAY "  CF".
       U-PF SECTION.
           USE BEFORE REPORTING PF-G.
       U-PF-1.
           DISPLAY "  PF".
       U-RF SECTION.
           USE BEFORE REPORTING RF-G.
       U-RF-1.
           DISPLAY "  RF".
       U-RH2 SECTION.
           USE BEFORE REPORTING RH2-G.
       U-RH2-1.
           DISPLAY "  RH2".
       U-RF2 SECTION.
           USE BEFORE REPORTING RF2-G.
       U-RF2-1.
           DISPLAY "  RF2".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-1.
           OPEN OUTPUT PRT PRT2.
           INITIATE R.
           DISPLAY "AFTER INITIATE".
           GENERATE DA-G.
           DISPLAY "AFTER GENERATE 1".
           GENERATE DA-G.
           DISPLAY "AFTER GENERATE 2".
           TERMINATE R.
           DISPLAY "AFTER TERMINATE".
           INITIATE R2.
           TERMINATE R2.
           DISPLAY "R2 DONE".
           CLOSE PRT PRT2.
           STOP RUN.
