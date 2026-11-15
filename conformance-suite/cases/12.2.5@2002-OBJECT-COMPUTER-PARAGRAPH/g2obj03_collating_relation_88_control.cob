      *>--- conformance-case
      *> id: g2obj03_collating_relation_88_control
      *> rules: [2002-GR-12.2.5.3-11]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.2.5.3 GR11 - "The alphanumeric program collating
      *> sequence and national program collating sequence are used to
      *> determine the truth value of any alphanumeric comparisons and
      *> national comparisons, respectively, that are: a) Explicitly
      *> specified in relation conditions. b) Explicitly specified in
      *> condition-name conditions. c) Implicitly specified by the
      *> presence of a CONTROL clause in a report description entry."
      *>   cite.py --check "§2002:12.2.5.3 GR11" "Explicitly specified
      *>   in relation conditions" -> OK
      *>   cite.py --check "§2002:12.2.5.3 GR11" "Implicitly specified
      *>   by the presence of a CONTROL clause in a report description
      *>   entry" -> OK
      *> The sequences: ALPHABET AREV IS "B" "A" ALSO "X" and ALPHABET
      *> NREV FOR NATIONAL IS N"B" N"A". By §2002:12.2.6.3 GR7k the
      *> literals take ascending positions in order of appearance (B 1,
      *> A 2), ALSO puts "X" at the same position as "A", and every
      *> unnamed character follows them in native order.
      *> a) relation, alphanumeric:
      *>   WS-REL  "A" > "B"        true under AREV   -> REV
      *>   WS-EQ   "A" = "X"        same position: equal -> REV
      *>   CD-REL  the CD field IN-Q (SYMBOLIC QUEUE) = "A" > "B" -> REV
      *> a) relation, national:
      *>   NAT-REL N"A" > N"B"      true under NREV   -> REV
      *> b) condition-name, 88 VALUE "A" THRU "C" holding "B":
      *>   "B" (1) is below "A" (2), outside the range -> false -> REV
      *>   WS-88 on working storage, CD-88 on the record under the CD
      *> c) CONTROL: RD R1 CONTROL IS CK; GENERATE with CK = "A", "X",
      *>   "B". §2002:13.16.15.3 GR3 tests each control "for equality
      *>   with the corresponding prior control"; under AREV "X" equals
      *>   the prior "A", so the second GENERATE breaks nothing and the
      *>   third ("B") breaks; TERMINATE breaks once more (GR5). The
      *>   USE BEFORE REPORTING section of the control footing prints
      *>   CTL-BREAK each time the footing is produced:
      *>   CTL-GEN-A CTL-GEN-X CTL-GEN-B CTL-BREAK CTL-TERM CTL-BREAK
      *> Every leg prints NAT (or breaks at "X" too) under the native
      *> sequence. The communication facility is obsolete at 2002
      *> (COBOL0903 warning).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OBJ03.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X
           PROGRAM COLLATING SEQUENCE
               FOR ALPHANUMERIC IS AREV
               FOR NATIONAL IS NREV.
       SPECIAL-NAMES.
           ALPHABET AREV IS "B" "A" ALSO "X"
           ALPHABET NREV FOR NATIONAL IS N"B" N"A".
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "g2obj03.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  W-A             PIC X VALUE "A".
       01  W-C             PIC X VALUE "B".
           88 W-IN-AC      VALUE "A" THRU "C".
       01  N-A             PIC N VALUE N"A".
       01  CK              PIC X.
       COMMUNICATION SECTION.
       CD  IN-CD FOR INPUT
           SYMBOLIC QUEUE IS IN-Q
           MESSAGE COUNT IS IN-CNT.
       01  IN-REC.
           05 IN-K         PIC X.
              88 IN-AC     VALUE "A" THRU "C".
           05 FILLER       PIC X(86).
       REPORT SECTION.
       RD  R1 CONTROL IS CK.
       01  R1-DET TYPE DETAIL.
           05 LINE PLUS 1.
              10 COLUMN 1 PIC X SOURCE CK.
       01  R1-CF TYPE CONTROL FOOTING CK.
           05 LINE PLUS 1.
              10 COLUMN 1 PIC X(5) VALUE "BREAK".
       PROCEDURE DIVISION.
       DECLARATIVES.
       CF-SECT SECTION.
           USE BEFORE REPORTING R1-CF.
       CF-PARA.
           DISPLAY "CTL-BREAK".
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN-PARA.
           IF W-A > "B" DISPLAY "WS-REL=REV"
           ELSE DISPLAY "WS-REL=NAT" END-IF
           IF W-A = "X" DISPLAY "WS-EQ=REV"
           ELSE DISPLAY "WS-EQ=NAT" END-IF
           MOVE "A" TO IN-Q
           IF IN-Q > "B" DISPLAY "CD-REL=REV"
           ELSE DISPLAY "CD-REL=NAT" END-IF
           IF N-A > N"B" DISPLAY "NAT-REL=REV"
           ELSE DISPLAY "NAT-REL=NAT" END-IF
           IF W-IN-AC DISPLAY "WS-88=NAT"
           ELSE DISPLAY "WS-88=REV" END-IF
           MOVE "B" TO IN-K
           IF IN-AC DISPLAY "CD-88=NAT"
           ELSE DISPLAY "CD-88=REV" END-IF
           OPEN OUTPUT RPT
           INITIATE R1
           MOVE "A" TO CK
           DISPLAY "CTL-GEN-A"
           GENERATE R1-DET
           MOVE "X" TO CK
           DISPLAY "CTL-GEN-X"
           GENERATE R1-DET
           MOVE "B" TO CK
           DISPLAY "CTL-GEN-B"
           GENERATE R1-DET
           DISPLAY "CTL-TERM"
           TERMINATE R1
           CLOSE RPT
           STOP RUN.
