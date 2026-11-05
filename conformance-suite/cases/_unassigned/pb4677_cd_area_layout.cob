      *>--- conformance-case
      *> id: pb4677_cd_area_layout
      *> rules: []
      *> accept: [85, 2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4677 - the record areas of a communication description
      *> entry, ISO/IEC 1989:2002 A.1.2.3 GR2, GR16, GR24 (the 2002 text is
      *> not in the repository). Each leg can fail: a clause's data-name at
      *> the wrong position reads another position of the written record;
      *> an unseeded first record loses its VALUE (A.1.2.2 SR5 lets the
      *> first redefinition have them); an unknown symbolic name is status
      *> 20 or 21 at any MCS, and RECEIVE passes control as if data had been
      *> made available (A.2.5.3 GR5 c), so NODATA never prints.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4677LY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-TEXT PIC X(10) VALUE "HELLOWORLD".
       01 W-IND  PIC 9 VALUE 0.
       COMMUNICATION SECTION.
       CD CIN FOR INPUT
           SYMBOLIC QUEUE IS IQ
           SYMBOLIC SUB-QUEUE-2 IS IS2
           MESSAGE DATE IS ID-D
           MESSAGE TIME IS ID-T
           SYMBOLIC SOURCE IS ISRC
           TEXT LENGTH IS IL
           END KEY IS IE
           STATUS KEY IS IK
           MESSAGE COUNT IS IC.
       01 CIN-R1.
           02 R-Q PIC X(12) VALUE "NOSUCHQUEUE".
           02 FILLER PIC X(36) VALUE SPACES.
           02 R-D PIC 9(6) VALUE 111111.
           02 R-T PIC 9(8) VALUE 22222222.
           02 FILLER PIC X(12) VALUE "SOURCENAME".
           02 R-L PIC 9(4) VALUE 3333.
           02 R-E PIC X VALUE "E".
           02 R-S PIC XX VALUE "SS".
           02 R-C PIC 9(6) VALUE 444444.
       01 CIN-R2 PIC X(87).
       CD COUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS OL
           STATUS KEY IS OK
           DESTINATION TABLE OCCURS 2 TIMES INDEXED BY OX
           ERROR KEY IS OE
           SYMBOLIC DESTINATION IS OD.
       01 COUT-R PIC X(36).
       CD CFLAT OUTPUT
           ERROR KEY IS FE
           DESTINATION FLD.
       01 CFLAT-R PIC X(23).
       CD CIO FOR I-O
           FILLER FILLER IOT IOL IOE IOK.
       01 CIO-R PIC X(33).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "Q " IQ.
           DISPLAY "D " ID-D " T " ID-T " S " ISRC.
           DISPLAY "L " IL " E " IE " K " IK " C " IC.
           MOVE "SECONDLEVEL" TO IS2.
           DISPLAY CIN-R2 (25:11).
           MOVE SPACES TO IS2.
           MOVE 7 TO DC.
           MOVE 9 TO OL.
           MOVE "KK" TO OK.
           MOVE "DEST1" TO OD (1).
           MOVE "X" TO OE (1).
           SET OX TO 2.
           MOVE "DEST2" TO OD (OX).
           MOVE "Y" TO OE (OX).
           DISPLAY "[" COUT-R "]".
           DISPLAY OK OF COUT.
           MOVE "Z" TO FE.
           MOVE "FLAT" TO FLD.
           DISPLAY "[" CFLAT-R (11:13) "]".
           MOVE "TERMINAL" TO IOT.
           MOVE 5 TO IOL.
           MOVE "3" TO IOE.
           MOVE "AB" TO IOK.
           DISPLAY "[" CIO-R (15:19) "]".
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "ACCEPT " IK " " IC.
           MOVE "99" TO IK.
           RECEIVE CIN MESSAGE INTO W-TEXT
               NO DATA DISPLAY "NODATA".
           DISPLAY "RECEIVE " IK " " W-TEXT.
           MOVE "99" TO IK.
           ENABLE INPUT CIN.
           DISPLAY "ENABLE " IK.
           MOVE "99" TO IK.
           DISABLE INPUT CIN.
           DISPLAY "DISABLE " IK.
           MOVE "NOSUCHSOURCE" TO ISRC.
           ENABLE INPUT TERMINAL CIN.
           DISPLAY "ENABLE T " IK.
           DISABLE INPUT TERMINAL CIN.
           DISPLAY "DISABLE T " IK.
           ENABLE I-O TERMINAL CIO.
           DISPLAY "ENABLE IO " IOK.
           RECEIVE CIO SEGMENT INTO W-TEXT.
           DISPLAY "RECEIVE IO " IOK.
           MOVE 1 TO DC.
           MOVE "NOSUCHDEST" TO OD (1).
           MOVE 5 TO OL.
           SEND COUT FROM W-TEXT WITH EMI.
           DISPLAY "SEND " OK " " OE (1).
           MOVE 0 TO DC.
           SEND COUT FROM W-TEXT.
           DISPLAY "SEND " OK.
           ENABLE OUTPUT COUT.
           DISPLAY "ENABLE O " OK.
           DISABLE OUTPUT COUT.
           DISPLAY "DISABLE O " OK.
           PURGE COUT.
           DISPLAY "PURGE " OK.
           MOVE 3 TO DC.
           SEND COUT FROM W-TEXT WITH ESI.
           DISPLAY "SEND " OK.
           MOVE 1 TO DC.
           MOVE 11 TO OL.
           SEND COUT FROM W-TEXT WITH EGI.
           DISPLAY "SEND " OK.
           MOVE 0 TO OL.
           SEND COUT FROM W-TEXT.
           DISPLAY "SEND " OK.
           SEND COUT WITH W-IND.
           DISPLAY "SEND " OK.
           STOP RUN.
