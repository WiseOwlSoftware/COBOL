*>--- conformance-case
*> id: initialize-message-tag
*> rules: [GR-14.9.20.4-4, GR-14.9.20.4-5, GR-14.9.20.4-6]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4740 - INITIALIZE of a message-tag data item (ISO 14.9.20). GR4: "If the category of a
      *> receiving-operand is data-pointer, function-pointer, message-tag, object-reference, or program-pointer,
      *> the implicit statement is SET receiving-operand TO sending-operand" (cite.py --check 14.9.20.4 OK, 4);
      *> GR6 c) 1. names the sending-operand of a message-tag receiver for the bare form, DEFAULT and the
      *> VALUE phrase as the predefined NULL (GR6: "Message-tag | Predefined content NULL"), and GR5 c) 1. a. makes
      *> a message-tag item a receiving-operand under the VALUE phrase unconditionally. REPLACING MESSAGE-TAG DATA
      *> BY identifier-2 is the implicit SET with identifier-2 as the sender (GR6 b)), valid when identifier-2 is a
      *> message-tag item (SR3, SR4).
      *> Every initialization here leaves the NULL content, which the relation `= NULL` observes. A tag copied by
      *> REPLACING from another NULL tag is NULL too.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INITMSGTAG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TG IS TYPEDEF STRONG.
          05 TGA PIC X(2).
          05 TGT USAGE MESSAGE-TAG.
       01 G1 TYPE TG.
       01 T1 USAGE MESSAGE-TAG.
       01 T2 USAGE MESSAGE-TAG.
       PROCEDURE DIVISION.
       MAIN.
           INITIALIZE T1
           IF T1 = NULL DISPLAY "BARE NULL" END-IF
           INITIALIZE T1 ALL TO VALUE
           IF T1 = NULL DISPLAY "VALUE NULL" END-IF
           INITIALIZE T1 WITH FILLER ALL TO VALUE THEN TO DEFAULT
           IF T1 = NULL DISPLAY "DEFAULT NULL" END-IF
           INITIALIZE T1 REPLACING MESSAGE-TAG DATA BY T2
           IF T1 = NULL DISPLAY "REPLACING NULL" END-IF
           INITIALIZE G1
           IF TGT OF G1 = NULL DISPLAY "GROUP NULL" END-IF
           INITIALIZE G1 REPLACING MESSAGE-TAG DATA BY T2
           IF TGT OF G1 = T2 DISPLAY "GROUP REPLACING" END-IF
           STOP RUN.
       END PROGRAM INITMSGTAG.
