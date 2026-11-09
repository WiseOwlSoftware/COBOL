      *>--- conformance-case
      *> id: pb4745-send-served-by-a-concurrent-run-unit
      *> rules: [GR-14.9.38.4-4, GR-14.9.38.4-6, GR-14.9.31.4-1, GR-14.9.31.4-3]
      *> accept: [2023]
      *> files: [pb4745-balance-server.cbl]
      *> servers: {Balance: pb4745-balance-server.cbl}
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> A requestor and the message server it addresses, two run units (DESIGN-message-control.md section 0 Q9: the servers field starts the
      *> declared unit, before the test, as a concurrent run unit serving the name, in the test's own MCS directory).
      *> 14.9.38.4 GR4: "A message is sent to the run unit identified by the content of literal-1 or message-server-name-1", and the SEND
      *> "suspends execution until the message is received by the server run unit" (cite.py --check 14.9.38.4 -> OK 4) ).
      *> 14.9.38.4 GR6: when the SEND is successful data-name-1 holds the message tag that identifies the server run unit and the message sent
      *> (cite.py --check 14.9.38.4 -> OK 6) ). 14.9.31.4 GR1: the RECEIVE receives a message from a server run unit
      *> (cite.py --check 14.9.31.4 -> OK 1) ), and GR3: data-name-2 contains the length, in alphanumeric characters, of the message received
      *> (cite.py --check 14.9.31.4 -> OK 3) ).
      *> The server is named Balance in the header and is addressed here as "BALANCE": a server name is compared with trailing spaces removed
      *> and without regard to case (DESIGN-message-control.md section 0 Q3, implementor-defined where the standard names it only as
      *> message-server-name-1, 14.9.38.3 SR2).
      *> EXPECTED VALUES, DERIVED. The SEND completes only once the server has received the 8-position message, so the tag is set (not NULL)
      *> and "SENT" is displayed after it. The server answers "BAL=0100" (8 positions), so the RECEIVE from the server tag returns it
      *> in WS-BUF with data-name-2 = 8, and the second line is "GOT BAL=0100 LEN=0008".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4745C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-MSG   PIC X(8) VALUE "ACCT-042".
       01 WS-TAG   USAGE MESSAGE-TAG.
       01 WS-BUF   PIC X(8) VALUE SPACES.
       01 WS-LEN   PIC 9(4) VALUE ZERO.
       PROCEDURE DIVISION.
       MAIN-P.
           SEND TO "BALANCE" FROM WS-MSG RETURNING WS-TAG
           END-SEND.
           IF WS-TAG NOT = NULL DISPLAY "SENT" END-IF.
           RECEIVE WS-TAG GIVING WS-BUF WS-LEN
           END-RECEIVE.
           DISPLAY "GOT " WS-BUF " LEN=" WS-LEN.
           STOP RUN.
