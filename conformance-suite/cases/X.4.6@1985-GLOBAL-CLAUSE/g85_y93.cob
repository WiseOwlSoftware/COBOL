      *>--- conformance-case
      *> id: g85_y93
      *> rules: [1985-GR-X.4.6.4-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.4.6.4 GR3 (cite.py --check OK): "If the GLOBAL clause is
      *> used in a data description entry which contains the REDEFINES
      *> clause, it is only the subject of that REDEFINES clause which
      *> possesses the global attribute." A refusal: WS-A is the object
      *> of REDEFINES, not its subject, so it is not a global name, and
      *> the contained program G85Y93B, which does not describe it, has
      *> no WS-A to reference (line DISPLAY WS-A). The positive half
      *> (WS-B is global) is pb1523_global_fd_records_and_redefiners.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85Y93.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-A    PIC X(4) VALUE "1234".
       01  WS-B    REDEFINES WS-A GLOBAL PIC X(4).
       PROCEDURE DIVISION.
       P1.
           CALL "G85Y93B".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85Y93B.
       PROCEDURE DIVISION.
       P2.
           DISPLAY WS-B.
           DISPLAY WS-A.
           EXIT PROGRAM.
       END PROGRAM G85Y93B.
       END PROGRAM G85Y93.
