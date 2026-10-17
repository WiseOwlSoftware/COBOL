*>--- conformance-case
*> id: pb3224_set_program_address_receiver_order
*> rules: [SR-14.9.39.3-22, GR-8.4.3.13.4-3]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.39.3 SR22 over a receiving LIST - identifier-7 is written `{ identifier-7 } ...`, and the rule's
      *> condition is each receiver's own restriction, so WHERE a restricted receiver stands in the list decides
      *> nothing:
      *>   "If identifier-7 references a restricted program-pointer, identifier-8 shall be the predefined address
      *>   NULL or shall reference a program-pointer and the program-prototypes associated with identifier-7 and
      *>   identifier-8 shall have the same signature."   cite.py: OK  §14.9.39.3 22)  (Syntax rules)
      *> GR3: "When program-prototype-name-1 is specified, the program-address-identifier has the
      *>   characteristics of a program-pointer restricted to program-prototype-name-1."
      *>   cite.py: OK  §8.4.3.13.4 3)  (General rules)
      *> RP is restricted to PB3224A; UPP is an ordinary (unrestricted) program-pointer, which SR22 does not
      *> constrain. The sender ADDRESS OF PROGRAM PB3224A is restricted to the very prototype RP is restricted to,
      *> so SR22 is met for RP in either position, and GR3's address is stored in every receiver in the order
      *> written (§14.9.39.4 GR16). The program is the accepted twin of the refusals
      *> pb3224-set-program-address-unrestricted-first-*, which differ only in the sender.
      *> EXPECTED OUTPUT, derived line by line:
      *>   UPP-SET   after `SET UPP RP TO ADDRESS OF PROGRAM PB3224A` UPP is no longer NULL.
      *>   RP-SET    ... and neither is RP.
      *>   PA OK     CALL through UPP activates the program GR2 identifies (PB3224D, externalized PB3224A).
      *>   PA OK     CALL through RP activates the same program.
      *>   UPP-SET2 / RP-SET2 / PA OK / PA OK   the same four lines after `SET RP UPP TO ...`, the receivers
      *>             in the other order.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3224A IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC X(2).
       PROCEDURE DIVISION USING L-X.
       END PROGRAM PB3224A.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3224M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM PB3224A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RPT IS TYPEDEF USAGE PROGRAM-POINTER TO PB3224A.
       01 RP TYPE RPT.
       01 UPP USAGE PROGRAM-POINTER.
       01 X1 PIC X(2) VALUE "OK".
       PROCEDURE DIVISION.
       MAIN-P.
           SET UPP RP TO ADDRESS OF PROGRAM PB3224A
           IF UPP NOT = NULL DISPLAY "UPP-SET" END-IF
           IF RP NOT = NULL DISPLAY "RP-SET" END-IF
           CALL UPP USING X1
           CALL RP USING X1
           SET UPP RP TO NULL
           SET RP UPP TO ADDRESS OF PROGRAM PB3224A
           IF UPP NOT = NULL DISPLAY "UPP-SET2" END-IF
           IF RP NOT = NULL DISPLAY "RP-SET2" END-IF
           CALL UPP USING X1
           CALL RP USING X1
           STOP RUN.
       END PROGRAM PB3224M.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3224D AS "PB3224A".
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC X(2).
       PROCEDURE DIVISION USING L-X.
       P-MAIN.
           DISPLAY "PA " L-X
           GOBACK.
       END PROGRAM PB3224D.
