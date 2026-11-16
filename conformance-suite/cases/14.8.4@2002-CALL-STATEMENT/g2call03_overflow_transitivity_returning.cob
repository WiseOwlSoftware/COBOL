      *>--- conformance-case
      *> id: g2call03_overflow_transitivity_returning
      *> rules: [2002-GR-14.8.4.3-4, 2002-GR-14.8.4.3-5]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.4.3 GR5 (FORMATS 1 AND 2) -- "Both the BY CONTENT
      *> and BY REFERENCE phrases are transitive across the parameters
      *> that follow them until another BY CONTENT or BY REFERENCE
      *> phrase is encountered. If neither ... is specified prior to
      *> the first parameter, the BY REFERENCE phrase is assumed."
      *> §2002:14.8.4.3 GR4 -- "If a RETURNING phrase is specified, the
      *> result of the activated program is placed into identifier-3."
      *> §2002:14.8.4.2 SR7 lets identifier-3 be a communication-section
      *> item. §2002:14.1.3 GR8/GR9: a by-reference formal shares the
      *> argument's storage, a by-content one does not.
      *> The callee stores 1111/2222/3333/4444 into its four formals and
      *> returns 21 on its first activation, 42 on its second (static
      *> working-storage counter).
      *> F1 (Format 1, ON OVERFLOW): USING W1 BY CONTENT A B
      *>   BY REFERENCE D RETURNING R: W1 by reference (assumed), A and
      *>   B by content (transitive), D by reference, R gets 21:
      *>   F1 W1=1111 A=AAAA B=BBBB D=4444 R=0021
      *> F2 (Format 2, ON EXCEPTION): USING BY CONTENT W1 BY REFERENCE
      *>   QN A BY CONTENT D RETURNING MC, QN and MC CD record items:
      *>   NOEXC, then F2 W1=WWWW QN=2222 A=3333 D=DDDD MC=0042
      *> No OVF/EXC line: the callee exists (§2002:14.8.4.3 GR3h).
      *> Fails if: a phrase is not transitive (B or A changes, or keeps
      *> its value), the leading default is not BY REFERENCE (W1=WWWW on
      *> F1), RETURNING is not stored (R=0007, MC=0000), or a CD item is
      *> mishandled as an argument or as identifier-3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2C3M.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W1 PIC X(4).
       01 A  PIC X(4).
       01 B  PIC X(4).
       01 D  PIC X(4).
       01 R  PIC 9(4) VALUE 7.
       COMMUNICATION SECTION.
       CD CDIN FOR INPUT.
       01 CDREC.
          02 QN PIC X(4).
          02 MC PIC 9(4).
          02 FILLER PIC X(79).
       PROCEDURE DIVISION.
       MAIN.
           MOVE "WWWW" TO W1 MOVE "AAAA" TO A
           MOVE "BBBB" TO B  MOVE "DDDD" TO D
           CALL "G2C3S" USING W1 BY CONTENT A B BY REFERENCE D
               RETURNING R
               ON OVERFLOW DISPLAY "OVF"
           END-CALL
           DISPLAY "F1 W1=" W1 " A=" A " B=" B " D=" D " R=" R
           MOVE "WWWW" TO W1 MOVE "AAAA" TO A
           MOVE "QQQQ" TO QN MOVE "DDDD" TO D
           MOVE 0 TO MC
           CALL "G2C3S" USING BY CONTENT W1 BY REFERENCE QN A
               BY CONTENT D
               RETURNING MC
               ON EXCEPTION DISPLAY "EXC"
               NOT ON EXCEPTION DISPLAY "NOEXC"
           END-CALL
           DISPLAY "F2 W1=" W1 " QN=" QN " A=" A " D=" D " MC=" MC
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2C3S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 0.
       LINKAGE SECTION.
       01 X1 PIC X(4).
       01 X2 PIC X(4).
       01 X3 PIC X(4).
       01 X4 PIC X(4).
       01 RR PIC 9(4).
       PROCEDURE DIVISION USING X1 X2 X3 X4 RETURNING RR.
       S1.
           ADD 1 TO N
           MOVE "1111" TO X1 MOVE "2222" TO X2
           MOVE "3333" TO X3 MOVE "4444" TO X4
           COMPUTE RR = N * 21
           GOBACK.
       END PROGRAM G2C3S.
       END PROGRAM G2C3M.
