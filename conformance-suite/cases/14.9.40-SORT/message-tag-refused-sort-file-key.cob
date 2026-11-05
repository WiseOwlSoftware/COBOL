*>--- conformance-case
*> id: message-tag-refused-sort-file-key
*> rules: [SR-14.9.40.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4740 review R1-3 - a FILE SORT KEY OF CLASS MESSAGE-TAG IS REFUSED BY SR6 c).
      *>   cite.py --check 14.9.40.3 "Key data items shall not be of the class boolean, message-tag, object, or
      *>     pointer." -> OK §14.9.40.3 6) c)
      *> SMT is a level-1 USAGE MESSAGE-TAG record of the SD (the placement 13.18.60.3 SR14 permits). The pointer twin is
      *> pb1173-sort-file-key-pointer; before the fix the message-tag key compiled with COBOL1756 and aborted the run.
      *> The SD record is one message tag, which has the 8-byte slot width (DataItem.PointerByteWidth), so the USING
      *> and GIVING records are X(8) and the key rule is the ONE reason the program is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MTSORTFK.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "mtsortfk.tmp".
           SELECT IN1 ASSIGN TO "mtsortfki.dat".
           SELECT OU1 ASSIGN TO "mtsortfko.dat".
       DATA DIVISION.
       FILE SECTION.
       SD SW.
       01 SMT USAGE MESSAGE-TAG.
       FD IN1.
       01 IR PIC X(8).
       FD OU1.
       01 OR1 PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           SORT SW ASCENDING KEY SMT USING IN1 GIVING OU1
           STOP RUN.
