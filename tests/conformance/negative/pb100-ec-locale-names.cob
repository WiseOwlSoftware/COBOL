      *> reject-at: 2002 2014 2023
      *> ISO 1989:2023 Annex A.4.9 item 1 - the EC-LOCALE family "in the RAISING phrase of the EXIT and GOBACK
      *> statements, the RAISING phrase of the procedure division header, the USE statement, the WHEN phrase" are
      *> elements of the optional locale module COBOL.NET documents as not supported (CONFORMANCE.md 4 item 5); A.4.1
      *> admits their syntax only when support is claimed, so every naming site refuses them BY NAME through the ONE
      *> exception-name funnel (COBOLNET1518, kb/Work PB100 - they used to be accepted, and could never occur).
      *>
      *> The WHEN phrase below named EC-ORDER-NOT-SUPPORTED and now names EC-LOCALE-MISSING (kb/Work PB101 T7):
      *> EC-ORDER-NOT-SUPPORTED is NOT an A.4.9 locale name. Annex A.3 item 25 groups it with STANDARD-COMPARE and
      *> the ORDER TABLE clause as ISO/IEC 14651:2020-dependent, that support is now claimed, and its raise site
      *> (15.85.4 r2) is live - so the name is legal again and 2002/pb101_ec_order_not_supported OBSERVES it. This
      *> fixture keeps testing the four sites it was written for, on names that are still refused.
       >>TURN EC-LOCALE-INCOMPATIBLE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB100EC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-LOCALE.
       H-P.
           CONTINUE.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           PERFORM
               MOVE 1 TO X
           WHEN EC-LOCALE-MISSING
               CONTINUE
           END-PERFORM
           RAISE EXCEPTION EC-LOCALE-SIZE
           GOBACK RAISING EXCEPTION EC-LOCALE-INVALID.
