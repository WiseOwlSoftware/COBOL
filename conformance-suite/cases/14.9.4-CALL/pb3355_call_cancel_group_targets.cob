*>--- conformance-case
*> id: pb3355_call_cancel_group_targets
*> rules: [SR-14.9.4.3-1, SR-14.9.5.3-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3355 - the LEGAL side of ISO 14.9.4.3 SR1 and 14.9.5.3 SR1 ("Identifier-1 shall be defined as an
      *> alphanumeric ... data item", cite.py --check 14.9.5.3 "Identifier-1 shall be defined as an alphanumeric or
      *> national data item" -> OK). 8.5.2.1: "an alphanumeric group item has class and category alphanumeric"
      *> (cite.py --check 8.5.2.1 "an alphanumeric group item has class and category alphanumeric" -> OK), so a plain
      *> group whose content is the program-name is a legal target of CALL and of CANCEL: CALL activates the program the
      *> group's characters name (14.9.4.4 GR3 b), and CANCEL of the same group makes the next CALL start it afresh.
      *> Why this can fail: the category screen the strongly-typed-group refusals pb3355-call-target-strong-group and
      *> pb3355-cancel-target-strong-group rest on must still admit an alphanumeric group; a screen that refused
      *> every group (a group has no PICTURE) fails here.
      *> Expected: the called program counts its activations in WORKING-STORAGE, and 14.9.5.4 GR3 (cite.py --check
      *> 14.9.5.4 "If the program referenced by a successfully executed CANCEL statement in a run unit is
      *> subsequently called in that run unit, that program is in its initial state" -> OK) puts it in its initial
      *> state after the CANCEL, so both CALLs print COUNT=1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3355GRP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TARGET-GROUP.
          05 NAME-PART PIC X(8) VALUE "PB3355CT".
       PROCEDURE DIVISION.
       MAIN.
           CALL TARGET-GROUP
           CANCEL TARGET-GROUP
           CALL TARGET-GROUP
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3355CT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ACTIVATIONS PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P.
           ADD 1 TO ACTIVATIONS
           DISPLAY "COUNT=" ACTIVATIONS
           GOBACK.
       END PROGRAM PB3355CT.
       END PROGRAM PB3355GRP.
