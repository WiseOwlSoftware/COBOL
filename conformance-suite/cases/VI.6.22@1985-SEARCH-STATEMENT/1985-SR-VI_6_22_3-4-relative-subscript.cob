      *>--- conformance-case
      *> id: 1985-SR-VI_6_22_3-4-relative-subscript
      *> rules: [1985-SR-VI.6.22.3-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5193 - §1985:VI.6.22.3 SR4 (THE SEARCH STATEMENT):
      *> "Each data-name-1, data-name-2 must be subscripted by the first
      *> index-name associated with identifier-1 along with other
      *> subscripts as required". K (IX + 1) is subscripted by IX, and
      *> the 1985 rule has no sentence against the relative subscript
      *> nor a subscript rule for a condition-name (both arrive with
      *> §2002:14.8.34.2 SR6 and SR7), so both searches must compile at
      *> 85. §1985:VI.6.22.4 GR4: "If all the conditions can be
      *> satisfied, the index indicates an occurrence that allows the
      *> conditions to be satisfied": K (IX + 1) = 2 holds only for
      *> IX = 1 (the keys are 1, 2, 3), so K (IX) displays 1. A
      *> compiler that refuses either WHEN fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SR4REL85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TBL-DATA PIC X(3) VALUE "123".
       01 TBL REDEFINES TBL-DATA.
          05 ENT OCCURS 3 TIMES ASCENDING KEY IS K INDEXED BY IX.
             10 K PIC 9.
                88 C2 VALUE 2.
       PROCEDURE DIVISION.
       P1.
           SEARCH ALL ENT AT END DISPLAY "NOT FOUND"
               WHEN K (IX + 1) = 2 DISPLAY "DATA-NAME " K (IX).
           SEARCH ALL ENT AT END DISPLAY "NOT FOUND"
               WHEN C2 (IX + 1) DISPLAY "CONDITION " K (IX).
           STOP RUN.
