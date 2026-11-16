      *>--- conformance-case
      *> id: g85_bf31_07
      *> rules: [1985-GR-X.4.5.4-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.4.5.4 GR3 - "Use of the EXTERNAL clause does not imply
      *> that the associated file-name or data-name is a global name."
      *> REFUSAL beside g85_bf31_06 (not itself the row's evidence: the
      *> row is a GR). BF31-XREC is EXTERNAL but not GLOBAL in MAIN, so
      *> the contained N, which does not describe it, has no
      *> declaration of the name: its DISPLAY BF31-XREC must be
      *> refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3107.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  BF31-XREC EXTERNAL PIC X(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "OUTER" TO BF31-XREC.
           CALL "G85BF3107N".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3107N.
       PROCEDURE DIVISION.
       N-PARA.
           DISPLAY BF31-XREC.
           EXIT PROGRAM.
       END PROGRAM G85BF3107N.
       END PROGRAM G85BF3107.
