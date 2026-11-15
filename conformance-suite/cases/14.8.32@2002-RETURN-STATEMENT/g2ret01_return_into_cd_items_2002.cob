      *>--- conformance-case
      *> id: g2ret01_return_into_cd_items_2002
      *> rules: [2002-SR-14.8.32.2-3, 2002-SR-14.8.32.2-5]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.32.2 SR3 - "The INTO phrase may be specified in a
      *>   RETURN statement: a) If only one record description is
      *>   subordinate to the sort-merge file description entry, or
      *>   b) If all record-names associated with file-name-1 and the
      *>   data item referenced by identifier-1 describe an
      *>   alphanumeric group item or an elementary item of category
      *>   alphanumeric or category national." identifier-1 here is an
      *>   item of a 2002 CD record, the reach the 2023 text lacks.
      *>   Arm b): SF has two records (SR1 PIC X(4), group SR2), and
      *>     identifier-1 is the CD group CG, then the CD alphanumeric
      *>     item CA: both accepted.
      *>   Arm a): SF2 has ONE record (S2R PIC 9(4)), and
      *>     identifier-1 is the CD numeric item CN: accepted.
      *> §2002:14.8.32.2 SR5 - "The AT END phrase and the NOT AT END
      *>   phrase, when specified, may be written in reversed order":
      *>   every RETURN below writes NOT AT END first.
      *> DERIVATION (2002 text only). SF is sorted ascending on SR1
      *>   from DDDD, BBBB (SORT GR7a): the first RETURN delivers
      *>   BBBB and (GR5b) "The current record is moved from the
      *>   record area to the area specified by identifier-1": a group
      *>   move into CG gives G BBBB; GR4 transfers to the NOT AT END
      *>   phrase. The second delivers DDDD into CA: A DDDD. The third
      *>   finds no next record, the at end condition (GR3): END1.
      *>   SF2 holds 0042; RETURN INTO CN (numeric to numeric): N 0042,
      *>   then END2. A compiler binding the phrases by position runs
      *>   the wrong arm; one refusing the CD operands fails to build.
      *> The CD draws the obsolete warning COBOL0903 at 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RET01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "g2ret01a.tmp".
           SELECT SF2 ASSIGN TO "g2ret01b.tmp".
       DATA DIVISION.
       FILE SECTION.
       SD SF.
       01 SR1 PIC X(4).
       01 SR2.
          05 SR2A PIC X(2).
          05 SR2B PIC X(2).
       SD SF2.
       01 S2R PIC 9(4).
       COMMUNICATION SECTION.
       CD COUT FOR OUTPUT.
       01 CREC.
          05 CG.
             10 CG1 PIC X(2).
             10 CG2 PIC X(2).
          05 CA PIC X(4).
          05 CN PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-P.
           SORT SF ON ASCENDING KEY SR1
               INPUT PROCEDURE IS IP
               OUTPUT PROCEDURE IS OP.
           SORT SF2 ON ASCENDING KEY S2R
               INPUT PROCEDURE IS IP2
               OUTPUT PROCEDURE IS OP2.
           STOP RUN.
       IP.
           RELEASE SR1 FROM "DDDD".
           RELEASE SR1 FROM "BBBB".
       OP.
           RETURN SF INTO CG NOT AT END DISPLAY "G " CG
               AT END DISPLAY "END1"
           END-RETURN.
           RETURN SF INTO CA NOT AT END DISPLAY "A " CA
               AT END DISPLAY "END1"
           END-RETURN.
           RETURN SF INTO CA NOT AT END DISPLAY "A " CA
               AT END DISPLAY "END1"
           END-RETURN.
       IP2.
           MOVE 42 TO S2R.
           RELEASE S2R.
       OP2.
           RETURN SF2 INTO CN NOT AT END DISPLAY "N " CN
               AT END DISPLAY "END2"
           END-RETURN.
           RETURN SF2 INTO CN NOT AT END DISPLAY "N " CN
               AT END DISPLAY "END2"
           END-RETURN.
