      *>--- conformance-case
      *> id: g85_bf11_06
      *> rules: [1985-GR-VI.5.12.4-2, 1985-GR-VI.5.12.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.12.4 GR2 - "If a SIGN clause is specified in a group
      *> item subordinate to a group item for which a SIGN clause is
      *> specified, the SIGN clause specified in the subordinate group
      *> item takes precedence for that subordinate group item."
      *> §1985:VI.5.12.4 GR3 - "... the SIGN clause specified in the
      *> subordinate elementary numeric data description entry takes
      *> precedence for that elementary numeric data item."
      *> Every clause is SEPARATE, so GR6a and GR6c fix the whole image:
      *> the sign is its own leading or trailing character, '+' or '-'.
      *> G carries LEADING SEPARATE; SUB (a group under G) TRAILING.
      *>   S1 (in SUB, no clause): GR2 gives SUB's clause, -12 = "012-".
      *>   S2 (in SUB, own LEADING): GR3 gives its own, -12 = "-012".
      *>   S3 (in G, no clause): G's clause (GR1), -12 = "-012".
      *>   S4 (in G, own TRAILING): GR3, +7 = "007+".
      *> Line SUB pins GR2 (and GR3 inside it), line S4 pins GR3; S3 is
      *> the control showing G's own clause still reaches its other
      *> members. Losing GR2 makes SUB read [-012-012]; losing GR3 makes
      *> SUB read [012-012-] or S4 read [+007]. The last line repeats
      *> the check on MOVE results: -345 into S1 is "345-", 678 into S4
      *> is "678+".
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. G85BF1106.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01  G SIGN IS LEADING SEPARATE CHARACTER.
000600     05  SUB SIGN IS TRAILING SEPARATE.
000700         10  S1    PIC S9(3) VALUE -12.
000800         10  S2    PIC S9(3) SIGN IS LEADING SEPARATE
000900                   VALUE -12.
001000     05  S3    PIC S9(3) VALUE -12.
001100     05  S4    PIC S9(3) SIGN IS TRAILING SEPARATE VALUE +7.
001200 01  GX REDEFINES G.
001300     05  XSUB  PIC X(8).
001400     05  XS3   PIC X(4).
001500     05  XS4   PIC X(4).
001600 PROCEDURE DIVISION.
001700 MAIN-PARA.
001800     DISPLAY "SUB=[" XSUB "]".
001900     DISPLAY "S3=[" XS3 "]".
002000     DISPLAY "S4=[" XS4 "]".
002100     MOVE -345 TO S1.
002200     MOVE 678 TO S4.
002300     DISPLAY "SUB=[" XSUB "] S4=[" XS4 "]".
002400     STOP RUN.
