*>--- conformance-case
*> id: pb6177-picture-floating-extended-after-digit
*> rules: [GR-13.18.40.5-6, SR-13.18.40.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB6177 - ISO 1989:2023 13.18.40.5 GR6: the floating insertion symbols are the currency symbol, the
      *> extended editing sign control symbols and the fixed '+' and '-'; a floating string stands for "the leading
      *> numeric character positions to the left of the decimal point position", and the floating result ends at
      *> "the first character position for which no floating insertion editing is specified". 13.18.40.6 Table 10
      *> leaves blank the cell that has a digit position precede a floating insertion symbol, which refuses
      *> 99++ and 99$$ (SR2). A floating string of an extended symbol is a floating insertion string as they are,
      *> so a digit position before it is the same prohibition:
      *> PE01 99LL   - the LL after the digits is a floating string no leading position reaches. Before
      *>               kb/Work PB6177 it bound clean and MOVE -5 dropped the sign.
      *> PE02 ZZLL9  - the same with zero-suppression positions before the string.
      *> COBOL1935, 2023 only (the EDITING phrase is a COBOL-2023 introduction, Annex E.3.3 item 19).
      *> The positive sibling pb6177_picture_floating_extended_after_insertion_2023 holds the shape GR6 admits.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6177NEG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PE01 PIC 99LL EDITING L FOR NEGATIVE IS "-".
       01 PE02 PIC ZZLL9 EDITING L FOR NEGATIVE IS "-".
       PROCEDURE DIVISION.
           STOP RUN.
