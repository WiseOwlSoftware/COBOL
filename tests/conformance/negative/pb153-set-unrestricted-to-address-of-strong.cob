      *> reject-at: 2002 2014 2023
      *> ISO 14.9.39.3 SR20 - THE SENDER ARM: "If identifier-6
      *> references a restricted data-pointer, either identifier-5
      *> shall reference a data-pointer restricted to the same type or
      *> data-name-1 shall be a typed item of the type to which
      *> identifier-6 is restricted."
      *> ADDRESS OF a strongly-typed group IS a restricted
      *> data-pointer - 8.4.3.11.4 GR2, and Annex D.9.2.2 item 2 lists
      *> it as one of the model's TWO sources. THIS ARM NEEDS NO
      *> GRAMMAR AT ALL and was violable from the day strong TYPEDEF
      *> landed. kb/Work PB153.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB153N4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TPT TYPEDEF STRONG.
          05 F PIC 9(4).
       01 V TYPE TPT.
       01 Q USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN.
           SET Q TO ADDRESS OF V.
           STOP RUN.
