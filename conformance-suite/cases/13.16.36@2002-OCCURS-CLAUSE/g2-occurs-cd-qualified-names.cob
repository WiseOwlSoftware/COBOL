      *>--- conformance-case
      *> id: g2-occurs-cd-qualified-names
      *> rules: [2002-SR-13.16.36.2-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.36.2 SR2 - "Data-name-1 and data-name-2 may be
      *> qualified." In a record after an OUTPUT CD (obsolete warning
      *> COBOL0903), the format 2 table E is written DEPENDING ON M OF
      *> C1 with ASCENDING KEY IS K OF E, and the format 1 table E1 with
      *> DESCENDING KEY IS K OF E1; M and K are each declared more than
      *> once, so only the qualifier picks the item.
      *> LEN: M OF C1 = 3 and M OF C2 = 9. §2002:13.16.36.3 GR7 - M OF
      *> C1 "represents the current number of occurrences", and GR8a
      *> with §2002:15.39.3 RV4b - T, whose data-name-1 is outside it,
      *> uses "only that part of the table area that is specified by
      *> the value" of M OF C1: LENGTH(T) = 3 x 1 = 3. LENGTH(T1) =
      *> 3 x 1 = 3 (format 1, fixed). Fails if M OF C2 is used (9 is
      *> out of the bounds 1 to 4).
      *> E AT / E1 AT: SEARCH ALL uses the KEY phrase (§2002:13.16.36.3
      *> GR3): K OF E holds 1, 2, 3 ascending and the WHEN asks for 2, so
      *> IX is set to occurrence 2; K OF E1 holds 9, 5, 1 descending and
      *> the WHEN asks for 1, so IX1 is set to occurrence 3. Fails if a
      *> KEY phrase is bound to another K.
      *> The lengths are moved to PIC 9 items before DISPLAY so the
      *> implementor-defined DISPLAY form of an integer function
      *> (§2002:14.8.10.3 GR1) does not enter the expectation.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OCCQ2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-P PIC 9.
       01  WS-L1 PIC 9.
       01  WS-L2 PIC 9.
       COMMUNICATION SECTION.
       CD  COUT FOR OUTPUT.
       01  R1.
           05 C1.
              10 M PIC 9.
              10 K PIC 9.
           05 C2.
              10 M PIC 9.
           05 T1.
              10 E1 OCCURS 3 TIMES
                   DESCENDING KEY IS K OF E1 INDEXED BY IX1.
                 15 K PIC 9.
           05 T.
              10 E OCCURS 1 TO 4 TIMES DEPENDING ON M OF C1
                   ASCENDING KEY IS K OF E INDEXED BY IX.
                 15 K PIC 9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 3 TO M OF C1
           MOVE 9 TO M OF C2
           MOVE 0 TO K OF C1
           MOVE 1 TO K OF E (1)
           MOVE 2 TO K OF E (2)
           MOVE 3 TO K OF E (3)
           MOVE 9 TO K OF E1 (1)
           MOVE 5 TO K OF E1 (2)
           MOVE 1 TO K OF E1 (3)
           MOVE FUNCTION LENGTH(T) TO WS-L1
           MOVE FUNCTION LENGTH(T1) TO WS-L2
           DISPLAY "LEN T " WS-L1 " T1 " WS-L2
           SEARCH ALL E
              AT END DISPLAY "E NOT FOUND"
              WHEN K OF E (IX) = 2
                 SET WS-P TO IX
                 DISPLAY "E AT " WS-P
           END-SEARCH
           SEARCH ALL E1
              AT END DISPLAY "E1 NOT FOUND"
              WHEN K OF E1 (IX1) = 1
                 SET WS-P TO IX1
                 DISPLAY "E1 AT " WS-P
           END-SEARCH
           STOP RUN.
