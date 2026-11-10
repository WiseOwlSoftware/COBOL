*>--- conformance-case
*> id: w68r_pb1079_external_entry_identity
*> rules: [GR-12.4.5.3-1]
*> accept: [2023]
*> files: [w68r_w68rsame.cbl, w68r_w68rres.cbl, w68r_w68rcol.cbl, w68r_w68rkcol.cbl, w68r_w68rloc.cbl, w68r_w68rsiz.cbl, w68r_w68rsup.cbl, w68r_w68raloc.cbl]
*> build: [w68r_w68rsame.cbl, w68r_w68rres.cbl, w68r_w68rcol.cbl, w68r_w68rkcol.cbl, w68r_w68rloc.cbl, w68r_w68rsiz.cbl, w68r_w68rsup.cbl, w68r_w68raloc.cbl, "{self}"]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1079 - an external file connector's entry identity is the WHOLE of
      *> ISO 12.4.5.3 GR1 (14.8.4.4: "the rules specified in 12.4.5, File control
      *> entry General rule 1 apply"). MAIN describes indexed external file XF; each
      *> CALLed sub describes XF with ONE GR1 item changed: d) RESERVE integer-1,
      *> g) the file-level and the key-level COLLATING SEQUENCE clause, j) the prime
      *> key's relative location and its data description (size), k) the SUPPRESS
      *> WHEN phrase and the alternate key's relative location. EC-EXTERNAL-FILE-
      *> MISMATCH is enabled in both elements (14.8.4.1), so every such CALL is not
      *> successful (14.9.4.4 GR3e) and takes its ON EXCEPTION phrase with the sub
      *> never running; the identical entry (W68RSAME) runs. A failed describer is not
      *> registered, so each sub is compared with MAIN and W68RSAME only.
*> Each called program is its own compilation unit (the build
*> steps): within ONE compilation group the entries' disagreement
*> is refused at compile time (COBOL6847, kb/Work PB5069), and
*> EC-EXTERNAL-FILE-MISMATCH is the check between separately
*> compiled runtime elements.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W68RMAIN.
       >>TURN EC-EXTERNAL-FILE-MISMATCH CHECKING ON
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XF ASSIGN TO "w68r-pb1079.dat"
               ORGANIZATION INDEXED ACCESS MODE IS DYNAMIC
               RECORD KEY IS XK
               ALTERNATE RECORD KEY IS XA WITH DUPLICATES
               .
       DATA DIVISION.
       FILE SECTION.
       FD XF IS EXTERNAL.
       01 XREC.
           05 XK PIC X(4).
           05 XA PIC X(4).
           05 XD PIC X(2).
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "W68RSAME"
               ON EXCEPTION
                   DISPLAY "SAME=[" FUNCTION EXCEPTION-STATUS "]"
               NOT ON EXCEPTION
                   DISPLAY "SAME=RAN"
           END-CALL
           CALL "W68RRES"
               ON EXCEPTION
                   DISPLAY "RESERVE=[" FUNCTION EXCEPTION-STATUS "]"
               NOT ON EXCEPTION
                   DISPLAY "RESERVE=RAN"
           END-CALL
           CALL "W68RCOL"
               ON EXCEPTION
                   DISPLAY "FILE-COLLATING=[" FUNCTION EXCEPTION-STATUS "]"
               NOT ON EXCEPTION
                   DISPLAY "FILE-COLLATING=RAN"
           END-CALL
           CALL "W68RKCOL"
               ON EXCEPTION
                   DISPLAY "KEY-COLLATING=[" FUNCTION EXCEPTION-STATUS "]"
               NOT ON EXCEPTION
                   DISPLAY "KEY-COLLATING=RAN"
           END-CALL
           CALL "W68RLOC"
               ON EXCEPTION
                   DISPLAY "KEY-LOCATION=[" FUNCTION EXCEPTION-STATUS "]"
               NOT ON EXCEPTION
                   DISPLAY "KEY-LOCATION=RAN"
           END-CALL
           CALL "W68RSIZ"
               ON EXCEPTION
                   DISPLAY "KEY-SIZE=[" FUNCTION EXCEPTION-STATUS "]"
               NOT ON EXCEPTION
                   DISPLAY "KEY-SIZE=RAN"
           END-CALL
           CALL "W68RSUP"
               ON EXCEPTION
                   DISPLAY "SUPPRESS=[" FUNCTION EXCEPTION-STATUS "]"
               NOT ON EXCEPTION
                   DISPLAY "SUPPRESS=RAN"
           END-CALL
           CALL "W68RALOC"
               ON EXCEPTION
                   DISPLAY "ALT-LOCATION=[" FUNCTION EXCEPTION-STATUS "]"
               NOT ON EXCEPTION
                   DISPLAY "ALT-LOCATION=RAN"
           END-CALL
           DISPLAY "DONE"
           STOP RUN.
       END PROGRAM W68RMAIN.
