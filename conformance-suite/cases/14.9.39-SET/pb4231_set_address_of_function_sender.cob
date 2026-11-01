*>--- conformance-case
*> id: pb4231_set_address_of_function_sender
*> rules: [SR-14.9.39.3-17, GR-14.9.39.4-12, GR-14.9.39.4-13, FMT-14.9.39.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4231 — SET Format 7 with an `ADDRESS OF data-name-1` receiver takes ANY identifier as its sender
      *> identifier-6, a function-identifier included: §14.9.39.2 Format 7 prints `SET { ADDRESS OF data-name-1 |
      *> identifier-5 } ... TO identifier-6`, §8.4.3.1.3 SR1 makes identifier-6 "any of the formats for an identifier",
      *> §8.4.3.2.1 "A function-identifier references the unique data item that results from the evaluation of a
      *> function", and §14.9.39.3 SR17 asks only "Identifier-6 shall be of category data-pointer" (PB4231ID RETURNS a
      *> USAGE POINTER item). The grammar's former Format-7 rule spelled the sender as a bare data reference, so this
      *> was COBOL0001 "unexpected 'FUNCTION'" at every edition; the one setToStatement rule reads the sender through
      *> the expression tier, as the identifier-5 route already did (kb/Work PB1929).
      *> EXPECTED OUTPUT, derived line by line:
      *>   1  P1 holds ADDRESS OF R1 (§8.4.3.11.4 GR1); PB4231ID returns it unchanged, and §14.9.39.4 GR13 "the address
      *>      identified by identifier-6 is assigned to each based item referenced by data-name-1", so B1 is R1  1=ABCD
      *>   2  the receiving list mixes the two spellings: GR13 rebases B1 and GR12 ("stored in each data item referenced
      *>      by identifier-5 in the order specified") stores the same address in P2; P1 now addresses R2         2=WXYZ
      *>   3  SET ADDRESS OF B1 TO P2: P2 holds the address line 2 stored, R2's                                   3=WXYZ
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB4231ID.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-P USAGE POINTER.
       01 L-RES USAGE POINTER.
       PROCEDURE DIVISION USING L-P RETURNING L-RES.
           SET L-RES TO L-P
           GOBACK.
       END FUNCTION PB4231ID.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4231M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PB4231ID.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R1 PIC X(4) VALUE "ABCD".
       01 R2 PIC X(4) VALUE "WXYZ".
       01 P1 USAGE POINTER.
       01 P2 USAGE POINTER.
       01 B1 PIC X(4) BASED.
       PROCEDURE DIVISION.
           SET P1 TO ADDRESS OF R1
           SET ADDRESS OF B1 TO FUNCTION PB4231ID(P1)
           DISPLAY "1=" B1
           SET P1 TO ADDRESS OF R2
           SET ADDRESS OF B1 P2 TO FUNCTION PB4231ID(P1)
           DISPLAY "2=" B1
           SET ADDRESS OF B1 TO P2
           DISPLAY "3=" B1
           GOBACK.
       END PROGRAM PB4231M.
