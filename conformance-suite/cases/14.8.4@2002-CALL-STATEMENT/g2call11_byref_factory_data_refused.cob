      *>--- conformance-case
      *> id: g2call11_byref_factory_data_refused
      *> rules: [2002-SR-14.8.4.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.4.2 SR3 sentence 2 - "If the BY REFERENCE phrase is
      *> specified or implied, identifier-2 shall not be defined in the
      *> working-storage or file section of a factory or an instance
      *> object." FAC-W is factory working storage, passed with BY
      *> REFERENCE implied (§2002:14.8.4.3 GR5), so the CALL shall be
      *> refused at its line. The instance-object arm is
      *> g2call09_byref_object_data_refused.
       IDENTIFICATION DIVISION.
       CLASS-ID. G2OBF.
       IDENTIFICATION DIVISION.
       FACTORY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FAC-W PIC 9(4).
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. M1.
       PROCEDURE DIVISION.
       P1.
           CALL "G2OBRS" USING FAC-W
           GOBACK.
       END METHOD M1.
       END FACTORY.
       END CLASS G2OBF.
