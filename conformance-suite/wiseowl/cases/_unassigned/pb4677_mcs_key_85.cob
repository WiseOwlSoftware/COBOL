      *>--- conformance-case
      *> id: pb4677_mcs_key_85
      *> rules: []
      *> accept: [85]
      *> files: [mcs.json]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4677 - the COBOL-85 KEY phrase of ENABLE and DISABLE, the password
      *> CCVS-85 CM102M tests: a wrong key is status 40 and takes no action, so the
      *> next statement's status 00 or 15 shows what state it found.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4677KY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-KEY PIC X(10) VALUE "DKEY".
       COMMUNICATION SECTION.
       CD COUT FOR OUTPUT
           DESTINATION COUNT IS DC
           STATUS KEY IS OK
           ERROR KEY IS OE
           SYMBOLIC DESTINATION IS OD.
       CD CIN FOR INPUT
           SYMBOLIC QUEUE IS Q
           SYMBOLIC SOURCE IS SRC
           STATUS KEY IS SK.
       CD CIO FOR I-O
           SYMBOLIC TERMINAL IS TN
           STATUS KEY IS IK.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 1 TO DC.
           MOVE "LOOP" TO OD.
           DISABLE OUTPUT COUT WITH KEY "WRONG".
           DISPLAY "O " OK.
           DISABLE OUTPUT COUT KEY W-KEY.
           DISPLAY "O " OK.
           MOVE "DEV" TO OD.
           DISABLE OUTPUT COUT WITH KEY "ANYTHING".
           DISPLAY "O " OK.
           MOVE "TOP" TO Q.
           DISABLE INPUT CIN WITH KEY "WRONG".
           DISPLAY "Q " SK.
           DISABLE INPUT CIN WITH KEY "TOPKEY".
           DISPLAY "Q " SK.
           MOVE "SRC1" TO SRC.
           DISABLE INPUT TERMINAL CIN WITH KEY "WRONG".
           DISPLAY "S " SK.
           DISABLE INPUT TERMINAL CIN WITH KEY "SRCKEY".
           DISPLAY "S " SK.
           MOVE "TERM1" TO TN.
           DISABLE I-O TERMINAL CIO WITH KEY "WRONG".
           DISPLAY "T " IK.
           DISABLE I-O TERMINAL CIO WITH KEY "TKEY".
           DISPLAY "T " IK.
           STOP RUN.
