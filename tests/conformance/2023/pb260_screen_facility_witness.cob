      *> ISO §14.9.11.4 GR12-GR19 / §14.9.1.3 SR4-SR5 / §14.9.11.3 SR3 —
      *> the Annex A.4.2 ACCEPT/DISPLAY screen module is DECLINED. A.4.2
      *> names it (item 1 ACCEPT format 3, item 8 SCREEN SECTION header,
      *> item 9 DISPLAY format 2, item 20 the screen description entry),
      *> A.4.1 makes "any associated syntax rules, general rules, other
      *> rules, exception conditions" optional with it, and §4.2.7 asks
      *> only that the decline be documented — docs/CONFORMANCE.md §4
      *> item 4 is that documentation.
      *> WITNESS that the DOCUMENTED posture is what ACTUALLY happens:
      *> the SCREEN SECTION and the Format-1 group / Format-2 elementary
      *> shapes those rules govern are ACCEPTED, the named COBOLNET1560
      *> non-support warning is emitted (pinned by conformance-test
      *> DocumentedNonSupportWitnessTests), and the facility is INERT —
      *> no screen behaviour reaches any device, so the program's only
      *> output is its ordinary Format-1 DISPLAY operands (§14.9.11.4
      *> GR1/GR6). A screen transfer would have to appear here.
      *> SG-OUT carries FROM/VALUE items and SG-IN a TO item: the entry
      *> shapes §14.9.11.4 GR13 transfers and GR12/GR14-GR16 position.
      *> NOTE — this fixture does NOT write §14.9.1.3 SR4's forbidden
      *> shape. SR4 constrains a REFERENCE ("screen-name-1 may
      *> reference a group item containing screen items with FROM or
      *> VALUE clauses only if the group also contains screen items
      *> with TO or USING clauses"), and no screen-name is referenced
      *> in this procedure division, so no declaration here can violate
      *> it. The SR4 reference is pb260_accept_screen_reference's
      *> witness; the Format-2 DISPLAY reference is
      *> screen_section_reference's.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SCRW01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC X(4) VALUE "ABCD".
       01 WS-N PIC 9(3) VALUE 7.
       SCREEN SECTION.
       01 SG-OUT.
          05 SO-1 LINE 1 COL 1 PIC X(4) FROM WS-A.
          05 SO-2 LINE 2 COL 1 PIC 9(3) FROM WS-N.
          05 SO-3 LINE 3 COL 1 PIC X(3) VALUE "ZZZ".
       01 SG-IN.
          05 SI-1 LINE 4 COL 1 PIC X(4) TO WS-A.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "A=" WS-A.
           DISPLAY "N=" WS-N.
           DISPLAY "NO SCREEN OUTPUT".
           STOP RUN.
