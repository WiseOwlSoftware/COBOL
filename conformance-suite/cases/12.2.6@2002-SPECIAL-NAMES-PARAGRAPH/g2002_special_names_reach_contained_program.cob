      *>--- conformance-case
      *> id: g2002_special_names_reach_contained_program
      *> rules: [2002-GR-12.2.6.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.2.6.3 GR1 - "All clauses specified in the SPECIAL-NAMES
      *> paragraph of a source unit that contains other source units
      *> apply to each directly or indirectly contained source unit. The
      *> condition-names, mnemonic-names, locale-names, class-names,
      *> currency signs and symbols, alphabet-names, and
      *> symbolic-characters specified in the SPECIAL-NAMES paragraph of
      *> the containing source unit may be referenced from any directly
      *> or indirectly contained source unit."
      *> G2SPN10 declares the names; G2SPN03I (directly contained) and
      *> G2SPN03J (indirectly contained, inside G2SPN03I) have no
      *> configuration section and reference them:
      *>   SYM=B     symbolic-character SC-B, ordinal 67 of STANDARD-1
      *>             (ISO/IEC 646: B), displayed UPON the container's
      *>             device mnemonic-name CONS;
      *>   CLASS-YES class-name ABC ("A" THRU "C") holds "AB";
      *>   ALPHA-YES alphabet-name STD1 in a class condition: "AB" is in
      *>             the ISO/IEC 646 coded character set;
      *>   AMT       the container's currency string EUR at its currency
      *>             symbol U: 12.5 into U999.99 is EUR012.50;
      *>   SW1-ONE   exactly one of the switch's condition-names holds;
      *>   J-SYM=B   the same names reach the indirectly contained unit.
      *> The feature mnemonic-name TOP-PAGE is referenced from the
      *> contained unit's SEND statement (an output CD; 2002 communication
      *> facility), placed after GOBACK so no message control system is
      *> needed: the leg fails if the name does not resolve there.
      *> Locale-names reaching a contained program are run at 2002 by
      *> cases/12.3.7-SPECIAL-NAMES/pb64t1_locale_scope_and_return.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SPN10.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SWITCH-1 IS SW1 ON STATUS IS SW1-ON OFF STATUS IS SW1-OFF
           C01 IS TOP-PAGE
           CONSOLE IS CONS
           ALPHABET STD1 IS STANDARD-1
           SYMBOLIC CHARACTERS SC-B IS 67 IN STD1
           CLASS ABC IS "A" THRU "C"
           CURRENCY SIGN IS "EUR" WITH PICTURE SYMBOL "U".
       PROCEDURE DIVISION.
       P-OUTER.
           CALL "G2SPN03I".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SPN03I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-B  PIC X.
       01  W-AB PIC XX VALUE "AB".
       01  AMT  PIC U999.99.
       01  MSG  PIC X(10) VALUE "HELLO".
       COMMUNICATION SECTION.
       CD  OUT-CD FOR OUTPUT
           DESTINATION COUNT IS OUT-COUNT
           STATUS KEY IS OUT-ST
           SYMBOLIC DESTINATION IS OUT-DEST.
       PROCEDURE DIVISION.
       P-INNER.
           MOVE SC-B TO W-B.
           DISPLAY "SYM=" W-B UPON CONS.
           IF W-AB IS ABC
               DISPLAY "CLASS-YES"
           ELSE
               DISPLAY "CLASS-NO".
           IF W-AB IS STD1
               DISPLAY "ALPHA-YES"
           ELSE
               DISPLAY "ALPHA-NO".
           MOVE 12.5 TO AMT.
           DISPLAY "AMT [" AMT "]".
           IF (SW1-ON AND NOT SW1-OFF) OR (SW1-OFF AND NOT SW1-ON)
               DISPLAY "SW1-ONE"
           ELSE
               DISPLAY "SW1-BOTH-OR-NEITHER".
           CALL "G2SPN03J".
           GOBACK.
       P-SEND.
           SEND OUT-CD FROM MSG WITH EMI
               AFTER ADVANCING TOP-PAGE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SPN03J.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-B  PIC X.
       PROCEDURE DIVISION.
       P-J.
           MOVE SC-B TO W-B.
           DISPLAY "J-SYM=" W-B UPON CONS.
           GOBACK.
       END PROGRAM G2SPN03J.
       END PROGRAM G2SPN03I.
       END PROGRAM G2SPN10.
