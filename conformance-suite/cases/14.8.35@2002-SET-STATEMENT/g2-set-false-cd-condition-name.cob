      *>--- conformance-case
      *> id: g2-set-false-cd-condition-name
      *> rules: [2002-SR-14.8.35.2-7]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.35.2 SR7 - "If the FALSE phrase is specified, the
      *> FALSE phrase shall be specified in the VALUE clause of the
      *> data description entry for condition-name-1." The
      *> condition-names are level-88 entries in a record under an
      *> output CD (§2002:A.1.2.2 SR9), a reach the 2023 text has no
      *> CD for. The CD facility is obsolete (COBOL0903 warning).
      *> §2002:14.8.35.3 GR5: TRUE places the (first) VALUE literal;
      *> GR6: FALSE places "the literal in the FALSE phrase".
      *>   SET C-ON TO TRUE   -> A=Y ; SET C-ON TO FALSE -> B=N
      *>   SET N-OK TO TRUE   -> C=1 (first of 1 2)
      *>   SET N-OK TO FALSE  -> D=0
      *>   IF C-ON after FALSE -> E=OFF
      *> Fails if a CD condition-name with WHEN SET TO FALSE is
      *> refused, or if FALSE moves anything but its own literal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SET01.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD COUT FOR OUTPUT.
       01 CREC.
          05 CFLAG PIC X.
             88 C-ON VALUE "Y" WHEN SET TO FALSE "N".
          05 CNUM PIC 9.
             88 N-OK VALUE 1 2 WHEN SET TO FALSE 0.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "?" TO CFLAG
           MOVE 7 TO CNUM
           SET C-ON TO TRUE
           DISPLAY "A=" CFLAG
           SET C-ON TO FALSE
           DISPLAY "B=" CFLAG
           SET N-OK TO TRUE
           DISPLAY "C=" CNUM
           SET N-OK TO FALSE
           DISPLAY "D=" CNUM
           IF C-ON DISPLAY "E=ON" ELSE DISPLAY "E=OFF" END-IF
           STOP RUN.
