      *> ISO §14.9.1.3 SR4/SR5 — the ACCEPT statement's Format 3
      *> (screen) is Annex A.4.2 item 1, an OPTIONAL element (§4.2.7)
      *> this implementation DECLINES (docs/CONFORMANCE.md §4 item 4);
      *> A.4.1 carries the licence to SR4 and SR5.
      *> COMPILE-ONLY control (no .out), the ACCEPT twin of
      *> screen_section_reference: "ACCEPT screen-name-1" with the AT
      *> phrase omitted is the MINIMAL legal Format 3 (§14.9.1.2 — the
      *> whole AT phrase sits inside the outer bracket), so it is the
      *> shape a Format-3 program is most likely to be written in.
      *> SR4 CONSTRAINS THE REFERENCE, NOT THE DECLARATION: "Screen-
      *> name-1 may reference a group item containing screen items with
      *> FROM or VALUE clauses only if the group also contains screen
      *> items with TO or USING clauses." A screen description entry on
      *> its own violates nothing — only an ACCEPT that REFERENCES such
      *> a group can. SG is therefore written as the shape SR4 forbids
      *> (one FROM item, NO TO or USING item) and the ACCEPT below
      *> references it. Under the decline that reference is ACCEPTED
      *> and SR4 is not enforced, but the facility is still NAMED:
      *> COBOLNET1560 is the only diagnostic the program draws.
      *> Under the decline SG is DECLARED (kb/Work R32 registers screen
      *> names) so the reference is not the §8.4.2.1 "not defined"
      *> verdict; the statement is refused at run time instead of
      *> quietly doing nothing. DocumentedNonSupportWitnessTests pins
      *> both halves: COBOLNET1560 at compile time, and a NON-clean run.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SCRW03.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC X(4) VALUE "ABCD".
       SCREEN SECTION.
       01 SG.
          05 SI-1 LINE 1 COL 1 PIC X(4) FROM WS-A.
       PROCEDURE DIVISION.
       MAIN.
           ACCEPT SG END-ACCEPT.
           STOP RUN.
