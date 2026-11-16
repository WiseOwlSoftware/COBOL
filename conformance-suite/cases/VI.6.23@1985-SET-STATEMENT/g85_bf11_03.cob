      *>--- conformance-case
      *> id: g85_bf11_03
      *> rules: [1985-GR-VI.6.23.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.23.4 GR1 - "Index-names are associated with a given
      *> table by being specified in the INDEXED BY phrase of the OCCURS
      *> clause for that table." Two tables of different element sizes
      *> (TS: 1 character, TL: 3 characters) each name their own index.
      *> SET IS1 IL1 TO 2 sets both to occurrence 2 (GR3a); each
      *> index then addresses occurrence 2 OF ITS OWN TABLE: TS (IS1) =
      *> "b", TL (IL1) = "BBB". SET IL1 TO IS1 carries the occurrence
      *> number from one table's index to the other's (GR3a), so after
      *> SET IS1 TO 4 both address occurrence 4: "d" and "DDD". After
      *> SET IS1 UP BY 1 from 2 (GR4), TS (IS1) = "c" while TL (IL1),
      *> set to 2, is still "BBB". An index associated with the wrong
      *> table, or holding a displacement of the wrong element size,
      *> prints another element.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. G85BF1103.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01  TSV       PIC X(4) VALUE "abcd".
000600 01  TSR REDEFINES TSV.
000700     05  TS    PIC X OCCURS 4 TIMES INDEXED BY IS1.
000800 01  TLV       PIC X(12) VALUE "AAABBBCCCDDD".
000900 01  TLR REDEFINES TLV.
001000     05  TL    PIC X(3) OCCURS 4 TIMES INDEXED BY IL1.
001100 PROCEDURE DIVISION.
001200 MAIN-PARA.
001300     SET IS1 IL1 TO 2.
001400     DISPLAY "S=" TS (IS1) " L=" TL (IL1).
001500     SET IS1 TO 4.
001600     SET IL1 TO IS1.
001700     DISPLAY "S=" TS (IS1) " L=" TL (IL1).
001800     SET IS1 TO 2.
001900     SET IS1 UP BY 1.
002000     SET IL1 TO 2.
002100     DISPLAY "S=" TS (IS1) " L=" TL (IL1).
002200     STOP RUN.
