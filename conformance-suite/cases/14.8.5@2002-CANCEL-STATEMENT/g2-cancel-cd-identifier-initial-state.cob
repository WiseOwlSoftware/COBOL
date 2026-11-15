      *>--- conformance-case
      *> id: g2-cancel-cd-identifier-initial-state
      *> rules: [2002-GR-14.8.5.3-1, 2002-GR-14.8.5.3-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.5.3 GR1 - "The program to be canceled is identified
      *> by one of the following: a) the content of the data item
      *> referenced by identifier-1, b) the value of literal-1". Here
      *> identifier-1 is PN, an alphanumeric item of a CD record
      *> (§2002:14.8.5.2 SR1), and WN, a working-storage item; the
      *> literal form closes the run.
      *> §2002:14.8.5.3 GR3 - "If the program referenced by a
      *> successfully executed CANCEL statement in a run unit is
      *> subsequently called in that run unit, that program is in its
      *> initial state", and §2002:14.5.2.1.2.1 places in the initial
      *> state "The internal data described in the working-storage
      *> section, local-storage section, and communication section".
      *> G2CAN1S counts its activations in CNT (working-storage) and in
      *> SN (communication section, VALUE 7, allowed in the first
      *> redefinition of an input CD record: §2002:A.1.2.2 SR5), and
      *> changes SV (VALUE "ORIG") to "CHGD" on every activation.
      *>  call, call            -> C=ORIG 008 1, C=CHGD 009 2
      *>  CANCEL PN (CD item), call       -> C=ORIG 008 1
      *>  CANCEL WN (WS item), call, call -> C=ORIG 008 1, C=CHGD 009 2
      *>  CANCEL "G2CAN1S", call          -> C=ORIG 008 1
      *> Why each leg can fail: a CANCEL whose identifier is resolved
      *> by its name rather than its content, a CD record item not
      *> readable as identifier-1, or an initial state that resets the
      *> working-storage but leaves the communication section in its
      *> last-used state (SV and SN would read CHGD and 010).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CAN01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WN PIC X(7) VALUE "G2CAN1S".
       COMMUNICATION SECTION.
       CD CDIN FOR INPUT
           SYMBOLIC QUEUE IS SQ.
       01 INREC.
          05 PN     PIC X(7).
          05 FILLER PIC X(80).
       PROCEDURE DIVISION.
       MAIN.
           MOVE "G2CAN1S" TO PN.
           CALL "G2CAN1S".
           CALL "G2CAN1S".
           CANCEL PN.
           CALL "G2CAN1S".
           CANCEL WN.
           CALL "G2CAN1S".
           CALL "G2CAN1S".
           CANCEL "G2CAN1S".
           CALL "G2CAN1S".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CAN1S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CNT PIC 9 VALUE 0.
       COMMUNICATION SECTION.
       CD CDS FOR INPUT
           SYMBOLIC QUEUE IS SQS.
       01 SREC.
          05 SV     PIC X(4) VALUE "ORIG".
          05 SN     PIC 9(3) VALUE 7.
          05 FILLER PIC X(80).
       PROCEDURE DIVISION.
       M1.
           ADD 1 TO CNT.
           ADD 1 TO SN.
           DISPLAY "C=" SV " " SN " " CNT.
           MOVE "CHGD" TO SV.
           GOBACK.
       END PROGRAM G2CAN1S.
       END PROGRAM G2CAN01.
