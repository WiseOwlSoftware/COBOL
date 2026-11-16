       >>IF G2X = 1
           DISPLAY MSG-A
           DISPLAY "LIT-A"
      D    DISPLAY DBG-A
           DISPLAY TXT-A(1:7)
           DISPLAY PSE-A
       >>ELSE
           DISPLAY "ELSE-ARM"
       >>END-IF
       >>IF G2X = 9
           DISPLAY "NOT-ARM"
       >>ELSE
           DISPLAY "T2 " "LIT-A"
       >>END-IF
