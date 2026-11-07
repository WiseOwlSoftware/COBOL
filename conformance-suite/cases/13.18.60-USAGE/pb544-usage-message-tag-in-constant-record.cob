*>--- conformance-case
*> id: pb544-usage-message-tag-in-constant-record
*> rules: [SR-13.18.60.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB544 — §2023:13.18.60.3 syntax rule 4's MESSAGE-TAG arm, which had no reachable subject until the
      *> phrase joined `usageKeyword` (kb/Work PB487): `05 Q USAGE MESSAGE-TAG.` used to be
      *> `COBOL0001: no viable alternative at input 'MESSAGE-TAG'`, so nothing could distinguish SR4's
      *> violation from the general format's gap, and the row stayed PARTIAL on exactly this one phrase.
      *> SR4 lists SIX phrases where the neighbouring SR14 lists five — INDEX is the difference — and the
      *> screen is written as SR14's set UNION INDEX so the difference stays structural.
      *> COBOL1726 is the SYNTAX RULE: a message tag is a bound category since kb/Work PB4740, and the screen
      *> reads its usage like its siblings'.
      *> Pinned at 2023: MESSAGE-TAG is an Annex E.2 item-25 COBOL-2023 addition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB544SR4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  CFG CONSTANT RECORD.
           05  Q USAGE MESSAGE-TAG.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
