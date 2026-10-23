*>--- conformance-case
*> id: declined-ec-flow-commit-turn
*> rules: [GR-14.9.7.4-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3338: EC-FLOW-COMMIT in the TURN compiler directive is ACCEPTED. The name was refused while the
*> commit and rollback facility was declined (Annex A.4.3 item 3, COBOLNET1710); the facility is now provided
*> for the keyed organizations and TransactionControl raises the condition (ISO 14.9.7.4 GR2), so the
*> directive enables a real check. The file keeps its id so the evidence that names it still resolves.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DCLECFT.
       PROCEDURE DIVISION.
       >>TURN EC-FLOW-COMMIT CHECKING ON
           DISPLAY "ACCEPTED".
           STOP RUN.
