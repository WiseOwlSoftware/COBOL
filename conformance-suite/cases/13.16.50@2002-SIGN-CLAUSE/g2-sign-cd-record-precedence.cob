      *>--- conformance-case
      *> id: g2-sign-cd-record-precedence
      *> rules: [2002-GR-13.16.50.3-1, 2002-GR-13.16.50.3-2, 2002-GR-13.16.50.3-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> SIGN clauses in a record description entry following a CD
      *> entry (§2002:A.1.1.2: data description entries of 13.14).
      *> Layout of CDREC (positions): S1 1-4, S2 5-8, S3 9-11, U2 12-14.
      *> §2002:13.16.50.3 GR1 - the group's SIGN TRAILING SEPARATE
      *> applies "for each numeric item subordinate to the group", and
      *> "applies only to numeric items whose picture character-string
      *> contains the symbol 'S'": S2 is "012-" / "012+" (GR6a: a
      *> separate trailing character; GR6b: '+' and '-'), length 4;
      *> unsigned U2 is untouched, "012", length 3.  CDREC is 14.
      *> §2002:13.16.50.3 GR2 - G1's SIGN LEADING SEPARATE "takes
      *> precedence for that subordinate group item": S1 is "-012".
      *> §2002:13.16.50.3 GR3 - S3's own SIGN LEADING (no SEPARATE)
      *> "takes precedence for that elementary entry": length 3 (no
      *> separate character), its trailing digits "12" carry no sign
      *> and the sign rides on the leading digit (GR5a), so the leading
      *> character differs between -12 and +12 while the value
      *> compares as -12.  The overpunch itself is implementor-defined
      *> (GR5b) and is not printed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SGN02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N2 PIC 99.
       01  NEG-CH PIC X.
       01  POS-CH PIC X.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC SIGN IS TRAILING SEPARATE.
           05 G1 SIGN IS LEADING SEPARATE.
              10 S1 PIC S9(3).
           05 S2 PIC S9(3).
           05 S3 PIC S9(3) SIGN IS LEADING.
           05 U2 PIC 9(3).
       PROCEDURE DIVISION.
       P-PARA.
           MOVE -12 TO S1 S2 S3
           MOVE 12 TO U2
           MOVE FUNCTION LENGTH(CDREC) TO N2
           DISPLAY "CDREC LEN=" N2
           DISPLAY "S1=[" CDREC(1:4) "]"
           DISPLAY "S2=[" CDREC(5:4) "]"
           MOVE FUNCTION LENGTH(S2) TO N2
           DISPLAY "S2 LEN=" N2
           MOVE FUNCTION LENGTH(U2) TO N2
           DISPLAY "U2=[" CDREC(12:3) "] LEN=" N2
           MOVE FUNCTION LENGTH(S3) TO N2
           DISPLAY "S3 LEN=" N2 " TAIL=[" CDREC(10:2) "]"
           IF S3 = -12
               DISPLAY "S3 = -12"
           ELSE
               DISPLAY "S3 WRONG VALUE"
           END-IF
           MOVE CDREC(9:1) TO NEG-CH
           MOVE 12 TO S3
           MOVE CDREC(9:1) TO POS-CH
           IF NEG-CH NOT = POS-CH
               DISPLAY "S3 LEADING SIGN"
           ELSE
               DISPLAY "S3 NO LEADING SIGN"
           END-IF
           DISPLAY "S3 TAIL=[" CDREC(10:2) "]"
           MOVE 12 TO S2
           DISPLAY "S2=[" CDREC(5:4) "]"
           STOP RUN.
