      *>--- conformance-case
      *> id: g85_bf09_01
      *> rules: [1985-SR-VI.5.3.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.3.3 SR2 - the data-name-1 or FILLER clause first,
      *> the REDEFINES clause next (or right after the level-number when
      *> neither is written), and the remaining clauses in any order.
      *>   cite.py: OK  §1985:VI.5.3.3 SR2 "The remaining clauses may be
      *>     written in any order"
      *>   cite.py: OK  §1985:VI.5.10.3 SR8 "Multiple redefinitions of
      *>     the same character positions must all use the data-name of
      *>     the entry that originally defined the area"
      *> DERIVATION. B, the FILLER entry and the unnamed entry each
      *> redefine A (SR2's three placements of REDEFINES); B is PIC 9(4)
      *> over A's "1234", so DISPLAY B shows 1234, and ADD 1 TO B leaves
      *> A "1235". C writes VALUE before PICTURE: XY. D writes USAGE and
      *> VALUE before PICTURE: 12. E writes SIGN and VALUE before
      *> PICTURE; a leading separate sign is the character "-" before
      *> the digit (§1985:VI.5.12.4 GR6c): -5. F writes JUSTIFIED before
      *> PICTURE, and MOVE "Z" right-justifies it: [   Z]. A compiler
      *> that refuses any of the orders fails to translate; one that
      *> misplaces a REDEFINES storage shows other digits on lines 1 and
      *> 2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0901.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  REC.
           05  A PIC X(4) VALUE "1234".
           05  B REDEFINES A PIC 9(4).
           05  FILLER REDEFINES A PIC X(2).
           05  REDEFINES A PIC X(1).
       01  C VALUE "XY" PIC X(2).
       01  D USAGE DISPLAY VALUE 12 PIC 99.
       01  E SIGN LEADING SEPARATE VALUE -5 PIC S9.
       01  F JUST RIGHT PIC X(4).
       PROCEDURE DIVISION.
       P1.
           DISPLAY B.
           ADD 1 TO B.
           DISPLAY A.
           DISPLAY C.
           DISPLAY D.
           DISPLAY E.
           MOVE "Z" TO F.
           DISPLAY "[" F "]".
           STOP RUN.
