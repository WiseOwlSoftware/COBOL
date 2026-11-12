      *>--- conformance-case
      *> id: pb5240_key_function_identifier
      *> rules: [1985-SR-IV.4.3.8.3.3-3, 1985-SR-XIV.3.3.3-4,
      *>     1985-SR-XIV.3.2.3-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      * X3.23-1985 (Amendment 1) IV 4.3.8.3.3 SR3: "A function-
      * identifier which references an alphanumeric function may be
      * specified anywhere in the general formats that an identifier
      * is permitted". The KEY phrase of ENABLE (XIV 3.3.2) and
      * DISABLE (XIV 3.2.2) prints identifier-1 or literal-1, and
      * SR4 of each asks only that it "be defined as alphanumeric"
      * (kb/Work PB5240, PB5082). Each statement below is legal at
      * 85, so the program translates; it fails if any KEY operand is
      * refused. No MCS definitions exist, so the statements only set
      * their status keys; the program displays nothing.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5240K.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-K PIC X(8) VALUE "password".
       01 WS-G.
          05 WS-G1 PIC X(4) VALUE "PASS".
       COMMUNICATION SECTION.
       CD CD-IN FOR INPUT
            SYMBOLIC QUEUE IS IQ
            STATUS KEY IS IST.
       CD CD-OUT FOR OUTPUT
            DESTINATION COUNT IS ODC
            STATUS KEY IS OST
            SYMBOLIC DESTINATION IS ODS.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "QUEUEA" TO IQ.
           ENABLE INPUT CD-IN WITH KEY FUNCTION UPPER-CASE (WS-K).
           DISABLE INPUT CD-IN KEY FUNCTION REVERSE (WS-K).
           ENABLE OUTPUT CD-OUT KEY WS-K.
           DISABLE OUTPUT CD-OUT WITH KEY WS-G.
           ENABLE OUTPUT CD-OUT KEY "PASSWORD".
           STOP RUN.
