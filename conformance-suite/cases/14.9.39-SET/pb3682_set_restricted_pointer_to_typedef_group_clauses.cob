*>--- conformance-case
*> id: pb3682_set_restricted_pointer_to_typedef_group_clauses
*> rules: [SR-14.9.39.3-19]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3682 - A RESTRICTED DATA-POINTER'S TYPE IS ONE DESCRIPTION, WHICHEVER FORM THE COMPILER HOLDS.
      *>   14.9.39.3 SR19: "If identifier-5 references a restricted data-pointer, identifier-6 shall be the predefined address
      *>   NULL or shall reference a data-pointer restricted to the same type" (cite.py: OK  14.9.39.3 19)), and 8.4.3.11.4 GR2:
      *>   the address of a strongly-typed group item is "a restricted data-pointer that is restricted to the type of
      *>   identifier-1" (cite.py: OK  8.4.3.11.4 2)). So PA, restricted to TA, accepts the address of WA, whose type is TA -
      *>   whatever clause the declaration's group wrote for its members (13.18.60.4 GR1 hands a group's USAGE down to each
      *>   elementary item, cite.py: OK  13.18.60.4 1); the SIGN clause of a group applies the same way, 13.18.52.4 GR1).
      *>   The pointer's restriction names the type declaration itself (the TEMPLATE) and WA's type is the declaration its TYPE
      *>   clause copied, and the §8.5.3.1 comparison reads both forms by the same characteristics. The usage walk and the SIGN
      *>   walk reached the copies and never the template, so the template's members kept the default usage and sign and the
      *>   same type was refused as two (COBOLNET0869).
      *>   Each leg can fail: any of the three SET statements is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3682N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TA TYPEDEF STRONG USAGE BINARY.
          05 A PIC 9(3).
       01 TB TYPEDEF STRONG SIGN IS LEADING SEPARATE.
          05 B PIC S9(3).
       01 TC TYPEDEF STRONG USAGE BINARY-LONG.
          05 C.
       01 PTA TYPEDEF USAGE POINTER TO TA.
       01 PTB TYPEDEF USAGE POINTER TO TB.
       01 PTC TYPEDEF USAGE POINTER TO TC.
       01 PA TYPE PTA.
       01 PB TYPE PTB.
       01 PC TYPE PTC.
       01 WA TYPE TA.
       01 WB TYPE TB.
       01 WC TYPE TC.
       PROCEDURE DIVISION.
           SET PA TO ADDRESS OF WA
           SET PB TO ADDRESS OF WB
           SET PC TO ADDRESS OF WC
           DISPLAY "OK"
           STOP RUN.
