      *>--- conformance-case
      *> id: pb4958_key_named_typedef_is_a_user_word_85
      *> rules: [SR-8.3.2.1-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> TYPEDEF IS A USER-DEFINED WORD AT COBOL-85 (kb/Work PB4958). ISO 8.9 reserves it
      *> from COBOL-2002 only, and only a reserved word is refused as a user-defined word
      *> (§2023:8.3.2.1 1): "Reserved words shall not be used as user-defined words or
      *> system-names"). The table keys on K and on the item TYPEDEF. In ASCENDING KEY IS
      *> K TYPEDEF the reading of TYPEDEF as the COBOL-2002 TYPEDEF clause (§2023:13.18.58)
      *> parses too, and the reservation gate gives a declared free word its keyword back
      *> only where its user-word reading fails, which it does not here. SEARCH ALL finds
      *> the entry whose keys are "B" and "2", the second, and DISPLAY shows its
      *> occurrence number: 2. Fails if TYPEDEF is read as the clause (COBOL0900).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4958K.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TBL.
          05 ENT OCCURS 3 ASCENDING KEY IS K TYPEDEF INDEXED BY IX.
             10 K PIC X.
             10 TYPEDEF PIC X.
       01 N PIC 9.
       PROCEDURE DIVISION.
       P1.
           MOVE "A" TO K (1).
           MOVE "1" TO TYPEDEF (1).
           MOVE "B" TO K (2).
           MOVE "2" TO TYPEDEF (2).
           MOVE "C" TO K (3).
           MOVE "3" TO TYPEDEF (3).
           SEARCH ALL ENT
               AT END
                   DISPLAY "NOT FOUND"
               WHEN K (IX) = "B" AND TYPEDEF (IX) = "2"
                   SET N TO IX
                   DISPLAY N.
           STOP RUN.
