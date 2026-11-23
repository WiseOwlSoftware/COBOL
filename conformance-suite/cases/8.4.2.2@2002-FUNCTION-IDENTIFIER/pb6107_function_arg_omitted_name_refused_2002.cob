      *>--- conformance-case
      *> id: pb6107_function_arg_omitted_name_refused_2002
      *> rules: [2002-GR-8.4.2.2.3-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.19.2 SR2 - "Exception-name-1 shall be one of the
      *>   exception names listed in 14.5.12.1, Exception conditions."
      *>   §2002:14.5.12.1.5 lists EC-PROGRAM-ARG-OMITTED for every
      *>   reference to an omitted argument and has no
      *>   EC-FUNCTION-ARG-OMITTED, a §2014 name (§2014:E.2 item 18), so
      *>   the TURN directive below is refused at 2002. Beside the
      *>   accepted case pb6107_function_arg_omitted_2002.
       >>TURN EC-FUNCTION-ARG-OMITTED CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P6107RF.
       PROCEDURE DIVISION.
           STOP RUN.
       END PROGRAM P6107RF.
