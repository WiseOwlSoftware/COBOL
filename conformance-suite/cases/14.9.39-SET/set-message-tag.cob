*>--- conformance-case
*> id: set-message-tag
*> rules: [SR-14.9.39.3-35, GR-14.9.39.4-40, GR-14.9.39.4-41]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4740 - SET Format 17 (message-tag), ISO 14.9.39.2: SET data-name-4 TO { data-name-5 | NULL }.
      *> SR35: "Data-name-4 and data-name-5 shall be message-tag data items." (cite.py --check 14.9.39.3 OK, 35.)
      *> 14.9.39.1: the statement is for "setting the content of a message-tag data item to NULL or another
      *> message-tag". GR41: "If NULL is specified, the content of data-name-5 shall be set to that of the null
      *> value." GR40: "If data-name-5 is specified, the content of data-name-4 is moved to data-name-5."
      *> The two printed general rules name the receiver as data-name-5 where the format and 14.9.39.1 make it
      *> data-name-4 (the owner-approved reading, docs/CONFORMANCE.md D-SET17): data-name-4 is the receiver in both.
      *> Only the message control system makes a tag that is not NULL, so the content that can be observed
      *> here is the null content: SET T2 TO NULL makes T2 null (GR41), and SET T1 TO T2 copies that null content
      *> into T1 (GR40 as read). The relation condition `T = NULL` (8.8.4.2.1 item 9) observes each.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SETMSGTAG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T1 USAGE MESSAGE-TAG.
       01 T2 USAGE MESSAGE-TAG.
       PROCEDURE DIVISION.
       MAIN.
           SET T2 TO NULL
           IF T2 = NULL
              DISPLAY "T2 NULL"
           ELSE
              DISPLAY "T2 NOT NULL"
           END-IF
           SET T1 TO T2
           IF T1 = NULL
              DISPLAY "T1 NULL"
           ELSE
              DISPLAY "T1 NOT NULL"
           END-IF
           IF T1 = T2
              DISPLAY "T1 SAME AS T2"
           ELSE
              DISPLAY "T1 NOT SAME AS T2"
           END-IF
           STOP RUN.
       END PROGRAM SETMSGTAG.
