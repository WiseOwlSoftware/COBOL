      *> kb/Work PB183's COMPANION DERIVATION, as a regression pin.
      *>
      *> THE QUESTION: does ISO 13.18.57.3 SR4's letter - "If type-name-1 is described with the STRONG
      *> phrase, the subject of the entry shall not be implicitly or explicitly redefined in whole or
      *> in part" - overturn the carve-out CheckStrongTypeDeclarations records, that an INTERNAL
      *> redefine (subject and target inside one strong subtree, cloned in from the template) is
      *> legitimate and not flagged? PB179's cross-root narrowing deliberately preserved that carve-out
      *> pending this adjudication.
      *>
      *> THE ANSWER IS NO. SR4's subject is the entry CARRYING the TYPE clause, and 13.18.57.3 SR2 - "A
      *> data description entry in which a TYPE clause is specified shall not be followed immediately
      *> by a subordinate data description entry or a level 88 entry" - makes a subordinate REDEFINES
      *> under a TYPE entry syntactically unwritable. The only REDEFINES that can reach a strong
      *> subtree from outside is `01 X REDEFINES S.`, which is already rejected. A REDEFINES written
      *> INSIDE the typedef TEMPLATE, as SB is here, has no TYPE-clause subject at all, so SR4 never
      *> reaches it. The carve-out stands; this golden is what keeps a later "tightening" from quietly
      *> removing it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB183STRREDEF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 STRT IS TYPEDEF STRONG.
          05 SA PIC X(4) VALUE "abcd".
          05 SB REDEFINES SA PIC X(2).
       01 S1 TYPE STRT.
       01 S2 TYPE STRT.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "SA=" SA IN S1 " SB=" SB IN S1.
           MOVE "wxyz" TO SA IN S2.
           DISPLAY "SB2=" SB IN S2.
           STOP RUN.
