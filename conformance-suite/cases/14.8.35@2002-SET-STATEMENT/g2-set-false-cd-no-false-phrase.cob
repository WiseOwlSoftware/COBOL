      *>--- conformance-case
      *> id: g2-set-false-cd-no-false-phrase
      *> rules: [2002-SR-14.8.35.2-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.35.2 SR7 - "If the FALSE phrase is specified, the
      *> FALSE phrase shall be specified in the VALUE clause of the
      *> data description entry for condition-name-1."
      *> Leg: C-ON (a level-88 in a record under an output CD) has no
      *> WHEN SET TO FALSE phrase; SET C-ON TO FALSE is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SETN1.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD COUT FOR OUTPUT.
       01 CREC.
          05 CFLAG PIC X.
             88 C-ON VALUE "Y".
       PROCEDURE DIVISION.
       MAIN.
           SET C-ON TO FALSE
           STOP RUN.
