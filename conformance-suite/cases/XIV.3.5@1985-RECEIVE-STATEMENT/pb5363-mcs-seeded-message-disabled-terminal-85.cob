      *>--- conformance-case
      *> id: pb5363-mcs-seeded-message-disabled-terminal-85
      *> rules: [1985-GR-XIV.3.5.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5363. "The conditions under which data is not made
      *> available are defined by the implementor" (§1985:XIV.3.5.4 GR5,
      *> cite.py: OK). WiseOwl's choice (docs/conformance, the PB5363
      *> entry): a message mcs.json seeds for a terminal it defines
      *> enabled:false is one the MCS already holds when the run starts,
      *> so the first RECEIVE takes HELLO (status 00) though the terminal
      *> was never enabled; ENABLE then answers 00 and the second RECEIVE
      *> finds no data. The program writes its own mcs.json first.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5363S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT DEF-FILE ASSIGN TO "mcs.json".
       DATA DIVISION.
       FILE SECTION.
       FD  DEF-FILE.
       01  DEF-REC PIC X(60).
       WORKING-STORAGE SECTION.
       01 BUF PIC X(10).
       COMMUNICATION SECTION.
       CD C FOR I-O
           MESSAGE DATE IS MD
           MESSAGE TIME IS MT
           SYMBOLIC TERMINAL IS ST
           TEXT LENGTH IS TL
           END KEY IS EK
           STATUS KEY IS SK.
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT DEF-FILE.
           MOVE "{""terminals"":[{""name"":""T2"",""enabled"":false,"
               TO DEF-REC.
           WRITE DEF-REC.
           MOVE """messages"":[""HELLO""]}]}" TO DEF-REC.
           WRITE DEF-REC.
           CLOSE DEF-FILE.
           MOVE "T2" TO ST.
           MOVE SPACES TO BUF.
           RECEIVE C MESSAGE INTO BUF NO DATA DISPLAY "NODATA-1".
           DISPLAY "R1 SK=" SK " BUF=[" BUF "]".
           ENABLE I-O TERMINAL C.
           DISPLAY "EN SK=" SK.
           MOVE SPACES TO BUF.
           RECEIVE C MESSAGE INTO BUF NO DATA DISPLAY "NODATA-2".
           DISPLAY "R2 SK=" SK " BUF=[" BUF "]".
           STOP RUN.
