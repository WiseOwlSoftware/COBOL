      *>--- conformance-case
      *> id: g2-sign-cd-record
      *> rules: [2002-SR-13.16.50.2-1, 2002-GR-13.16.50.3-5, 2002-GR-13.16.50.3-6, 2002-GR-13.16.50.3-7]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> The SIGN clause on the data description entries of a record
      *> under a CD (§2002:A.1.1.2 "A record description consists of a set
      *> of data description entries").
      *> §2002:13.16.50.2 SR1 - "The SIGN clause may be specified only
      *> for: ... a numeric data or screen description entry whose picture
      *> character-string contains the symbol 'S' ... an alphanumeric group
      *> item, national group item, or strongly-typed group item." Every
      *> SIGN below is on a granted subject: S9(3) entries L1 T1 LS1 TS1
      *> R1 and the alphanumeric group GN in the CD record; the national
      *> group GNN sits in working storage, since the communications
      *> facility omits national data (§2002:A NOTE "Support for data of
      *> classes boolean and national and exception handling are
      *> intentionally omitted from the obsolete communications
      *> facility."); the strong
      *> type SU is used by SGW in working storage, since a strongly-typed
      *> item may not sit in a CD record (§2002:13.16.55.2 SR4, SR6: it is
      *> level 1 only and never implicitly redefined, and §2002:A.1.1.2
      *> records under a CD implicitly redefine). Compiling and running is the
      *> accepted leg; the refusal of PIC 9 and PIC X subjects is
      *> g2-sign-cd-record-subject.
      *> §2002:13.16.50.3 GR5 a) "The operational sign is presumed to be
      *> associated with the leading (or, respectively, trailing) digit
      *> position": L1 and T1 are 3 positions (LEN), +12 and -12 leave the
      *> other digit positions "12" / "01" alike (L1-REST, T1-REST) and
      *> differ in the leading / trailing position (L1-SIGNPOS,
      *> T1-SIGNPOS). b) leaves the sign characters to the implementor, so
      *> no sign character of L1 or T1 is pinned.
      *> §2002:13.16.50.3 GR6 a) the separate sign is a character position
      *> that is not a digit position, so LS1 TS1 G1 NN1 SS are 4 positions
      *> (LEN; FUNCTION LENGTH per §2002:15.39.3 RV2 / RV3); b) the signs
      *> are '+' and '-': SEP [-012][012-][-012], POS [+012][012+].
      *> G1, NN1 and SS take their group's clause (§2002:13.16.50.3 GR1).
      *> §2002:13.16.50.3 GR7 - conversion for comparison and computation
      *> is automatic: LS1 -12 vs TS1 +12 unequal (CMP1 NE), L1 -12 vs LS1
      *> -12 equal (CMP2 EQ), R1 = L1 + TS1 + G1 = -36 shown as "-036",
      *> then R1 - NN1 = -36 - (-12) = -24 shown as "-024".
      *> Each leg fails if the sign is misplaced, made a digit position,
      *> dropped from a group, or not converted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SGN03.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  SU TYPEDEF STRONG SIGN IS LEADING SEPARATE.
           05 SS PIC S9(3).
       01  SGW TYPE SU.
       01  GNN GROUP-USAGE NATIONAL SIGN IS TRAILING SEPARATE.
           05 NN1 PIC S9(3) USAGE NATIONAL.
       01  LP PIC X(3).
       01  TP PIC X(3).
       01  K1 PIC 9.
       01  K2 PIC 9.
       01  K3 PIC 9.
       01  K4 PIC 9.
       01  K5 PIC 9.
       01  K6 PIC 9.
       01  K7 PIC 9.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 L1 PIC S9(3) SIGN IS LEADING.
           05 T1 PIC S9(3) SIGN IS TRAILING.
           05 LS1 PIC S9(3) SIGN IS LEADING SEPARATE.
           05 TS1 PIC S9(3) SIGN IS TRAILING SEPARATE CHARACTER.
           05 GN SIGN IS LEADING SEPARATE.
              10 G1 PIC S9(3).
           05 R1 PIC S9(3) SIGN IS LEADING SEPARATE.
       PROCEDURE DIVISION.
           MOVE 12 TO L1 T1
           MOVE CDREC(1:3) TO LP
           MOVE CDREC(4:3) TO TP
           MOVE -12 TO L1 T1 LS1 TS1 G1 NN1 SS OF SGW
           MOVE FUNCTION LENGTH(L1) TO K1
           MOVE FUNCTION LENGTH(T1) TO K2
           MOVE FUNCTION LENGTH(LS1) TO K3
           MOVE FUNCTION LENGTH(TS1) TO K4
           MOVE FUNCTION LENGTH(G1) TO K5
           MOVE FUNCTION LENGTH(NN1) TO K6
           MOVE FUNCTION LENGTH(SS OF SGW) TO K7
           DISPLAY "LEN " K1 " " K2 " " K3 " " K4 " " K5 " " K6 " " K7
           DISPLAY "L1-REST [" CDREC(2:2) "][" LP(2:2) "]"
           IF CDREC(1:1) NOT = LP(1:1)
              DISPLAY "L1-SIGNPOS DIFFER"
           ELSE
              DISPLAY "L1-SIGNPOS SAME"
           END-IF
           DISPLAY "T1-REST [" CDREC(4:2) "][" TP(1:2) "]"
           IF CDREC(6:1) NOT = TP(3:1)
              DISPLAY "T1-SIGNPOS DIFFER"
           ELSE
              DISPLAY "T1-SIGNPOS SAME"
           END-IF
           DISPLAY "SEP [" CDREC(7:4) "][" CDREC(11:4) "]["
                   CDREC(15:4) "]"
           MOVE 12 TO LS1 TS1
           DISPLAY "POS [" CDREC(7:4) "][" CDREC(11:4) "]"
           MOVE -12 TO LS1
           IF LS1 = TS1
              DISPLAY "CMP1 EQ"
           ELSE
              DISPLAY "CMP1 NE"
           END-IF
           IF L1 = LS1
              DISPLAY "CMP2 EQ"
           ELSE
              DISPLAY "CMP2 NE"
           END-IF
           MOVE -12 TO TS1
           COMPUTE R1 = L1 + TS1 + G1
           DISPLAY "SUM [" CDREC(19:4) "]"
           COMPUTE R1 = R1 - NN1
           DISPLAY "SUM2 [" CDREC(19:4) "]"
           STOP RUN.
