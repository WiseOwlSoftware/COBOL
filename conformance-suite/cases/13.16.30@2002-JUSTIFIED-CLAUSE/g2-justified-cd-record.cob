      *>--- conformance-case
      *> id: g2-justified-cd-record
      *> rules: [2002-GR-13.16.30.3-1, 2002-GR-13.16.30.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> JUSTIFIED receiving items in a record description entry
      *> under an output CD; such entries are data description
      *> entries (§2002:A.1.1.2), so 13.16.30 governs them.
      *> §2002:13.16.30.3 GR1 - sender larger than the JUSTIFIED
      *> receiver: "the leftmost character positions or boolean
      *> positions of the sending operand shall be truncated".
      *>   J2 X(4) JUST    <- "ABCDEFG"  : [DEFG]
      *>   J4 N(2) JUST    <- N"WXYZ"    : N"YZ"
      *>   J6 1(2) BIT     <- B"1101"    : B"01"
      *>   J7 A(3) JUST    <- "ABCDE"    : [CDE]
      *> §2002:13.16.30.3 GR2 - receiver larger: aligned at the
      *> rightmost position, space fill for character positions
      *> (national spaces for usage national, alphanumeric for
      *> display), zero fill for boolean positions (national zeros
      *> for usage national, alphanumeric zeros for usage display,
      *> bit zeros for usage bit).
      *>   J1 X(4)         <- "AB"       : [  AB]
      *>   J3 N(4)         <- N"XY"      : N"  XY" (national spaces)
      *>   J5 1(4) display <- B"11"      : [0011]
      *>   J8 1(4) NATIONAL <- B"11"     : B"0011" (national zeros;
      *>      a national boolean item, §2002:13.16.58.2 SR11)
      *>   J9 1(4) BIT     <- B"1"       : B"0001"
      *> Each national and boolean result is also compared with the
      *> literal its rule gives (OK/BAD), so the check does not rest
      *> on how DISPLAY renders a national or bit item. Fails if the
      *> rightmost (not leftmost) positions are truncated, or the
      *> data is left-aligned, or the fill is of the wrong kind.
      *> COBOL0903 (the communication facility is obsolete) is a
      *> warning, not a refusal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2JUS01.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 J1 PIC X(4) JUSTIFIED RIGHT.
           05 J2 PIC X(4) JUST.
           05 J3 PIC N(4) JUSTIFIED RIGHT.
           05 J4 PIC N(2) JUSTIFIED.
           05 J5 PIC 1(4) JUSTIFIED RIGHT.
           05 J6 PIC 1(2) USAGE BIT JUSTIFIED RIGHT.
           05 J7 PIC A(3) JUSTIFIED RIGHT.
           05 J8 PIC 1(4) USAGE NATIONAL JUSTIFIED RIGHT.
           05 J9 PIC 1(4) USAGE BIT JUST RIGHT.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "AB" TO J1
           MOVE "ABCDEFG" TO J2
           MOVE N"XY" TO J3
           MOVE N"WXYZ" TO J4
           MOVE B"11" TO J5
           MOVE B"1101" TO J6
           MOVE "ABCDE" TO J7
           MOVE B"11" TO J8
           MOVE B"1" TO J9
           DISPLAY "J1=[" J1 "] J2=[" J2 "] J7=[" J7 "]"
           DISPLAY "J5=[" J5 "]"
           IF J3 = N"  XY"
               DISPLAY "J3 OK"
           ELSE
               DISPLAY "J3 BAD"
           END-IF
           IF J4 = N"YZ"
               DISPLAY "J4 OK"
           ELSE
               DISPLAY "J4 BAD"
           END-IF
           IF J5 = B"0011"
               DISPLAY "J5 OK"
           ELSE
               DISPLAY "J5 BAD"
           END-IF
           IF J6 = B"01"
               DISPLAY "J6 OK"
           ELSE
               DISPLAY "J6 BAD"
           END-IF
           IF J8 = B"0011"
               DISPLAY "J8 OK"
           ELSE
               DISPLAY "J8 BAD"
           END-IF
           IF J9 = B"0001"
               DISPLAY "J9 OK"
           ELSE
               DISPLAY "J9 BAD"
           END-IF
           STOP RUN.
