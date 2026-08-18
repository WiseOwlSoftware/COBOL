      *> PB96 - a level-66 RENAMES THRU alias is the record's STORAGE WINDOW from data-name-2's first character to
      *> data-name-3's last (13.18.45.4 GR1/GR2): a REDEFINES view inside the range adds no characters (13.18.44 -
      *> B overlays A), a FROM / THRU that is itself a redefinition maps to the area it overlays, and a boundary that
      *> falls inside a leaf (AB: A THRU B, where B redefines only the first two characters of A) is a partial part -
      *> the alias reads and writes exactly those characters. Before PB96 the span listed every leaf between FROM and
      *> THRU, views included: AC was "abcdabef" (8), not "abcdef" (6).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB96RN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 REC.
          05 A PIC X(4) VALUE "abcd".
          05 B REDEFINES A PIC X(2).
          05 C PIC X(2) VALUE "ef".
          05 G.
             10 G1 PIC X(2) VALUE "gh".
             10 G2 PIC X(2) VALUE "ij".
       66 AC RENAMES A THRU C.
       66 AB RENAMES A THRU B.
       66 BC RENAMES B THRU C.
       66 CG RENAMES C THRU G1.
       66 BG RENAMES B THRU G.
       PROCEDURE DIVISION.
           DISPLAY "AC=[" AC "] " FUNCTION LENGTH(AC).
           DISPLAY "AB=[" AB "] " FUNCTION LENGTH(AB).
           DISPLAY "BC=[" BC "] " FUNCTION LENGTH(BC).
           DISPLAY "CG=[" CG "] " FUNCTION LENGTH(CG).
           DISPLAY "BG=[" BG "] " FUNCTION LENGTH(BG).
           MOVE "12345678" TO BG.
           DISPLAY "A=[" A "] C=[" C "] G1=[" G1 "] G2=[" G2 "]".
           MOVE "XY" TO AB.
           DISPLAY "A=[" A "] AC=[" AC "]".
           STOP RUN.
