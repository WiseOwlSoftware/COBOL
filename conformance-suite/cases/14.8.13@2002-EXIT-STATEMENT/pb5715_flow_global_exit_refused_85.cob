      *>--- conformance-case
      *> id: pb5715_flow_global_exit_refused_85
      *> rules: [2002-GR-14.8.13.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.13.3 GR2 sets EC-FLOW-GLOBAL-EXIT, an exception
      *>   condition of the exception model COBOL-2002 introduced
      *>   (§2002:14.5.12.1); COBOL-85 has no exception conditions and
      *>   no >>TURN directive to enable their checking (§2002:7.2.19),
      *>   so the source is refused at the directive. Beside the
      *>   accepted case pb5715_exit_program_global_declarative_range.
       >>TURN EC-FLOW-GLOBAL-EXIT CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G5715C.
       PROCEDURE DIVISION.
       MAIN-PARA.
           EXIT PROGRAM.
           STOP RUN.
       END PROGRAM G5715C.
