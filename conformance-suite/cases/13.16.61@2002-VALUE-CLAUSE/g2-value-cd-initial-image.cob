      *>--- conformance-case
      *> id: g2-value-cd-initial-image
      *> rules: [2002-GR-13.16.61.3-4, 2002-GR-13.16.61.3-6, 2002-GR-13.16.61.3-7, 2002-GR-13.16.61.3-8, 2002-GR-13.16.61.3-12, 2002-GR-13.16.61.3-13, 2002-GR-13.16.61.3-14, 2002-GR-13.16.61.3-17, 2002-SR-13.16.61.2-8]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> Format 1, 2 and 3 VALUE clauses in the record description
      *> entry under an output CD. The 2002 VALUE clause governs
      *> communication section items (§2002:13.16.61 opening
      *> paragraph); the record is the CD's only record, so it may
      *> carry VALUE clauses. COBOL0903 (the communication facility
      *> is obsolete) is a warning, not a refusal.
      *> INIT  §2002:13.16.61.3 GR4 - communication-section VALUEs
      *>       take effect when the program is placed in initial state
      *>       (every line before AFTER-INIT) and during INITIALIZE
      *>       ... ALL TO VALUE (AFTER-INIT restores A1, E3, J1, B2 and
      *>       N2 after they were overwritten). Fails if a CD record is
      *>       not seeded, or INITIALIZE does not re-apply the VALUE.
      *> N1 N3 §2002:13.16.61.2 SR2 accepted legs - 1200 for 99PP and
      *>       .0012 for VPP99 have zero in every P position and need no
      *>       truncation: N1 moved to 9(4) is 1200, N3 * 10000 is 0012.
      *> N2    §2002:13.16.61.3 GR7 with §2002:14.5.8 rule 1a - 1.5 in
      *>       9(3)V99 aligns by decimal point with zero fill, so N2 * 100
      *>       is 150 (shown through 9(4): 0150; DISPLAY of N2 itself is an
      *>       implementor conversion, §2002:14.8.10.3 GR1, so not shown).
      *> E1 E3 E4  §2002:13.16.61.2 SR8 / GR7 - editing characters count
      *>       in the size but no editing happens: XX/XX holds "ABCD "
      *>       (5 positions, '/' not inserted), ZZ9 holds "007" (no zero
      *>       suppression), ZZ9.99 holds "001.50" (SR4: numeric-edited
      *>       of usage display is class alphanumeric, alphanumeric
      *>       literal). Fails if the literal is edited on initialization.
      *> E2    §2002:13.16.61.2 SR5 accepted leg - numeric-edited of usage
      *>       national is class national and takes N"1.00".
      *> BL    §2002:13.16.61.2 SR6 accepted leg - B"10" in PIC 1(2).
      *> B1 B2 §2002:13.16.61.3 GR8 - BLANK WHEN ZERO does not blank the
      *>       initial value: ZERO fills ZZ9.99 with six zeros
      *>       (§2002:8.3.1.2.5.3 GR2b), "000.00" stays "000.00".
      *> J1    §2002:13.16.61.3 GR7 - JUSTIFIED RIGHT is ignored: "AB   ".
      *> T1 T5 T4 T6  §2002:13.16.61.3 GR12/GR13/GR14 - T1 "P" FROM (1)
      *>       TO (2) "Q" FROM (3) is PPQ; T4 "A" "B" FROM (1) TO (3)
      *>       reuses the literals: ABA; T5 "M" "N" FROM (2) with no TO
      *>       runs to the maximum (GR14): MNM in T5(2..4); T6C "A" "B"
      *>       "C" FROM (1 2) TO (2 2) carries the least inclusive
      *>       subscript into the next dimension (GR12): (1 2)=A (2 1)=B
      *>       (2 2)=C. T4(4) and T5(1) are not shown: no literal is
      *>       given for them, so their values are undefined (GR4).
      *> OD IX §2002:13.16.61.3 GR6 - OD is initialized as if IX held 5,
      *>       then IX's own VALUE 3 is placed: IX=3, and after MOVE 5
      *>       TO IX all five OD occurrences are Z. Fails if only three
      *>       occurrences are seeded.
      *> C1    §2002:13.16.61.3 GR10 and SR24 via §2002:14.6.7 rule 1 -
      *>       1 THRU 3 includes both ends: 1 and 3 true, 4 false;
      *>       §2002:13.16.61.3 GR17 - SET C1-LOW TO FALSE places 50; the
      *>       initial 5 is also a false value (GR17 NOTE).
      *> A1    §2002:13.16.61.3 GR10 and SR24 via §2002:14.6.7 rule 2 -
      *>       "AA" THRU "AZ" IN STANDARD-1 (ASCII order): AA, AB, AZ in
      *>       range, BA not.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2VQ01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET ALPH IS STANDARD-1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W4 PIC 9(4).
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT.
       01  CR.
           05 N1 PIC 99PP VALUE 1200.
           05 N2 PIC 9(3)V99 VALUE 1.5.
           05 N3 PIC VPP99 VALUE .0012.
           05 E1 PIC ZZ9.99 VALUE "001.50".
           05 E2 PIC 9.99 USAGE NATIONAL VALUE N"1.00".
           05 E3 PIC XX/XX VALUE "ABCD".
           05 E4 PIC ZZ9 VALUE "007".
           05 B1 PIC ZZ9.99 BLANK WHEN ZERO VALUE ZERO.
           05 B2 PIC ZZ9.99 BLANK WHEN ZERO VALUE "000.00".
           05 J1 PIC X(5) JUSTIFIED RIGHT VALUE "AB".
           05 BL PIC 1(2) VALUE B"10".
           05 T1 PIC X OCCURS 3 VALUE "P" FROM (1) TO (2) "Q" FROM (3).
           05 T4 PIC X OCCURS 4 VALUE "A" "B" FROM (1) TO (3).
           05 T5 PIC X OCCURS 4 VALUE "M" "N" FROM (2).
           05 T6 OCCURS 2.
              10 T6C PIC X OCCURS 2 VALUE "A" "B" "C" FROM (1 2)
                 TO (2 2).
           05 C1 PIC 99 VALUE 5.
              88 C1-LOW VALUE 1 THRU 3 WHEN SET TO FALSE IS 50.
           05 A1 PIC X(2) VALUE "AB".
              88 A1-R VALUE "AA" THRU "AZ" IN ALPH.
           05 IX PIC 9 VALUE 3.
           05 OD PIC X OCCURS 1 TO 5 DEPENDING ON IX VALUE "Z".
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE N1 TO W4
           DISPLAY "N1=" W4
           COMPUTE W4 = N3 * 10000
           DISPLAY "N3=" W4
           COMPUTE W4 = N2 * 100
           DISPLAY "N2=" W4
           DISPLAY "E1=[" E1 "] E3=[" E3 "] E4=[" E4 "]"
           IF E2 = N"1.00"
               DISPLAY "E2=1.00"
           ELSE
               DISPLAY "E2=OTHER"
           END-IF
           IF BL = B"10"
               DISPLAY "BL=10"
           ELSE
               DISPLAY "BL=OTHER"
           END-IF
           DISPLAY "B1=[" B1 "] B2=[" B2 "] J1=[" J1 "]"
           DISPLAY "T1=" T1 (1) T1 (2) T1 (3)
           DISPLAY "T4=" T4 (1) T4 (2) T4 (3)
           DISPLAY "T5=" T5 (2) T5 (3) T5 (4)
           DISPLAY "T6=" T6C (1 2) T6C (2 1) T6C (2 2)
           DISPLAY "IX=" IX
           MOVE 5 TO IX
           DISPLAY "OD=" OD (1) OD (2) OD (3) OD (4) OD (5)
           IF C1-LOW DISPLAY "C1 5 TRUE" ELSE DISPLAY "C1 5 FALSE"
           END-IF
           MOVE 1 TO C1
           IF C1-LOW DISPLAY "C1 1 TRUE" ELSE DISPLAY "C1 1 FALSE"
           END-IF
           MOVE 3 TO C1
           IF C1-LOW DISPLAY "C1 3 TRUE" ELSE DISPLAY "C1 3 FALSE"
           END-IF
           MOVE 4 TO C1
           IF C1-LOW DISPLAY "C1 4 TRUE" ELSE DISPLAY "C1 4 FALSE"
           END-IF
           MOVE 2 TO C1
           SET C1-LOW TO FALSE
           DISPLAY "C1 FALSE=" C1
           IF A1-R DISPLAY "A1 AB IN" ELSE DISPLAY "A1 AB OUT" END-IF
           MOVE "AA" TO A1
           IF A1-R DISPLAY "A1 AA IN" ELSE DISPLAY "A1 AA OUT" END-IF
           MOVE "AZ" TO A1
           IF A1-R DISPLAY "A1 AZ IN" ELSE DISPLAY "A1 AZ OUT" END-IF
           MOVE "BA" TO A1
           IF A1-R DISPLAY "A1 BA IN" ELSE DISPLAY "A1 BA OUT" END-IF
           MOVE "XX" TO A1 E3 J1
           MOVE 5 TO B2
           MOVE 9 TO N2
           INITIALIZE A1 E3 J1 B2 N2 ALL TO VALUE
           COMPUTE W4 = N2 * 100
           DISPLAY "AFTER-INIT A1=" A1 " E3=[" E3 "] J1=[" J1 "] B2=["
               B2 "] N2=" W4
           STOP RUN.
