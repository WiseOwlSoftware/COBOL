      *>--- conformance-case
      *> id: g2mov02_cd_space_to_boolean
      *> rules: [2002-SR-14.8.24.2-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.24.2 SR7 - "Any figurative constant for which the
      *>   associated character or characters are not boolean
      *>   characters shall not be moved to a boolean data item."
      *>   SPACE is not a boolean character; OB is a boolean item of
      *>   a record under an output CD (§2002:A.1.2.2 SR9). The MOVE
      *>   is refused at its line (COBOL0819); the CD facility's
      *>   obsolete warning COBOL0903 is not the refusal. Fails if a
      *>   CD record's boolean item escapes the screen.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2MOV02.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD CDOUT FOR OUTPUT
           TEXT LENGTH IS OTL.
       01 OUTREC.
          05 FILLER PIC X(10).
          05 OB     PIC 1(4).
       PROCEDURE DIVISION.
       MAIN.
           MOVE SPACE TO OB.
           STOP RUN.
