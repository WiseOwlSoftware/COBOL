      *>--- conformance-case
      *> id: g2asg01_assign_using_content
      *> rules: [2002-GR-12.3.4.3-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.3.4.3 GR4 - "When the USING phrase is specified, the
      *> meaning and rules for the allowable content of the data item
      *> referenced by data-name-1 are defined by the implementor. The
      *> consistency rules, if any, that apply between the content of
      *> the data item referenced by data-name-1 and either the
      *> specification of device-name-1 or the value of literal-1 are
      *> defined by the implementor."
      *>   cite.py --check "§2002:12.3.4.3 GR4" (both sentences) -> OK
      *> §2002:12.3.4.3 GR3 b): with USING the connector "is associated
      *> with a physical file identified by the content of the data item
      *> referenced by data-name-1"  -> cite.py OK §2002:12.3.4.3 GR3b
      *> §2002:9.1.12.5 item 2: status 31 when "the content of the data
      *> item referenced by the data-name specified in the USING phrase
      *> of the file control entry is not consistent with the
      *> specification"  -> cite.py OK §2002:9.1.12.5
      *> THE IMPLEMENTOR'S DEFINITIONS (docs/conformance/7/DOC-A.1-73 and
      *> DOC-A.1-10): the content with leading and trailing spaces
      *> removed IS the assign target, mapped to a host path by exactly
      *> the rule that maps literal-1; content empty after that removal
      *> names no file (OPEN unsuccessful, '31'); and there are NO
      *> consistency rules between the content and literal-1 - where
      *> both are written the content alone identifies the file.
      *> DERIVATION OF THE EXPECTED OUTPUT:
      *>   A: content "  G2ASGA.DAT" (leading and trailing spaces): OPEN
      *>     OUTPUT 00, WRITE AAAA; the literal entry CHKA "G2ASGA.DAT"
      *>     reaches the same file: OPEN INPUT 00, READ AAAA
      *>                         => OPENA=00 / READA=00 AAAA
      *>   B: content "G2ASGB" (no extension) is mapped as the literal
      *>     "G2ASGB" is, so CHKB reads it back
      *>                         => OPENB=00 / READB=00 BBBB
      *>   M: ASSIGN TO "G2ASGM.DAT" USING WS-NAME, content
      *>     "G2ASGE.DAT": no consistency rule, so the OPEN succeeds and
      *>     the content wins - G2ASGM.DAT is never created (optional
      *>     CHKM opens 05) and CHKE reads EEEE from G2ASGE.DAT
      *>                         => OPENM=00 / CHKM=05 / READE=00 EEEE
      *>   blank content: no file named, the OPEN is unsuccessful
      *>                         => BLANK=31
      *> A leg fails if the spaces are kept (CHKA 05), if the content
      *> and the literal map differently (CHKB 05), if a consistency
      *> rule is imposed or the literal is used (OPENM not 00, CHKM 00),
      *> or if a blank content opens a file.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ASG01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT DYN ASSIGN USING WS-NAME
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS ST-D.
           SELECT MIX ASSIGN TO "G2ASGM.DAT" USING WS-NAME
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS ST-M.
           SELECT OPTIONAL CHKA ASSIGN TO "G2ASGA.DAT"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS ST-C.
           SELECT OPTIONAL CHKB ASSIGN TO "G2ASGB"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS ST-C.
           SELECT OPTIONAL CHKM ASSIGN TO "G2ASGM.DAT"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS ST-C.
           SELECT OPTIONAL CHKE ASSIGN TO "G2ASGE.DAT"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS ST-C.
       DATA DIVISION.
       FILE SECTION.
       FD  DYN.
       01  DYN-REC     PIC X(4).
       FD  MIX.
       01  MIX-REC     PIC X(4).
       FD  CHKA.
       01  CHKA-REC    PIC X(4).
       FD  CHKB.
       01  CHKB-REC    PIC X(4).
       FD  CHKM.
       01  CHKM-REC    PIC X(4).
       FD  CHKE.
       01  CHKE-REC    PIC X(4).
       WORKING-STORAGE SECTION.
       01  WS-NAME     PIC X(20).
       01  ST-D        PIC XX.
       01  ST-M        PIC XX.
       01  ST-C        PIC XX.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "  G2ASGA.DAT" TO WS-NAME.
           OPEN OUTPUT DYN.
           DISPLAY "OPENA=" ST-D.
           MOVE "AAAA" TO DYN-REC. WRITE DYN-REC.
           CLOSE DYN.
           OPEN INPUT CHKA.
           MOVE SPACES TO CHKA-REC.
           READ CHKA AT END CONTINUE END-READ.
           DISPLAY "READA=" ST-C " " CHKA-REC.
           CLOSE CHKA.
           MOVE "G2ASGB" TO WS-NAME.
           OPEN OUTPUT DYN.
           DISPLAY "OPENB=" ST-D.
           MOVE "BBBB" TO DYN-REC. WRITE DYN-REC.
           CLOSE DYN.
           OPEN INPUT CHKB.
           MOVE SPACES TO CHKB-REC.
           READ CHKB AT END CONTINUE END-READ.
           DISPLAY "READB=" ST-C " " CHKB-REC.
           CLOSE CHKB.
           MOVE "G2ASGE.DAT" TO WS-NAME.
           OPEN OUTPUT MIX.
           DISPLAY "OPENM=" ST-M.
           MOVE "EEEE" TO MIX-REC. WRITE MIX-REC.
           CLOSE MIX.
           OPEN INPUT CHKM.
           DISPLAY "CHKM=" ST-C.
           CLOSE CHKM.
           OPEN INPUT CHKE.
           MOVE SPACES TO CHKE-REC.
           READ CHKE AT END CONTINUE END-READ.
           DISPLAY "READE=" ST-C " " CHKE-REC.
           CLOSE CHKE.
           MOVE SPACES TO WS-NAME.
           OPEN OUTPUT DYN.
           DISPLAY "BLANK=" ST-D.
           STOP RUN.
