      *>--- conformance-case
      *> id: g2-mod-noninteger-refused
      *> rules: [2002-AR-15.52.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.52.2 AR1 - "Argument-1 and argument-2 shall be
      *> integers." The literal 2.5 is neither an integer value nor an
      *> integer data item (§2002:15.2 AR5), so MOD(7 2.5) is refused at
      *> 2002. The accepted arm is g2-mod-integer-arguments.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2MOD02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  R       PIC -9(9).
       PROCEDURE DIVISION.
       P-MAIN.
           COMPUTE R = FUNCTION MOD(7 2.5)
           STOP RUN.
