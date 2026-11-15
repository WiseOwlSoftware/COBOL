      *>--- conformance-case
      *> id: g2can02_alnum_national_targets
      *> rules: [2002-SR-14.8.5.2-1, 2002-SR-14.8.5.2-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.5.2 SR1 - "Identifier-1 shall be defined as an
      *> alphanumeric or national data item": WN (PIC N, national) and
      *> PN (alphanumeric, in the record of an input CD) and WA
      *> (alphanumeric, working storage) are each accepted as
      *> identifier-1.
      *> §2002:14.8.5.2 SR2 - "Literal-1 shall be an alphanumeric or
      *> national literal": N"G2CNL2S" (national) and "NOSUCH2"
      *> (alphanumeric) are each accepted as literal-1.
      *> Each accepted form must also CANCEL: §2002:14.8.5.3 GR3 puts
      *> the next-called program "in its initial state", and
      *> §2002:8.6.5 leaves it "in the last-used state on other than
      *> the first activation" otherwise. G2CNL2S counts its calls:
      *>   CALL, CALL                          C=1, C=2
      *>   CANCEL WN (national identifier)     CALL C=1, CALL C=2
      *>   CANCEL N"G2CNL2S" (national lit.)   CALL C=1, CALL C=2
      *>   CANCEL PN (CD record alphanumeric)  CALL C=1, CALL C=2
      *>   CANCEL WA "NOSUCH2" (GR7: no-op)    CALL C=1
      *> Fails if a national or CD-record operand is refused, or if a
      *> CANCEL through it is a silent no-op (C=3 where C=1 is
      *> derived). COBOL0903 (the obsolete communication facility)
      *> is a warning.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CNL2M.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WA PIC X(7) VALUE "G2CNL2S".
       01 WN PIC N(7) VALUE N"G2CNL2S".
       COMMUNICATION SECTION.
       CD CDIN FOR INPUT
           SYMBOLIC QUEUE IS SQ.
       01 INREC.
          05 PN     PIC X(7).
          05 FILLER PIC X(80).
       PROCEDURE DIVISION.
       MAIN.
           CALL "G2CNL2S"
           CALL "G2CNL2S"
           CANCEL WN
           CALL "G2CNL2S"
           CALL "G2CNL2S"
           CANCEL N"G2CNL2S"
           CALL "G2CNL2S"
           CALL "G2CNL2S"
           MOVE "G2CNL2S" TO PN
           CANCEL PN
           CALL "G2CNL2S"
           CALL "G2CNL2S"
           CANCEL WA "NOSUCH2"
           CALL "G2CNL2S"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CNL2S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CNT PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       S1.
           ADD 1 TO CNT
           DISPLAY "C=" CNT
           GOBACK.
       END PROGRAM G2CNL2S.
       END PROGRAM G2CNL2M.
