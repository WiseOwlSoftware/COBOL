      *>--- conformance-case
      *> id: g2ret02-return-into-cd-not-admissible-2002
      *> rules: [2002-SR-14.8.32.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.32.2 SR3 - INTO is permitted only under arm a)
      *>   (one record description) or arm b) (all record-names and
      *>   identifier-1 alphanumeric group, alphanumeric or national).
      *>   SF has TWO record descriptions, so arm a) is out, and
      *>   identifier-1 is CNUM, a NUMERIC item of a 2002 CD record,
      *>   so arm b) is out: RETURN SF INTO CNUM is refused at its
      *>   line. Accepted leg: g2ret01_return_into_cd_items_2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RET02.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "g2ret02.tmp".
       DATA DIVISION.
       FILE SECTION.
       SD SF.
       01 SR1 PIC X(4).
       01 SR2 PIC X(4).
       COMMUNICATION SECTION.
       CD COUT FOR OUTPUT.
       01 CREC.
          05 CNUM PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-P.
           SORT SF ON ASCENDING KEY SR1
               INPUT PROCEDURE IS IP
               OUTPUT PROCEDURE IS OP.
           STOP RUN.
       IP.
           RELEASE SR1 FROM "DDDD".
       OP.
           RETURN SF INTO CNUM AT END DISPLAY "END" END-RETURN.
