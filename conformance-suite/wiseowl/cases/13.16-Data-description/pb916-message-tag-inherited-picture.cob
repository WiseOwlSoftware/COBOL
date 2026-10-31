*>--- conformance-case
*> id: pb916-message-tag-inherited-picture
*> rules: [SR-13.16.3-8]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB916 - an INHERITED message-tag usage is reported as a message-tag usage, not as a floating-point one.
      *> ISO 13.16.3 SR8: "The PICTURE clause shall not be specified ... for an item whose usage is binary-char,
      *> binary-short, binary-long, binary-double, float-short, float-long, float-extended, index, message-tag, object
      *> reference, pointer, function-pointer, or program-pointer." (cite.py --check 13.16.3 "float-extended, index,
      *> message-tag" OK, Syntax rules 8).  13.18.60.4 GR1: "If the USAGE clause is specified or implied at a group
      *> level, it applies only to each elementary item in the group."  So `05 M PIC X.` under `01 G USAGE MESSAGE-TAG.` is an item of usage
      *> message-tag that has a PICTURE, and the screen is right to refuse it - but its message told the
      *> programmer the item was "a floating-point usage (COMP-1/COMP-2/FLOAT-SHORT...)", because the verdict's
      *> fall-through arm was the float family for every picture-less usage without a verdict of its own.
      *> The refusal now names USAGE MESSAGE-TAG, at every edition (the SR8 screen is not edition-keyed; below 2023
      *> the entry also draws COBOL0900, pb916-message-tag-introduction).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB916MTP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G USAGE MESSAGE-TAG.
          05 M PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
