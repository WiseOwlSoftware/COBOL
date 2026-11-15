      *>--- conformance-case
      *> id: dns_validate_picture_locale_witness
      *> rules: [GR-13.18.40.4-19]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §2023:13.18.40.4 GR19 (PICTURE format 2) — "The PICTURE clause
      *> takes effect during the format validation stage of the
      *> execution of a VALIDATE statement that refers directly or
      *> indirectly to the subject of the entry. The validation is
      *> carried out in accordance with the character classification and
      *> monetary specification in the locale."
      *> CITE GR19 BY BOTH SENTENCES: §2023:13.18.40.4 rule 15 (FORMAT 1)
      *> opens with the same first sentence, so a shortened quote
      *> resolves to the Format-1 twin. The locale sentence is unique
      *> to rule 19, which sits under the printed FORMAT 2 heading.
      *> The rule's whole trigger is the VALIDATE statement. The statement is implemented (kb/Work PB5577,
      *> docs/rearchitecture/DESIGN-validate-facility.md), but the format check of an EDITED picture (a currency symbol and the
      *> locale's monetary specification, which is what GR19 adds) is a later stage of that design, so VALIDATE of an operand
      *> holding an edited item is refused (COBOL7363): a validation that passed it as valid would report a record the
      *> standard calls invalid (§2023:14.9.50.4 GR6 a). The program is otherwise legal; this pins the refusal, not GR19.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1VALW01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE US IS "en-US".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 M      PIC $9 LOCALE IS US SIZE IS 2.
       01 M-SAVE PIC X(2).
       01 WS-N   PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 1 TO M.
           MOVE M TO M-SAVE.
           VALIDATE M.
           ADD 1 TO WS-N.
           IF M = M-SAVE
               DISPLAY "SUBJECT UNCHANGED"
           ELSE
               DISPLAY "SUBJECT CHANGED"
           END-IF.
           DISPLAY "N=" WS-N.
           STOP RUN.
