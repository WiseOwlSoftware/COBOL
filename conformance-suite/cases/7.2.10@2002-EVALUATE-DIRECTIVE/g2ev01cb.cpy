       >>EVALUATE 2
       >>WHEN 2
           DISPLAY "OLD-LIT3"
           MOVE "W1-OK" TO OLDWD
           DISPLAY OLDWD
           MOVE "TX-OK" TO OLDA OF OLDG
           DISPLAY OLDA OF OLDG
      D    DISPLAY "OLD-DLINE"
       >>WHEN OTHER
           DISPLAY "OLD-LIT3"
       >>END-EVALUATE
