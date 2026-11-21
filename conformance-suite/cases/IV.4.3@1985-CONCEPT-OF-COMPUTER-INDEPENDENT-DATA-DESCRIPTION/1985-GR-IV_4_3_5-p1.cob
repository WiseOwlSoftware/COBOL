      *>--- conformance-case
      *> id: 1985-GR-IV_4_3_5-p1
      *> rules: [1985-GR-IV.4.3.5-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.5 p1 (CONCEPT OF COMPUTER INDEPENDENT DATA
      *> DESCRIPTION). An output test: the program must translate and
      *> run at 85 and DISPLAY exactly the lines its .out holds, the
      *> outcome the rule requires. Placed from written and run by the
      *> PB5453 reader of lineage-reads-1985/s-04.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * 1985-GR-IV.4.3.5-p1: operational signs on signed numeric items
      * and literals carry the algebraic value; editing signs show the
      * sign in an edited item.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SIGNS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  S-NUM             PIC S9(3) VALUE -12.
       01  U-NUM             PIC 9(3).
       01  ED-MINUS          PIC -ZZ9.
       01  ED-PLUS           PIC +ZZ9.
       01  ED-CR             PIC ZZ9CR.
       PROCEDURE DIVISION.
       MAIN-PARA.
           ADD -5 TO S-NUM.
           MOVE S-NUM TO ED-MINUS ED-PLUS ED-CR.
           DISPLAY "MINUS=[" ED-MINUS "] PLUS=[" ED-PLUS "] CR=[" ED-CR
               "]".
           MOVE S-NUM TO U-NUM.
           DISPLAY "UNSIGNED=" U-NUM.
           IF S-NUM < 0 DISPLAY "NEGATIVE OK".
           MOVE 7 TO S-NUM.
           MOVE S-NUM TO ED-PLUS ED-CR.
           DISPLAY "PLUS=[" ED-PLUS "] CR=[" ED-CR "]".
           STOP RUN.
