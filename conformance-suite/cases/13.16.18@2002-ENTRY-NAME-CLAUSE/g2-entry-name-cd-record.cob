      *>--- conformance-case
      *> id: g2-entry-name-cd-record
      *> rules: [2002-FMT-13.16.18.1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.18.1 - the entry-name clause, Format 1
      *> (data-name-1) and Format 3 (FILLER), written in record
      *> description entries that follow a CD entry: §2002:A.1.1.2
      *> "Each data description entry consists of a level-number
      *> followed by the data-name or FILLER clause", so the clause's
      *> formats govern these entries.  §2002:13.16.18.2 SR1: an entry
      *> with no entry-name clause is FILLER ("the word FILLER is
      *> assumed").  Format 2 (screen-name-1) belongs to the screen
      *> section, not a CD record, and is not exercised here.
      *> Derivation: CDREC is 10 alphanumeric positions (5 entries of
      *> PIC X(2): N1, a FILLER, a nameless entry, a FILLER group
      *> holding N3, and N2), so LENGTH is 10 (§2002:15.39.3 RV3).
      *> MOVE "AABBCCDDEE" fills it in order: N1 = "AA", the FILLER
      *> and the nameless entry hold "BB" and "CC", N3 (under the FILLER
      *> group) = "DD", N2 = "EE".  Each leg fails if a FILLER or a
      *> nameless entry were dropped from the record (N3/N2 shift, the
      *> length shrinks) or if either form were refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ENT02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N99 PIC 99.
       COMMUNICATION SECTION.
       CD  OUT-CD FOR OUTPUT
           DESTINATION COUNT IS OUT-COUNT
           STATUS KEY IS OUT-ST
           SYMBOLIC DESTINATION IS OUT-DEST.
       01  CDREC.
           05 N1 PIC X(2).
           05 FILLER PIC X(2).
           05 PIC X(2).
           05 FILLER.
              10 N3 PIC X(2).
           05 N2 PIC X(2).
       PROCEDURE DIVISION.
       P-PARA.
           MOVE FUNCTION LENGTH(CDREC) TO N99
           DISPLAY "LEN=" N99
           MOVE "AABBCCDDEE" TO CDREC
           DISPLAY "N1=" N1 " N3=" N3 " N2=" N2
           DISPLAY "REC=" CDREC
           STOP RUN.
