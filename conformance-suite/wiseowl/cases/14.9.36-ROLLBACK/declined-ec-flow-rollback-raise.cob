*>--- conformance-case
*> id: declined-ec-flow-rollback-raise
*> rules: [GR-14.9.36.4-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3338: EC-FLOW-ROLLBACK is ACCEPTED as an operand of the TURN directive and of a USE declarative. The
*> name was refused while the commit and rollback facility was declined (Annex A.4.3 item 3, COBOLNET1710); the
*> facility is now provided for the keyed organizations and TransactionControl raises the condition (ISO
*> 14.9.36.4 GR2). The file keeps its id so the evidence that names it still resolves.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DCLECFR.
       PROCEDURE DIVISION.
       >>TURN EC-FLOW-ROLLBACK CHECKING ON
       DECLARATIVES.
       D-SEC SECTION.
           USE AFTER EXCEPTION CONDITION EC-FLOW-ROLLBACK.
       D-PARA.
           DISPLAY "HANDLER".
       END DECLARATIVES.
       MAIN SECTION.
       M-PARA.
           DISPLAY "ACCEPTED".
           STOP RUN.
