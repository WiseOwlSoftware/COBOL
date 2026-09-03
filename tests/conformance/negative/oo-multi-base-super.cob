*> reject-at: 2002 2014 2023
*> ISO 8.4.3.8.3 SR5's OWN subject: "If the INHERITS clause of the containing class definition specifies
*> more than one object-class-name, object-class-name-1 shall be specified" - i.e. a bare SUPER is illegal
*> once a class has two bases. Annex A.4.10 item 1 declines the two-base clause, so the SR is unreachable.
*> The method body uses SUPER so the subject is actually present in the source.
*> NOTE a separate, GENUINELY OPEN gap this fixture does not close: the "object-class-name-1 OF" prefix of
*> FMT-8.4.3.8.2 has no grammar surface at all (objectReference admits a bare SUPER only), which leaves
*> SR-8.4.3.8.3-4/-6 and GR-8.4.3.8.4-4 open NOT-IMPLEMENTED rows rather than module-licensed ones.
       IDENTIFICATION DIVISION.
       CLASS-ID. MBSUP INHERITS FROM MBSBASEA MBSBASEB.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. GO-UP.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE SUPER "SHOW".
       END METHOD GO-UP.
       END OBJECT.
       END CLASS MBSUP.
       IDENTIFICATION DIVISION.
       CLASS-ID. MBSBASEA.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SHOW.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "A".
       END METHOD SHOW.
       END OBJECT.
       END CLASS MBSBASEA.
       IDENTIFICATION DIVISION.
       CLASS-ID. MBSBASEB.
       END CLASS MBSBASEB.
