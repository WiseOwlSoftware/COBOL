      *>--- conformance-case
      *> id: g2call09_byref_object_data_refused
      *> rules: [2002-SR-14.8.4.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.4.2 SR3 sentence 2 - "If the BY REFERENCE phrase is
      *> specified or implied, identifier-2 shall not be defined in the
      *> working-storage or file section of a factory or an instance
      *> object." OBJ-W is instance-object working storage, passed with
      *> BY REFERENCE implied (§2002:14.8.4.3 GR5), so the CALL shall be
      *> refused at its line. The permitted sections are
      *> g2call08_operand_sections.
       IDENTIFICATION DIVISION.
       CLASS-ID. G2OBR.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OBJ-W PIC 9(4).
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. M1.
       PROCEDURE DIVISION.
       P1.
           CALL "G2OBRS" USING OBJ-W
           GOBACK.
       END METHOD M1.
       END OBJECT.
       END CLASS G2OBR.
