*> reject-at: 85 2002 2014 2023
*> ISO 1989:2023 Annex A.4.8 item 2) "SELECT WHEN clause (13.18.51)" - the module's second item, DECLINED
*> (docs/CONFORMANCE.md section 5, row A.4.8), refused by name with COBOLNET1705. 13.18.51.2's printed
*> general format (RENDERED, PDF p481) is `SELECT WHEN { condition-name-1 | OTHER }` - plain braces, NO
*> choice indicators, so exactly one of the two. This witness is the condition-name-1 arm.
*> Refused rather than accepted inert because 13.18.51.4 GR1/GR2 make the clause decide WHICH record
*> description entry is selected: compiled inert it would silently select the wrong one, and 9.1.13.7
*> rule 5 (I-O status 45, record identification failure) is the path that then cannot be reached.
*> 13.18.51.3 rule 1 admits the clause at the 01 level of the file, linkage, local-storage OR
*> working-storage section; the witness uses working-storage so the refusal is provably NOT keyed to an FD.
IDENTIFICATION DIVISION.
PROGRAM-ID. A48SWC9AL.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 REC-A SELECT WHEN COND-A.
   05 R-KIND PIC X.
      88 COND-A VALUE "A".
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
