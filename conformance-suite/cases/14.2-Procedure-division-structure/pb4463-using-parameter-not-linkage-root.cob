*>--- conformance-case
*> id: pb4463-using-parameter-not-linkage-root
*> rules: [SR-14.2.2-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §2023:14.2.2 SR1 (cite.py --check 14.2.2 "Data-name-1 shall be defined as a level 01 entry or a level 77 entry in
      *> the linkage section" -> OK 1)): a USING parameter is a level 01 or 77 LINKAGE SECTION entry. WS-X is a
      *> WORKING-STORAGE item, so the header is refused (COBOL0888, kb/Work PB4463) at every edition.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4463US.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 WS-X PIC X.
PROCEDURE DIVISION USING WS-X.
MAIN.
    STOP RUN.
