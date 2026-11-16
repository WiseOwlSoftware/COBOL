      *>--- conformance-case
      *> id: 2002-SR-A_1_2_2-9
      *> rules: [2002-SR-A.1.2.2-9, 2002-SR-A.1.2.2-8]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:A.1.2.2 SR9 - "Record description entries subordinate
      *> to an output CD entry implicitly redefine the record area
      *> established by the output CD entry. Multiple redefinitions of
      *> this record are permitted; however, only the first
      *> redefinition may contain VALUE clauses."
      *> §2002:A.1.2.2 SR8 (the satisfied branch) - CD COUT names none
      *> of the optional clauses, so "a level 01 data description entry
      *> shall follow the CD entry": OREC1 does, and the case compiles.
      *> The area is 10 + 13 x 1 = 23 positions (§2002:A.1.2.3 GR16;
      *> SR11: no DESTINATION TABLE, one error key and one symbolic
      *> destination), laid out 9(4) 9(4) X(2) X(1) X(12); each record
      *> below is 23 positions.
      *> VALUE in the first redefinition takes effect when the program
      *> is placed in its initial state (§2002:13.16.61.3 GR4 c).
      *> Legs (each fails if the records do not share one area, if a
      *> third redefinition is refused, or if OREC1's VALUEs are not
      *> applied):
      *>   R2 - OREC2 reads OREC1's VALUEs: 0001 0005 "00" "0" "DESTA"
      *>        + 7 spaces, so [00010005000DESTA       ].
      *>   R3 - OREC3's 9(8) is positions 1-8: 00010005.
      *>   M1 - MOVE ALL "Z" TO R2-ALL, then OREC1's status key and
      *>        symbolic destination read ZZ and ZZZZZZZZZZZZ.
      *>   M2 - MOVE 12345678 TO R3-NUM, then OREC1's destination count
      *>        and text length read 1234 and 5678.
      *> The communication facility is obsolete at 2002 (§2002:G):
      *> the COBOL0903 warning does not fail it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CDS9.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD COUT FOR OUTPUT.
       01 OREC1.
          02 R1-DC PIC 9(4) VALUE 1.
          02 R1-TL PIC 9(4) VALUE 5.
          02 R1-SK PIC X(2) VALUE "00".
          02 R1-EK PIC X VALUE "0".
          02 R1-SD PIC X(12) VALUE "DESTA".
       01 OREC2.
          02 R2-ALL PIC X(23).
       01 OREC3.
          02 R3-NUM PIC 9(8).
          02 R3-REST PIC X(15).
       PROCEDURE DIVISION.
       P1.
           DISPLAY "R2=[" R2-ALL "]".
           DISPLAY "R3=" R3-NUM.
           MOVE ALL "Z" TO R2-ALL.
           DISPLAY "M1=" R1-SK " " R1-SD.
           MOVE 12345678 TO R3-NUM.
           DISPLAY "M2=" R1-DC " " R1-TL.
           STOP RUN.
