      *>--- conformance-case
      *> id: pb5071_sort_merge_entry_organization
      *> rules: [SR-12.4.5.2-13]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *>---
      *> kb/Work PB5071. §2023:12.4.5.1 Format 4 (sort-merge) prints
      *> SELECT [OPTIONAL] file-name-1 ASSIGN ... [[ORGANIZATION IS]
      *> SEQUENTIAL], and §2023:12.4.5.2 SR13 makes it the entry of a
      *> file an SD describes, so the entry below is legal from 2002
      *> (the 1985 entry, SELECT and ASSIGN only, refuses it:
      *> 1985-SR-XI_2_3_3-2). The SORT releases 2 then 1 and the
      *> ASCENDING key returns 1 first (§2023:14.9.40.4 GR8 a), so the
      *> program displays S=1, S=2; a compiler that refuses the entry
      *> fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5071SO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OPTIONAL SRT ASSIGN TO "P5071SO"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       SD  SRT.
       01  SR-REC.
           05  SR-K PIC X.
       WORKING-STORAGE SECTION.
       01  EOF-FLAG PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
           SORT SRT ON ASCENDING KEY SR-K
               INPUT PROCEDURE IS FEED
               OUTPUT PROCEDURE IS DRAIN.
           STOP RUN.
       FEED.
           MOVE "2" TO SR-K.
           RELEASE SR-REC.
           MOVE "1" TO SR-K.
           RELEASE SR-REC.
       DRAIN.
           PERFORM UNTIL EOF-FLAG = "Y"
               RETURN SRT RECORD
                   AT END MOVE "Y" TO EOF-FLAG
                   NOT AT END DISPLAY "S=" SR-K
               END-RETURN
           END-PERFORM.
