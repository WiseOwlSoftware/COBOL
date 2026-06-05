      *> ISO §14.9.20 — COBOL-2002 INITIALIZE phrases: WITH FILLER, [ALL|category] TO VALUE,
      *> THEN TO DEFAULT. Per-item precedence: TO VALUE (item's VALUE clause) > REPLACING > default.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INITPHRASE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GRP.
          05 A   PIC X(3) VALUE "AAA".
          05 B   PIC 9(3) VALUE 123.
          05 FILLER PIC X(2) VALUE "FF".
          05 C   PIC X(3).
          05 D   PIC 9(3).
       01 GRPR REDEFINES GRP PIC X(14).
       PROCEDURE DIVISION.
       MAIN.
           PERFORM MESSUP.
           INITIALIZE GRP TO VALUE.
           DISPLAY "TV=[" A "][" B "][" C "][" D "]".
           PERFORM MESSUP.
           INITIALIZE GRP.
           DISPLAY "DF=[" A "][" B "][" C "][" D "]".
           PERFORM MESSUP.
           INITIALIZE GRP ALL TO VALUE THEN TO DEFAULT.
           DISPLAY "VD=[" A "][" B "][" C "][" D "]".
           PERFORM MESSUP.
           INITIALIZE GRP REPLACING NUMERIC DATA BY 7.
           DISPLAY "RP=[" A "][" B "][" C "][" D "]".
           MOVE "XXXXXXXXXXXXXX" TO GRPR.
           INITIALIZE GRP.
           DISPLAY "NOFILL=[" GRPR "]".
           MOVE "XXXXXXXXXXXXXX" TO GRPR.
           INITIALIZE GRP WITH FILLER.
           DISPLAY "FILL=[" GRPR "]".
           STOP RUN.
       MESSUP.
           MOVE "ZZZ" TO A. MOVE 999 TO B.
           MOVE "QQQ" TO C. MOVE 555 TO D.
