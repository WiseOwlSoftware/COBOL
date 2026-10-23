*>--- conformance-case
*> id: declined-ec-flow-apply-commit-use
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3338: a USE declarative for EC-FLOW-APPLY-COMMIT is ACCEPTED. The name was refused while the commit
*> and rollback facility was declined (Annex A.4.3 item 3, COBOLNET1710); the facility is now provided for the keyed
*> organizations and the activation of an APPLY COMMIT clause raises the condition (ISO 12.4.6.3.4 GR3/GR4).
*> NOTE the standard writes EC-FLOW-APPLY (no -COMMIT suffix) at 9.1.18.7 and 12.4.6.3.4 GR3/GR4, a name that has
*> no row in the 14.6.13.1.6 table at all; Table 13's EC-FLOW-APPLY-COMMIT row describes the condition, and it is
*> the name raised. The file keeps its id so the evidence that names it still resolves.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DCLECFA.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-SEC SECTION.
           USE AFTER EXCEPTION CONDITION EC-FLOW-APPLY-COMMIT.
       D-PARA.
           DISPLAY "HANDLER".
       END DECLARATIVES.
       MAIN SECTION.
       M-PARA.
           DISPLAY "ACCEPTED".
           STOP RUN.
