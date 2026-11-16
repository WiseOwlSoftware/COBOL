      *>--- conformance-case
      *> id: g2002-cd-typedef-member-refs
      *> rules: [2002-GR-13.16.56.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> At 2002 the record description entries after a CD entry are
      *> ordinary data description entries (§2002:A.1.1.2), and
      *> §2002:13.14.2 SR14 limits TYPEDEF to a level-1 entry with no
      *> section restriction, so the type declarations TC and TD below
      *> are written under an output CD and TC types two groups of the
      *> CD record CR.
      *> §2002:13.16.56.3 GR1 - "The data-names of the items described
      *>   in these subordinate entries may be referenced only as
      *>   subordinate items of groups defined using the type-name."
      *>   and "If there is more than one such group, qualification
      *>   with the name of the group is necessary.": TA and TB, and
      *>   the condition-name TB-OK (a condition-name entry is part of
      *>   the type declaration), are reached as items of CX and CY,
      *>   qualified by the group name. CX and CY are distinct items:
      *>   line CX=AB CY=CD, line COND OK.
      *> §2002:13.16.56.3 GR1 - "any reference to a name that is the
      *>   same as a data-name in the subordinate entry is a reference
      *>   to another resource, if there is another resource with that
      *>   name": no group is defined using TD, so the unqualified TQ is
      *>   the working-storage TQ: line TQ=OTHR.
      *> §2002:13.16.56.3 GR3 - the subordinate descriptions are assumed
      *>   by CX and CY (2 + 2 positions each), so CR is 8 positions and
      *>   holds AB NO CD OK in order: lines CR LEN=08, CR=ABNOCDOK.
      *> Each leg fails if a member of a CD type declaration is not
      *> reachable through its group, if the two groups share storage,
      *> if the 88 is lost, or if TQ resolves to the member of TD.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2TDF01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TQ PIC X(4) VALUE "OTHR".
       01  W-LEN PIC 99.
       COMMUNICATION SECTION.
       CD  CO FOR OUTPUT
           TEXT LENGTH IS CO-LEN
           STATUS KEY IS CO-STAT.
       01  TC TYPEDEF.
           05  TA PIC X(2).
           05  TB PIC X(2).
               88  TB-OK VALUE "OK".
       01  TD TYPEDEF.
           05  TQ PIC X(2).
       01  CR.
           05  CX TYPE TC.
           05  CY TYPE TC.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "AB" TO TA OF CX OF CR
           MOVE "CD" TO TA OF CY
           MOVE "OK" TO TB OF CY
           MOVE "NO" TO TB OF CX
           DISPLAY "CX=" TA OF CX " CY=" TA OF CY
           IF TB-OK OF CY AND NOT TB-OK OF CX
               DISPLAY "COND OK"
           ELSE
               DISPLAY "COND BAD"
           END-IF
           DISPLAY "TQ=" TQ
           COMPUTE W-LEN = FUNCTION LENGTH(CR)
           DISPLAY "CR LEN=" W-LEN
           DISPLAY "CR=" CR
           STOP RUN.
