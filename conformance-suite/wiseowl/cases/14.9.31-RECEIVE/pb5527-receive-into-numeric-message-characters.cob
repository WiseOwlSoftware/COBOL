      *>--- conformance-case
      *> id: pb5527-receive-into-numeric-message-characters
      *> rules: [GR-14.9.31.4-1, GR-14.9.31.4-3]
      *> accept: [2023]
      *> files: [pb5527-receive-into-numeric-message-characters-server.cbl]
      *> servers: {Balance: pb5527-receive-into-numeric-message-characters-server.cbl}
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5527. 14.9.31.4 GR1b: "The content of identifier-1 is the content of the message" (cite.py --check 14.9.31.4 GR1b -> OK),
      *> the sender's storage positions verbatim. The server replies the four characters AB34; the requestor receives them into the
      *> elementary numeric display item K-NUM (PIC 9(4) VALUE 1234), so K-NUM holds the characters AB34 and DISPLAY shows them. A native
      *> numeric carrier parses digits only and printed 0034. GR3: data-name-2 holds the length, 4.
      *> EXPECTED VALUES, DERIVED: "GOT AB34 LEN=0004".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5527C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-MSG   PIC X(4) VALUE "ACCT".
       01 WS-TAG   USAGE MESSAGE-TAG.
       01 K-NUM    PIC 9(4) VALUE 1234.
       01 WS-LEN   PIC 9(4) VALUE ZERO.
       PROCEDURE DIVISION.
       MAIN-P.
           SEND TO "BALANCE" FROM WS-MSG RETURNING WS-TAG
           END-SEND.
           RECEIVE WS-TAG GIVING K-NUM WS-LEN
           END-RECEIVE.
           DISPLAY "GOT " K-NUM " LEN=" WS-LEN.
           STOP RUN.
