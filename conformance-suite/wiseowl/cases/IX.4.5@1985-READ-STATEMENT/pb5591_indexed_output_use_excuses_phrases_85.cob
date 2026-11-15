      *>--- conformance-case
      *> id: pb5591_indexed_output_use_excuses_phrases_85
      *> rules: [1985-GR-IX.4.5.4-10, 1985-SR-IX.4.5.3-7,
      *>     1985-SR-IX.4.6.3-3, 1985-SR-IX.4.7.3-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5591 - a LATITUDE CHOICE (docs/CONFORMANCE.md 3.5).
      *> "The INVALID KEY phrase or the AT END phrase must be
      *> specified if no applicable USE AFTER STANDARD EXCEPTION
      *> procedure is specified for file-name-1" (§1985:IX.4.5.3
      *> SR7; the START rule §1985:IX.4.7.3 SR3 likewise, and the
      *> REWRITE rule §1985:IX.4.6.3 SR3 says "appropriate"). The
      *> 1985 text defines neither word; WiseOwl COBOL reads both as
      *> a procedure that applies to the file in some mode it can be
      *> open in (reading A), so the only declarative, USE ... ON
      *> OUTPUT, excuses the READ NEXT with no AT END (GR10's
      *> phrase), the keyed READ and the START with no INVALID KEY
      *> and the REWRITE with none. A compiler on reading B refuses
      *> all four. At run time the file is open in the output mode, so
      *> each statement fails (§1985:IX.1.3.4: status 47 for a READ
      *> or START "attempted on a file not open in the input or I-O
      *> mode", 49 for a REWRITE "attempted on a file not open in the
      *> I-O mode") and the procedure runs "for any file open in the
      *> output mode" (§1985:IX.4.8.4 GR5c). After a critical status
      *> WiseOwl COBOL continues with the next statement, the
      *> implementor's action (docs/CONFORMANCE.md 3.5). The test
      *> fails if the program is refused, if a procedure does not
      *> run, or if a status differs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5591B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XF ASSIGN TO "P5591B.IDX"
               ORGANIZATION IS INDEXED ACCESS MODE IS DYNAMIC
               RECORD KEY IS XK FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  XF.
       01  XREC.
           05 XK PIC X(3).
           05 XD PIC X(5).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-OUT SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON OUTPUT.
       D-OUT-P.
           DISPLAY "USE OUTPUT " FS.
       END DECLARATIVES.
       MAIN SECTION.
       M-1.
           OPEN OUTPUT XF.
           DISPLAY "OPEN " FS.
           READ XF NEXT RECORD.
           DISPLAY "AFTER READ NEXT " FS.
           MOVE "K01" TO XK.
           READ XF.
           DISPLAY "AFTER READ KEY " FS.
           START XF KEY IS EQUAL TO XK.
           DISPLAY "AFTER START " FS.
           MOVE "ALPHA" TO XD.
           REWRITE XREC.
           DISPLAY "AFTER REWRITE " FS.
           WRITE XREC.
           DISPLAY "WRITE " FS.
           CLOSE XF.
           STOP RUN.
