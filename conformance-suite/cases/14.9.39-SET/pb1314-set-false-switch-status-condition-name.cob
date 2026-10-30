*>--- conformance-case
*> id: pb1314-set-false-switch-status-condition-name
*> rules: [SR-14.9.39.3-6]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB1314 - the switch-status arm of the SR6 refusal, the sibling of
*> pb390-set-switch-status-condition-name (which writes TRUE). ISO 14.9.39.3 SR6: "Condition-name-1 shall be
*> associated with a conditional variable." SW-ON is a SPECIAL-NAMES condition-name (8.4.4.1's second kind),
*> associated with the status of an external switch and not with a conditional variable, so Format 4 refuses it
*> whichever of TRUE / FALSE is written. Both arms of RejectSetConditionName hard-coded `TO TRUE`; this one is
*> the arm that names the switch, and it must quote FALSE when FALSE is what the program wrote.
*> cite.py: OK  14.9.39.3 6)
*> Refused COBOL1757 at every edition; at 85 the FALSE phrase draws its introduction gate as well.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1314SW.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
SPECIAL-NAMES.
    SWITCH-1 IS SW-M ON STATUS IS SW-ON OFF STATUS IS SW-OFF.
PROCEDURE DIVISION.
MAIN-P.
    SET SW-ON TO FALSE.
    STOP RUN.
