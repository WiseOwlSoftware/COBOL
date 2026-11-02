      *>--- conformance-case
      *> id: pb4286-receive-giving-omitted-next-statement-read
      *> rules: [FMT-14.9.31.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4286 - the statements after an incomplete RECEIVE are read as written. ISO 14.9.31.2 prints
      *>   RECEIVE FROM data-name-1 GIVING identifier-1 data-name-2
      *>   cite.py: OK  14.9.31.2  (General format) - "identifier-1 data-name-2"
      *> so the bare GIVING omits both operands (the first fault, at DISPLAY). The MOVE after the DISPLAY omits
      *> its TO (14.9.25.2 Format 1 prints TO, underlined, before "{ identifier-2 }"; cite.py OK 14.9.25.2),
      *> a second fault of its own, and this refusal expects it. It can fail two ways: the deletion drops the
      *> DISPLAY (W-Y as identifier-1), or the recovery, having stopped at the DISPLAY for the missing
      *> identifier-1, consumes it at the missing data-name-2 (ANTLR's same-state guard, keyed on the state
      *> alone) and the rest of the sentence is lost into the RECEIVE: the MOVE's fault then went unreported.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4286R2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-X PIC X(4).
       01 W-Y PIC X(4).
       01 W-N PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-P.
           RECEIVE FROM W-X GIVING
           DISPLAY W-Y
           MOVE 1 W-N.
           STOP RUN.
