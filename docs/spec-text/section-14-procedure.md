

--- Page 600 ---

ISO /IEC 1989.2023 (E)
3) If either  the argument or the formal  parameter is described with an object-class-name, the corresponding formal parameter or argument shall be described with the same object-class-name and the FACTORY and ONLY phrases shall be the same_
4) If the formal parameter is described with the ACTIVE-CLASS phrase, one of the following conditions shall be true:
a) The argument shall be an object reference described with the ACTIVE-CLASS phrase, where the presence 0r absence of the FACTORY phrase is the same as in the formal parameter, and the method to be activated shall be invoked with the predefined object references SELF or SUPER, or with an object reference described with the ACTIVE-CLASS phrase:
b) The argument shall be an object reference described with an object-class-name and the ONLY phrase, where the presence or absence of the FACTORY phrase is the same as in the formal parameter, and the method to be activated shall be invoked with that object-class-name or with an object reference described with that object-class-name and the ONLY phrase:
If either the argument or the formal parameter is of class pointer, the corresponding formal parameter or argument shall be of class pointer and the corresponding items shall be ofthe same category Ifeither is a restricted pointer; both shall be restricted and of the same type:
Ifthe formal parameter or the correspondingargument is of class object-reference, both shall be of class object-reference_
If neither the formal parameter nor the argument is of class message-tag, object, or pointer, the conformance rules are the following:
1) If the activated element is a program for which there is no program-specifier in the REPOSITORY paragraph ofthe activating element and there is no NESTED phrase specified on the CALL statement, the formal parameter shall be of the same length as the corresponding argument
2) Ifthe activated element is one of the following:
program for which thereis a program-specifier in the REPOSITORY paragraph ofthe activating element a program and the NESTED phrase is specified on the CALL statement a method a function
then the definition of the formal parameter and the definition of the argument shall have the same ALIGN, BLANK WHEN ZERO, DYNAMIC LENGTH, JUSTIFIED, PICTURE, SIGN, and USAGE clauses, with the following exceptions:
a) Currency symbols match ifand only if the corresponding currency strings are the same:
b) Period picture symbols match ifand only ifthe DECIMAL-POINT IS COMMA clause is in effect for both the activating and the activated runtime elements or for neither of them_ Comma picture symbols match if and only if the DECIMAL-POINT IS COMMA clause is in effect for both the activating and the activated runtime elements or for neither of them:
570
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 601 ---

ISO /IEC 1989.2023 (E)
Additionally:
a) Locale specifications in the PICTURE clauses match ifand only if:
both specify the same SIZE phrase in the LOCALE phrase of the PICTURE clause, and
both specify the LOCALE phrase without a locale-name or both specify the LOCALE phrase with the same external identification, where the external identification is the external-locale- name or literal value associated with a locale-name in the LOCALE clause of the SPECIAL- NAMES paragraph_
b) A bit group item matches an elementary bit data item described with the same number of boolean positions.
A national group item matches an elementary data item of usage national described with the same number of national character positions_
If the formal parameter is described with the ANY LENGTH clause, its length is considered to match the length of the corresponding argument
Ifthe argument is described with the ANY LENGTH clause,the corresponding formal parameter shall be described with the ANY LENGTH clause
14.8.2.3.3 Elementary items passed by content or by value
If the formal parameter is an object reference described with the ACTIVE-CLASS phrase, one f the following conditions shall be true:
1) The method to be activated shall be invoked with the predefined object references SELF or SUPER, or with an object reference described with the ACTIVE-CLASS phrase, and a SET statement shall be valid in the activating unit with the argument as the sending operand and an object reference described with the ACTIVE-CLASS phrase, where the presence or absence of the FACTORY phrase is the same aS in the formal parameter, as the receiving operand.
2) The method to be activated shall be invoked with an object-class-name or with an object reference described with an object-class-name and the ONLY phrase, and a SET statement shall be valid in the activating unit with the argument as the sending operand and an object reference described with that object-class-name and the ONLY phrase, where the presence or absence ofthe FACTORY phrase is the same as in the formal parameter,as the receiving operand:
Ifthe formal parameter is of class pointer or an object reference described without the ACTIVE-CLASS phrase; the conformance rules shall be the same as if a SET statement were performed in the activating runtime elementwith the argument as the sending operand and the corresponding formal parameter as the receiving operand.
Ifthe formal parameter is not of class object or pointer; the conformance rules are the following:
@ISO/IEC 2023
571
[per-copy licence stamp]


--- Page 602 ---

ISO /IEC 1989.2023 (E)
1) If the activated element is a program for which there is no program-specifier in the REPOSITORY paragraph of the activating elementand there is no NESTED phrase specified on the CALL statement, the formal parameter shall be of the same length as the corresponding argument
2) If the activated element is one of the following:
a program for which there is a program-specifier in the REPOSITORY paragraph oftheactivating element a program and the NESTED phrase is specified on the CALL statement method a function
then the conformance rules depend on the type ofthe formal parameter as specified in the following rules:
a) If the formal parameter is numeric, the conformance rules are the same as for a COMPUTE statement with the argument as the sending operand and the corresponding formal parameter as the receiving operand.
b) If the formal parameter is an index data item; the conformance rules are the same as for a SET statement with the argument as the sending operand and the corresponding formal parameter as the receiving operand:
c) If the formal parameter is described with the ANY LENGTH clause; its length is considered to match the length of the corresponding argument
Otherwise, the conformance rules are the same as for a MOVE statement with the argument as the sending operand and the corresponding formal parameter as the receiving operand.
14.8.3 Returning items
14.8.3.1 General
returning item shall be specified in the activating statement if and only ifa returning item is specified in the procedure division header ofthe activated element: A returning item is implicitly specified in the activating element when a function or inline method invocation is referenced.
The returning item in theactivated elementis the sending operand,the corresponding returning item in the activating element is the receiving operand.
The rules for conformance between the sending operand and the receiving operand depend on whether at least one of the operands is an alphanumeric group item or both operands are elementary items
NOTE Abit group or national group is treated as an elementary item
14.8.3.2 Group items
If either the sending or the receiving operand is an alphanumeric group item, and neither of them is strongly typed or a variable length group; the corresponding returning item shall be an alphanumeric
572
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 603 ---

ISO /IEC 1989.2023 (E)
group item or an elementary item of category alphanumeric, and the receiving operand shall be of the same length as the sending operand:
NOTE Ifa returning item in an activating element is a group with a level number other than 1 and its subordinate items are described such that the implementation inserts slack bits or bytes, the alignment of the subordinate elementary items might not correspond between the returning item in the activating runtime element and the returning item in the activated runtime element;
If either ofthe operands is a strongly-typed group item, both shall be ofthe same type_
If either the sending or the receiving operand is a variable length group, the sending operand and the receiving operand shall be compatible,as described in 8.5.1.12, Variable-length groups.
For an operand that is described as a variable-occurrence data item, the maximum length is used.
14.8.3.3 Elementary items
Ifeither ofthe operands is an object reference; the corresponding item shall be an object reference, and the following rules apply:
1) If the returning item in the activated element is not described with an ACTIVE-CLASS phrase, the conformance rules are the same as if a SET statement were performed in the activated runtime element with the returning item in the activated element as the sending operand and the corresponding returning item in the activating element as the receiving operand.
2) If the returning item in the activated element is described with an ACTIVE-CLASS phrase, the conformance rules are the same as the conformance rules for a SET statement specified in the activating element with the following operands:
a) A receiving operand that is the returning item in the activating element:
b)
sending operand that is an object reference described as follows:
Ifthe activated method is invoked with an object-class-name,the sending object reference is described with that same object-class-name and an ONLY phrase:
Ifthe activated method is invoked with the predefined object reference SELF or SUPER, the sending object reference is described with an ACTIVE-CLASS phrase_
If the activated method is invoked with an object reference that is described with an interface-name, the sending object reference is a universal object reference
If the activated method is invoked with any other object reference, the sending operand has the same description as that object reference_
If the sending operand defined above is described with an object-class-name or an ACTIVE-CLASS phrase, the presence or absence of the FACTORY phrase is the same as in the returning item of the activated element;
@ISO/IEC 2023
573
[per-copy licence stamp]


--- Page 604 ---

ISO /IEC 1989.2023 (E)
If the sending operand is not an object reference, the receiving operand shall have the same ALIGN, BLANK WHEN ZERO, DYNAMIC LENGTH, JUSTIFIED , PICTURE, SIGN, and USAGE clauses, with the following exceptions:
1) Currency symbols match ifand only if the corresponding currency strings are the same
2) Period picture symbols match if and only if the DECIMAL-POINT IS COMMA clause is in effect for both the activating and the activated runtime elements or for neither of them
3) Comma picture symbols match if and only if the DECIMAL-POINT IS COMMA clause is in effect for both the activating and the activated runtime elements or for neither ofthem:
Additionally, if the sending operand is not an object reference:
1) Locale specifications in the PICTURE clauses match ifand only if:
both specify the same SIZE phrase in the LOCALE phrase ofthe PICTURE clause; and
both specify the LOCALE phrase without a locale-name or both specify the LOCALE phrase with the same external identification, where the external identification is the external-locale-name or literal value associated with a locale-name in the LOCALE clause of the SPECIAL-NAMES paragraph_
2) A bit group item matches an elementary boolean data item of usage bit described with the same number of boolean positions:
3) A national group item matches an elementary data item of usage national described with the same number of national character positions_
4) Ifthe receiving operand is described with the ANY LENGTH clause,the sending operand shallalso be described with the ANY LENGTH clause.
5) Ifthe sending operand is described with the ANY LENGTH clause, the length of the sending operand is considered to match the length of the receiving operand:
14.8.4 External items
14.8.4.1 General
In order to be able to check the conformance of external items between runtime elements, the EC- EXTERNAL exception conditions to be checked shall be enabled in both the activating and activated runtime elements, which for activated runtime elements shall be before the Environment division.
14.8.4.2 Correspondence of external data items used in external files
For each external file connector; the file status, linage and relative key data items shall be external data items and shall refer to the same corresponding storage in each runtime element for which the file connector is used.
574
OISO /IEC 2023
[per-copy licence stamp]


--- Page 605 ---

ISO /IEC 1989.2023 (E)
14.8.4.3 Correspondence of external data item formats
For external data items, the rules specified in 13.18.22, EXTERNAL clause, General rule 6 apply.
For external type declarations and theiruse,the rules specified in 8.5.3,Typesand 13.18.57,TYPE clause apply.
For external data items with strongly typed record descriptions, the record descriptions shall have the same corresponding external strong type declarations and the same presence or absence of the CONSTANT RECORD clause:
14.8.4.4 Correspondence of external file control entries
For each external file connector; the rules specified in 12.4.5,File control entry General rule 1 apply:
@ISO/IEC 2023
575
[per-copy licence stamp]


--- Page 606 ---

ISO /IEC 1989.2023 (E)
14.9 Statements
14.9.1 ACCEPT statement
14.9.1.1 General
The execution ofa device format ACCEPT statement causes information from a device to be transferred to the specified data item, where the device is a hardware or software device in the operating environment;
The execution of a temporal format ACCEPT statement causes the information requested from the operating environment to be made available to the specified data item:
The execution of a screen format ACCEPT statement causes the following sequence of events:
Specified or default initial values are moved to the input fields ofthe screen. The screen is displayed with the specified attributes on the terminal display screen at the specified or default location_ The cursor is positioned to the specified or default input field: The operator is given the opportunity to modify the elementary input screen items If inconsistent data is entered by the operator; the implementation may prompt the operator to correct the data or it may set an exception condition to exist: The contents of the screen items that are consistent with their descriptions are moved to the specified destination fields. The line and column of the cursor when input terminates are placed into the data item referenced in the CURSOR clause, if any. Appropriate statements in the ON EXCEPTION or NOT ON EXCEPTION clauses, if any, are executed:
14.9.1.2 General formats
Format 1 (device):
ACCEPT identifier-1 FROM mnemonic-name-1 END-ACCEPT ]
Format 2 (temporal):
DATE YYYYMMDD DAY YYYYDDD ] DAY-OF-WEEK TIME
ACCEPT identifier-2 FROM
END-ACCEPT
576
OISO /IEC 2023
[per-copy licence stamp]


--- Page 607 ---

ISO /IEC 1989.2023 (E)
Format 3 (screen):
ACCEPT screen-name-1
identifier-3 integer-1
LINE NUMBER
AT
COLUMN COL
identifier-4 integer-2
NUMBER
ON EXCEPTION imperative-statement-1 NOT ON EXCEPTION imperative-statement-2 END-ACCEPT
14.9.1.3 Syntax rules
1) Identifier-1 shall reference neither strongly-typed group item nor a data item of class index, message-tag, object; or pointer:
2) Mnemonic-name-1 shall be specified in the SPECIAL-NAMES paragraph ofthe environment division and shall be associated with an implementor-defined device-name that is identified in the operating environment as a hardware or software device capable ofproviding data to the program:
3) Identifier-2 shall not reference a data item of class alphabetic; boolean, index, message-tag, object; or pointer:
4) Screen-name-1 may reference a group item containing screen items with FROM or VALUE clauses only ifthe group also contains screen items with TO or USING clauses
5) Identifier-3 and identifier-4 shall be unsigned integer data items
6) Neither identifier-1 nor identifier-2 shall reference a variable-length group.
14.9.1.4 General rules
FORMAT 1
1) The ACCEPT statement causes the transfer of data from the device. This data replaces the content of the data item referenced by identifier-1. Any conversion of data required between the device and the data item referenced by identifier-1 is defined by the implementor.
2) The implementor shall define, for each device,the size ofa data transfer:
@ISO/IEC 2023
577
[per-copy licence stamp]


--- Page 608 ---

ISO /IEC 1989.2023 (E)
3) Ifa device is ~capable of transferring data of the same size as the receiving data item; the transferred data is stored in the receiving data item:
4) Ifa device is not capable of transferring data ofthe same size as the receiving data item, then:
a) Ifthe size ofthe receiving data item (or ofthe portion ofthe receiving data item notyet currently occupied by transferred data) exceeds the size of the transferred data, the transferred data is stored aligned to the left in the receiving data item (or the portion ofthe receiving data item not yet occupied), and additional data is requested.
b) Ifthe size ofthe transferred data exceeds the size ofthe receiving data item (or the portion ofthe receiving data item not yet occupied by transferred data), only the leftmost characters of the transferred data are stored in the receiving data item (or in the portion remaining)_ The remaining characters of the transferred data that do not fit into the receiving data item are ignored. If identifier-1 references a zero-length item, all the characters of the transferred data are ignored:
5) The implementor shall specify the device that is used ifthe FROM phrase is not specified.
FORMAT 2
The ACCEPT statement causes the information requested to be transferred to the data item specified by identifier-2 accordingto the rules for the MOVE statement: DATE,DAY,DAY-OF-WEEK,and TIME reference the current date and time provided by the system on which the ACCEPT statement is executed. DATE, DAY, DAY-OF-WEEK, and TIME are conceptual data items and, therefore, are not described in the COBOL source unit;
7) DATE without the phrase YYYYMMDD behavesas ifithad been described as an unsigned elementary integer data item of usage display six digits in length, the character positions of which, numbered from left to right; are:
Character Positions Contents
1-2 3-4 5-6
The two low-order digits of the year in the Gregorian calendar: Two numeric characters of the month of the year in the range 01 through 12_ Two numeric characters of the day of the month in the range 01 through 31.
8) DATE with the phrase YYYYMMDD behaves as if it had been described as an unsigned elementary integer data item of usage display eight digits in length, the character positions of which, numbered from left to right; are:
Character Positions
Contents
1-4 5-6 7-8
Four numeric characters of the year in the Gregorian calendar: Two numeric characters of the month of the year in the range 01 through 12 Two numeric characters of the day ofthe month in the range 01 through 31.
578
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 609 ---

ISO /IEC 1989.2023 (E)
9) DAY without the phrase YYYYDDD behaves as if it had been described as an unsigned elementary integer data item of usage display five digits in length; the character positions of which, numbered from left to right; are:
Character Positions
Contents
1-2 3-5
The two low-order digits of the year in the Gregorian calendar: Three numeric characters ofthe day " oftheyearin the range 001 through 366
10) DAY with the phrase YYYYDDD behaves as if it had been described as an unsigned elementary integer data item ofusage display seven digits in length,the character positions ofwhich, numbered from left to right; are:
Character Positions
Contents
1-4 5-7
Four numeric characters of the year in the Gregorian calendar: Three numeric characters ofthe day oftheyearin the range 001 through 366_
11) " TIME is based on the elapsed time since midnight on a 24-hour clock: If the system does not have the facility to provide fractional parts ofa second the value zero is returned for those parts that are notavailable: TIME behaves as ifithad been described asan unsigned elementary integer data item of usage display eight digits in length, the characters positions of which, numbered from left to right; are:
Character Positions
Contents
1-2
Two numeric characters of the hours past midnight in the range 00 through 23. Two numeric characters ofthe minutes past thehour in the range 00 through 59. Two numeric characters of the seconds past the minute in the range: a) 00 through 59 when a LEAP-SECOND directive with the OFF phrase is in effect b) 00 through nn; where nn is defined by the implementor; when a LEAP- SECOND directive with the ON phrase is in effect Two numeric characters of the hundredths ofa second past the second in the range 00 through 99. 00 is returned if the system on which the ACCEPT statement is executed does not have the facility to provide the fractional part ofa second:
3-4
5-6
7-8
12) DAY-OF-WEEK behaves as if it had been described as an unsigned elementary numeric integer data item one digit in length and of usage display: In DAY-OF-WEEK, the value 1 represents Monday, 2 represents Tuesday, 3 represents Wednesday, represents Sunday:
FORMAT 3
13) Identifiers specified in FROM or USING clauses or literals specified in FROM or VALUE clauses provide the initial values displayed for the associated screen item during execution of an ACCEPT screen statement: For elementary screen items that have no FROM, USING, or VALUE clause, the
@ISO/IEC 2023
579
[per-copy licence stamp]


--- Page 610 ---

ISO /IEC 1989.2023 (E)
initial value is as ifa MOVE statement were executed with the screen item as the receiving field. The sending item of the MOVE statement is a figurative constant that depends on the category of the screen item as follows.
Screen item
constant
Alphabetic Alphanumeric SPACES Alphanumeric Alphanumeric SPACES Alphanumeric-edited Alphanumeric SPACES Boolean ZEROS National National SPACES National-edited National SPACES Numeric ZEROS Numeric-edited ZEROS
14) Any conversion of data required between the hardware device and the data items referenced in screen-name-1 is defined by the implementor:
15) The LINE and COLUMN phrases give the position on the terminal display screen at which the screen record associated with screen-name-1 isto start: Column and line number positions are specified in terms of alphanumeric character positions. The position is relative to the leftmost character column in the topmost line ofthe display that is identified as column 1 of line 1. Each subordinate elementary screen item is located relative to the start of the containing screen record. Identifier-3 and identifier-4 are evaluated once at the start of execution ofthe statement.
16) If the LINE phrase is not specified,the screen record starts on line 1.
17) If the COLUMN phrase is not specified, the screen record starts in column 1
18) The initial position of the cursor is determined by the CURSOR clause in the SPECIAL-NAMES paragraph.
a) If the CURSOR clause is not specified, the initial cursor position during the execution of an ACCEPT screen statement is the start of the first input field described within screen-name-1.
b) If the CURSOR clause is specified, the initial cursor position is that represented by the value of the cursor locator at the beginning of the execution of the ACCEPT screen statement If the cursor locator does not indicate a position within an input field,the cursor shall be positioned as if the CURSOR clause had not been specified.
19) During the period while the operator isable to modify each elementary screen item, each screen item is   displayed on the terminal screen in accordance with any attributes specified in its screen description entry. The display may be modified as the operator selects or deselects each screen item as being the current screen item; The display of the current screen item may be modified as the operator keys data:
20) Data entered by the terminal operator in the current screen item shall be consistent with the PICTURE clause of that item: Ifthe screen item is numeric, the entered data shall be acceptable asan argument to the NUMVAL function: If the screen item is numeric-edited, the entered data shall be
580
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 611 ---

ISO /IEC 1989.2023 (E)
acceptable as an argument to the NUMVAL-C function. It is implementor-defined when the entered data is verified. is   implementor-defined whether inconsistent data causes the EC-DATA- INCOMPATIBLE  exception condition to exist or whether the system indicates an error  until consistent data is entered or until execution of the ACCEPT statement is terminated.
21) If inconsistent data is entered into a screen item and allowed by the implementor to remain there, the EC-DATA-INCOMPATIBLE exception condition is set to exist If consistent data was entered into one 0r more screen fields, these fields are transferred as specified in General rule 22,but the fields with inconsistent data are not transferred. The ACCEPT statement results in an unsuccessful completion, and execution proceeds as specified in General rule 25.
22) The ACCEPT screen statement causes the transfer of data from each elementary screen item that is subordinate to screen-name-1 and is specified with the TO or USING clause to the data item referenced in the TO or USING clause. For the purpose of these specifications, all such screen items are considered to be referenced by the ACCEPT screen statement Iftwo or more ofthese elementary screen items overlap, the EC-SCREEN-FIELD-OVERLAP exception condition is set to exist and the result of the transfer of data from the elementary screen items is defined by the implementor:
The transfer occurs after the terminal operator has been given the opportunity to modify the elementary screen items and the operator has pressed a terminator key or a user-defined or context-dependent function key: This transfer occurs in the following manner:
a) If the screen item is numeric, the data is transferred as though the following statement were executed:
COMPUTE receiving-field FUNCTION NUMVAL (screen-item)
b) If the screen item is numeric-edited, the data is transferred as though the following statement were executed:
COMPUTE receiving-field = FUNCTION NUMVAL-C (screen-item) Otherwise, the data is transferred as if the following statement were executed:
MOVE screen-item TO receiving-field
where:
receiving-field is the data item referenced in the TO or USING clause, and screen-item is the screen item:
23) If the CURSOR clause is specified in the special-names paragraph; the data item referenced in the CURSOR clause shall be updated during the execution of an ACCEPT screen statement and prior to the execution of any imperative statement associated with any ON EXCEPTION or NOT ON EXCEPTION clauses for that ACCEPT statement: It shall be updated to give the line and column position of the cursor when the ACCEPT terminates
24) Ifthe execution ofthe ACCEPT statement results in a successful completion with normal termination, the ON EXCEPTION phrase,if specified,is ignored and control is transferredto the end ofthe ACCEPT
@ISO/IEC 2023
581
[per-copy licence stamp]


--- Page 612 ---

ISO /IEC 1989.2023 (E)
statement Or, if the NOT ON EXCEPTION phrase is specified, to imperative-statement-2. If control is returned from imperative-statement-2, control is then transferred to the end of the ACCEPT statement
25) If the execution of the ACCEPT statement results in an unsuccessful completion, is terminated by a function key stroke, or causes an EC-SCREEN exception condition to exist; then:
a) If the ON EXCEPTION phrase is specified in the ACCEPT statement; control is transferred to imperative-statement-1, If control is returned from imperative-statement-1, control is then transferred to the end of the ACCEPT statement
b) If the ON EXCEPTION phrase is not specified in the ACCEPT statement; the following occurs
If the ACCEPT statement is specified in a statement that is in imperative-statement-1 in an exception-checking PERFORM statement and a WHEN phrase in that statement specifies the exception condition that occurred, control is transferred to the imperative-statement in that WHEN phrase and the flow of control is specified in the rules for the WHEN phrase. If control is returned from the WHEN phrase, control is then transferred to the end of the ACCEPT statement:
Otherwise, if there is no applicable WHEN phrase and there is an applicable declarative, control is transferred to that declarative. If control is returned from the declarative, control is transferred to the end of the ACCEPT statement;
If the ON EXCEPTION phrase is not specified in the ACCEPT statement and there are no other applicable exception processing procedures,
Ifthe EC-DATA-INCOMPATIBLE exception condition exists, execution continues as specified in 14.6.13.1.3,Fatal exception conditions:
If the EC-DATA-INCOMPATIBLE exception condition does not exist; control is transferred to the end ofthe ACCEPT statement and the NOT ON EXCEPTION phrase, if specified,is ignored:
582
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 613 ---

ISO /IEC 1989.2023 (E)
14.9.2 ADD statement
14.9.2.1 General
The ADD statement causes two 0r more numeric operands t0 be summed and the result to be stored:
14.9.2.2 General formats
Format 1 (simple)=
ADD
identifier-1 literal-1
TO { identifier-2 rounded-phrase ] }
ON SIZE ERROR imperative-statement-1 NOT ON SIZE ERROR imperative-statement-2 END-ADD
Format 2 (giving)
identifier-1 literal-1
identifier-2 literal-2
ADD
TO
GIVING {identifier-3 rounded-phrase ] }
ON SIZE ERROR imperative-statement-1 NOT ON SIZE ERROR imperative-statement-2 END-ADD
Format 3 (corresponding):
CORRESPONDING CORR
ADD
identifier-4 TO identifier-5 rounded-phrase
ON SIZE ERROR imperative-statement-1 NOT ON SIZE ERROR imperative-statement-2 END-ADD
where rounded-phrase is described in 14.7.4, ROUNDED phrase.
@ISO/IEC 2023
583
[per-copy licence stamp]


--- Page 614 ---

ISO /IEC 1989.2023 (E)
14.9.23 Syntax rules
1) When native arithmetic is in effect; the composite of operands described in 14.7.7, Arithmetic statements, is determined as follows:
a) In format 1,by using all of the operands in the statement
b) In format 2,by using all of the operands in the statement excluding the data items that follow the word GIVING.
In format 3, by using the two corresponding operands for each separate pair of corresponding data items_
2) Identifier-1 and identifier-2 shall reference numeric data items:
3) Literal-1 and literal-2 shall be numeric literals.
4) Identifier-3 shall reference a numeric data item 0r a numeric-edited data item
5) The words CORR and CORRESPONDING are equivalent.
6) Identifier-4 and identifier-5 shall be alphanumeric group items, national group items, variable- length groups, or strongly-typed group items and shall not be described with level-number 66.
14.9.2.4 General rules
1) When format 1 is used, the initial evaluation consists of determining the value to be added, that is literal-1 or the value of the data item referenced by identifier-1, 0r if more than one operand is specified,the sum of such operands The sum of the initial evaluation and the value ofthe data item referenced by identifier-2 is stored as the new value of the data item referenced by identifier-2.
When standard-decimal arithmetic or standard-binary arithmetic is in effect; the result ofthe initial evaluation is equivalent to the result of the arithmetic expression
(operand-11 operand-12
operand-In)
where the values of operand-1 are the values of literal-1 and the data items referenced by identifier-1 in the order in which they are specified in the ADD statement The result of the sum of the initial evaluation and the value of the data item referenced by identifier-2 is equivalent to the result of the arithmetic expression
(initial-evaluation identifier-2)
where initial-evaluation represents the result of the initial evaluation_
2) When format 2 is used, the initial evaluation consists of determining the sum of the operands preceding the word GIVING,that is literal-1 or the value of the data item referenced by identifier-1, and literal-2 or the value of the data item referenced by identifier-2. This value is stored as the new value of each data item referenced by identifier-3_
584
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 615 ---

ISO /IEC 1989.2023 (E)
When standard-decimal arithmetic or standard-binary arithmetic is in effect; the result ofthe initial evaluation is equivalent to the result of the arithmetic expression
(operand-11 + operand-12 +
operand-In operand-2)
where the values of operand-1 are the values of literal-1 and the data items referenced by identifier-1 in the order in which they are specified in the ADD statement and the value of operand-2 is the value of either literal-2 or the data item referenced by identifier-2 in the ADD statement:
3) When format 3 is used, data items in identifier-4 are added to and stored in corresponding items in identifier-5.
When standard-decimal arithmetic or standard-binary arithmetic is in effect; the result of the addition is equivalent to
(operand-1 operand-2)
where the value of operand-1 is the value ofthe data item in identifier-4 and the value of operand-2 is the value of the corresponding data item in identifier-5.
4) When native arithmetic is in effect and none of the operands is described with usage binary-char, binary-short; binary-long binary-double, float-short; float-long; or float-extended, enough places shall be carried so as not to lose any significant digits during execution.
5) Data items within identifier-4 are selected to be added to selected data items within identifier-5 accordingto the rules specified in 14.7.6,CORRESPONDING phrase. The results are the same as ifthe user had referred to each pair of corresponding identifiers in separate ADD statements
6) Additional rules and explanations relative to this statement are given in 14.6.13.2, Incompatible data; 14.7.4, ROUNDED phrase; 14.7.5, SIZE ERROR phrase and size error condition; 14.7.6, CORRESPONDING phrase; and 14.7.7, Arithmetic statements_
@ISO/IEC 2023
585
[per-copy licence stamp]


--- Page 616 ---

ISO /IEC 1989.2023 (E)
14.93 ALLOCATE statement
14.9.3.1 General
The ALLOCATE statement obtains dynamic storage_
If storage is being requested for a based item, the based item is assigned the address of the obtained storage and a data-pointer; if specified, is returned containing that address:
Ifa specified number of characters ofmemoryis being requested,a data-pointer addressing the obtained storage is returned:
14.9.3.2 General format
arithmetic-expression-1 CHARACTERS ALLOCATE data-name-1
INITIALIZED ] RETURNING data-name-2 ]
14.9.3.3 Syntax rules
1) The data item referenced by data-name-1 shall be described with the BASED clause:
2) If data-name-1 is specified, the RETURNING phrase may be omitted; otherwise, the RETURNING phrase shall be specified.
3) Data-name-2 shall reference a data item of category data-pointer:
4) If data-name-2 references restricted data-pointer; data-name-1 shall be specified and shall reference a typed data item, and the data item referenced by data-name-2 shall be restricted to the type of data-name-1
5) If both data-name-1 and data-name-2 are specified and data-name-1 references a strongly-typed group item, the data item referenced by data-name-2 shall be restricted to the type of data-name-1.
14.93.4 General rules
1)   Arithmetic-expression-1 specifies number of bytes of storage to be allocated., If arithmetic-expression-1 does not evaluate to an integer, the result is rounded up to the next whole number
2) If  arithmetic-expression-1 evaluates to or negative   value, the data item referenced  by data-name-2 is set to the predefined address NULL_
3) If data-name-1 is specified, the amount of storage to be allocated is the number of bytes required to hold an item as described by data-name-1. If a data description entry subordinate to data-name-1 contains an OCCURS DEPENDING ON clause; the maximum length of the record is allocated;
4) Ifthe specified amount of storage is available for allocation, it shall be obtained and:
586
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 617 ---

ISO /IEC 1989.2023 (E)
if the RETURNING phrase is specified, the data item referenced by data-name-2 is set to the address of that storage,
b) if data-name-1 is specified, the address of the based data item referenced by data-name-1 is set to the address of that storage
5) If the specified amount of storage is not available for allocation:
a) if the RETURNING phrase is specified, the data item referenced by data-name-2 is set to the predefined address NULL,
b) if data-name-1 is specified, the address ofthe based data item referenced by data-name-l is set to the predefined address NULL;
C) the EC-STORAGE-NOT-AVAIL exception condition is set to exist;
If both the INITIALIZED phrase and arithmetic-expression-1 are specified, all bytes of the allocated storage are initialized to binary zeros:
Ifboth the INITIALIZED phrase and data-name-1 are specified, the allocated storage is initialized as if an INITIALIZE data-name-1 WITH FILLER ALL TO VALUE THEN TO DEFAULT statement were executed:
8) Ifthe INITIALIZED phrase is not specified and arithmetic-expression-1 is specified,the content ofthe allocated storage depends on the INITIALIZE clause of the OPTIONS paragraph Ifit is specified; the content is that of the specified-fill-character: Otherwise, the content is undefined.
9) Ifthe INITIALIZED phrase is not specified and data-name-1 is specified, data items of class object or class pointer in the allocated storage are initialized to null and the content of the other data items in the allocated storage depends on the INITIALIZE clause of the OPTIONS paragraph: If it is specified, the content is that of the specified-fill-character: Otherwise, the content is undefined:
10) The allocated storage persists until explicitly released with a FREE statement or the run unit is terminated, whichever occurs first:
@ISO/IEC 2023
587
[per-copy licence stamp]


--- Page 618 ---

ISO /IEC 1989.2023 (E)
14.9.4 CALL statement
14.9.4.1 General
The CALL statement causes control to be transferred to a specific program within the run unit:
14.9.4.2 General formats
Format 1 (Program):
identifier-1 CALL literal-1
BY REFERENCE ] { identifier-2 } USING BY CONTENT { identifier-2 }
RETURNING identifier-3 ]
ON EXCEPTION imperative-statement-1 NOT ON EXCEPTION imperative-statement-2 END-CALL ]
588
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 619 ---

ISO /IEC 1989.2023 (E)
Format 2 (program-prototype):
identifier-1 literal-1
NESTED program-prototype-name-1
CALL
AS
identifier-2 [BY REFERENCE ] OMITTED
arithmetic-expression-1 boolean-expression-1 identifier-4 literal-2
[BY CONTENT
USING
arithmetic-expression-1 identifier-4 literal-2
[BY VALUE
RETURNING identifier-3 ]
ON EXCEPTION imperative-statement-1 NOT ON EXCEPTION imperative-statement-2
END-CALL ]
14.9.4.3 Syntax rules
FORMATS 1 AND 2
1) Identifier-1 shall be defined as an alphanumeric, national, or program-pointer data item_
2) Literal-1 shall be an alphanumeric or national literal and shall not be a zero-length literal:
3) Identifier-2 shall reference an address-identifier or a data item defined in the file, working-storage, local-storage, or linkage section: If the BY REFERENCE phrase is specified or implied, identifier-2 shall not be defined in the working-storage or file section ofa factory or an instance object
4) If the BY REFERENCE phrase is not specified 0r implied for an identifier-2 0r if identifier-2 is an address-identifier, identifier-2 is a sending operand.
5) If the BY REFERENCE phrase is specified or implied for an identifier-2 and identifier-2 is not an address-identifier,it is a receiving operand.
6) If the BY REFERENCE phrase is specified or implied for an identifier-2 that is a bit data item, identifier-2 shall be described such that it is aligned on a byte boundary and that subscripting and
@ISO/IEC 2023
589
[per-copy licence stamp]


--- Page 620 ---

ISO /IEC 1989.2023 (E)
the leftmost position in a reference modification of identifier-2 consist of only fixed-point numeric literals or arithmetic expressions whose result is positive integer, in which all operands are numeric literals and in which the exponentiation operator is not specified.
7) Identifier-3 shall reference a data item defined in the file, working-storage, local-storage; or linkage section;
8) If identifier-3 references a bit data item, it shall be described such that it is aligned on byte boundary and that subscripting and the leftmost position in a reference modification of identifier-3 consist of only fixed-point numeric literals or arithmetic expressions whose result is positive integer in which all operands are numeric literals and in which the exponentiation operator is not specified.
9) Identifier-3 is a receiving operand:
FORMAT 1
10) Ifthe BY REFERENCE phrase is specified or implied for an identifier-2,thatidentifier shall be neither strongly-typed group item nor a data item of class object or pointer.
11) Identifier-2 and identifier-3 shall not be described with the ANY LENGTH clause:
12) Identifier-2 shall not reference a variable-length group:
FORMAT 2
13) The NESTED phrase may be specified only in a program definition_
14) If identifier-1 references a restricted program-pointer; the signature of the program-prototype specified in the definition of that pointer shall be the same as the signature of program-prototype- name-1
15) If the NESTED phrase is specified, literal-1 shall be specified. Literal-1 shall be the same as the program-name specified in a PROGRAM-ID paragraph ofa common program as specified in 8.4.6.3_ Scope of program-names, or ofa program that is directly contained in the calling program:
16) Program-prototype-name-1 shall be specified in a program-specifier in the REPOSITORY paragraph: 17) Identifier-4 and any identifier specified in arithmetic-expression-1 or boolean-expression-1 is a sending operand:
18) Identifier-4 shall not be described with the ANY LENGTH clause_
19) If the BY CONTENT or BY REFERENCE phrase is specified or implied for an argument; the BY REFERENCE phrase shall be specified or implied for the corresponding formal parameter in the procedure division header:
20) BY CONTENT shall not be omitted when identifier-4 is an identifier that is permitted as a receiving operand, except that BY CONTENT may be omitted when identifier-4 is an object property.
590
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 621 ---

ISO /IEC 1989.2023 (E)
21) If the BY VALUE phrase is specified for an argument; the BY VALUE phrase shall be specified for the corresponding formal parameter in the procedure division header:
22) If identifier-4 or its corresponding formal parameter is specified with a BY VALUE phrase, identifier- 4 shall be of class numeric, object; or pointer:
23) If literal-2 or its corresponding formal parameter is specified with the BY VALUE phrase, literal-2 shall be a numeric literal.
24) If the OMITTED phrase is specified, the OPTIONAL phrase shall be specified for the corresponding formal parameter in the procedure division header:
25) The rules for conformance specified in 14.8.2,Parameters and 14.8.3, Returning items apply:
14.9.4.4 General rules
FORMATS 1 AND 2
1) The instance of the program, function, or method that executes the CALL statement is the activating runtime element:
2) The sequence of arguments in the USING phrase of the CALL statement and the sequence of formal parameters in the USING phrase of the called program's procedure division header determine the correspondence between arguments and formal parameters. This correspondence is positional and not by name equivalence:
NOTE The first argument corresponds to the first formal parameter; the second to the second,and the nth to the nth;
The effect of the USING phrase on the activated runtime element is described in 14.2, Procedure division structure, general rules:
3) Execution of the CALL statement proceeds as follows:
a) Arithmetic-expression-1, boolean-expression-1, identifier-1, identifier-2, and identifier-4 are evaluated and item identification is done for identifier-3 at the beginning ofthe execution of the CALL statement: Ifan exception condition exists,no program is called and execution proceeds as specified in General rule 3h. If an exception condition does not exist; the values of identifier-2_ identifier-4, arithmetic-expression-1, boolean-expression-1, or literal-2 are made available to the called program at the time control is transferred to that program:
b) The program being called is identified by its program-name or its location, which are determined as follows:
If identifier-1 references an alphanumeric or national data item or literal-1 is specified, the value of literal-1 or the content of the data item referenced by identifier-1 is the program-name of the program being called,as described in 8.3.2.2, User-defined words_
@ISO/IEC 2023
591
[per-copy licence stamp]


--- Page 622 ---

ISO /IEC 1989.2023 (E)
If identifier-1 references program-pointer data item, the data item referenced by identifier-1 contains the location of the program being called.
If neither identifier-1 nor literal-1 is specified, program-prototype-name-1 determines the externalized program-name of the program being called, according to the rules specified in 12.3.8,REPOSITORY paragraph.
If the program being called is a COBOL program, the runtime system attempts to locate the program being called_ When the program-name is used for locating the program, the rules specified in 8.4.6, Scope of names and 8.4.6.3, Scope of program-names, apply. If the program being called is not a COBOL program,the rules for program-name formation and for locating the program are defined by the implementor.
If  the data item referenced by identifier-1 contains the predefined   address   NULL, the EC-PROGRAM-PTR-NULL exception condition is set to exist: If the program cannot be located or identifier-1 references a zero-length item, the EC-PROGRAM-NOT-FOUND exception condition is set to exist: If either the EC-PROGRAM-NOT-FOUND or the EC-PROGRAM-PTR-NULL exception condition exists; the program call is not successful, and execution continues as specified in General rule 3h.
If the program is located but the resources necessary to execute the program are not available the EC-PROGRAM-RESOURCES exception condition is set to exist, the program call is not successful, and execution continues as specified in General rule 3h. The runtime resources that are checked in order to determine the availability ofthe called program for execution are defined by the implementor:
d) If the resources are available and the program being called is a COBOL program, the rules for conformance specified in 14.8.2, Parameters and 14.8.3,Returning items apply. If a violation of these rules is detected, the EC-PROGRAM-ARG-MISMATCH exception condition is set to exist if checking for it is enabled in both the activated program and activating runtime element; the program call is not successful, and execution continues as specified in General rule 3h.
e) External items are checked to ensure that they comply with the following rules as specified in 14.8.4, External items:
Rule
Exception condition EC-EXTERNAL-DATA-MISMATCH
14.8.4.2, Correspondence of external data items used in external files
14.8.4.3, Correspondence of external data item formats 14.8.4.4, Correspondence of external file control entries
EC-EXTERNAL-FORMAT- CONFLICT
EC-EXTERNAL-FILE-MISMATCH
If one of the rules listed above is violated and checking for it is enabled for the associated exception in both the activated program and activating runtime element, that exception is set to exist; the program call is not successful,and execution continues as specified in General rule 3h.
592
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 623 ---

ISO /IEC 1989.2023 (E)
f) If the program being called is in the active state and that program does not have the recursive attribute; the EC-PROGRAM-RECURSIVE-CALL exception condition is set to exist; the program call is not successful, and execution continues as specified in General rule 3h.
g) Ifa fatal exception condition has not been raised, the program specified by the CALL statement is made available for execution and control is transferred to the called program If identifier-1 is defined as a program-pointer data item and contains an invalid program address, execution of the CALL statement is undefined. If the called program is a COBOL program; its execution is described in 14.2, Procedure division structure; otherwise the execution is defined by the implementor
h) Ifthe program was not successfully called and an exception condition was set to exist; one ofthe following actions occurs:
Ifthe exception condition is any ofthe EC-PROGRAM or EC-EXTERNAL exception conditions and an ON EXCEPTION phrase is specified in the CALL statement; control is transferred to imperative-statement-1. If control is returned from imperative-statement-1, control is then transferred to the end of the CALL statement
Ifchecking for the exception condition is enabled,and ifthe exception condition is one ofthe EC-PROGRAM or EC-EXTERNAL exception conditions and an ON EXCEPTION phrase is not specified or if the exception condition is not one of the EC-PROGRAM exception conditions, any applicable exception processing statements are executed. If control is returned from these statements, control is then transferred to the end of the CALL statement All other effects of the CALL statement are defined by the implementor
If checking for the exception condition is not enabled, subsequent behavior is as specified in 14.6.13.1, Exception conditions.
i) If the program was successfully called, after control is returned from the called program the ON EXCEPTION phrase, if specified, is ignored: If an exception condition is propagated from the called program, execution continues as specified in 14.6.13.1, Exception conditions; otherwise, control is transferred to the end of the CALL statement Or,if the NOT ON EXCEPTION phrase is specified,to imperative-statement-2  If controlis returned from imperative-statement-2, control is then transferred to the end of the CALL statement
4) If a RETURNING phrase is specified,the result of the activated program is placed into identifier-3_
FORMAT 1
5) Both the BY CONTENT and BY REFERENCE phrases are transitive across the parameters that follow them until another BY CONTENT or BY REFERENCE phrase is encountered: If neither the BY CONTENT nor the BY REFERENCE phrase is  specified prior to the first   parameter, the BY REFERENCE phrase is assumed:
FORMAT 2
If the NESTED phrase is specified, the common or contained program that has literal-1 specified in the PROGRAM-ID paragraph is used to determine the characteristics of the called program:
@ISO/IEC 2023
593
[per-copy licence stamp]


--- Page 624 ---

ISO /IEC 1989.2023 (E)
7) If the NESTED phrase is not specified, program-prototype-name-1 is used to determine the characteristics of the called program:
8) An argument that consists merely of a single identifier or literal is regarded as an identifier or literal rather than an arithmetic or boolean expression:
9) If an argument is specified without any of the keywords BY REFERENCE, BY CONTENT; or BY VALUE the manner used for passing this argument is determined as follows:
a) When the BY REFERENCE   phrase is  specified or  implied for the corresponding formal parameter:
if the argument meets the requirements of Syntax rule 3, BY REFERENCE is assumed;
if the argument does not meet the requirements of Syntax rule 3,BY CONTENT is assumed:
b) When the BY VALUE phrase is specified or implied for the corresponding formal parameter, BY VALUE is assumed,
10) Control is transferred to the called program in a manner consistent with the entry convention specified for the program:
11) Ifan OMITTED phrase is specified or a trailing argumentis omitted, the omitted-argument condition for that parameter evaluates to true in the called program (8.8.4.8, Simple omitted argument condition.)
12) If a parameter for which the omitted-argument condition is true is referenced in a called program, except as an argument or in the omitted-argument condition, the EC-PROGRAM-ARG-OMITTED exception condition is set to exist
594
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 625 ---

ISO /IEC 1989.2023 (E)
14.9.5 CANCEL statement
14.9.5.1 General
The CANCEL statement ensures that the next time the referenced program is called it will be in its initial state;
14.9.5.2 General format
identifier-1 literal-1 program-prototype-name-1
CANCEL
14.9.5.3 Syntax rules
1) Identifier-1 shall be defined as an alphanumeric or national data item_
2) Literal-1 shall be an alphanumeric or national literal and shall not be a zero-length literal:
3) Program-prototype-name-1 shall be a program prototype specified in the REPOSITORY paragraph
14.9.5.4 General rules
1) The program to be canceled is identified by one of the following:
a) the content of the data item referenced by identifier-1,
b) the value of literal-1,
program-prototype-name-l_
If identifier-1 or literal-1 is specified, 8.3.2.2, User-defined words, describes how this value is used to identify the program to be canceled:
2) The program-name is used by the runtime system to locate the program according to the rules specified in 8.4.6,Scope of names,and 8.4.6.3, Scope of program-names
3) Subsequent to the execution of a CANCEL statement; the program referred to therein ceases to have any logical relationship to the run unit in which the CANCEL statement appears If the program referenced by a successfully executed CANCEL statement in a run unit is subsequently called in that run unit; that program is in its initial state: (See 14.6.2, State of a function, method, object; or program )
NOTE It is neither prohibited nor required that the storage of the specified program be freed by the execution ofa CANCEL statement
4) When a CANCEL statement is executed, all programs contained within the program referenced by the CANCEL statement are also canceled: The result is the same as if an explicit CANCEL statement
@ISO/IEC 2023
595
[per-copy licence stamp]


--- Page 626 ---

ISO /IEC 1989.2023 (E)
were executed for each contained program in the reverse order in which the programs appear in the outermost program:
5) The program to be canceled shall not be in the active state or contain a program in the active state If a program in the active state is explicitly or implicitly referenced in a CANCEL statement and checking for EC-PROGRAM-CANCEL-ACTIVE is enabled in both the program to be canceled and the runtime element containing the CANCEL statement, the EC-PROGRAM-CANCEL-ACTIVE exception condition is raised in the runtime element containing the CANCEL statement and the referenced program is not canceled: If checking for EC-PROGRAM-CANCEL-ACTIVE is not enabled, the results of such a reference are defined by the implementor:
6) logical relationship to a canceled program is established only by execution of a subsequent CALL statement referencing that program:
7) No action is taken when a CANCEL statement is executed referencing a program that has not been called in this run unit orhas been called and is at present canceled. Control is transferred to the next executable statement following the explicit CANCEL statement:
8) The contents of data items in external data records described by a program are not changed when that program is canceled.
9)   During execution ofa CANCEL statement,an implicit CLOSE statement without any optional phrases is executed for each file-name associated with an internal file connector that is open in the program referenced in the CANCEL statement These implicit CLOSE statements are executed for all such files, even when an error occurs during the execution of such CLOSE statements. Any USE EXCEPTION procedures associated with any of these files or associated with any EC-[-0 exception conditions raised for any of these files are not executed. If the CANCEL statement is executed in the flow of control in imperative-statement-1 ofa PERFORM statement that contains a WHEN phrase and an EC- [-0 exception condition specified for that exception exists, the statements in that WHEN phrase are not executed.
10) If the program to be canceled is other than a COBOL program, the effects of the CANCEL statement are implementor-defined.
11) If a program-pointer has been set to point to the program to be canceled; the result of referencing the program-pointer in a subsequent CALL statement is undefined.
12) If identifier-1 references a zero-length item, the CANCEL statement has no effect.
596
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 627 ---

ISO /IEC 1989.2023 (E)
14.9.6 CLOSE statement
14.9.6.1 General
The CLOSE statement terminates the processing of reels/units and files with rewind or removal where applicable_
14.9.6.2 General format
REEL UNIT
FOR REMOVAL ]
CLOSE
file-name-1
WITH NO REWIND
14.9.6.3 Syntax rules
1) The NO REWIND, REEL, and UNIT phrases may be used only with files that are of sequential organization.
2) The words REEL and UNIT are equivalent:
3) A CLOSE statement that specifies more than one file-name shall not be specified in imperative- statement-l in an exception-checking PERFORM statement:
14.9.6.4 General rules
1) The file connector referenced by file-name-1 shall be open: If the file connector is not open, the CLOSE statement is unsuccessful and the [-0 status indicator for the file connector is set to '42'_
2) For the purpose of showing the effect of various types of CLOSE statements as applied t0 various storage media, all files are divided into the following categories, where the term 'file' means the physical file:
a) Non-unit:A file whose input or output medium is such that the concepts ofrewind and units have no meaning:
b) Sequential single-unit. A sequential file that is entirely contained on one unit
Sequential multi-unit A sequential file that is contained on more than one unit:
d) Non-sequential single/multi-unit A file with organization other than sequential, that resides on mass storage device
3) The results of executing each type of CLOSE for each category of physical file are summarized in Table 14,Relationship of categories of physical files and the format of the CLOSE statement:
@ISO/IEC 2023
597
[per-copy licence stamp]


--- Page 628 ---

ISO /IEC 1989.2023 (E)
Table 14 _ Relationship of categories ofphysical files and the format of the CLOSE statement
CLOSE statement format
File category Non-unit Sequential Sequential Non-sequential single-unit multi-unit single /multi- unit
CLOSE
C,f
a,,f
CLOSE WITH NO REWIND C,g CLOSE UNIT
b,c
a,b,c
N/A
e,f
e,f
N/A N/A
CLOSE UNIT FOR REMOVAL
d,e,f
d,,f
The definitions of the symbols in Table 14,Relationship of categories of physical files and the format of the CLOSE statement; are given below. The notation 'N/A means that the combination is not applicable: The other symbols apply to the rules below. Where the definition depends on whether the file is an input; output; or input-output file,alternate definitions are given; otherwise, a definition applies to input; output; and input-output files_
a) Effect on previous units
Input files and input-output files:
All units in the physical file prior to the current unit are closed except those units controlled by prior CLOSE UNIT statement;
If the current unit is not the last in the physical file, the units in the physical file following the current one are not processed.
Output files:
All units in the physical file prior to the current unit are closed except those units controlled by prior CLOSE UNIT statement:
b) No rewind of current reel
The current unit is left in its current position.
c) Close file
Closing operations specified by the implementor are executed:
Unit removal
The current unit is rewound, when applicable, and the unit is logically removed from the run unit; however,the unit may be accessed again, in its proper order ofunits within the physical file, ifa CLOSE statement without the UNIT phrase is subsequently executed for this file followed by the execution ofan OPEN statement for the file_
598
@ISO /IEC 2023
[per-copy licence stamp]


--- Page 629 ---

ISO /IEC 1989.2023 (E)
NOTE This Working Draft International Standard does not address when the unit is unloaded or left loaded.
Close unit
Input files andinput-output files (unit medial:
Ifthe current unitis thelast or only unit forthe physical file, there is no unit swap,the current volume pointer remains unchanged, and the file position indicator is set to indicate that no next or previous logical record exists
If another unit exists for the physical file, a unit swap Occurs, the current volume pointer is updated to point to the next unit existing in the physical file, and the file position indicator is set to one less than the number of the first record existing on the new current volume If no records exist for the current volume,another unit swap occurs:
Qutput files (unit medial:
A unit swap occurs and the current volume pointer is updated to point to the new unit
Input files input-output files_and output files (non-unit media}:
Execution of this statement is considered successful: The file remains in the open mode, the file position indicator is unchanged, the [-0 status indicator for the file connector is set to '07', and no other action takes place:
9) Rewind
The current reel or analogous device is positioned at its physical beginning:
g) Optional phrases ignored
The CLOSE statement is executed as if none ofthe optional phrases were present: The [-0 status indicator for the file connector is set to '07'
4) The execution ofthe CLOSE statement causes the value of the [-0 status associated with file-name-1 to be updated as specified in 9.1.13,1-0 status_
5) No report associated with a report file that is referenced in the CLOSE statement shall be in the active state. If any report is in the active state, the CLOSE statement for that file is completed and the EC- REPORT-NOT-TERMINATED exception condition is set to exist
If the file position indicator of the file connector referenced by file-name-1 is set to indicate that an optional input file is not present; no end-of-file or unit processing is performed for the file and the file position indicator and the current volume pointer are unchanged
7) The availability of the record area associated with file-name-1 to the runtime element depends on the successful or unsuccessful execution of the CLOSE statement without the UNIT phrase and whether file-name-1 is referenced in a SAME RECORD AREA clause: If file-name-1 is specified in a
@ISO/IEC 2023
599
[per-copy licence stamp]


--- Page 630 ---

ISO /IEC 1989.2023 (E)
SAME RECORD AREA clause, the record area is available to the runtime element if any of the file connectors referenced by the other file-names in that SAME RECORD AREA clause are open: If none ofthese file connectors is open 0r if file-name-1 is not specified in a SAME RECORD AREA clause,the successful execution ofa CLOSE statement makes the record area unavailable to the runtime element and the unsuccessful execution of the CLOSE statement makes the availability of the record area undefined.
8) Following the successful execution ofa CLOSE statement without the UNIT phrase, the physical file is no longer associated with the file connector referenced by file-name-1 and the open mode ofthat file connector is set such that it is no longer in an open mode
9)    Except when the file is specified in an APPLY COMMIT clause, the file lock and any record locks associated with the file connector referenced by file-name-1 are released by the execution of the CLOSE statement
10) If more than one file-name-1 is specified in a CLOSE statement; the result of executing this CLOSE statement is the same as ifa separate CLOSE statement had been written for each file-name-1 in the same order as specified in the CLOSE statement: If an implicit CLOSE statement results in the execution ofa declarative procedure that executes a RESUME statement with the NEXT STATEMENT phrase, processing resumes at the next implicit CLOSE statement; ifany:
600
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 631 ---

ISO /IEC 1989.2023 (E)
14.9.7 COMMIT statement
14.9.7.1 General
The COMMIT statement makes permanent all changes to all files subject to active APPLY COMMIT clauses in the run unit and releases all record locks on those files It also saves the contents of any data- items explicitly or implicitly referenced in active APPLY COMMIT clauses for potential use in a subsequent rollback
14.9.7.2 General forms
COMMIT
14.9.7.3 Syntax rules
1) This statement shall not be specified in a recursive source element
2) This statement shall not be specified in the input or output procedure of a MERGE or file SORT statement;
14.9.7.4 General rules
1) If this statement is executed when there is no active APPLY COMMIT clause, then it has the same effect as a CONTINUE statement with no additional phrases.
NOTE 1 When there is no active APPLY COMMIT clause then no files or data items will have been specified for commit and rollback
2) If this statement is attempted to be executed under the control of a recursive runtime element or a file SORT or MERGE statement; then the exception condition EC-FLOW-COMMIT is set to exist
NOTE 2 This will result in abnormal termination when the implicit ROLLBACK statement is executed as specified in 14.6.13.1.3,Fatal exception conditions
3) The execution ofthe COMMIT statement permanently applies any changes made to the files specified in all the active APPLY COMMIT clauses and releases all record locks associated with those files: The COMMIT statement also deactivates any APPLY COMMIT clauses in exited initial programs and canceled runtime elements
4) For files specified in active APPLY COMMIT clauses that have been closed and not reopened prior to the COMMIT statement; the file locks are released.
5) The contents ofall data-items referenced in the remaining active APPLY COMMIT clauses are saved for potential use in subsequent rollback: This includes the file status data items and data-items specified in the linage or record clauses of the file descriptions_
@ISO/IEC 2023
601
[per-copy licence stamp]


--- Page 632 ---

ISO /IEC 1989.2023 (E)
14.9.8 COMPUTE statement
14.9.8.1 General
The COMPUTE statement assigns to one or more data items the value ofan arithmetic or boolean expression,
14.9.8.2 General formats
Format 1 (arithmetic-compute):
COMPUTE { identifier-1 rounded-phrase ]
arithmetic-expression-1
ON SIZE ERROR imperative-statement-1 NOT ON SIZE ERROR imperative-statement-2 END-COMPUTE
Format 2 (boolean-compute):
COMPUTE { identifier-2 }
boolean-expression-1 END-COMPUTE
where rounded-phrase is described in 14.7.4, ROUNDED phrase.
14.9.8.3 Syntax rules
FORMAT 1
1) Identifier-1 shall reference either an elementary numeric item 0r an elementary numeric-edited item
FORMAT 2
2) Identifier-2 shall reference an elementary boolean data item:
3) Boolean-expression-1 shall not consist solely of the figurative constant ALL literal:
14.9.8.4 General rules
FORMAT 1
1) The execution ofan arithmetic-compute statement consists of the determination ofa numeric value and the subsequent storing of that numeric value:
a) When native arithmetic, or standard-decimal arithmetic is in effect, and arithmetic-expression- 1 consists of a single fixed-point numeric literal or single fixed-point numeric data item, arithmetic-expression-1 evaluates to the exact algebraic value of that literal or item, within the
602
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 633 ---

ISO /IEC 1989.2023 (E)
constraints specified in 14.6.13.2, Incompatible data Rounding truncation; and decimal point alignment specifications do not apply to the production of that exact algebraic value
NOTE Noninteger decimal values are frequently inexact when expressed in binary floating-point formats, including that of an SBIDI. For that reason, arithmetic-expression-1 is unconditionally evaluated according to the rules for arithmetic expressions for standard-binary arithmetic, regardless ofthe specific contents ofarithmetic-expression-1_
b) Otherwise, arithmetic-expression-1 is evaluated to produce an algebraic value according to the specifications in 8.8.1, Arithmetic expressions.
2) The value obtained according to rule 1 is then stored, in conformance with the specifications in 14.6.8, Alignment and transfer of data into data items, 14.7.4, ROUNDED phrase, and 14.7.5, SIZE ERROR phrase and size error condition, into each data item referenced by identifier-1.
FORMAT 2
3) The execution ofa boolean-compute statement consists ofthe determination ofa boolean value and the subsequent storing of that boolean value:
Boolean-expression-1 evaluates to the value ofthat boolean expression, subject to the specifications in 8.8.2, Boolean expressions, and 14.6.13.2, Incompatible data: The number of boolean positions in the value resulting from the evaluation of boolean-expression-1 is the number of boolean positions in the largest boolean item referenced in the expression. The resulting value is then stored in each data item referenced by identifier-2
@ISO/IEC 2023
603
[per-copy licence stamp]


--- Page 634 ---

ISO /IEC 1989.2023 (E)
14.9.9 CONTINUE statement
14.9.9.1 General
The CONTINUE statement is a no-operation statement: It indicates that no executable statement is present and that execution will continue with the next executable statement: The CONTINUE statement may also specify a time period in seconds that execution will be suspended
14.9.9.2 General format
CONTINUE [AFTER arithmetic-expression-1 SECONDS]
14.9.93 Syntax rules
1) The CONTINUE statement may be used anywhere imperative-statement may be used.
conditional statement or an
2) If the AFTER phrase is not specified, then the statement is processed as if an AFTER phrase were specified with arithmetic-expression-1 specified as zero.
14.9.9.4 General rules
1) If the AFTER phrase is specified, arithmetic-expression-1 specifies the number of seconds that execution is suspended. The CONTINUE statement behaves as though the length of time that execution shall be suspended was stored in a temporary data item whose picture is 9(nJV9(m), in the manner specified by this rule. The implementor shall specify the value of m; which may be zero, andthe value ofn,which shall be greater than zero. Any - value ofm that is greater than 2 is processor- dependent: The implementor shall specify the maximum meaningful value ofarithmetic-expression- 1.Ifthe valuearithmetic-expression-1 is greaterthan this maximum meaningful value, the maximum meaningful value is placed into the temporary data item; otherwise, the value of arithmetic- expression-1 is used as the sending item and the temporary data item as the receiving item in an implicit COMPUTE statement without the ROUNDED phrase. Ifarithmetic-expression-1 evaluates to a value that is less than zero, the following takes place:
a) The value of arithmetic-expression-1 is set to 0.
b) checking for FC-CONTINUE-LESS-THAN-ZERO is enabled, the EC-CONTINUE-LESS-THAN- ZERO exception condition is set to exist and processing continues as specified in 14.6.13.1.4_ Nonfatal exception conditions:
C) If checking for FC-CONTINUE-LESS-THAN-ZERO is not enabled, processing continues with the next executable statement
Otherwise, execution is suspended for the period of time determined by arithmetic-expression-1_ When the time is passed, execution continues with the next executable statement;
2) Implicit CONTINUE statements shall be processed as if AFTER ZERO SECONDS were specified:
604
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 635 ---

ISO /IEC 1989.2023 (E)
14.9.10 DELETE statement
14.9.10.1 General
The DELETE RECORD statement logically removes a record from a mass storage file. The DELETE FILE statement causes the removal of the referenced files from the mass storage device.
14.9.10.2 General formats
Format 1 (record):
DELETE file-name-1 RECORD
retry-phrase ] INVALID KEY imperative-statement-1 NOT INVALID KEY imperative-statement-2 END-DELETE
Format 2 (file}:
DELETE FILE OVERRIDE ] file-name-1 [retry-phrase ] ON EXCEPTION imperative-statement-3 NOT ON EXCEPTION imperative-statement-4
END-DELETE
where retry-phrase is described in 14.7.9,RETRY phrase
14.9.10.3 Syntax rules
FORMAT 1
1) The DELETE RECORD statement shall not be specified for a file with sequential organization:
2) The INVALID KEY and the NOT INVALID KEY phrases shall not be specified for a DELETE RECORD statement that references a file that is in sequential access mode
FORMAT 2
3) The file description entry associated with the DELETE FILE statement shall not be a sort-merge file description entry.
4) DELETE FILE statement that  specifies more than one  file-name shall not be specified in imperative-statement-1 in an exception-checking PERFORM statement
@ISO/IEC 2023
605
[per-copy licence stamp]


--- Page 636 ---

ISO /IEC 1989.2023 (E)
14.9.10.4 General rules
FORMAT 1
1) The open mode of the file connector referenced by file-name-1 shall be [-0 and the physical file associated with that file connector shall be a mass storage file
2) For a file that is in the sequential access mode, the last input-output statement executed for file-name-1 prior to the execution of the DELETE RECORD statement shall have been a successfully executed READ statement The mass storage control system logically removes from the physical file the record that was accessed by that READ statement:
3) If the file is indexed and the access mode is random or dynamic; the mass storage control system logically removes from the physical file the record identified by the content ofthe prime record key data item associated with file-name-1. fthe physical file does not contain the record specified by the key, the invalid key condition exists: (See 9.1.14,Invalid key condition:)
4) If the file is relative and the access mode is random or dynamic, the mass storage control system logically removes from the physical file that record identified by the content of the relative key data item associated with file-name-1. Ifthe physical file does not contain the record specified by the key, the invalid key condition exists. (See 9.1.14, Invalid key condition:)
5) After the successful execution of a DELETE RECORD statement; the identified record has been logically removed from the physical file and can no longer be accessed.
6) Ifrecord locking is enabled for the file connector referenced by file-name-1 and the record identified for deletion is locked by another file connector; the result of the operation depends on the presence or absence of the RETRY phrase. If the RETRY phrase is specified, additional attempts may be made to delete the record as specified in the rules in 14.7.9, RETRY phrase. If the RETRY phrase is not specified or the record is not successfully removed as specified by the RETRY phrase, the record operation conflict condition exists. The -0 status is set in accordance with the rules for the RETRY phrase:
When the record operation conflict condition exists as a result of the DELETE RECORD statement:
a) The record is not logically removed, and may be accessed:
b) A value is placed into the [-0 status associated with file-name-1 to indicate the record operation conflict condition_
c) The DELETE RECORD statement is unsuccessful
7) If record locks are in effect and the file is not subject to an active APPLY COMMIT clause, the following actions take place:
a) If single record locking is specified for the file connector associated with filename-l:
A lock held by that file connector on the deleted record is released at the completion of the successful execution of the DELETE RECORD statement
606
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 637 ---

ISO /IEC 1989.2023 (E)
A lock held by that file connector on another record is released at the beginning of the execution of the DELETE RECORD statement:
b) If multiple record locking is specified for the file connector associated with file-name-1,all locks held on the deleted record are released at the completion of the successful execution of the DELETE RECORD statement;
8) The execution of a DELETE RECORD statement does not affect the content ofthe record area or the content of the data item referenced by the data-name specified in the DEPENDING ON phrase of the RECORD clause associated with file-name-1_
9) The file position indicator is not affected by the execution ofa DELETE RECORD statement:
10) The execution of the DELETE RECORD statement causes the value of the [-0 status associated with file-name-1 to be updated as specified in 9.1.13,1-0 status
11) Transfer of control following the successful or unsuccessful execution of the DELETE RECORD operation depends on the presence or absence of the optional INVALID KEY and NOT INVALID KEY phrases in the DELETE RECORD statement as specified in 9.1.14, Invalid key condition.
FORMAT 2
12) If more than one file-name-1 is specified in a DELETE FILE statement; the result of executing this statement is the same as if a separate DELETE FILE statement had been written for each file-name- 1in the same order as specified in the DELETE FILE statement:
13) The file connector referenced by file-name-1 shall not be open. If the file is open the -0 status value in the file connector referenced by file-name-l is set to '41'.
14) If the file associated with file-name-1 is not present; the execution of the DELETE FILE statement is successful and the [-0 status value in the file connector referenced by file-name-1 is set to '05'.
15) If file locking is in effect for file-name-1 and the file is locked by another file connector; the result of the operation depends on the presence or absence of the RETRY phrase. If the RETRY phrase is specified, additional attempts may be made to delete the file as specified in the rules in 14.7.9,RETRY phrase If the RETRY phrase is not specified or the file is not successfully deleted as specified by the RETRY phrase, the file sharing conflict condition exists The [-0 status is set in accordance with the rules for the RETRY phrase:
When the file sharing conflict condition exists as a result of the DELETE FILE statement:
a) The file is not deleted, and may be accessed:
b) The valu62' is placed into the [-0 status associated with file-name-1 to indicate the file operation conflict condition:
The DELETE FILE statement is unsuccessful_
@ISO/IEC 2023
607
[per-copy licence stamp]


--- Page 638 ---

ISO /IEC 1989.2023 (E)
NOTE For file connectors subject to APPLY COMMIT clauses, while those APPLY COMMIT clauses remain active, then file and record locking persists Such files can still be deleted,however ifthey are,then in the event of a rollback they will be restored to the state they were in at the last commit Or, if none, the start of the run unit
16) Ifthe file associated with file-name-1 is presentand insufficientauthority exists to delete the file, the execution ofthe DELETE FILE statementis unsuccessful, andthe [-0 status value in the file connector referenced by file-namel is set t37'.
17) If the storage medium for the file does not allow file deletion, the execution of the DELETE FILE statement is unsuccessful, and the [-0 status value in the file connector referenced by file-name-1 is set to '37'.
18) If the OVERRIDE phrase is not specified, the attributes ofthe file connector referenced by file-name- 1 and the fixed file attributes of the physical file shall match: If the attributes do not match the DELETE FILE statement is unsuccessful, and the [-0 status value in the file connector referenced by file-name-l1 is set to '39'. If the OVERRIDE phrase is specified, the file attributes are not checked:
19) The implementor shall define which ofthe fixed-file attributes are validated during the execution of the DELETE FILE statement: The validation of fixed-file attributes may vary depending on the organization or storage medium of the file. (9.1.6, Fixed file attributes)
20) If the execution of the DELETE FILE statement is successful; the file is deleted if it exists or no action takes place ifthe mass storage file does not exist and the following actions take place in the following order:
a) Either [-0 status value '00' 005' is placed in the [-0 status associated with file-name-1.
b) If it is enabled, the level-3 EC-I-0 exception condition associated with the -0 status value is set to exist
c) Ifthe ON EXCEPTION phrase is specified in the DELETE FILE statement;any applicable exception processing statements are not executed and control is transferred to the imperative-statement- 3 specified in the ON EXCEPTION phrase If control is returned from these statements, control is then transferred to the end ofthe DELETE statement:
Ifthe ON EXCEPTION phrase is not specified in the DELETE FILE statement,any applicable input- output exception processing statements are executed as specified by the rules for 9.1.12, Input- output exception processing: If control is returned from these statements, control is then transferred to the end of the DELETE statement
21) If the execution of the DELETE FILE statement is successful, the file is deleted or has already been deleted,and the following actions take place in the following order:
a) A successful value is placed in the I-0 status associated with file-name-1.
b) If the NOT ON EXCEPTION phrase is specified in the DELETE FILE statement, control is transferred to the imperative-statement-4 specified in the NOT ON EXCEPTION phrase: The ON
608
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 639 ---

ISO /IEC 1989.2023 (E)
EXCEPTION phrase is ignored, ifitis specified. If control is returned from imperative-statement- 4,control is then transferred to the end of the DELETE statement
@ISO/IEC 2023
609
[per-copy licence stamp]


--- Page 640 ---

ISO /IEC 1989.2023 (E)
14.9.11 DISPLAY statement
14.9.11.1 General
The device format of the DISPLAY statement causes data to be transferred t0 a hardware or software device in the operating environment
The screen format ofthe DISPLAY statement causes data associated with a literal or data item that is referenced in a screen item to be made available to the specified screen item and to be displayed on the terminal screen with specified attributes and at the specified position:
14.9.11.2 General formats
Format 1 (device):
DISPLAY
identifier-1 literal-1
UPON mnemonic-name-1
WITH NO ADVANCING
END-DISPLAY
Format 2 (screen):
DISPLAY screen-name-1
identifier-2 integer-1
LINE NUMBER
AT
COLUMN COL
identifier-3 integer-2
NUMBER
ON EXCEPTION imperative-statement-1 NOT ON EXCEPTION imperative-statement-2 END-DISPLAY ]
14.9.11.3 Syntax rules
FORMAT 1
1)   Identifier-1 shall not reference a data item of class message-tag, object; or pointer:
2) Mnemonic-name-1 shall be specified in the SPECIAL-NAMES paragraph of the environment division and shall be associated with an implementor-defined device-name that is identified in the operating environment as a hardware or software device capable of receiving data from the program:
610
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 641 ---

ISO /IEC 1989.2023 (E)
FORMAT 2
3) Identifier-2 and identifier-3 shall be unsigned integer data items_
14.9.11.4 General rules
FORMAT 1
1) The DISPLAY statement causes the content of each operand to be transferred to the device in the order listed: If an operand is a zero-length data item or a zero-length literal, no data is transferred for that operand. Any conversion of data required between literal-1 or the data item referenced by identifier-1 and the device is defined by the implementor:
2) The implementor shall define, for each device,the size ofa data transfer:
3) If a figurative constant is specified as one of the operands, only a single occurrence of the figurative constant is displayed. A figurative constant other than ALL national literal shall be the alphanumeric representation of that figurative constant
4) If the device is capable of receiving data ofthe same size as the data item being transferred, then the data item is transferred,
5) If a device is not capable of receiving data of the same size as the data item being transferred, then one of the following applies:
a) Ifthe size ofthe data item being transferred exceeds the size ofthe data that the device is capable of receiving in a single transfer, the data beginning with the leftmost character is stored aligned to the left in the receiving device,and the remaining data is then transferred according to General rules 4 and 5 until all the data has been transferred_
b) Ifthe size ofthe data item thatthe device is capable ofreceiving exceeds the size ofthe data being transferred, the transferred data is stored aligned to the left in the receiving device:
When a DISPLAY statement contains more than one operand; the size ofthe sending item is the sum of the sizes associated with the operands, and the values of the operands are transferred in the sequence in which the operands are encountered without modifying the positioning of the device between the successive operands.
7) If identifier-1 references a variable-length group, the format in which its contents are displayed is defined by the implementor_
8) If the UPON phrase is not specified, the implementor's standard display device is used.
9) Ifthe WITH NO ADVANCING phrase is specified, then the positioning ofthe device shall not be reset to the next line or changed in any other way following the display of the last operand. If the device is capable of positioning to a specific character position, it will remain positioned at the character position immediately following the last character ofthe last operand displayed. If the device is not capable of positioning to a specific character position, only the vertical position, if applicable, is affected:. This may cause overprinting if the device supports overprinting:
@ISO/IEC 2023
611
[per-copy licence stamp]


--- Page 642 ---

ISO /IEC 1989.2023 (E)
10) Ifthe WITH NO ADVANCING phrase is not specified, then after the last operand has been transferred to the device, the positioning of the device shall be reset to the leftmost position of the next line of the device.
11) If vertical positioning is not applicable on the device, the vertical positioning shall be ignored:
FORMAT 2
12) Column and line number positions are specified in terms of alphanumeric character positions.
13) The DISPLAY statement causes the transfer of data in accordance with the MOVE statement rules to each elementary screen item that is subordinate to screen-name-1 and is specified with the FROM_ USING, or VALUE clause, from the data item or literal referenced in the FROM, USING, or VALUE clause For the purpose ofthese specifications, all such screen items are considered to be referenced by the DISPLAY screen statement: If two or more of these elementary screen items overlap, the EC- SCREEN-FIELD-OVERLAP exception condition is set to exist: The transfer of data to the elementary screen items is done in the order that the screen items are specified within screen-name-1_
NOTE When two screen items overlap, the display on the screen for the common character positions is determined by the second screen item specified within screen-name-l.
The transfer takes place and each elementary screen item is displayed on the terminal display subject to any editing implied in the character-string specified in the PICTURE clause of each elementary screen description entry.
14) The LINE and COLUMN phrases give the position on the terminal display screen at which the screen record associated with screen-name-1 is to start. The position is relative to the leftmost character column in the topmost line of the display that is identified as column 1 of line 1. Each subordinate elementary screen item is located relative to the start of the containing screen record. Identifier-2 and identifier-3 are evaluated once at the start of execution of the statement;
15) Ifthe LINE phrase is not specified,the screen record starts on line 1.
16) If the COLUMN phrase is not specified; the screen record starts in column 1.
17) If the execution of the DISPLAY statement is successful, the ON EXCEPTION phrase, if specified, is ignored and control is transferredto the end ofthe DISPLAY statement or, ifthe NOT ON EXCEPTION phrase is specified, to imperative-statement-2 If control is transferred to imperative-statement-2, execution continues according to the rules for each statement specified in imperative-statement-2. Ifa procedure branching or conditional statement that causes explicit transfer of control is executed, control is transferred in accordance with the rules for that statement; otherwise, upon completion of the execution of imperative-statement-2, control is transferred to the end of the DISPLAY statement:
18) If the execution of the DISPLAY statement is unsuccessful, then:
a) If the ON EXCEPTION phrase is specified in the DISPLAY statement; control is transferred to imperative-statement-1. Execution then continues according to the rules for each statement specified in imperative-statement-1. If a procedure branching or conditional statement that
612
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 643 ---

ISO /IEC 1989.2023 (E)
causes explicit transfer of control is executed, control is transferred in accordance with the rules for that statement; otherwise, upon completion of the execution of imperative-statement-1, control is transferred to the end of the DISPLAY statement and the NOT ON EXCEPTION phrase, if specified, is ignored:
b) Ifthe ON EXCEPTION phrase is not specified in the DISPLAY statement; the following occurs_
If the DISPLAY statement is specified in a statement that is in imperative-statement-1 in an exception-checking PERFORM statement and a WHEN phrase. If control is returned from the PERFORM statement; control is then transferred to the end ofthe DISPLAY statementand the NOT ON EXCEPTION phrase, if specified, is ignored.
If there is no applicable WHEN phrase and there is an applicable USE declarative, control is transferred to the declarative: If control is returned from the declarative, control is then transferred to the end of the DISPLAY statement and the NOT ON EXCEPTION phrase, if specified, is ignored.
Ifthe ON EXCEPTION phrase is not specified in the DISPLAY statement and there is no applicable exception processing procedure, control is transferred to the end of the DISPLAY statement and the NOT ON EXCEPTION phrase; if specified, is ignored:
19) If one or more of the exception conditions EC-SCREEN-FIELD-OVERLAP, EC-SCREEN- ITEM-TRUNCATED, EC-SCREEN-LINE-NUMBER, or EC-SCREEN-STARTING-COLUMN exists during the execution of the DISPLAY statement; the execution of the DISPLAY statement continues as described forthat exception condition and upon completion;the execution ofthe DISPLAY statement is considered unsuccessful.
@ISO/IEC 2023
613
[per-copy licence stamp]


--- Page 644 ---

ISO /IEC 1989.2023 (E)
14.9.12 DIVIDE statement
14.9.12.1 General
The DIVIDE statement divides one numeric data item into others and sets the values of data items equal to the quotient and remainder:
14.9.12.2 General formats
Format 1 (into):
DIVIDE identifier-1 literal-1
INTO {identifier-2 rounded-phrase ] }
ON SIZE ERROR imperative-statement-1 NOT ON SIZE ERROR imperative-statement-2 END-DIVIDE
Format 2 (into-giving):
DIVIDE
identifier-1 literal-1
INTO identifier-2 literal-2 GIVING { identifier-3 rounded-phrase ] } ON SIZE ERROR imperative-statement-1 NOT ON SIZE ERROR imperative-statement-2 END-DIVIDE
Format 3 (by-giving):
identifier-2 literal-2
identifier-1 literal-1
DIVIDE
BY
GIVING identifier-3 [ rounded-phrase ] }
ON SIZE ERROR imperative-statement-1 NOT ON SIZE ERROR imperative-statement-2
END-DIVIDE
614
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 645 ---

ISO /IEC 1989.2023 (E)
Format 4 (into-remainder):
identifier-1 DIVIDE literal-1
identifier-2 literal-2
INTO
GIVING identifier-3 rounded-phrase ] REMAINDER identifier-4
ON SIZE ERROR imperative-statement-1 NOT ON SIZE ERROR imperative-statement-2 END-DIVIDE
Format 5 (by-remainder):
identifier-2 literal-2
DIVIDE identifier-1 BY literal-1 GIVING identifier-3 rounded-phrase ] REMAINDER identifier-4
ON SIZE ERROR imperative-statement-1 NOT ON SIZE ERROR imperative-statement-2 END-DIVIDE
where rounded-phrase is described in 14.7.4, ROUNDED phrase:
14.9.12.3 Syntax rules
1) Identifier-1 and identifier-2 shall reference an elementary data item of category numeric
2) Identifier-3 and identifier-4shall reference an elementary data item of category numeric or numeric-edited.
3) Literal-1 and literal-2 shall be numeric literals_
4) When native arithmetic is in effect; the composite of operands described in 14.7.7, Arithmetic statements, is determined by using all ofthe operands in the statement excluding the data item that follows the word REMAINDER
@ISO/IEC 2023
615
[per-copy licence stamp]


--- Page 646 ---

ISO /IEC 1989.2023 (E)
14.9.12.4 General rules
ALL FORMATS
1) When native arithmetic is in effect, the quotient is the result of dividing the dividend by the divisor_ When standard-decimal arithmetic, or standard-binary arithmetic is in effect; the quotient; the quotient is the result ofthe arithmetic expression
(dividend divisor)
where the values of dividend and divisor are defined in the following general rules:
2) The process of determining the dividend and determining the divisor consists of the following:
a) The dividend is identifier-2 or literal-2. The divisor is identifier-1 or literal-1,
b) Ifan identifier is specified, item identification is done and the content ofthe resulting data item is the dividend or divisor.
C) Ifa literal is specified, the value ofthe literal is the dividend or divisor_
3) Additional rules and explanations relative to this statement are given in 14.6.13.2, Incompatible data; 14.7.4, ROUNDED phrase; 14.7.5, SIZE ERROR phrase and size error condition; and 14.7.7_ Arithmetic statements.
FORMAT 1
4) The evaluation proceeds in the following order:
a) The initial evaluation consists of determining the divisor:
b) This divisor is used with each dividend, which is each identifier-2 proceeding from left to right: Item identification for identifier-2 is done as each dividend is determined: The quotient is then formed as specified in General rule 1 and stored in the corresponding identifier-2 as indicated in 14.7.7,Arithmetic statements.
FORMATS 2 AND 3
5) The evaluation proceeds in the following order:
a) The initial evaluation is determining the divisor and determining the dividend.
b) The quotient is then formed as specified in General rule 1 and stored in each identifier-3 as specified in 14.7.7, Arithmetic statements_
FORMATS 4 AND 5
The evaluation proceeds in the following order:
616
@ISO /IEC 2023
[per-copy licence stamp]


--- Page 647 ---

ISO /IEC 1989.2023 (E)
a) The initial evaluation is determining the divisor and determining the dividend.
b) The quotient is then formed as specified in General rule 1 and stored in identifier-3 as specified in 14.7.7, Arithmetic statements.
If the size error condition is not raised, subsidiary quotient is developed that is signed and derived from the quotient by truncation of digits at the least significant end and that has the same number of digits and the same decimal point location as the data item referenced by identifier-3. The remainder is calculated as indicated in General rule and is stored in the data item referenced by identifier-4 unless storing the value would cause a size error condition, in which case the content of identifier-4 is unchanged and execution proceeds as indicated in 14.7.5, SIZE ERROR phrase and size error condition, Item identification of the data item referenced by identifier-4 is done after the quotient is stored in the data item referenced by identifier-3_
7) When native arithmetic is in effect, the remainder is the result ofmultiplying the subsidiary quotient and the divisor and subtracting the product from the dividend. When standard-decimal arithmetic, or standard-binary arithmetic is in effect; the remainder is the result ofthe arithmetic expression
(dividend (subsidiary-quotient divisor))
where the values of dividend and divisor are defined in General rule 2 and where subsidiary- quotient represents the subsidiary quotientas defined in General rule 6.
@ISO/IEC 2023
617
[per-copy licence stamp]


--- Page 648 ---

ISO /IEC 1989.2023 (E)
14.9.13 EVALUATE statement
14.9.13.1 General
The EVALUATE statement describes a multi-branch, multi-join structure: It may cause multiple conditions to be evaluated. The subsequent action ofthe runtime element depends on the results of these evaluations
14.9.13.2 General format
EVALUATE selection-subject ALSO selection-subject ] WHEN selection-object [ ALSO selection-object ] } imperative-statement-1 } WHEN OTHER imperative-statement-2 ] END-EVALUATE
where selection-subject is:
identifier-1 literal-1 arithmetic-expression-1 boolean-expression-1 condition-1 TRUE FALSE
where selection-object is:
NOT identifier-2 NOT literal-2 NOT arithmetic-expression-2 NOT boolean-expression-2 NOT range-expression condition-2 partial-expression-1 TRUE FALSE ANY
618
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 649 ---

ISO /IEC 1989.2023 (E)
where range-expression is:
identifier-3 literal-3 arithmetic-expression-3
THROUGH THRU
identifier-4 literal-4 [IN alphabet-name-1 ] arithmetic-expression-4
14.9.133 Syntax rules
1) The words THROUGH and THRU are equivalent:
2) The number of selection objects within each set of selection objects shall be equal to the number of selection subjects:
3) Alphabet-name-1 may be specified only when the literals or identifiers specified in the THROUGH phrase are of class alphabetic, alphanumeric; 0r national: If literal-3 or identifier-3 is of class national, alphabet-name-1 shall reference an alphabet that defines a national collating sequence; otherwise, alphabet-name-1 shall reference an alphabet that defines an alphanumeric collating sequence
4) The two operands in a range-expression shall be ofthe same class and shall not be of class boolean, message-tag, object; or pointer
5) A selection object is a partial-expression if the leftmost portion of the selection object is a relational operator; a class condition without the identifier; a sign condition without the identifier; 0r a sign condition without the arithmetic expression.
The classification of some selection subjects or selection objects is changed for a particular WHEN phrase as follows:
a) If the selection subject is TRUE or FALSE and the selection object is a boolean expression that results in one boolean character, the selection object is treated as boolean condition and therefore condition-2_
b) If the selection object is TRUE or FALSE and the selection subject is a boolean expression that results in one boolean character; the selection subject is treated as a boolean condition and therefore condition-1.
If the selection subject is other than TRUE or FALSE and the selection object is a boolean expression that results in one boolean character, the selection object is treated as a boolean expression and therefore boolean-expression-2_
d) If the selection object is other than TRUE or FALSE and the selection subject is a boolean expression that results in one boolean character; the selection subject is treated as a boolean expression and therefore boolean-expression-1.
e) If the selection object is a partial expression and the selection subject is a data item of the class boolean or numeric, the selection subject is treated as an identifier:
@ISO/IEC 2023
619
[per-copy licence stamp]


--- Page 650 ---

ISO /IEC 1989.2023 (E)
7) Each selection object within a set of selection objects shall correspond to the selection subjecthaving the same ordinal position within the set of selection subjects according to the following rules:
a) Identifiers, literals, or expressions appearing within a selection object shall be valid operands for comparison to the corresponding operand in the set of selection subjects in accordance with 8.8.4.2 , Simple relation conditions:
b) Condition-2 or the words TRUE or FALSE appearing as a selection object shall correspond to condition-1 or the words TRUE or FALSE in the set of selection subjects:
The word ANY may correspond to a selection subject ofany type_
d) Partial-expression-1 shall correspond to a selection subject that is an identifier, literal, an arithmetic expression, or a boolean expression. Partial-expression-1 shall be a sequence of COBOL words such that; were it preceded by the corresponding selection subject; a conditional expression would result:
8) If a selection object is specified by partial-expression-1,that selection object is treated as though it were specified as condition-2, where condition-2 is the conditional expression that results from preceding partial-expression-1 by the selection subject: The corresponding selection subject is treated as though it were specified by the word TRUE
9) Neither identifier-3 nor identifier-4 shall reference a variable-length group_
10) The permissible combinations of selection subject and selection object operands are indicated in Table 15, Combination of operands in the EVALUATE statement
Table 15 Combination of operands in the EVALUATE statement
Selection subject Arithmetic Boolean expression expression
Identifier
Literal
Condition
TRUE or FALSE
Selection object
[NOT] identifier [NOT] literal Y [NOT] Y arithmetic-expression [NOT] boolean-expression Y Y [NOT] range-expression Y Y Condition Partial-expression TRUE or FALSE ANY Y The letter 'Y' indicates a permissible combination. space indicates an invalid combination:
Y
620
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 651 ---

ISO /IEC 1989.2023 (E)
14.9.13.4 General rules
1) If an operand of the EVALUATE statement consists of a single literal, that operand is treated as a literal, not as an expression:
2) The manner of determining the range of values specified by the THROUGH phrase is described in 14.7.8, THROUGH phrase.
3) At the beginning of the execution of the EVALUATE statement, each selection subject is evaluated and assigned a value, a range of values, or a truth value as follows:
a) Any selection subject specified by identifier-1 is assigned the value and class of the data item referenced by the identifier: Ifthe selection subject is a numeric data item or a boolean data item whose length is one boolean position, the selection subject for this evaluation is treated as identifier-1 and not an arithmetic or boolean expression:
b) Any selection subject specified by literal-1 is assigned the value and class of the specified literal:
c) Any selection subject specified by arithmetic-expression-1 is assigneda numeric value according to the rules for evaluating an arithmetic expression:
d) Any selection subject in which boolean-expression-1 is specified is assigned a boolean value according to the rules for evaluating boolean expressions.
Any selection subject specified by condition-1 is assigned a truth value according to the rules for evaluating conditional expressions
0) Any selection subject specified by the words TRUE or FALSE is assigned a truth value The truth value 'true' is assigned to those items specified with the word TRUE,and the truth value 'false' is assigned to those items specified with the word FALSE
4) The execution of the EVALUATE statement proceeds by processing each WHEN phrase from left to right in the following manner:
a) Each selection object within the set of selection objects for each WHEN phrase is paired with the selection subject having the same ordinal position within the set of selection subjects. The result of the analysis of this set of selection subjects and objects is either true or false as follows:
If the selection object is the word ANY, the result is true
If the selection object is partial-expression-1,the selection subject is placed to the left of the leading relational operator and the resulting conditional expression is evaluated: The result of the evaluation is the truth value of the expression:
If the selection object is condition-2, the selection subject is either TRUE or FALSE If the truth value of the selection subject and selection object match, the result of the analysis is true If they do not match,the result is false.
@ISO/IEC 2023
621
[per-copy licence stamp]


--- Page 652 ---

ISO /IEC 1989.2023 (E)
If the selection object is either TRUE or FALSE, the selection subject is condition-1. If the truth value of the selection subject and selection object match, the result of the analysis is true: If 'they do not match, the result is false.
If the selection object is range-expression, the pair is considered to be a conditional expression of one of the following forms:
when 'NOT' is not specified in the selection object;
selection-subject >= left-part AND selection-subject <= right-part
when 'NOT" is specified in the selection object
selection-subject left-part OR selection-subject right-part
where   left-part is   identifier-3, literal-3 , or   arithmetic-expression-3 and right-part   is identifier-4,literal-4,or arithmetic-expression-4. The result ofthe analysis is the truth value of the resulting conditional expression:
If the selection object is   identifier-2, literal-2, arithmetic-expression-2, or boolean- expression-2,the pair is considered to be a conditional expression of the following form:
selection-subject [NOT] selection-object
where 'NOT' is present if it is present in the selection object: The result of the analysis is the truth value of the resulting conditional expression.
b) If the result of the analysis is true for every pair in a WHEN phrase, that WHEN phrase satisfies the set of selection subjects and no more WHEN phrases are analyzed:
c) If the result of the analysis is false for any pair in a WHEN phrase, no more pairs in that WHEN phrase are evaluated and the WHEN phrase does not match the set of selection subjects:
d) This procedure is repeated for subsequent WHEN phrases, in the order of their appearance in the source element, until either a WHEN phrase satisfying the set ofselection subjects is selected or until all sets of selection objects are exhausted_
5) The execution of the EVALUATE statement then proceeds as follows:
a) If WHEN phrase is  selected, execution continues with the first  imperative-statement-1 following the selected WHEN phrase:
b) Ifno WHEN phrase is selected and a WHEN OTHER phrase is specified, execution continues with imperative-statement-2
c) The execution of the EVALUATE statement is terminated when execution reaches the end of imperative-statement-1 of the selected WHEN phrase or the end of imperative-statement-2, Or when no WHEN phrase is selected and no WHEN OTHER phrase is specified.
622
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 653 ---

ISO /IEC 1989.2023 (E)
14.9.14 EXIT statement
14.9.14.1 General
The EXIT statement provides a common end point for a series of procedures_
The EXIT PROGRAM statement marks the logical end ofa called program:
The EXIT PERFORM statement provides a means of exiting an inline PERFORM (with or without returning to any specified test):
The EXIT PERFORM statement also provides a means of exiting an exception-checking PERFORM statement:
The EXIT PARAGRAPH and EXIT SECTION statements provide a means of exiting a structured procedure without executing any of the following statements within the procedure:
14.9.14.2 General formats
Format 1 (simple):
EXIT
Format 2 (program)
EXCEPTION exception-name-1 identifier-1 LAST EXCEPTION
EXIT PROGRAM
RAISING
NOTE The Program format of the EXIT statement is an archaic feature: For details see F.1,Archaic language elements.
Format 3 (inline-perform)-
EXIT PERFORM CYCLE ]
Format 4 (procedure):
PARAGRAPH SECTION
EXIT
@ISO/IEC 2023
623
[per-copy licence stamp]


--- Page 654 ---

ISO /IEC 1989.2023 (E)
14.9.14.3 Syntax rules
FORMAT 1
1) The EXIT statement shall appear in a sentence by itself that shall be the only sentence in the paragraph or in a section without paragraphs.
FORMAT 2
2) The EXIT statement shall not be specified in a declarative procedure for which the GLOBAL phrase is specified in the associated USE statement:
3) Exception-name-1 shall be level-3 exception-name as specified in the rules for exception conditions specified in 14.6.13.1, Exception conditions
If exception-name-1 is a level-3 exception-name for EC-USER, exception-name-1 shall be specified in the RAISING phrase of the procedure division header of the source element in which this EXIT statement is contained.
4) Identifier-1 is a sending operand:
5) Identifier-1 shall be an object reference, subject to the following constraints:
a) If the data description entry of identifier-1 specifies an object-class-name,the class identified by that object-class-name or one ofthe superclasses of that class is specified in the RAISING phrase of the procedure division header of the source element containing this EXIT statement and the presence 0r absence of the FACTORY phrase shall be the same in the data description entry of identifier-1 as in the RAISING phrase of the procedure division header of the containing source element;
b) If the data description entry of identifier-1 specifies an interface-name, the interface referenced by that interface-name shall conform to an interface specified in the RAISING phrase of the procedure division header of the source element that contains this EXIT statement; and the presence or  absence of the FACTORY phrase is the same in the data description entry of identifier-1 as in the RAISING phrase of the procedure division header of the containing source element:
c) If the data description entry of identifier-1 specifies an ACTIVE-CLASS phrase, the class of the object containing the EXIT statement; or one ofthe super classes ofthat object, and the presence or absence of the FACTORY phrase shall be the same as that specified in the RAISING phrase of the procedure division header ofthe source element containing this EXIT statement:
d) Identifier-1 shall not be a universal object reference:
The LAST phrase may be specified only in a declarative procedure or a WHEN phrase in a PERFORM statement:
7) An EXIT PROGRAM statement may be specified only in a program procedure division.
624
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 655 ---

ISO /IEC 1989.2023 (E)
FORMAT 3
8) The EXIT PERFORM statement may be specified only in an inline or exception-checking PERFORM statement: The CYCLE phrase shall not be specified within an exception-checking  PERFORM statement
FORMAT 4
9) The EXIT statement with the SECTION phrase may be specified only in a section:
10) The EXIT statement with the PARAGRAPH phrase may be specified only in a paragraph:
14.9.14.4 General rules
FORMAT 1
1) An EXIT statement serves only to enable the user to assign a procedure-name to a given point in a procedure division. Such an EXIT statement has no other effect on the compilation Or execution.
FORMAT 2
2) If the EXIT PROGRAM statement is executed in a program that is not under the control of a calling runtime element; the EXIT PROGRAM statement is treated as if it were a CONTINUE statement: No exception condition is raised even if the RAISING phrase is specified,
3) Ifan EXIT PROGRAM statementis executed in a program thatis under the control ofa calling runtime element; execution proceeds as specified in 14.9.18,GOBACK statement; General rules 3 and 4.
FORMAT 3
4) If an EXIT PERFORM statement is specified in an exception-checking PERFORM statement; control passes to an implicit CONTINUE statement immediately preceding the END-PERFORM phrase specified for that PERFORM statement Or, ifthe FINALLY phrase is specified, immediately preceding the FINALLY phrase.
5) If an EXIT PERFORM statement is not specified in an exception-checking PERFORM statement; control passes as indicated in the following rules:
a) The execution of an EXIT PERFORM statement without the CYCLE phrase causes control to be passed to an implicit CONTINUE statement immediately following the END-PERFORM phrase that matches the most closely preceding, and as yet unterminated, inline PERFORM statement_
b) The execution of an EXIT PERFORM statement with the CYCLE phrase causes control to be passed to an implicit CONTINUE statement immediately preceding the END-PERFORM phrase that matches the most closely preceding, and as yet unterminated, inline PERFORM statement:
@ISO/IEC 2023
625
[per-copy licence stamp]


--- Page 656 ---

ISO /IEC 1989.2023 (E)
FORMAT 4
The execution of an EXIT PARAGRAPH statement causes control to be passed to an implicit CONTINUE statement immediately following the last explicit statement of the current paragraph, preceding any return mechanisms for that paragraph:
NOTE The return mechanisms mentioned in the rules for EXIT PARAGRAPH and EXIT SECTION are those associated with language elements such as PERFORM, SORT, and USE
7) The execution of an EXIT SECTION statement causes control to be passed to an unnamed empty paragraph immediately following the last paragraph of the current section; preceding any return mechanisms for that section;
626
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 657 ---

ISO /IEC 1989.2023 (E)
14.9.15 FREE statement
14.9.15.1 General
The FREE statement releases dynamic storage previously obtained with an ALLOCATE statement
14.9.15.2 General format
FREE data-name-1 }
14.9.15. Syntax rule
1) The data item referenced by data-name-1 shall be of category data-pointer.
14.9.15.4 General rules
1) The FREE statement is processed as follows:
a) If the data-pointer referenced by data-name-1 identifies the start of storage that is currently allocated by an ALLOCATE statement; that storage is released and the data-pointer referenced by data-name-1 is set to NULL, the length of the released storage is the length of the storage obtained by the ALLOCATE statement; and the contents of any data items located within the released storage area become undefined;
b) otherwise, ifthe data-pointer referenced by data-name-1 contains the predefined address NULL no action is taken for that operand;
c) otherwise, the EC-STORAGE-NOT-ALLOC exception condition is set to exist;
2) If more than one data-name-1 is specified in a FREE statement; the result of executing this FREE statement is the same as ifa separate FREE statement had been written for each data-name-l in the same order as specified in the FREE statement: Ifan implicit FREE statement results in an exception condition being raised and the exception condition is nonfatal, after any applicable exception processing statements are executed, processing resumes at the next implicit FREE statement; if = any- If the exception condition is fatal and the applicable exception processing statements do not result in abnormal run unit termination, processing resumes at the next implicit FREE statement; if applicable. If there is no next implicit FREE statement; processing resumes at the next executable statement as if no exception condition was raised during the execution ofthe FREE statement:
@ISO/IEC 2023
627
[per-copy licence stamp]


--- Page 658 ---

ISO /IEC 1989.2023 (E)
14.9.16 GENERATE statement
14.9.16.1 General
The GENERATE statement performs control break processing and, unless a report-name is specified, prints one instance ofthe specified detail.
14.9.16.2 General format
data-name-1 report-name-1
GENERATE
14.9.16.3 Syntax rules
1) Data-name-1 shall name a detail report group It may be qualified by a report-name
2)  Report-name-1 may be used only if the referenced report description entry contains a CONTROL clause:
3) If data-name-1 is   defined in containing program, the   report  description entry in which data-name-1 is specified and the file description entry associated with that report description entry shall contain a GLOBAL clause.
4) If report-name-1 is defined in containing program, the file description entry associated with report-name-1 shall contain a GLOBAL clause:
14.9.16.4 General rules
1) Execution of a GENERATE data-name statement causes one instance of the specified detail to be printed, following any necessary control break and page fit processing, as detailed below:
2) Execution of GENERATE report-name-l statement results in the same processing as for GENERATE data-name statement for the same report; except that no detail is printed. This process is called summary reporting: If all the GENERATE statements executed for a report between the execution of the INITIATE and TERMINATE statements are of this form, the report that is produced is called a summary report:
3) If a CONTROL clause is specified in the report description entry, each execution of the GENERATE statement causes the specified control data items to be saved and subsequently compared S0 as to sense for control breaks  This processing is described by the CONTROL clause_
4) Execution of the chronologically first GENERATE statement following an INITIATE causes the following actions to take place in order:
a) The report heading is printed if defined: If the report heading appears on page by itself; an advance is made to the next physical page, and PAGE-COUNTER is either incremented by 1 or,if the report heading's NEXT GROUP clause has the WITH RESET phrase, set to 1. (See 13.18.57, TYPE clause; and 13.18.37, NEXT GROUP clause:)
628
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 659 ---

ISO /IEC 1989.2023 (E)
b) A page heading is printed if defined: (See 13.18.57, TYPE clause:)
Ifa CONTROL clause is defined for the report; each control heading is printed, wherever defined, in order from major to minor: (See 13.18.16,CONTROL clause; and 13.18.57, TYPE clause:)
d) The specified detail is printed, unless summary reporting is specified.
5) Execution of any chronologically second and subsequent GENERATE statements following an INITIATE causes the following actions to take place in order:
a) Ifa CONTROL clause is defined for the report;and a control break has been detected,each control footing and control heading is printed, if defined, up to the level of the control break (See 13.18.16,CONTROL clause; and 13.18.57, TYPE clause.)
b) The specified detail is printed, unless summary reporting is specified.
Ifthe associated report is divided into pages,the chronologically first body group since the INITIATE is preceded by a page heading, if defined, and the printing of any subsequent body groups is preceded by a page fit test that; if unsuccessful, results in a page advance, consisting of the following actions in this order:
a) Ifa page footing is defined it is printed:
b) An advance is made to the next physical page:
c) Ifthe associated report description entry contains a CODE clause with an identifier operand, the identifier is evaluated.
d) If the page advance was preceded by the printing of a group whose description has a NEXT GROUP clause with the NEXT PAGE and WITH RESET phrases, PAGE-COUNTER is set to 1; otherwise PAGE-COUNTER is incremented by 1.
LINE-COUNTER is set to zero.
f) If a page heading is defined it is printed.
7) The report associated with data-name-1 or report-name-1 shall be in the active state. If it is not; the EC-REPORT-INACTIVE exception condition is set to exist; if it is enabled.
8) Ifa nonfatal exception condition is raised during the execution ofa GENERATE statement; execution resumes at the next report item, line, or report group, whichever follows in logical order
@ISO/IEC 2023
629
[per-copy licence stamp]


--- Page 660 ---

ISO /IEC 1989.2023 (E)
14.9.17 GO TO statement
14.9.17.1 General
The GO TO statement causes control to be transferred from one part of the procedure division to another:
NOTE The use ofa GO TO statement to exit a PERFORM statement range can leave a perform exit unsatisfied and cause unexpected results If possible,its use should be avoided because it leads to difficulties in following program flow and unexpected results in many cases:
14.9.17.2 General formats
Format 1 (unconditional):
GO TO procedure-name-1
Format 2 (depending):
GO TO procedure-name-1
DEPENDING ON identifier-1
14.9.17.3 Syntax rules
1) Identifier-1 shall reference a numeric elementary data item that is an integer:
2) If a GO TO statement represented by format appears in a consecutive sequence of imperative statements within a sentence, it shall appear as the last statement in that sequence
3) A GO TO statement shall not be specified in a WHEN phrase of an exception-checking PERFORM statement_
14.9.17.4 General rules
1) When GO TO statement  represented by format procedure-name-1_
is executed, control is   transferred to
2) When GO TO statement  represented by format 2 is executed, control is transferred to procedure-name-1, etc , depending on the value of identifier-1 being 1, 2, If the content of identifier-1 is not numeric; the EC-DATA-INCOMPATIBLE exception condition is set to exist: If checking for EC-DATA-INCOMPATIBLE is enabled, processing occurs as specified for that exception condition: If checking for EC-DATA-INCOMPATIBLE is not enabled or the value of identifier-1 is anything other than the positive or unsigned integers 1,2, n,then no transfer occurs and control passes to the next statement in the normal sequence for execution:
630
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 661 ---

ISO /IEC 1989.2023 (E)
14.9.18 GOBACK statement
14.9.18.1 General
The GOBACK statement marks the logical end ofa function, a method, or a program
14.9.18.2 General format
GOBACK raising- phrase status-phrase
Where raising-phrase is:
EXCEPTION exception-name-1 RAISING identifier-1 LAST EXCEPTION
Where status-phrase is:
ERROR NORMAL
STATUS identifier-2 literal-1
WITH
14.9.18.3 Syntax rules
1) The GOBACK statement shall not be specified in a declarative procedure for which the GLOBAL phrase is specified in the associated USE statement:
2) Exception-name-1 shall be a level-3 exception-name as specified in 14.6.13.1, Exception conditions_
If exception-name-1 is a level-3 exception-name for EC-USER,exception-name-1 shall be specified in the RAISING phrase of the procedure division header of the source element in which this GOBACK statement is contained.
3) Identifier-1 is a sending operand.
4) Identifier-1 shall be an object reference, subject to the following constraints:
a) If the data description entry of identifier-1 specifies an object-class-name,the class identified by that object-class-name or one of the superclasses of that class shall be specified in the RAISING phrase of the procedure division header of the source element   containing this GOBACK statement and the presence or absence of the FACTORY phrase is the same in the data description entry of identifier-1 as in the RAISING phrase ofthe procedure division header of the containing source element:
@ISO/IEC 2023
631
[per-copy licence stamp]


--- Page 662 ---

ISO /IEC 1989.2023 (E)
b) If the data description entry of identifier-1 specifies an interface-name, the interface referenced by that interface-name shall conform to an interface specified in the RAISING phrase of the procedure division header of the source element containing this GOBACK statement; and the presence or absence of the FACTORY phrase shall be the same in the data description entry of identifier-1 as in the RAISING phrase of the procedure division header of the containing source element
c) If the data description entry of identifier-1 specifies an ACTIVE-CLASS phrase, the class of the object containing the GOBACK statement; or one of the super classes of that object; and the presence or absence of the FACTORY phrase shall be the same as that specified in the RAISING phrase of the procedure division header of the source element  containing this GOBACK statement;
d) Identifier-1 shall not be a universal object reference:
5) The LAST phrase may be specified only in a declarative procedure or WHEN phrase of a PERFORM statement
6) Identifier-2 shall reference an integer data item or a data item with usage display or usage national
7) If literal-1 is numeric, it shall be an integer:
8) Literal-1 shall not be a zero-length literal.
14.9.18.4 General rules
1) If a GOBACK statement is executed in program that is under the control of a calling runtime element; execution proceeds as follows:
a) If the activating runtime element is a non-COBOL element; execution continues in the activating element in an implementor-defined fashion 
b) If the RAISING phrase is specified, an exception condition is raised in the activating runtime element ifchecking for that exception condition is enabled in the activating runtime element,and execution continues in that runtime element as specified in the rules for the activating statement after the result; if any, of the activated element is returned to the activating element: The exception condition that exists is determined as follows:
If exception-name-1 is specified,it is that exception condition:
If  identifier-1 is  specified, the object referenced by  identifier-1 becomes the current exception object   in the activating runtime element. Additional rules are specified in 14.6.13.1.5,Exception objects:
If the LAST phrase is specified one of the following occurs:
Ifan exception condition is currently raised,that exception condition is set to exist in the activating runtime element: If the exception condition is an exception object, additional rules are specified in 14.6.13.1.5, Exception objects Ifthe exception condition is a level-
632
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 663 ---

ISO /IEC 1989.2023 (E)
exception for EC-USER and that exception condition is not specified in the RAISING phrase of the procedure division header of the source element in which this EXIT statement is contained, the EC-RAISING-NOT-SPECIFIED exception condition is set to exist in the activating runtime element instead of the EC-USER exception condition.
If no exception condition is raised, the RAISING phrase is ignored and no exception condition is set to exist in the activating runtime element:
2) The execution of an GOBACK statement causes the executing program to terminate, and control to return to the calling statement: Ifa RETURNING phrase is specified in the procedure division header of the program containing the GOBACK statement; the value in the data item referenced by that RETURNING phrase becomes the result of the program activation_
Execution continues in the calling element as specified in the rules for the CALL statement: The state of the calling runtime element is not altered and is identical to that which existed at the time it executed the CALL statement except that the contents of data items and the contents of files shared between the calling runtime elementand the called program may have been changed. Ifthe program in which the GOBACK statement is specified is an initial program, an implicit CANCEL statement referencing that program is executed upon return to the calling runtime element: That implicit CANCEL statement will not raise any exception conditions in the calling runtime element: If the RAISING phrase is specified in the GOBACK statement; the exception condition thatis specified in the RAISING phrase exists after the execution of that implicit CANCEL statement
3) Ifa GOBACK statement is executed in a program that is not under the control of a calling runtime element; the program operates as if executing a STOP statement; with a status phrase, if any: A RAISING phrase, if specified, is ignored:
4) The execution of a GOBACK statement within a method causes the executing method t0 terminate, and control to return to the invoking statement: If a RETURNING phrase is present in the method definition   containing the GOBACK statement; the value in the data item referenced   by the RETURNING phrase becomes the result of the method invocation:
5) The execution ofa GOBACK statement within a function causes the executing function to terminate, and control to return to the activating statement: The value in the data item referenced by the RETURNING phrase in the function definition containing the GOBACK statement becomes the result of the function activation.
Ifa GOBACK statementis executed within the range ofa declarative procedure whose USE statement contains the GLOBAL phrase and that USE statementis specified in the same program as the GOBACK statement; the EC-FLOW-GLOBAL-GOBACK exception condition is set to exist:
7) If the GOBACK is executing in a main program and the ERROR phrase is specified, the operating system will indicate an error termination of the run unit if such capability exists within the operating system.
8) If the GOBACK statement is executing in a main program and the NORMAL phrase is specified, the operating system will indicate a normal termination of the run unit if such a capability exists within the operating system:
@ISO/IEC 2023
633
[per-copy licence stamp]


--- Page 664 ---

ISO /IEC 1989.2023 (E)
9) If the GOBACK statement is executing in a main program and neither the ERROR phrase nor the NORMAL phrase is specified, the operating system will indicate a normal termination of the run unit if such a capability exists within the operating system unless error termination has been indicated by an implementor-defined mechanism:
10) During execution ofthe GOBACK statement in a main program with literal-1 or identifier-2 specified, literal-1 or the contents of the data item referenced by identifier-2 are passed to the operating system: Any constraints on the value of literal-1 or the contents of the data item referenced by identifier-2 are defined by the implementor
634
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 665 ---

ISO /IEC 1989.2023 (E)
14.9.19 IF statement
14.9.19.1 General
The IF statement causes a condition to be evaluated: The subsequent action of the runtime element depends on whether the value of the condition is true or false:
14.9.19.2 General formats
Format 1 (delimited):
IF condition-1 THEN statement-1 ELSE statement-2 END-IF
Format 2 (historic}:
statement-1 NEXT SENTENCE
statement-2 NEXT SENTENCE
IF condition-1 THEN
ELSE
NOTE NEXT SENTENCE is an archaic feature. For details see F.1, Archaic language elements_
14.9.193 Syntax rules
ALL FORMATS
1) Statement-1 and statement-2 represent either one or more imperative statements 0r a conditional statement optionally preceded by one or more imperative statements. A further description of the rules governing statement-1 and statement-2 is given in 14.5.3, Scope of statements_
2) Statement-1 and statement-2 may each contain an IF statement: In this case, the IF statement is said to be nested. More detailed rules on nesting are given in 14.5.3, Scope of statements
Nested IF statements may contain an ELSE phrase, and may also be terminated using an END-IF terminator: Proceeding from left to right; whether an ELSE or END-IF matches preceding IF statement is determined as follows:
a) any ELSE encountered is matched with the nearest preceding IF that either has not been already matched with an ELSE or has not been implicitly or explicitly terminated,
b) any END-IF encountered is matched with the nearest preceding IF that has not been implicitly or explicitly terminated:
NOTE A nested IF statement is terminated by terminal separator period ofthe containing IF statement
@ISO/IEC 2023
635
[per-copy licence stamp]


--- Page 666 ---

ISO /IEC 1989.2023 (E)
FORMAT 2
3) The ELSE NEXT SENTENCE phrase may be omitted ifit immediately precedes the terminal separator period of the sentence
14.9.19.4 General rules
FORMAT 1
1) If condition-1 is true, control is transferred to the first statement of statement-1 and execution continues according to the rules for each statement specified in statement-1. The ELSE phrase, if specified, is ignored.
2) If condition-1 is false, the THEN phrase is ignored: If the ELSE phrase is specified, control is transferred to the first statement of statement-2 and execution continues according to the rules for each statement specified in statement-2.
FORMAT 2
3) If condition-1 is true and statement-1 is specified, control is transferred to the first statement of statement-1and  execution continues according to the rules  for each statement specified in statement-1. The ELSE phrase, if specified, is ignored
4) If condition-1 is true and NEXT SENTENCE is specified in the THEN phrase, the ELSE phrase, if specified, is ignored and control is transferred to an implicit CONTINUE statement immediately preceding the next separator period.
5) If condition-1 is falseand statement-2 is specified,the THEN phrase is ignored, control is transferred to the first statement of statement-2, and execution continues according to the rules for each statement specified in statement-2.
If condition-1 is false and NEXT SENTENCE is specified in the ELSE phrase, the THEN phrase is ignored and control is transferred to an implicit CONTINUE statement immediately preceding the next separator period.
7) If condition-1 is false and the ELSE phrase is not specified, the THEN phrase is ignored:
636
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 667 ---

ISO /IEC 1989.2023 (E)
14.9.20 INITIALIZE statement
14.9.20.1 General
The INITIALIZE statement provides the ability to set selected data items to specified values.
14.9.20.2 General format
INITIALIZE {identifier-1 }
[WITH FILLER ]
ALL
TO VALUE category-name
THEN REPLACING
identifier-2 category-name DATA BY literal-1
THEN TO DEFAULT ]
where category-name is:
ALPHABETIC ALPHANUMERIC ALPHANUMERIC-EDITED BOOLEAN DATA-POINTER FUNCTION-POINTER MESSAGE-TAG NATIONAL NATIONAL-EDITED NUMERIC NUMERIC-EDITED OBJECT-REFERENCE PROGRAM-POINTER
14.9.20.3 Syntax rules
1) Identifier-1 shall be strongly typed or of class alphabetic, alphanumeric, boolean, message-tag national, numeric; object; Or pointer:
@ISO/IEC 2023
637
[per-copy licence stamp]


--- Page 668 ---

ISO /IEC 1989.2023 (E)
2) Identifier-1 shall be specified only once if the INITIALIZE statement is specified in imperative- statement-1 of an exception-checking PERFORM statement:
3) For each DATA-POINTER, FUNCTION-POINTER, MESSAGE-TAG, OBJECT-REFERENCE, or PROGRAM-POINTER phrase specified as the category-name in the REPLACING phrase, identifier-2 shall be specified.
4) For each of the categories data-pointer, function-pointer, message-tag   object-reference, and program-pointer specified in the REPLACING phrase, a SET statement with identifier-2 as the sending operand and an item of the specified category as the receiving operand shall be valid.
For each of the other categories specified in the REPLACING phrase, MOVE statement with identifier-2 or literal-1 as the sending item and an item of the specified category as the receiving operand shall be valid.
5) The data description entry for the data item referenced by identifier-1 shall not contain a RENAMES clause
6) The same category shall not be repeated in a REPLACING phrase
7) The data item referenced by identifier-1 is the receiving operand.
8) If the REPLACING phrase is specified, literal-1 or the data item referenced by identifier-2 is the sending operand: If the REPLACING phrase is not specified, the sending operand is determined according to the following general rules:
14.9.20.4 General rules
1) When identifier-1 references a bit group item ora national group item, identifier-1 is processed as a group item. When identifier-2 references a bit group item 0r a national group item, identifier-2 is processed as an elementary data item_
2) The keywords in category-name correspond to a category of data as specified in 8.5.2, Class and category of data items and literals_ If ALL is specified in the VALUE phrase it is as if all of the categories listed in category-name were specified.
3) If more than one identifier-1 is specified in an INITIALIZE statement; the result of executing this INITIALIZE statement is the same as if a separate INITIALIZE statement had been written for each identifier-1 in the same order as specified in the INITIALIZE statement: If an implicit INITIALIZE statement results in the execution of a declarative procedure that executes a RESUME statement with the NEXT STATEMENT phrase, processing resumes at the next implicit INITIALIZE statement; if any:
4) Whether identifier-1 references an elementaryitem or a group item, the effect ofthe execution ofthe INITIALIZE statement is as though a series of implicit MOVE or SET statements, each of which has an elementary data item as its receiving operand, were executed_ The sending-operands of these implicit statements are defined in General rule 6 and the receiving-operands are defined in General rule 5_
638
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 669 ---

ISO /IEC 1989.2023 (E)
If the category of receiving-operand is data-pointer, function-pointer, message-tag,  object- reference, or program-pointer; the implicit statement is:
SET receiving-operand TO sending-operand
Otherwise, the implicit statement is:
MOVE sending-operand TO receiving-operand:
5) The receiving-operand in each implicit MOVE or SET statement is determined by applying the following steps in order:
a) First; the following data items are excluded as receiving-operands:
Any identifiers thatare not valid receiving operands ofa MOVE statement; except data items of category data-message-tag pointer, object-reference, or program-pointer.
If the FILLER phrase is not specified, elementary data items with an explicit or implicit FILLER clause.
Any elementary data item subordinate to identifier-1 whose data description entry contains a REDEFINES or RENAMES clause or is subordinate t0 a data item whose data description entry contains a REDEFINES clause:   However; identifier-1 may itself have a REDEFINES clause or be subordinate to a data item with a REDEFINES clause.
b) Second, an elementary data item is a possible receiving item if:
It is explicitly referenced by identifier-1; or
It is contained within the group data item referenced by identifier-1. If the elementary data item is table  element; each occurrence of the elementary data item is possible receiving-operand:
Finally, each possible receiving-operand is a receiving-operand ifat least one of the following is true:
The VALUE phrase is specified, the category of the elementary data item is one of the categories specified or implied in the VALUE phrase,and one ofthe following is true:
Either the category of the elementary data item is data-pointer; message-tag object- reference, or program-pointer, or
b. data-item format VALUE clause is specified in the data description entry of the elementary data item:
A table format VALUE clause is specified in the data description entry of the elementary item and that VALUE clause specifies a value for the particular occurrence of the elementary data item
@ISO/IEC 2023
639
[per-copy licence stamp]


--- Page 670 ---

ISO /IEC 1989.2023 (E)
The REPLACING phrase is specified and the category of the elementary data item is one of the categories specified in the REPLACING phrase; or
The DEFAULT phrase is specified; 0r
Neither the REPLACING phrase nor the VALUE phrase is specified.
The sending-operand in each implicit MOVE and SET statement is determined as follows:
a) If the data item qualifies as a receiving-operand because of the VALUE phrase:
If the category of the receiving-operand is data-pointer, function-pointer, message-tag, or program-pointer; the sending-operand is the predefined address NULL;
If the category of the receiving-operand is object-reference, the sending-operand is the predefined object reference NULL;
Otherwise, the sending-operand is determined by the literal in the VALUE clause specified in the data description entry of the data item: If the data item is a table element; the literal in the VALUE clause that corresponds to the occurrence being initialized determines the sending-operand, The actual  sending-operand is literal that; when moved to the receiving-operand with a MOVE statement; produces the same result as the initial value of the data item as produced by the application of the VALUE clause_
b) If the data item does not qualify as a receiving-operand because of the VALUE phrase, but does qualify because of the REPLACING phrase, the sending-operand is the literal-1 or identifier-2 associated with the category specified in the REPLACING phrase:
c) Ifthe data item doesnot qualify in accordance with General rules 6a and 6b,the sending-operand used depends on the category of the receiving-operand as follows:
Receiving operand
Figurative constant
Alphabetic Alphanumeric Alphanumeric-edited Boolean Data-pointer Function-pointer Message-tag National National-edited Numeric Numeric-edited Object-reference Program-pointer
Figurative constant alphanumeric SPACES Figurative constant alphanumeric SPACES Figurative constant alphanumeric SPACES Figurative constant ZEROES Predefined address NULL Predefined address NULL Predefined content NULL Figurative constant national SPACES Figurative constant national SPACES Figurative constant ZEROES Figurative constant ZEROES Predefined object reference NULL Predefined address NULL
7) When a dynamic-length elementary item is initialized, its length is set to zero.
640
OISO /IEC 2023
[per-copy licence stamp]


--- Page 671 ---

ISO /IEC 1989.2023 (E)
8) If identifier-1 references a group data item, affected elementary data items are initialized in the sequence of their definition within the group data item: For a variable-occurrence data item; the number of occurrences initialized is determined by therules ofthe OCCURS clause fora receiving data item:
9) If identifier-1 occupies the same storage area as identifier-2, the result of the execution of this statement is undefined, even if they are defined by the same data description entry. (See 14.6.10, Overlapping operands:)
10) When a group containing a dynamic-capacity table is initialized, all the elements of the table up to current capacity, if any, are initialized, whether 0r not the INITIALIZED phrase is present in the OCCURS clause, and the current capacity ofthe table is left unchanged.
@ISO/IEC 2023
641
[per-copy licence stamp]


--- Page 672 ---

ISO /IEC 1989.2023 (E)
14.9.21 INITIATE statement
14.9.21.1 General
The INITIATE statement initializes any internal storage locations used by the specified reports before any printing begins for these reports_
14.9.21.2 General format
INITIATE {report-name-1 }
14.9.21.3 Syntax rules
1) Report-name-1 shall be defined by a report description entry in the report section
2) If report-name-1 is defined in containing program, the file description entry associated with report-name-1 shall contain a GLOBAL clause
3) An INITIATE statement that specifies more than one report-name-1 shall not be specified in an exception checking PERFORM statement:
14.9.21.4 General rules
1) The INITIATE statement performs the following initialization functions for each specified report:
a) All sum counters and all size error indicators are set to zero.
b) LINE-COUNTER is set to zero.
C) PAGE-COUNTER is set to 1.
2) An INITIATE statement shall not be executed if report-name-l is in the active state If it is in the active state, the EC-REPORT-ACTIVE exception condition is set to exist and the execution of the INITIATE statement has no other effect:
3) The INITIATE statement does not open any file connector with which report-name-1 is associated. Therefore, the INITIATE statement may be executed only ifthe corresponding file connector is open in the extend mode or the output mode: If the file connector is not open in the output or extend mode; the EC-REPORT-FILE-MODE exception condition is set to exist and no action is taken on the report:
4)
successful INITIATE statement places the report in the active state
5) The results of executing an INITIATE statement in which more than one report-name-1 is specified is the same as if a separate INITIATE statement had been executed for each report-name-1 in the same order as specified in the statement: Ifan implicit INITIATE statement results in the execution of a declarative procedure that executes a RESUME statement with the NEXT STATEMENT phrase, processing resumes at the next implicit INITIATE statement; ifany.
642
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 673 ---

ISO /IEC 1989.2023 (E)
14.9.22 INSPECT statement
14.9.22.1 General
The INSPECT statement provides the ability to tally or replace occurrences of single characters 0r sequences of characters in a data item:
14.9.22.2 General formats
Format 1 (tallying):
INSPECT BACKWARD ] identifier-1 TALLYING tallying-phrase
Format 2 (replacing):
INSPECT BACKWARD ] identifier-1 REPLACING replacing-phrase
Format 3 (tallying-and-replacing):
INSPECT BACKWARD ] identifier-1 TALLYING tallying-phrase REPLACING replacing-phrase
Format 4 (converting):
INSPECT BACKWARD identifier-1 CONVERTING
identifier-6 literal-4
identifier-7 TQ literal-5
[after-before-phrase
where tallying-phrase is:
CHARACTERS [ after-before-phrase
ALL
identifier-3 literal-1
after-before-phrase
identifier-2 FOR
LEADING
identifier-3 literal-1
[after-before-phrase ]
@ISO/IEC 2023
643
[per-copy licence stamp]


--- Page 674 ---

ISO /IEC 1989.2023 (E)
where after-before-phrase is:
identifier-4 literal-2
AFTER INITIAL
identifier-4 BEFORE INITIAL literal-2
where replacing-phrase is:
CHARACTERS BY replacement-item after-before-phrase ]
ALL
identifier-3 literal-1
BY replacement-item after-before-phrase ]
LEADING
identifier-3 BY replacement-item [after-before-phrase ] literal-1
FIRST
identifier-3 literal-1
BY replacement-item [after-before-phrase ]
where replacement-item is:
identifier-5 literal-3
14.9.22.3 Syntax rules
ALL FORMATS
1) Identifier-1 shall reference either an alphanumeric or national group item 0r an elementary item described implicitly or explicitly as usage display or national:
2) Identifier-3, identifier-n shall reference an elementary item described implicitly or explicitly as usage display or national.
644
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 675 ---

ISO /IEC 1989.2023 (E)
3) Each literal shall be an alphanumeric, boolean, or national literal: Literal-1, literal-2, literal-3, and literal-4 shall not be a figurative constant that begins with the word ALL. If literal-1, literal-2, or literal-4 is a figurative constant; it refers to an implicit one character data item. When identifier-1 is of class national, the class of the figurative constant is national; when identifier-1 is of class boolean, the figurative constant is of class boolean and only the figurative constant ZERO may be specified; otherwise, the class of the figurative constant is alphanumeric Literal-1,literal-2, literal-3, literal-4 or literal-5 shall not be a zero-length literal:
4) If   any of   identifier-1, identifier-3, identifier-4, identifier-5, identifier-6, identifier-7, literal-1, literal-2, literal-3,literal-4,or literal-5 references an elementary data item or literal of class boolean or national,then all shall reference a data item or literal of class boolean or national, respectively:
FORMATS 1 AND 3
5) Identifier-2 shall reference an elementary numeric data item
FORMATS 2 AND 3
6) When both literal-1 and literal-3 are specified, they shall be the same size except when literal-3 is a figurative constant; in which case it is expanded or contracted to be the size of literal-1.
7) When the CHARACTERS phrase is specified, literal-3 shall be one character in length
FORMAT 1
8) Identifier-1 is a sending operand.
FORMAT 4
9) When both literal-4 and literal-5 are specified they shall be the same size except when literal-5 is a figurative constant:
14.9.22.4 General rules
ALL FORMATS
1) For purposes of determining its length, identifier-1 is treated as a sending data item.
2) If the data item referenced by identifier-1 is a zero-length item:
a) the contents of the data items referenced by identifier-1 and identifier-2 are unchanged
b) the execution ofthe INSPECT statement is successful
c) control is immediately transferred to the end of the INSPECT statement
3) Inspection (which includes the comparison cycle, the establishment of boundaries for the BEFORE or AFTER phrase,and the mechanism for tallying and/or replacing) begins at the leftmost character position of the data item referenced by identifier-1, regardless ofits class,and proceeds from left to
@ISO/IEC 2023
645
[per-copy licence stamp]


--- Page 676 ---

ISO /IEC 1989.2023 (E)
right to the rightmost character position as described in General rules 6 through 8, unless BACKWARD is specified, in which case, inspection (which includes the comparison cycle, the establishment of boundaries for the BEFORE or AFTER phrase, and the mechanism for tallying and/ or replacing) begins at the rightmost character position of the data item referenced by identifier-1 regardless ofits class,and proceeds from right to left to the leftmost character position as described in General rules 6 through 8_
NOTE 1 When BACKWARD is specified the BEFORE and AFTER phrases are evaluated in the direction of the scan_ INSPECT BACKWARD A12C21D12EF" TALLYING data-name-1 CHARACTERS BEFORE "12" would return 2 in data-name-1,not 5_
4) For  use in the INSPECT statement; the content of the data item referenced by identifier-1, identifier-3,identifier-4,identifier-5,identifier-6,or identifier-7 shall be treated as follows:
a) Ifany of identifier-1,identifier-3,identifier-4,identifier-5,identifier-6,0r identifier-7 references an alphabetic, alphanumeric, boolean, or national data item; the INSPECT statement shall treat the content of each such identifier as a character-string of the category associated with that identifier.
b) Ifany of identifier-1,identifier-3,identifier-4,identifier-5,identifier-6,0r identifier-7 references an alphanumeric-edited data item, or a numeric-edited or unsigned numeric data item described explicitly or implicitly with usage display, the data item is inspected as though it had been redefined as alphanumeric (see General rule 4a) and the INSPECT statement had been written to reference the redefined data item:
c) Ifany of identifier-1,identifier-3,identifier-4,identifier-5,identifier-6,0r identifier-7 references national-edited data item, or numeric-edited or unsigned numeric data item described explicitly or implicitly with usage national, the data item is inspected as though it had been redefined as category national and the INSPECT statement has been written to reference the redefined data item
d) Ifany of identifier-1,identifier-3,identifier-4,identifier-5,identifier-6,0r identifier-7 references signed numeric data item, the data item is inspected as though it had been moved to an unsigned numeric data item with length equal to the length of the signed item excluding any separate sign position,and then the rules in General rule 4bor 4chad been applied. Ifidentifier-1 is a signed numeric item, the original value of the sign is retained upon completion of the INSPECT statement:
5) In General rules 6 through 21,all references to literal-1,literal-2,literal-3,literal-4,or literal-5 apply equally to the content ofthe data item referenced by  identifier-3, identifier-4, identifier-5, identifier-6,or identifier-7 respectively.
6) Item identification for any identifier is done only once as the first operation in the execution of the INSPECT statement;
FORMATS 1 AND 2
7) During inspection of the content of the data item referenced by identifier-1, each properly matched occurrence of literal-1 is tallied (format 1) or replaced by literal-3 (format 2).
646
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 677 ---

ISO /IEC 1989.2023 (E)
8) The comparison operation to determine the occurrence of literal-1 to be tallied or to be replaced, occurs as follows:
a) The operands of the TALLYING or REPLACING phrase are considered in the order they are specified in the INSPECT statement from left to right The first literal-1 is compared to an equal number of contiguous characters, starting with the leftmost character position, Or,if BACKWARD is specified,the rightmost character position in the data item referenced by identifier-1. Literal-1 matches that portion of the content ofthe data item referenced by identifier-1 if they are equal, character for character and:
If neither LEADING nor FIRST is specified; or
If the LEADING adjective applies to literal-1 and literal-1 is a leading occurrence as defined in General rules 12 and 17; or
If the FIRST adjective applies to literal-1 and literal-1 is the first occurrence as defined in General rule 17.
b) If no match occurs in the comparison of the first literal-1, the comparison is repeated with each successive literal-1,ifany, until either a match is found or there is no next successive literal-1_ When there is no next successive literal-1,the character position in the data item referenced by identifier-1 immediately to the right; or if BACKWARD is specified to the left, of the leftmost character position considered in the last comparison  cycle is considered as the leftmost character position, and the comparison cycle begins again with the first literal-1
NOTE 2 The keyword BACKWARD specifies only the direction of the scan, not the direction of the matching: Matching always takes place starting at the leftmost character atthe current character position as specified in General rule 8 with the leftmost position ofthe characters being tallied or replaced.
c) Whenever a match occurs, tallying or replacing takes place as described in General rules 12 and 15.The character position in the data item referenced by identifier-1 immediately to the right of the rightmost character position that participated in the match Or,if BACKWARD is specified, the character position in the data item referenced by identifier-1 immediately to the left of the leftmost character position that participated in the match is now considered to be the leftmost character position of the data item referenced by identifier-1,and the comparison cycle starts again with the first literal-1_
d) If BACKWARD was not specified the comparison operation continues until the rightmost character position of the data item referenced by identifier-1 has participated in a match or has been considered as the leftmost character position otherwise if BACKWARD has been specified, the comparison operation continues until the leftmost character position of the data item referenced by identifier-1 has participated in a match or has been considered as the rightmost character position. When this occurs, inspection is terminated:
If the CHARACTERS phrase is specified, an implied one character operand participates in the cycle described in General rules 8a through 8d above as if it had been specified by literal-1, except that no comparison to the content ofthe data item referenced by identifier-1 takes place. This implied character is considered always to match the leftmost character ofthe content ofthe data item referenced by identifier-1 participating in the current comparison cycle.
@ISO/IEC 2023
647
[per-copy licence stamp]


--- Page 678 ---

ISO /IEC 1989.2023 (E)
9) The comparison operation defined in General rule 8 is restricted by the BEFORE and AFTER phrase as follows:
a) If neither the BEFORE nor AFTER phrase is specified or identifier-4 references a zero-length item, literal-L or the implied operand of the CHARACTERS phrase participates in the comparison operation as described in General rule 8. Literal-1 or the implied operand of the CHARACTERS phrase is first eligible to participate in matching at the leftmost character position, 0r, if BACKWARD is specified,at the rightmost character position of identifier-1.
b) If the BEFORE phrase is specified, the associated literal-1 or the implied operand of the CHARACTERS phrase participates only in those comparison cycles that involve that portion of the content of the data item referenced by identifier-1 the first character position that is eligible to participate up to, but not including; the first occurrence encountered of literal-2 within the content of the data item referenced by identifier-1. The position of this first occurrence is determined before the first cycle of the comparison operation described in General rule 8 is begun: If, on any comparison cycle, literal-1 or the implied operand of the CHARACTERS phrase is not eligible to participate,it is considered not to match the content ofthe data item referenced by identifier-1.Ifthere is no occurrence ofliteral-2 within the content ofthe data item referenced by identifier-1, its associated literal-l or the implied operand of the CHARACTERS phrase participates in the comparison operation as though the BEFORE phrase had not been specified.
Ifthe AFTER phrase is specified:
If BACKWARD is   specified, the associated literal-1 or the   implied   operand of  the CHARACTERS phrase participates only in those comparison cycles that involve that portion of the content of the data item referenced by identifier-1 from the character position immediately to the left of the leftmost character position of the first occurrence of literal-2 within the content of the data item referenced by identifier-1 t0 the leftmost character position ofthe data item referenced by identifier-1, else
the associated literal-1 or the implied operand ofthe CHARACTERS phrase participates only in those comparison  cycles that involve that portion of the content of the data item referenced by identifier-1 from the character position immediately to the right of the rightmost character position of the first occurrence of literal-2 within the content of the data item referenced by  identifier-1 to the rightmost character position of the data item referenced by identifier-1.
This is the character position at which literal-1 or the implied operand of the CHARACTERS phrase is first eligible to participate in matching: The position of this first occurrence is determined before the first cycle of the comparison operation described in General rule 8 is begun. If,on any comparison cycle, literal-1 or the implied operand ofthe CHARACTERS phrase is not eligible to participate, it is considered not to match the content of the data item referenced by identifier-1. If there is no occurrence of literal-2 within the content ofthe data item referenced by identifier-1, its associated literal-1 or the implied operand of the CHARACTERS phrase is never eligible to participate in the comparison operation.
648
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 679 ---

ISO /IEC 1989.2023 (E)
FORMAT 1
10) Both the ALL and LEADING phrases are transitive across the operands that follow them until another ALL or LEADING phrase is encountered:
11) The content of the data item referenced by identifier-2 is not initialized by the execution of the INSPECT statement
12) The rules for tallying are as follows:
a) If the ALL phrase is specified, the content of the data item referenced by identifier-2 is incremented by one for each occurrence of literal-1 matched within the content of the data item referenced by identifier-1.
b) If the LEADING phrase is specified, the content of the data item referenced by identifier-2 is incremented by one for the first and each subsequent contiguous occurrence ofliteral-1 matched within the content of the data item referenced by identifier-1, provided that the first such occurrence encountered is at the point where comparison began in the first comparison cycle in which literal-1 was eligible to participate_
Ifthe CHARACTERS phrase is specified, the content ofthe data item referenced by identifier-2 is incremented by one for each character matched, in the sense of General rule 8e, within the content of the data item referenced by identifier-1_
13) If identifier-1,identifier-3,0r identifier-4 occupies the same storage area as identifier-2,the result of the execution of this statement is undefined, even if they are defined by the same data description entry: (See 14.6.10, Overlapping operands:)
FORMATS 2 AND 3
14) The size of literal-3 or the data item referenced by identifier-5 shall be equal to the size of literal-1 or the data item referenced by identifier-3. Ifthese sizes are not equal,the EC-RANGE-INSPECT-SIZE exception condition is set to exist and the results of the execution of the INSPECT statement are undefined When a figurative constant is used as literal-3,the size of the figurative constant is equal to the size of literal-1 or the size of the data item referenced by identifier-3_
15) When the CHARACTERS phrase is used, the data item referenced by identifier-5 shall be one character in length: If it is not; the EC-RANGE-INSPECT-SIZE exception condition is set to exist and the results of the execution of the INSPECT statement are undefined:
FORMAT 2
16) The ALL, FIRST, and LEADING phrases are transitive across the operands that follow them until another ALL, FIRST, or LEADING phrase is encountered.
17) The rules for replacement are as follows:
a) When the CHARACTERS phrase is specified, each character matched, in the sense of General rule 8e, in the content ofthe data item referenced by identifier-1 is replaced by literal-3_
@ISO/IEC 2023
649
[per-copy licence stamp]


--- Page 680 ---

ISO /IEC 1989.2023 (E)
b) When the adjective ALL is specified, each occurrence of literal-1 matched in the content of the data item referenced by identifier-1 is replaced by literal-3.
When the adjective LEADING is specified,the first and each successive contiguous occurrence of literal-1 matched in the content of the data item referenced by identifier-1 is replaced by literal-3, provided that the first such occurrence encountered is at the point where comparison began in the first comparison cycle in which literal-1,was eligible to participate.
d) When the adjective FIRST is specified, the leftmost occurrence of literal-1, Or, if BACKWARD is specified, the rightmost occurrence of literal-1 matched within the content of the data item referenced by identifier-1 is   replaced by   literal-3 This rule   applies to each successive specification of the FIRST phrase regardless ofthe value of literal-1.
18) Ifidentifier-3, identifier-4,0r identifier-5 occupies the same storage area as identifier-1,the result of the execution of this statement is undefined, even if they are defined by the same data description entry: (See 14.6.10, Overlapping operands:)
FORMAT 3
19)A format 3 INSPECT statement is interpreted and executed as though two successive INSPECT statements specifying the same identifier-1 had been written with one statement being a format 1 statement with TALLYING phrases identical to those specified in the format 3 statement; and the other statement being a format 2 statement with REPLACING phrases identical to those specified in the format 3 statement The general rules given for matching and counting apply to the format 1 statement and the general rules given for matching and replacing apply to the format 2 statement: Item identification ofany identifier in the format 2 statement is done only once before executing the format 1 statement:
FORMAT 4
20) A format 4 INSPECT statement is interpreted and executedas though a format 2 INSPECT statement specifying the same identifier-1 had been written with a series of ALL phrases, one for each character of literal-4_ The effect is as if each of these ALL phrases referenced, as literal-1,a single character of literal-4 and referenced,as literal-3,the corresponding single character of literal-5. Correspondence between the characters of literal-4 and the characters of literal-5 is by ordinal position within the data item
21) Ifidentifier-4, identifier-6,0r identifier-7 occupies the same storage area as identifier-1,the result of the execution of this statement is undefined, even if they are defined by the same data description entry. (See 14.6.10, Overlapping operands)
22) The size of literal-5 or the data item referenced by identifier-7 shall be equal to the size of literal-4 or the data item referenced by identifier-6. Ifthese sizes are not equal,the EC-RANGE-INSPECT-SIZE exception condition is set to exist and the results of the execution of the INSPECT statement are undefined: When a figurative constant is used as literal-5,the size of the figurative constant is equal to the size of literal-4 or the size ofthe data item referenced by identifier-6_
23) If the same character appears more than once in the data item referenced by identifier-6 or in literal-4,the first occurrence of the character is used for replacement
650
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 681 ---

ISO /IEC 1989.2023 (E)
14.9.23 INVOKE statement
14.9.23.1 General
The INVOKE statement causes a method to be invoked:
14.9.23.2 General format
identifier-3 OMITTED
BY REFERENCE
arithmetic-expression-1 [BY CONTENT ] boolean-expression-1 USING identifier-5 literal-2
INVOKE object-class-name-1 identifier-1
identifier-2 literal-1
arithmetic-expression-1 identifier-5 literal-2
[BY VALUE
RETURNING identifier-4 ]
14.9.23.3 Syntax rules
1) Identifier-1 shall be an object reference:
2) Literal-1 shall be of class alphanumeric or national and shall not be a zero-length literal:
3) Ifobject-class-name-1 is specified, literal-1 shall be specified The value of literal-1 shall be the name ofa method defined in the factory interface of object-class-name-1_
4) If identifier-1 is specified and it does not reference a universal object reference, literal-1 shall be specified. The value of literal-1 shall be the name ofa method, subject to the following conditions:
a) If identifier-1 references an object reference described with an object-class-name and the FACTORY phrase, literal-1 shall be the name of a method contained in the factory interface of that object-class-name:
b) If identifier-1 references an object reference described with an object-class-name without the FACTORY phrase, literal-1 shall be the name of a method contained in the instance interface of that object-class-name
If identifier-1 references an object   reference described with the ACTIVE-CLASS  and the FACTORY phrases, literal-1 shall be the name ofa method contained in the factory interface of the class containing the INVOKE statement
@ISO/IEC 2023
651
[per-copy licence stamp]


--- Page 682 ---

ISO /IEC 1989.2023 (E)
If identifier-1 references an object reference described with the ACTIVE-CLASS phrase and without the FACTORY phrase; literal-1 shall be the name of a method contained in the instance interface of the class containing the INVOKE statement
If identifier-1 references an object reference described with an interface-name, literal-l shall be the name ofa method contained in the interface referenced by that interface-name
0 If identifier-1 references the predefined object reference SELF and the method containing the INVOKE statement is a factory method, literal-1 shall be the name ofa method contained in the factory interface of the class containing the INVOKE statement:
g) If identifier-1 references the predefined object reference SELF and the method containing the INVOKE statement is an instance method, literal-1 shall be the name of a method contained in the instance interface ofthe class containing the INVOKE statement
h) If identifier-1 references the predefined object reference SUPER and the method containing the INVOKE statement is a factory method, literal-1 shall be the name ofa method contained in the factory interface ofa class inherited by the class containing the INVOKE statement
i) If identifier-1 references the predefined object reference SUPER and the method containing the INVOKE statement is an instance method, literal-1 shall be the name of a method contained in the instance interface ofa class inherited by the class containing the INVOKE statement:
5)   If object-class-name-1 is specified or the data item referenced by identifier-1 is nota universal object reference, the following applies:
a) If a BY CONTENT or BY REFERENCE phrase is specified for an argument; a BY REFERENCE phrase shall be specified or implied for the corresponding formal parameter in the procedure division header;
b) If a BY VALUE phrase is specified for an argument; a BY VALUE phrase shall be specified or implied for the corresponding formal parameter in the procedure division header:
c) The rules for conformance as specified in 14.8.2, Parameters and 14.8.3,Returning items apply.
If identifier-1 references a universal object reference, neither the BY CONTENT nor the BY VALUE phrase shall be specified and the BY REFERENCE phrase, if not specified explicitly, is assumed implicitly:
7) Identifier-2 may be specified only when identifier-1 is a universal object reference:
8) Identifier-2 shall reference an alphanumeric or national data item.
9) Identifier-3 shall be an address-identifier or shall reference a data item defined in the file, working- storage, local-storage, or linkage section.
10) Identifier-3 shall not reference a data item defined in the file or working-storage section ofa factory or instance object:
652
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 683 ---

ISO /IEC 1989.2023 (E)
11)" Identifier-4 shall reference a data item defined in the file, working-storage, local-storage, or linkage section.
12) If identifier-3 or identifier-4 references a bit data item, it shall be described such that it is aligned on byte boundary and that subscripting and the leftmost position in a reference modification of that identifier consist of only fixed-point numeric literals or arithmetic expressions whose result is a positive integer,in which all operandsare numeric literals and in which the exponentiation operator iS not specified:
13) If Identifier-3, identifier-4, 0r identifier-5 references group item, there shall not be an item subordinate to that group item that is an object reference described with the ACTIVE-CLASS phrase
14) If an argument is specified without any BY phrase, BY REFERENCE is implied for that argument when:
a) that argument is valid for being passed by reference, and
b) the corresponding formal parameter is not specified with the BY VALUE phrase:
15) If identifier-5 or its corresponding formal parameter is specified with the BY VALUE phrase, identifier-5 shall be ofclass message-tag, numeric, object 0r pointer.
16) If literal-2 or its corresponding formal parameter is specified with the BY VALUE phrase, literal-2 shall be a numeric literal,
17) Literal-2 shall not be a zero-length literal.
18) If an OMITTED phrase is specified, an OPTIONAL phrase shall be specified for the corresponding formal parameter in the procedure division header_
19) If identifier-3 references an address-identifier, identifier-3 is a sending operand.
20) If identifier-3 does not reference an address-identifier, identifier-3 is a receiving operand.
21) Identifier-5 and any identifier specified in arithmetic-expression-1 or boolean-expression-1 is a sending operand:
22) Identifier-4 is a receiving operand.
14.9.23.4 General rules
1) The instance of the program, function, or method that executes the INVOKE statement is the activating runtime element
2) Identifier-1 identifies an instance object: If object-class-name-1 is specified, it identifies the factory object of the object-class referenced by that object-class-name. Literal-1 or the content of the data item referenced by identifier-2 identifies a method of that object that will act upon that instance object:
@ISO/IEC 2023
653
[per-copy licence stamp]


--- Page 684 ---

ISO /IEC 1989.2023 (E)
a) If the method to be invoked is a COBOL method, literal-1 or the content of the data item referenced by identifier-2 is the name of the method to be invoked as described in 8.3.2.2, User-defined words.
b) If the method to be invoked is a non-COBOL method, the behavior of the INVOKE statement is implementor-defined.
3) The sequence of arguments in the USING phrase of the INVOKE statement and the corresponding formal  parameters in the USING phrase of the invoked method's procedure division header determines the correspondence between arguments and formal parameters This correspondence is positional and not by name equivalence
NOTE The first argument corresponds to the first formal parameter;the second to the second,and the nth to the nth:
The effect of the USING phrase on the activated runtime element is described in 14.3, Procedure division, general rules:
4) An argument that consists merely of a single identifier or literal is regarded as an identifier or literal rather than an arithmetic or boolean expression:
5) If identifier-l is null,the EC-OO-NULL exception condition is set to exist and execution ofthe INVOKE statement is terminated:
6) If object-class-name-1 is specified or the data item referenced by identifier-1 is not a universal object reference, the following applies: If an argument is specified without any of the keywords BY REFERENCE, BY CONTENT; or BY VALUE, the manner used for passing that argument is determined as follows:
a) When the BY REFERENCE phrase is specified or   implied for the corresponding formal parameter:
if the argument meets the requirements of Syntax rules 9 and 10, BY REFERENCE is assumed;
if the argument does not meet the requirements of Syntax rules 9 and 10, BY CONTENT is assumed_
b) When the BY VALUE phrase is specified or implied for the corresponding formal parameter, BY VALUE is assumed.
7) Execution of the INVOKE statement proceeds as follows:
a) Arithmetic-expression-1, boolean-expression-1, identifier-1, identifier-2, identifier-3, and identifier-5 are evaluated and item identification is done for identifier-4 at the beginning of the execution of the INVOKE statement: If an exception condition exists, no method is invoked and execution proceeds as specified in General rule 7g. If an exception condition does not exist; the values of identifier-3, identifier-5, arithmetic-expression-1, boolean-expression-1, or literal-2 are made available to the invoked method at the time control is transferred to that method:
654
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 685 ---

ISO /IEC 1989.2023 (E)
b) The runtime system attempts to locate the method being invoked using the rules specified in 8.4.6, Scope of names; 8.4.6.5, Scope of method-names; 9.3.6, Method invocation; and 12.3.8, REPOSITORY paragraph. If the method is not found or the resources necessary to execute the method are not available; the EC-OO-METHOD exception condition is set to exist; the method invocation is not successful, and execution continues as specified in General rule 7g
If identifier-1 is a universal object reference and the method being invoked is a COBOL method, neithera formal parameter northe returning item in the invoked method shall be described with the ANY LENGTH clause, and the rules for conformance specified in 14.8.2, Parameters and 14.8.3, Returning items apply: If a violation of these rules is detected, the EC-OO-UNIVERSAL exception condition is set to exist if checking for it is enabled in both the activated method and the activating runtime element;the method invocation is not successful,and execution continues as specified in General rule 7g:
d) External items are checked to ensure that they comply with the following rules as specified in 14.8.4,External items:
Rule
Exception condition EC-EXTERNAL-DATA-MISMATCH
14.8.4.2, Correspondence of external data items used in external files 14.8.4.3, Correspondence of external data item formats 14.8.4.4, Correspondence of external file control entries
EC-EXTERNAL-FORMAT- CONFLICT
EC-EXTERNAL-FILE-MISMATCH
If one of the rules listed above is violated and checking for it is enabled for the associated exception in both the activated method and the activating runtime element; the method invocation is not successful, and execution continues as specified in General rule 7g:
The method specified by the INVOKE statement is made available for execution and control is transferred to the invoked method. Control is transferred to the invoked method in a manner consistent with the entry convention specified for the method. Ifthe invoked method is a COBOL method, its execution is described in 14.2.3, General rules of the procedure division; otherwise, the execution is defined by the implementor
9) After control is returned from the invoked method, ifan exception condition is propagated from the invoked method, execution continues as specified in General rule 7g; otherwise, control is transferred to the end of the INVOKE statement;
g) If an  exception condition has been  raised, any exception processing statements that are associated with that exception condition are executed. Execution then proceeds as defined in 14.6.13, Exception condition handling;
8) Ifa RETURNING phrase is specified,the result ofthe activated method is placed into identifier-4_
9) Ifan OMITTED phrase is specified ora trailing argument is omitted, the omitted-argument condition for that parameter shall be true in the invoked method.
@ISO/IEC 2023
655
[per-copy licence stamp]


--- Page 686 ---

ISO /IEC 1989.2023 (E)
10) Ifa parameter for which the omitted-argument condition is true is referenced in an invoked method, except as an argument or in the omitted-argument condition, the EC-OO-ARG-OMITTED exception condition is set to exist;
656
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 687 ---

ISO /IEC 1989.2023 (E)
14.9.24 MERGE statement
14.9.24.1 General
The MERGE statement combines two or more identically sequenced files on a set of specified keys, and during the process makes records available,in merged order,to an outputprocedure or toan output file:
14.9.24.2 General format
ASCENDING DESCENDING
MERGE file-name-1
ON
KEY data-name-1
IS alphabet-name-1 [ alphabet-name-2 ] COLLATING SEQUENCE FOR ALPHANUMERIC IS alphabet-name-1 FOR NATIONAL IS alphabet-name-2
USING file-name-2 { file-name-3
THROUGH procedure-name-2 THRU
OUTPUT PROCEDURE IS procedure-name-1
GIVING file-name-4
14.9.24.3 Syntax rules
1) MERGE statement may appear anywhere in the procedure division except in  imperative- statement-1 of an exception-checking PERFORM statement; Or in an output procedure of another MERGE statement; or an input or output procedure of a file format SORT statement; or in declarative procedure_
2) File-name-1 shall be described in a sort-merge file description entry in the data division.
3) If the file description entry for file-name-1 describes variable-length records, the file description entry for file-name-2 or file-name-3 shall describe neither records smaller than the smallest record nor larger than the largest record described for file-name-1. If the file description entry for file-name-1 describes fixed-length records, the file description entry for file-name-2 or file-name-3 shall not describe a record that is larger than the record described for file-name-1.
4) Data-name-1 is a key data-name_ Key data-names are subject to the following rules:
a) The data items identified by key data-names shall be described in records associated with file-name-1.
@ISO/IEC 2023
657
[per-copy licence stamp]


--- Page 688 ---

ISO /IEC 1989.2023 (E)
b) Key data names shall not be subject to any OCCURS clauses:
c) Key . data items shall not be ofthe class boolean, message-tag; object; or pointer:
d) key data item shall not be variable-length group, an occurs-depending-on data item, dynamic-length elementary item, or an item subordinate to a dynamic-capacity table:
e) If file-name-1has more than one record description, the data items identified by key data-names need be described in only one of the record descriptions The same byte positions referenced by key data-name in one record description entry are taken as the key in all records ofthe file:
f) None of the data items identified by key data-names may be described by an entry that either contains an OCCURS clause or is subordinate to an entry that contains an OCCURS clause:
Ifthe file referenced by file-name-1 contains variable-length records,all the data items identified by key data-names shall be contained within the first x bytes of the record, where X equals the minimum record size specified for the file referenced by file-name-1.
5) Alphabet-name-1 shall reference an alphabet that defines an alphanumeric collating sequence
Alphabet-name-2 shall reference an alphabet that defines a national collating sequence
7) File-names shall not be repeated within the MERGE statement:
8) The words THROUGH and THRU are equivalent
9) File-name-2, file-name-3,and file-name-4 shall be described in a file description entry that is not for a report file and is not a sort-merge file description entry.
10) If file-name-4 references an indexed file, the first specification of data-name-1 shall be associated with an ASCENDING phrase and the data item referenced by that data-name-1 shall occupy the same byte positions in its record as the data item associated with the prime record key for that file.
11) No pair of file-names in a MERGE statement may be specified in the same SAME AREA, SAME SORT AREA, or SAME SORT-MERGE AREA clause. The only file-names in a MERGE statement that may be specified in the same SAME RECORD AREA clause are those associated with the GIVING phrase
12) If the  GIVING phrase is   specified and the file   description entry for   file-name-4 describes variable-length records, the file description entry for file-name-1 shall describe neither records smaller than the smallest record nor larger than the largest record described for file-name-4. If the file description entry for file-name-4 describes fixed-length records, the file description entry for file-name-1 shall not describe a record that is larger than the record described for file-name-4.
13) If file-name-2 or file-name-3 references a relative or an indexed file, its access mode shall be sequential or dynamic:
658
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 689 ---

ISO /IEC 1989.2023 (E)
14.9.24.4 General rules
1) The MERGE statement merges all records contained on the files referenced by file-name-2 and file-name-3
2) If the file referenced by file-name-1 contains only fixed-length records, any record in the file referenced by file-name-2 or file-name-3 containing fewer character positions than that fixed length is space filled on the right to that fixed length, beginning with the first character position after the last character in the record, when that record is released to the file referenced by file-name-1, as follows:
a) Ifthere is only one record description entry associated with the file referenced by file-name-2 or file-name-3 and that record is described as a national data item or as an elementary data item of usage national and of category numeric, numeric-edited, or boolean, the record is filled with national space characters:
b) Ifthere are multiple record description entries associated with the file referenced by file-name- 2 or file-name-3 and the descriptions include a SELECT WHEN clause; the rules of the SELECT WHEN clause are applied to the record to select its description. When the record is described as national data item or as an elementary data item of usage national and of category numeric, numeric-edited, 0r boolean, the record is filled with national space characters.
c) Otherwise, the record is filled with alphanumeric space characters
3) The data-names following the word KEY arelisted from left to right in the MERGE statement in order of decreasing significance without regard to how they are divided into KEY phrases: The leftmost data-name is the major key, the next data-name is the next most significant key, etc:
a) When the ASCENDING phrase is specified, the merged sequence is from the lowest value ofthe contents ofthe data items identified by the key data-names to the highest value, according to the rules for comparison of operands in a relation condition:
b) When the DESCENDING phrase is specified,the merged sequence is from the highest value ofthe contents of the data items identified by the key data-names to the lowest value, according to the rules for comparison of operands in a relation condition_
The words ASCENDING and DESCENDING are transitive across all occurrences of data-name-1 until another occurrence of the word ASCENDING or DESCENDING is encountered.
4) When, according to the rules for the comparison of operands in a relation condition, the contents of all the key data items of one record are equal to the contents of the corresponding key data items of one or more other records, the order of return of these records:
a) Follows the order ofthe associated input files as specified in the MERGE statement
b) Is such that all records associated with one input file are returned prior to the return of records from another input file:
@ISO/IEC 2023
659
[per-copy licence stamp]


--- Page 690 ---

ISO /IEC 1989.2023 (E)
5) The alphanumeric collating sequence that applies to the comparison of key data items of class alphabetic and class   alphanumeric, and the national   collating sequence that applies to the comparison of key data items of class national, are separately determined at the beginning of the execution of the MERGE statement in the following order ofprecedence:
a) First; the collating sequence established by the COLLATING SEQUENCE phrase, if specified, in this MERGE statement: The collating sequence associated with alphabet-name-1 applies to key data items of class alphabetic and alphanumeric; the collating sequence associated with alphabet-name-2 applies to key data items of class national.
b) Second, the collating sequences established as the program collating sequences_
If the records in the file referenced by file-name-2 and file-name-3 are not ordered as described in the ASCENDING or DESCENDING KEY clauses andthe collating sequence associated with the MERGE statement; the EC-SORT-MERGE-SEQUENCE exception condition is set to exist; all files associated with the MERGE statement are closed, and the results ofthe merge operation are undefined.
7) AlI the records in the files referenced by file-name-2 and file-name-3 are transferred to the file referenced by file-name-1. At the start of execution of the MERGE statement; the file connectors referenced by file-name-2 and file-name-3 shall not be in the open mode, otherwise the EC-SORT- MERGE-FILE-OPEN exception condition is set to exist and the execution of the MERGE statement terminates: For each of the files referenced by file-name-2 and file-name-3 the execution of the MERGE statement causes the following actions to be taken:
a) The processing of the file is initiated. If the file-control entry for the file has a SHARING clause with the ALL phrase, the initiation is performed as ifan OPEN statement with the INPUT phrase and the SHARING WITH READ ONLY phrase had been executed; otherwise, the initiation is performed as if an OPEN statement with the INPUT phrase and without a SHARING phrase is executed. If an output procedure is specified, this initiation is performed before control passes to the output procedure Ifa nonfatal exception condition exists as a result ofthe execution ofthe implicit OPEN statement; the MERGE statement is terminated unless there is an applicable USE procedure that completes normally, after which the MERGE statement continues processing as if the exception condition did not exist:
b) The logical records are obtained and released to the merge operation  Each record is obtained as if a READ statement with the NEXT phrase, the IGNORING LOCK phrase, and the AT END phrase had been executed. When the at end condition exists for file-name-2 or file-name-3,the processing for that file connector is terminated. If the file referenced by file-name-1 is described with variable-length records, the size of any record written to file-name-1 is the size of that record when it was read from file-name-2 or file-name-3, regardless of the content of the data item referenced by the DEPENDING ON phrase of either a RECORD IS VARYING clause 0r an OCCURSclause specified in the sort-merge file description entry for file-name-1. Ifthe size ofthe record read from the file referenced by file-name-2 or file-name-3 is larger than the largest record allowed in the file description entry for file-name-1, the EC-SORT-MERGE-RELEASE exception condition is set to exist and the execution of the MERGE statement is terminated. If file-name-1 is specified with variable-length records and the size of the record read from the file referenced by file-name-2 or file-name-3 is smaller than the smallest record allowed in the file description entry for file-name-1, the EC-SORT-MERGE-RELEASE exception condition is set to exist and the execution of the MERGE statement is terminated.
660
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 691 ---

ISO /IEC 1989.2023 (E)
C) The processing of the file is terminated. The termination is performed as ifa CLOSE statement without  optional phrases had been executed: If an output procedure is  specified, this termination is not performed until   after control passes the last  statement in the output procedure: For a relative file, the content of the relative key data item is undefined after the execution ofthe MERGE statement
These implicit functions are performed such that any applicable USE procedures are executed: If a nonfatal exception condition exists from an attempt to CLOSE file-name-2 or file-name-3:
If there is an applicable USE procedure that completes normally, the MERGE statement continues:
If there is no applicable USE procedure; the exception condition is ignored and the MERGE statement continues execution_
The value ofthe data item referenced by the DEPENDING ON phrase ofa RECORD IS VARYING clause specified in the file description entry for file-name-2 or file-name-3 is undefined upon completion of the MERGE statement
8) The output procedure may consist of any procedure needed to select; modify, or copy the records that are made available one at a time by the RETURN statement in merged order from the file referenced by file-name-1 The range includes all statements that are executed as the result of a transfer of control in the range of the output procedure, as well as all statements in declarative procedures that are executed as a result of the execution of statements in the range of the output procedure The range ofthe output procedure shall not cause the execution ofany MERGE, RELEASE, or the file format ofthe SORT statement: (See 14.6.3,Explicit and implicit transfers of control ) If this rule is violated,the EC-SORT-MERGE-ACTIVE exception condition is set to exist and the results ofthe merge operation are undefined:
9) If an output procedure is specified, control passes to it during execution of the MERGE statement: The compiler inserts a return mechanism after the last statement in the output procedure: When control passes to that return mechanism, the mechanism provides for termination ofthe merge, and then passes control to the next executable statement after the MERGE statement Before entering the output procedure; the merge procedure reaches a pointat which it selects the next record in merged order when requested. The RETURN statements in the output procedure are the requests for the next record.
NOTE This return mechanism transfers control from the end of the output procedure and is not associated with the RETURN statement
10) During the execution of the output procedure, no statement may be executed manipulating the file referenced by or accessing the record area associated with file-name-2 or file-name-3
11) During the execution of any USE procedure  implicitly invoked while executing the MERGE statement,no statement may be executed manipulating the file referenced by or accessingthe record area associated with file-name-2,file-name-3,0r file-name-4
12) If the GIVING phrase is specified, all the merged records are written on each file referenced by file-name-4 as the implied output procedure for the MERGE statement At the start of execution of
@ISO/IEC 2023
661
[per-copy licence stamp]


--- Page 692 ---

ISO /IEC 1989.2023 (E)
the MERGE statement; the file referenced by file-name-4 shall not be in the open mode. If the file is in an open mode, the EC-SORT-MERGE-FILE-OPEN exception condition is set to exist and the execution of the MERGE statement terminates For each of the files referenced by file-name-4, the execution of the MERGE statement causes the following actions to be taken:
a) The processing of the file is initiated. The initiation is performed as ifan OPEN statement with the OUTPUT and SHARING WITH NO OTHER phrases had been executed. If a fatal exception condition exists as a result of this implicit OPEN statement and there is an applicable USE procedure that completes normally, processing for the file connector that caused the exception condition is bypassed. If a nonfatal exception condition exists as a result of this implicit OPEN statement and there is an applicable USE procedure that completes normally, the file connector that caused the exception condition is processed as if the exception did not exist
b) The merged logical records are returned and written onto the file. Each record is written as if a WRITE statement without any optional phrases had been executed. If the file referenced by file-name-4 is  described with   variable-length records, the size of any record written to file-name-4 is the size ofthat record when it was read from file-name-1,regardless ofthe content ofthe data item referenced by the DEPENDING ON phrase of eithera RECORD IS VARYING clause or an OCCURS clause specified in the file description entry for file-name-4. If an exception condition exists as a result of this implicit WRITE statement and there is an applicable USE procedure that completes normally, the MERGE continues execution, otherwise the MERGE statement is terminated:
For a relative file,the relative key data item for the first record returned has the value 1; for the second record returned,the value 2; etc. After execution of the MERGE statement; the content of the relative key data item indicates the last record returned to the file
c) The processing of the file is terminated: The termination is performed as if a CLOSE statement without optional phrases had been executed.
These implicit functions are performed such that any associated USE procedures are executed; however, the execution of such a USE procedure shall not cause the execution of any statement manipulating the file referenced by, or accessing the record area associated with, file-name-4. On the first attempt to write outside the externally defined boundaries of the file, any USE procedure associated with the file connector referenced by file-name-4 is executed; if control is returned from that USE procedure or ifno such USE procedure is specified, the processing of the file is terminated as in General rule 12c above.
The value ofthe data item referenced by the DEPENDING ON phrase ofa RECORD IS VARYING clause specified in the sort-merge file description entry for file-name-1 is undefined upon completion ofthe MERGE statement for which the GIVING phrase is specified.
13) If the file referenced by file-name-4 contains only fixed-length records, any record in the file referenced by file-name-1 containing fewer character positions than that fixed-length is space filled on the right to that fixed length, beginning with the first character position after the last character in the record, when that record is returned to the file referenced by file-name-4,as follows:
a) If there is only one record description entry associated with the file referenced by file-name-4 and that record is described as a national data item or as an elementary data item of usage
662
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 693 ---

ISO /IEC 1989.2023 (E)
national and of category numeric, numeric-edited, or boolean; the record is filled with national space characters
b) Ifthere are multiple record description entries associated with the file referenced by file-name- 4 and the descriptions include a SELECT WHEN clause, the rules ofthe SELECT WHEN clause are applied to the record to select its description. When the record is described as a national data item or as an elementary data item of usage national and of category numeric, numeric-edited, or boolean, the record is filled with national space characters_
c) Otherwise, the record is filled with alphanumeric space characters.
14) Additional rules affecting the execution of the MERGE statement are given in 12.4.5, File control entry, General rules 3 and 4.
@ISO/IEC 2023
663
[per-copy licence stamp]


--- Page 694 ---

ISO /IEC 1989.2023 (E)
14.9.25 MOVE statement
14.9.25.1 General
The MOVE statement transfers data, in accordance with the rules of editing, to one or more data areas_
14.9.25.2 General formats
Format 1 (simple):
MOVE
identifier-1 literal-1
TQ { identifier-2
Format 2 (corresponding):
CORRESPONDING identifier-3 TQ identifier-4 CORR
MOVE
14.9.25.3 Syntax rules
FORMAT 1
1) The class of identifier-1 or identifier-2 shall not be index, message-tag, object, or pointer
2) Ifidentifier-2 references a strongly-typed group item,identifier-1 shall be specified and be described as a group item of the same type:
3) Literal-1 and the data item referenced by identifier-1 are sending operands.
4) Identifier-2 is a receiving operand:
5) It is permitted to move an ALL "literal" figurative constant (containing only digits) or an ALL symbolic-character (representing a digit) to an integer numeric item. In all other cases, the move of an alphanumeric figurative constant (SPACE, QUOTE, HIGH-VALUE, LOW-VALUE, ALL "literal" , 0r ALL symbolic-character) to either a numeric item or a numeric-edited item is prohibited.
NOTE Moving an ALL "literal" figurative constant containing only digits or moving an ALL symbolic character figurative constant representing a digit to an integer numeric receiving item is an obsolete feature and is to be removed from the next edition of standard COBOL
6) The figurative constant ZERO shall not be moved to an alphabetic data item:
7) Any figurative constant for which the associated character or characters are not boolean characters shall not be moved to a boolean data item_
8) If identifier-1 referencesa data item described with usage binary-char; binary-short; binary-long or binary-double, identifier-2 shall reference a numeric Or numeric-edited item:
664
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 695 ---

ISO /IEC 1989.2023 (E)
9) If   identifier-1 or   identifier-2 references variable-length group then these groups shall be compatible groups as specified in 8.5.1.12, Variable-length groups.
10) For all other cases not described in Syntax rules 8 and 9, table 16, Validity of types of MOVE statements, specifies the validity of the move
@ISO/IEC 2023
665
[per-copy licence stamp]


--- Page 696 ---

ISO /IEC 1989.2023 (E)
Table 16 _ Validity of types of MOVE statements Category ofreceiving operand Alphabetic Alphanumeric National, Numeric, edited, Boolean National- Numeric- Alphanumeric edited edited
Type-name
Category of sending operand
Alphabetic Alphanumeric Alphanumeric- edited
Yes
Yes
No
Yes
No
No
Yes
Yes
Yes
Yes
Yes
No
Yes
Yes
No
Yes
No
No
Boolean
No
Yes
Yes
Yes
No
No
National
No
No
Yes
Yes
No
National-edited
No
No
No
Yes
No
No
Integer Numeric Noninteger Numeric-edited
No
Yes
No
Yes
Yes
No
No
No
No
No
Yes
No
No
Yes
No
Yes
No
Type-name
Yes
Yes
Yes
Yes
Yes
Yes
FORMAT 2
11) The words CORR and CORRESPONDING are equivalent:
12) Identifier-3 and identifier-4 shall specify group data items and shall not be reference-modified.
13) The corresponding data items within identifier-3 are sending operands The corresponding data items within identifier-4 are receiving operands The corresponding data items are determined according to the rules specified in 14.7.6, CORRESPONDING phrase
14.9.25.4 General rules
FORMAT 1
1) Literal-1 or the content of the data item referenced by identifier-1 is moved to the data item referenced by each identifier-2 in the order specified.
Item identification for identifier-2 is performed immediately before the data is moved to the respective data item: If identifier-2 is a zero-length item; the MOVE statement leaves identifier-2 unchanged,
If identifier-1 is reference-modified, subscripted, 0r is a function-identifier, the reference modifier, subscript; or function-identifier is evaluated only once, immediately before data is moved to the first ofthe receiving operands.
666
@ISO/IEC 2023
[per-copy licence stamp]
Yes
Yes


--- Page 697 ---

ISO /IEC 1989.2023 (E)
The length ofthe data item referenced by identifier-1 is evaluated only once, immediately before the data is moved to the first of the receiving operands If identifier-1 is a zero-length item, it is as if literal-1 were specified as a zero-length literal.
The evaluation of the length of identifier-1 or identifier-2 may be affected by the DEPENDING ON phrase of the OCCURS clause.
The result of the statement
MOVE a (b) TO b, € (b)
is equivalent to:
MOVE a (b) TO temp MOVE temp TO b MOVE temp to c (b)
where 'temp' is an intermediate result item provided by the implementor_
2) If literal-1 is an alphanumeric or national zero-length literal and the receiving operand is other than dynamic-length elementary item, literal-1 is treated as ifit were the figurative constant SPACE.
3) If literal-1 is a boolean zero-length literal and the receiving operand is other than a dynamic-length elementary item, literal-1 is treated as if it were the figurative constant ZERO_
4) Any move in which the sending operand is either a literal or an elementary item and the receiving item is an elementary item is an elementary move. Bit group items and national group items are treated as elementary items in the MOVE statement
Any move that is not an elementary move,and does not reference a variable-length group, is treated exactly as if it were an alphanumeric to alphanumeric elementary move, except that there is no conversion of data from one form of internal representation to another: In such a move,the receiving area will be filled without consideration for the individual elementary Or group items contained within either the sending or receiving area, except as specified in General rule 8 of the OCCURS clause and except as may be required by an implementation for subordinate items of class message- tag object; or pointer
5)   De-editing takes place only when the sending operand is numeric-edited data item and the receiving item is a numeric or a numeric-edited data item:
Any necessary conversion of data from one form of internal representation to another takes place during valid elementary moves, along with any editing specified for, or de-editing implied by, the receiving data item: When a national group item is referenced in a MOVE statement,no editingor de- editing takes place:
NOTE 1 Bit group items and national group items are treated as elementary items in the MOVE statement:
Any necessary conversion from alphanumeric character to national character representation shall be  performed, before any  alignment; in  accordance with correspondence defined by the
@ISO/IEC 2023
667
[per-copy licence stamp]


--- Page 698 ---

ISO /IEC 1989.2023 (E)
implementor: Ifno correspondence exists forany given alphanumeric character in the sending item, an implementor-defined substitution character is used as the corresponding national character in the receiving item and the EC-DATA-CONVERSION exception condition is set to exist.
The following rules apply:
a) When an  alphanumeric, alphanumeric-edited, national, or  national-edited data item is receiving operand,alignment and any necessary space filling shall take place as defined in 14.6.8, Alignment and transfer of data into data items If the sending operand is described as being signed numeric, the operational sign is not moved; if the operational sign occupies a separate character position,that character is not moved and the size ofthe sending operand is considered to be oneless than its actual size. (See 13.18.52,SIGN clause:) Ifthe usage ofthe sending operand is different from that ofthe receiving operand, conversion of the sending operand to the internal representation of the receiving operand takes place If the sending operand is numeric and contains the picture symbol 'P', all digit positions specified with this symbol are considered to have the value zero and are counted in the size ofthe sending operand.
Ifthe sending item is of class boolean, its boolean value shall be moved.
NOTE 2 When the runtime coded character set is the UTF-16 format of the UCS,the COBOL system does not detect truncation that bisects the two halves ofa surrogate pair:
b) When the sending operand and a receiving operand are identified as referencing the same data item, then:
If the category of the operand is alphanumeric-edited or national-edited, the result of execution of the statement is undefined.
2. If the operand is a variable-length data item, the result of execution of the statement is the same aS if the content ofthe sending operand had been moved to a temporary fixed-length data item of the same class, category and length as the sending operand, and the content of that temporary data item were then moved to the receiving operand:
c) When the receiving data item is described with the same usage specification as the sending operand, the data in the sending operand is transferred to the receiving data item without change: When the usage specifications ofthe receiving data item differ from those ofthe sending operand only in endianness specifications, the data in the sending operand is transferred to the receiving data item in the endianness ofthe receiving operand, but without any other change to the data,
NOTE 3 These provisions allow the preservation ofthe exact contents of sending operands, such as NaN representations and infinity representations, as well as finite numeric values, in  either the same endianness as, or a different endianness from, the sending operand:
d) When a numeric or numeric-edited item is the receiving item,and General rule 6c does not apply:
Ifthe category ofthe sending operand is numeric-edited,the content ofthe sending operand shall be as specified in 14.6.13.2,Incompatible data,and de-editing establishes the operand's numeric value, which may be signed:
668
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 699 ---

ISO /IEC 1989.2023 (E)
Otherwise, if the content of the sending operand would result in a false value in a numeric class condition, the EC-DATA-INCOMPATIBLE exception condition is set to exist; and the results of the execution of the MOVE statement are undefined:
When the sending operand is numeric, Or is the numeric value produced by de-editing:
If the sending operand is   described with FLOAT-SHORT, FLOAT-LONG, or FLOAT - EXTENDED usage, the implementor specifies any exception conditions that might be set to exist during data conversion, and, for fixed-point numeric and fixed-point numeric-edited receiving data items, specifies alignment of the data by decimal point
When a signed numeric item is the receiving item, the sign ofthe numeric value shall be represented in the receiving operand: If the sending operand is unsigned, the sign shall be positive:
When an unsigned numeric item is the receiving item, the absolute value ofthe sending value is used, and no operational sign is generated for the receiving item_
When the sending operand is described as alphanumeric or national, the sending operand is treated as if  it were an unsigned   integer of  category numeric with the   following characteristics:
If the sending operand is a data item, the number of digits is the number of character positions in the sending data item unless the number of character positions is greater than 31,in which case the rightmost 31 character positions are used:
b. If the sending operand is a figurative constant; the number of digits is the same as the number of digits in the receiving operand and the figurative constant is replicated in this item, from left to right;as described in the rules for figurative constants. Ifthe receiving item is not an integer,the number of digits includes both those to the right and the left of the decimal point:
Ifthe sending operand is an alphanumeric Or national literal, the number of digits is the same as the number of characters in the literal: If the number of characters exceeds 31, the size of the sending operand is 31 digits and only the rightmost 31 characters in the literal are used.
If the receiving data item is described with a standard floating-point usage or is a floating- point numeric-edited item:
Ifthe algebraic value ofthe sending operand is farther from zero than is permitted by the usage specifications of the receiving data item; the EC-DATA-OVERFLOW exception condition is set to exist; and the content ofthe receiving data item is undefined.
If the algebraic value of the sending operand is nearer to zero than is permitted by the data description ofthe receiving operand,the numeric value is treated as zero.
@ISO/IEC 2023
669
[per-copy licence stamp]


--- Page 700 ---

ISO /IEC 1989.2023 (E)
Alignment of the numeric value by decimal point; any necessary zero filling, any truncation of digits, and transfer of the algebraic data into the receiving data item, take place as defined in 14.6.8, Alignment and transfer of data into data items:
7) Alphanumeric, boolean, national, and numeric literals belong to the categories alphanumeric, boolean, national, and numeric, respectively. The category of figurative constants when used in the MOVE statement depends on the category of the receiving operand as shown in Table 17, Category of figurative constants used in the MOVE statement
670
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 701 ---

ISO /IEC 1989.2023 (E)
Table 17 Category of figurative constants used in the MOVE statement Category of Category of Figurative constant receiving operand figurative constant ALL literal, where literal is: Alphanumeric Alphanumeric Boolean Boolean National National ALL symbolic character; where symbolic character is: Alphanumeric Alphanumeric National National HIGH-VALUE, HIGH-VALUES; Alphabetic Alphanumeric LOw-VALUE, LOW-VALUES; Alphanumeric Alphanumeric QUOTE, QUOTES Alphanumeric-edited Alphanumeric National National National-edited National Numeric Alphanumeric Numeric-edited if usage is display Alphanumeric if usage is national National SPACE, SPACES, ZERO, ZEROS_ Alphabetic Alphanumeric ZEROES Alphanumeric Alphanumeric Alphanumeric-edited Alphanumeric Boolean Boolean National National National-edited National
indicates the figurative constant category does not depend on the category of the receiving operand
8) If the sending or receiving item is a dynamic-length elementary item; the current content of the dynamic-length elementary item is moved or changed as specified in 8.5.1.10.4, Operations on dynamic-length elementary items_
9) Ifboth the sending operand and the receiving data itemare group itemsand one orboth isa variable- length group, the following rules apply:
a) If the groups are of equal length:
The content of each character position that is not occupied by a corresponding table is moved to the corresponding character position in the receiving group
Where two tables correspond, as specified in 8.5.1.12.2, Positional correspondence, the table in the sending group is moved to the corresponding table in the receiving group, as specified in 14.6.9.2,Moving a table:
b) If the groups are of unequal length:
@ISO/IEC 2023
671
[per-copy licence stamp]


--- Page 702 ---

ISO /IEC 1989.2023 (E)
If the sending group is longer than the receiving group, the character positions that occupy the excess part are ignored by the operation:
If the sending group is shorter than the receiving group, each location that occupies the excess part is space filled, according to the following recursive procedure:
If the data item to be space-filled is a dynamic-length elementary item, the length of the receiving operand is set to zero
If the data item to be space filled is a dynamic-capacity table, it is space filled according to 14.6.9.4, Space filling dynamic table:
3. All other character positions are filled with space characters_
The common part ofthe variable-length groups is now moved according to the rules for variable- length groups of equal length, as defined in General rule 9a above
NOTE 4 Ifthe receiving table is an occurs-depending table,it is the programmer's responsibility to store an appropriate value in the data item associated with the DEPENDING phrase independently ofthe MOVE statement; because this data item is not changed by the operation.
10) Additional rules and explanations relative to this statement are given in 14.6.10, Overlapping operands
FORMAT 2
11) Data items within identifier-3 are selected to be moved to selected data items within identifier-4 according to the rules specified in 14.7.6, CORRESPONDING phrase The results are the same as ifthe user had referred to each pair of corresponding identifiers in separate MOVE statements exceptthat if subscripting is specified for identifier-3 0r identifier-4,the subscript value used is that resulting from the evaluation of the subscript at the start of the execution of the statement:
NOTE 5 For purposes of MOVE CORRESPONDING,bit group items and national group items are processed as group items, rather than as elementary items
672
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 703 ---

ISO /IEC 1989.2023 (E)
14.9.26 MULTIPLY statement
14.9.26.1 General
The MULTIPLY statement causes numeric data items t0 be multiplied and sets the values of data items equal to the results_
14.9.26.2 General formats
Format 1 (by):
MULTIPLY
identifier-1 literal-1
BY identifier-2 rounded-phrase ]
ON SIZE ERROR imperative-statement-1 NOT ON SIZE ERROR imperative-statement-2 END-MULTIPLY
Format 2 (giving):
identifier-1 literal-1
identifier-2 BY literal-2
MULTIPLY
GIVING { identifier-3 rounded-phrase ] }
ON SIZE ERROR imperative-statement-1 NOT ON SIZE ERROR imperative-statement-2 END-MULTIPLY
where rounded-phrase is described in 14.7.4, ROUNDED phrase:
14.9.26.3 Syntax rules
1) Identifier-1 and identifier-2 shall reference a data item of category numeric:
2) Identifier-3 shall reference a data item of category numeric or numeric-edited.
3) Literal-1 and literal-2 shall be numeric literals
4) When native arithmetic is in effect, the composite of operands described in 14.7.7, Arithmetic statements, is determined by using all of the operands in the statement
@ISO/IEC 2023
673
[per-copy licence stamp]


--- Page 704 ---

ISO /IEC 1989.2023 (E)
14.9.26.4 General rules
1) When format 1 is used and native arithmetic is in effect;the initial evaluation consists of determining the multiplier, which is literal-1 or the value of the data item referenced by identifier-1. The multiplicand is the value of the data item referenced by identifier-2 The product of the multiplier and the multiplicand is stored as the new value of the data item referenced by identifier-2.
2) When format 2 is used and native arithmetic is in effect;the initial evaluation consists of determining the multiplier; which is literal-1 or the value of the data item referenced by identifier-1; determining the multiplicand, which is literal-2 or the value of the data item referenced by identifier-2; and forming the product ofthe multiplier and the multiplicand: The product is stored as the new value of each data item referenced by identifier-3.
3) When format 1 or 2 is used and standard-decimal, or standard-binary arithmetic is in effect; the product equals the result ofthe arithmetic expression
(multiplier multiplicand)
where the values for multiplier and multiplicand are as defined in General rules 1 and 2 for the respective formats
4) Additional rules and explanations relative to this statement are given in 14.6.13.2, Incompatible data; 14.7.4, ROUNDED phrase; 14.7.5, SIZE ERROR phrase and size error condition; and 14.7.7, Arithmetic statements.
674
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 705 ---

ISO /IEC 1989.2023 (E)
14.9.27 OPEN statement
14.9.27.1 General
The OPEN statement initiates the processing of files:
14.9.27.2 General format
INPUT OUTPUT 1-0 EXTEND
OPEN
sharing-phrase retry-phrase ] file-name-1 WITH NQ REWIND ] }
where sharing-phrase is:
ALL OTHER NO OTHER READ ONLY
SHARING WITH
where retry-phrase is described in 14.7.9,RETRY phrase
14.9.27.3 Syntax rules
1) The OPEN statement for a report file shall not contain the INPUT phrase or the [-0 phrase:
2) The EXTEND phrase shall be specified only ifthe access mode ofthe file connector referenced by file- name-1 is sequential and the LINAGE clause is not specified in the file description entry for file- name-1.
3) An OPEN statement that specifies file-name-1 more than once shall not be specified in imperative- statement-1 ofan exception-checking PERFORM statement:
4) The files referenced in the OPEN statement need not all have the same organization or access:
5) The NO REWIND phrase may be specified only for sequential files.
The NO REWIND phrase may be specified only when the INPUT or OUTPUT phrase is specified.
7) The sharing phrase shall not be specified for a file subject to an APPLY COMMIT clause_
8) When file-name-1 is not subject to an APPLY COMMIT clause, then if the sharing phrase is omitted from the OPEN statement and the ALL phrase is specified in the SHARING clause of the file control entry for file-name-1 or ifthe ALL phrase is specified on the OPEN statement,the LOCK MODE clause shall be specified in the file control entry for file-name-1.
@ISO/IEC 2023
675
[per-copy licence stamp]


--- Page 706 ---

ISO /IEC 1989.2023 (E)
NOTE Files subject to an APPLY COMMIT clause already have an implicit LOCK mode clause.
9) The [-0 phrase shallnot be specified ifthe FORMAT clause is specified in the file description entry for file-name-1.
14.9.27.4 General rules
1) The execution of the OPEN statement causes the value of the [-0 status associated with file-name-1 to be updated to one of the values in 9.1.13,1-0 status
2) The file connector referenced by file-name-1 shall not be open. Ifitis open,the execution ofthe OPEN statement is unsuccessful and the [-0 status associated with file-name-1 is set to '41'.
3) Ifthe file associated with file-name-1 is present and insufficient authority exists to open the file, the execution of the OPEN statement is unsuccessful, and the -0 status value in the file connector referenced by file-name-1 is set to '37'
4) The successful execution of an OPEN statement associates the file connector referenced by file- name-1 with a file ifthe file is available,and sets the open mode ofthe file connector to input; output; 1-0, or extend, depending on the keywords INPUT, OUTPUT, 1-0 or EXTEND specified in the OPEN statement The open mode determines the input-output statements that are allowed to reference the file connector as shown in Table 20,Permissible [-0 statements by access mode and open mode.
A file is available ifit is physically presentand is recognized by the operating environment Table 18 Opening available and unavailable files (file not currently open), shows the results of opening available and unavailable files that are not currently open by another file connector Table 19, Opening available shared files that are currently open by another file connector, shows the results of opening available files that are currently open by another file connector; including those implicitly opened by the SORT and MERGE statements
Table 18
Opening available and unavailable files (file not currently open)
Open mode INPUT
File is available
File is unavailable
Normal open Normal open
Open is unsuccessful Normal open; the first read causes the at end condition or invalid key condition Open is unsuccessful Open causes the file to be created
INPUT (optional file)
1-0
Normal open Normal open
1-0 (optional file)
OUTPUT
Normal open; the file contains Open causes the file to be no records created Normal open Open is unsuccessful Normal open Open causes the file to be created
EXTEND
EXTEND (optional file)
676
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 707 ---

ISO /IEC 1989.2023 (E)
Table 19 _ Opening available shared files that are currently open by another file connector Most restrictive existing sharing mode and open mode sharing sharing with sharing with with read only all other no other extend extend input extend input 1-0 1-0 1-0 Open request input output output output EXTEND SHARING 1-0 Unsuccessful WITH NO INPUT open Unsuccessful Unsuccessful Unsuccessful Unsuccessful OTHER OUTPUT open open open open
EXTEND 1-0 SHARING INPUT WITH READ OUTPUT ONLY
Unsuccessful Unsuccessful open open
Unsuccessful Unsuccessful Normal open open open
Unsuccessful Unsuccessful Normal Unsuccessful Normal open open open open open Unsuccessful Unsuccessful Unsuccessful Unsuccessful Unsuccessful open open open open open
EXTEND 1-0 SHARING INPUT WITH ALL OTHER OUTPUT
Unsuccessful Unsuccessful Unsuccessful Normal open open open open
Normal open
Unsuccessful Normal open open
Normal open
Normal open
Normal open Unsuccessful Unsuccessful Unsuccessful Unsuccessful Unsuccessful open open open open open
5) The successful execution of an OPEN statement makes the associated record area available to the runtime element: Ifthe file connector associated with file-name-1 is an external file connector, there is only one record area associated with the file connector for the run unit:
When a file connector is not open, no statement shall be executed that references the associated file- name, either explicitly or implicitly, except for a MERGE or SORT statement with the USING or GIVING phrase, the COMMIT and ROLLBACK statements, a DELETE FILE statement; 0r an OPEN statement:
7) The OPEN statement for report file connector shall be executed before the execution of an INITIATE statement that references a report-name that is associated with file-name-1.
8) For the file connector referenced by file-name-1,an OPEN statement shall previously be successfully executed for that file connector and the file connector shall be in an open mode at the time of the execution of any other permissible input-output statement referencing that file connector: In Table
@ISO/IEC 2023
677
[per-copy licence stamp]


--- Page 708 ---

ISO /IEC 1989.2023 (E)
20, Permissible I-0 statements by access mode and open mode; 'X' at an intersection indicates that the specified statement;used in the access mode given for that row, may be used with the open mode given at the top ofthe column.
678
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 709 ---

ISO /IEC 1989.2023 (E)
Table 20 _ Permissible [-0 statements by access mode and open mode
Open mode Output F-0
Access mode
Statemen
Input
Extend
READ
X
X
WRITE
X
Sequential
REWRITE
X X
START
X
Sequential (relative DELETE and indexed files only) RECORD
READ
WRITE
'
Random
REWRITE
START
DELETE RECORD
X
READ
X
WRITE
7
' X X
Dynamic
REWRITE
START
DELETE RECORD
9) Execution ofthe OPEN statement does not obtain or release the first record_
10) During the execution ofthe OPEN statement when the file connector is matched with the file and the file exists, the attributes of the file connector as specified in the file control paragraph and the file description entry are compared with the fixed file attributes of the file. If the attributes don't match, a file attribute conflict condition occurs, the execution ofthe OPEN statement is unsuccessful,and the 1-0 status associated with file-name-1 is set to '39' The implementor defines which of the fixed-file attributes are validated during the execution of the OPEN statement: The validation of fixed-file attributes may vary depending on the organization or storage medium of the file (See 9.1.6, Fixed file attributes:)
11) The NO REWIND phrase will be ignored ifit does not apply to the storage medium on which the file resides. If the NO REWIND phrase is ignored, the OPEN statement is successful and the [-0 status associated with file-name-1 is set to '07'.
12) Ifthe storage medium for the file permits rewinding the following rules apply:
@ISO/IEC 2023
679
[per-copy licence stamp]


--- Page 710 ---

ISO /IEC 1989.2023 (E)
a) When neither the EXTEND, nor the NO REWIND phrase is specified, execution of the OPEN statement causes the file to be positioned at its beginning:
b) When the NO REWIND phrase is specified, execution of the OPEN statement does not cause the file to be repositioned; the file shall be already positioned at its beginning prior to execution of the OPEN statement
13) If the file is not present,and the INPUT phrase is specified in the OPEN statement,and the OPTIONAL clause is specified in the file control entry for file-name-1, the file position indicator in the file connector referenced by file-name-L is set to indicate that an optional input file is not present
14) When the organization of the file referenced by file-name-1 is sequential or relative and the INPUT or [-0 phrase is specified in the OPEN statement; the file position indicator for that file connector is setto 1. When the organization is indexed,the file position indicator is set to the characters that have the lowest ordinal position in the collating sequence associated with the file, and the prime record key is established as the key of reference:
15) When the EXTEND phrase is specified, the OPEN statement positions the file immediately after the last logical record for that file The last logical record for a sequential file is the last record written in the file. The last logical record for a relative file is the currently existing record with the highest relative record number. The last logical record for an indexed file is the currently existing record with the highest prime key value:
16) Ifthe [-0 phrase is specified,the file shall supportthe input and output statements thatare permitted for the organization of that file when opened in the [-0 mode: If the file does not support those statements, the [-0 status value for file-name-1 is set to '37' and the execution ofthe OPEN statement is unsuccessful: The successful execution of an OPEN statement with the 1-0 phrase sets the open mode ofthe file connector referenced by file-name-1 to open in the [-0 mode
17) Ifthe file is not present; and the EXTEND or [-0 phrase is specified in the OPEN statement; and the OPTIONAL clause is specified in the file control entry for file-name-1,the OPEN statement creates the file This creation takes place as ifthe following statements were executed in the order shown:
OPEN OUTPUT file-name-1. CLOSE file-name-1_
These statements are followed by execution of the OPEN statement specified in the source element and the [-0 status value associated with file-name-1 is set t05'
18) If the OUTPUT phrase is specified, the successful execution of the OPEN statement creates the file_ After the creation ofthe file, the file contains no records If physical pages have meaning for the file, the positioning of the output medium with respect to physical page boundaries is implementor- defined following the successful execution ofthe OPEN statement; whether or not the LINAGE clause is specified in the file description entry of file-name-1.
19) Upon successful execution ofthe OPEN statement; the current volume pointer is set:
a) To point to the first or only reel/unit in the physical file if INPUT or I-0 is specified.
680
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 711 ---

ISO /IEC 1989.2023 (E)
b) To point to the reel/unit containing the last record in the physical file if EXTEND is specified:
C) To point to the newly created reel/unit in the physical file for an unavailable file if EXTEND, [-0, or OUTPUT is specified.
20) If more than one file-name is specified in an OPEN statement; the result of executing this OPEN statement is the same as if a separate OPEN statement had been written for each file-name in the same order as specified in the OPEN statement: These separate OPEN statements would each have the same open mode specification, the sharing-phrase, retry-phrase, and REWIND   phrase as specified in the OPEN statement: If an implicit OPEN statement results in the execution of a declarative procedure that executes a RESUME statement with the NEXT STATEMENT phrase, processing resumes at the next implicit OPEN statement; ifany.
21) The SHARING phrase is effective only for files that are shareable_
22) The SHARING phrase specifies the level of sharing permitted for the physical file associated with file- name-1 and specifies the operations that may be performed on the physical file through other file connectors sharing the physical file, as indicated in 9.1.15,Sharing mode:
23) The SHARING phrase overrides any SHARING clause in the file control entry of file-name-1. Ifthere is no SHARING phrase on the OPEN statement; then file sharing is completely specified in the file control entry. If neither a SHARING phrase on the OPEN statement nor a SHARING clause in the file control entry is specified,the implementor shall define the sharing mode thatis established for each file connector;
24) The RETRY phrase is used to control the behavior of an OPEN statement when the open mode or sharing mode requested conflicts with those of other file connectors that are currently associated with the physical file: The [-0 status is set in accordance with the rules in 14.7.9,RETRY phrase:
25) If the execution of the OPEN statement is unsuccessful, the file is not affected and the following actions take place in the following order:
a) A value is placed in the 1-0 status associated with file-name to indicate the condition that caused the OPEN statement to be unsuccessful:
b) If it is enabled, the level-3 EC-[-0 exception condition associated with the [-0 status value is set to exist:
c) the OPEN statement is   specified in imperative-statement-1 in an exception-checking PERFORM statement and there is an applicable WHEN phrase in that PERFORM statement; then execution continues according to the rules for that WHEN phrase No applicable USE declarative is executed.
d) Any applicable USE FOR EXCEPTION or USE AFTER EXCEPTION procedure is executed as specified for the rules for the USE statement:
26) Additional rules affecting the execution ofthe OPEN statementare given in 12.4.5,File control entry, General rules 3 and 4_
@ISO/IEC 2023
681
[per-copy licence stamp]


--- Page 712 ---

ISO /IEC 1989.2023 (E)
14.9.28 PERFORM statement
14.9.28.1 General
The PERFORM statement is used t0 transfer control explicitly t0 one or more procedures and t0 return control implicitly whenever execution ofthe specified procedure is complete: The PERFORM statement is also used to control execution of one or more imperative statements that are within the scope of that PERFORM statement with or without exception checking within those statements_
14.9.28.2 General formats
Format 1 (out-of-line):
times-phrase until-phrase varying-phrase
THROUGH THRU
PERFORM procedure-name-1
procedure-name-2
Format 2 (inline)=
times-phrase PERFORM until-phrase imperative-statement-1 END-PERFORM varying-phrase
Format 3 (exception-checking):
PERFORM WITH LOCATION imperative-statement-1
{file-name-1} _ INPUT OUTPUT
EXCEPTION
WHEN
imperative-statement-2
IO EXTEND
exception-name-1 exception-name-2 FILE file-name-2 }
WHEN OTHER EXCEPTION imperative-statement-3 ] WHEN COMMON EXCEPTION imperative-statement-4 ] FINALLY imperative-statement-5 ] END-PERFORM
682
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 713 ---

ISO /IEC 1989.2023 (E)
where times-phrase is:
identifier-1 integer-1
TIMES
where until-phrase is:
BEFORE AFTER
condition-1 UNTIL EXIT
WITH TEST
where varying-phrase is:
BEFORE AFTER
WITH TEST
identifier-3 index-name-2 literal-1
VARYING
identifier-2 index-name-1
FROM
identifier-4 literal-2
UNTIL condition-1
identifier-6 index-name-4 literal-3
AFTER
identifier-5 index-name-3
FROM
identifier-7 literal-4
UNTIL condition-2
14.9.28.3 Syntax rules
FORMATS 1 AND 2
1) If neither the TEST BEFORE nor the TEST AFTER phrase is specified, the TEST BEFORE phrase is assumed.
2) Each identifier shall reference a numeric elementary item described in the data division. Identifier-1 shall be an integer:
3) Each literal shall be numeric
4) If an index-name is specified in the VARYING or AFTER phrase, then:
a) The identifier in the associated FROM and BY phrases shall reference an integer data item:
b) The literal in the associated FROM phrase shall be a positive integer:
C) The literal in the associated BY phrase shall be a nonzero integer_
@ISO/IEC 2023
683
[per-copy licence stamp]


--- Page 714 ---

ISO /IEC 1989.2023 (E)
5) If an index-name is specified in the FROM phrase, then:
a) The identifier in the associated VARYING or AFTER phrase shall reference an integer data item
b) The identifier in the associated BY phrase shall reference an integer data item
c) The literal in the associated BY phrase shall be an integer.
6) The literal in the BY phrase shall not be zero:
7) Condition-1, condition-2, may be any conditional expression: (See 8.8.4,Conditional expressions:)
8) The UNTIL EXIT phrase shall not be specified in a PERFORM statement with 0r under a PERFORM statement with the VARYING phrase or either the TEST BEFORE or TEST AFTER phrase
9) At least six AFTER phrases shall be permitted in varying-phrase_
FORMAT 1
10) The words THROUGH and THRU are equivalent
11) When procedure-name-1 and procedure-name-2 are both specified and either is the name of a procedure in the declaratives portion of the procedure division, both shall be procedure-names in the same declarative section.
12) Procedure-name-1 shall be the name of either a paragraph or a section in the same source element as that in which the PERFORM statement is specified.
13) Procedure-name-2 shall be the name of either a paragraph or a section in the same source element as that in which the PERFORM statement is specified:
FORMAT 3
14) If file-name-1 or file-name-2 is specified in a WHEN phrase, it shall not be specified more than once in any of the WHEN phrases within the scope of a format 3 PERFORM statement unless all such instances are 'specified in conjunction with an exception-name
15) All instances of an exception-name shall be specified only once in a format 3 PERFORM statement unless done s0 in conjunction with different file-names:
16) If file-name-2 is specified, exception-name-2 shall begin with the COBOL characters 'EC-|-0' .
14.9.28.4 General rules
ALL FORMATS
1) The range ofa PERFORM statement consists logically of all those statements that are executed as a result of executing the PERFORM statement through execution of the implicit transfer of control to the end ofthe PERFORM statement The range includes all statements that are executed asthe result
684
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 715 ---

ISO /IEC 1989.2023 (E)
of a transfer of control in the range of the PERFORM statement, except for statements executed as the result ofa transfer of control by an EXIT PROGRAM or GOBACK statement specified in the same instance of the same source element as the PERFORM statement Declarative procedures that are executedasa result ofthe execution ofstatements in therange ofa PERFORM statementare included in the range of the PERFORM statement The statements in the range ofa PERFORM statement need not appear consecutively in the source element;
2) The results of executing the following sequence of PERFORM statements are undefined and no exception condition is set to exist when the sequence is executed:
a) a PERFORM statement is executed and has not yet terminated,then
b) within the range of that PERFORM statement another PERFORM statement is executed, then
C) the execution of the second PERFORM statement passes through the exit of the first PERFORM statement.
NOTE 1 On some implementations it causes stack overflows, on some it causes returnsto unlikely places, and on other implementations other actions can occur: Therefore, the results are unpredictable and are unlikely to be portable
FORMATS 1 AND 2
3) If an index-name is specified in the VARYING or AFTER phrase, and an identifier is specified in the associated FROMphrase,atthe time the data item referenced by the identifier is used to initialize the index associated with the index-name, the data item shall have a positive value. Ifthe data item does not have a positive value, the EC-RANGE-PERFORM-VARYING exception condition is set to exist;
4) An inline PERFORM statementand an out-of-line PERFORM statement function identically according to the following rules_ For an out-of-line PERFORM statement; the specified set of statements consists ofall statements beginning with the first statement of procedure-name-1 and ending with the last statement of procedure-name-2,Or, if procedure-name-2 is not specified, the last statement of procedure-name-1. For an inline PERFORM statement; the specified set of statements consists of all statements contained within the PERFORM statement:
5) When the PERFORM statement is executed, control is transferred to the first statement of the specified set of statements except as indicated in General rules 9, 10,and 11. For those cases where transfer of control to the specified set of statements does take place,an implicit transfer of control to the end of the PERFORM statement is established as follows:
a) If procedure-name-2 is not specified, the return mechanism is after the last statement of procedure-name-1.
b) If procedure-name-2 is specified,the return mechanism is after the last statement of procedure- name-2.
There is no necessary relationship between procedure-name-1 and procedure-name-2 except that a consecutive sequence of  operations is to be executed beginning at the   procedure named procedure-name-1 and ending with the execution ofthe procedure named procedure-name-2_
@ISO/IEC 2023
685
[per-copy licence stamp]


--- Page 716 ---

ISO /IEC 1989.2023 (E)
NOTE 2 Statements such as the GO TO statement; the PERFORM statement; and the procedure format ofthe EXIT statement can occur in the flow of execution of the specified set of statements, however the flow of execution should eventually pass to the end of procedure-name-2_
7) If control passes to the specified set of statements by means other than PERFORM statement control will pass through the last statement of the set to the next executable statement as if no PERFORM statement referenced the set;
8) A PERFORM statement without times-phrase, until-phrase, or varying-phrase is the basic PERFORM statement: The specified set of statements referenced by this type of PERFORM statement is executed once and then control passes to the end of the PERFORM statement
9) If times-phrase is specified, the specified set of statements is performed the number of times specified by integer-1 or by the value of the data item referenced by identifier-1 at the start of the execution of the PERFORM statement; If at the start of the execution of a PERFORM statement, the value ofthe data item referenced by identifier-1 is equal to zero or is negative, control passes to the end of the PERFORM statement:  Following the execution of the specified set of statements the specified number of times, control is transferred to the end of the PERFORM statement
NOTE 3 During execution of the PERFORM statement; a change to the contents of identifier-1 does not alter the number of times the specified set of statements is performed.
10) If until-phrase with condition-1 is specified, the specified set of statements is performed until the condition specified by the UNTIL phrase is true. When the condition is true, control is transferred to the end ofthe PERFORM statement. Ifthe condition is true when the PERFORM statementis entered, and the TEST BEFORE phrase is specified or implied, no transfer t0 the specified set of statements takes place, and control is passed to the end of the PERFORM statement: Ifthe TEST AFTER phrase is specified,the PERFORM statement functions as if the TEST BEFORE phrase were specified except that the condition is tested after the specified set of statements has been executed: Item identification associated with the operands specified in condition-1 is done each time the condition is tested:
11) If the until-phrase with the EXIT reserved word is specified, execution proceeds exactly as if the same PERFORM statement were coded but with condition-1 specified except that condition-1 never evaluates as true
NOTE 4 When UNTIL EXIT is specified,it is the programmer's responsibility to ensure thatan "escape" from the PERFORM loop will be reached For an inline PERFORM, this can be done by an EXIT PERFORM (but not EXIT PERFORM CYCLE) statement: For an out-of-line PERFORM this can be done by a GOBACK or STOP statement It is also the programmers responsibility to take care that the escape statement that they use does actually escape the PERFORM loop. Several statements appear to do so, but don't actually escape the loop. For example,an EXIT PARAGRAPH (from a performed paragraph) or an EXIT SECTION (from a performed section) do not escape a PERFORM with the UNTIL EXIT phrase.
12) If varying-phrase is specified, the execution of the PERFORM statement augments the data items referenced by one or more identifiers 0r the indexes referenced by one or more index-names in an orderly fashion: In the following rules, the data items referenced by identifier-2 and identifier-5 and the indexes referenced by index-name-1 and index-name-3 are referred to as the induction variables. The content of the data item  referenced by the identifier, the occurrence number corresponding to the value of the index referenced by the index-name, or the value of the literal
686
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 717 ---

ISO /IEC 1989.2023 (E)
referenced in the FROM phrase is referred to as the initialization value: The content ofthe data item referenced by the identifier or the value of the literal in a BY phrase is referred t0 as the augment value: For any BY phrase that is omitted, the augment value is 1. Item identification for identifier-2, identifier-5, index-name-1, or  index-name-3 is done each  time the content of the data item referenced by the identifier or the value of the index referenced by the index-name is set or augmented. Item identification for identifier-3,identifier-4, identifier-6, identifier-7, index-name-2, and index-name-4 is done each time the content of the data item referenced by the identifier or the index referenced by the index-name is used in a setting or augmenting operation. Item identification associated with the operands specified in condition-1 or condition-2 is done each time the condition is tested.
NOTE 5 If an augment value is less than 0, the induction variable is actually decremented by the absolute value ofthe augment value_
13) The sequence of operation of the PERFORM statement is as follows=
a) All induction variables are set to their associated initialization values in the left-to-right order in which the induction variables are specified:
b) If the TEST AFTER phrase is specified, and there is no AFTER phrase, the specified set of statements is executed once and condition-1 is tested, If the condition is false, the induction variable is incremented by the augment value, and the specified set of statements is executed again: The cycle , continues until condition-1 is tested and found to be true,at which point control is transferred to the end of the PERFORM statement; At that point; the induction variable contains the value it contained at the completion of the execution of the specified set of statements_
c) If the TEST AFTER phrase is specified, and there is one or more AFTER phrase, the following occurs:
The specified set of statements is executed.
The rightmost condition-2 is then evaluated.
If the rightmost condition-2 is false, the associated induction variable is incremented by the associated augment value, and execution proceeds with step 13 a.
If the last condition evaluated is true, the condition to its left is evaluated: This is repeated until either a false condition is found or the last condition evaluated is condition-1 and condition-1 is true. If a false condition is found, the induction variable associated with that condition is incremented by theassociated augment value,all induction variablesto the right of the false condition are set to their initialization values, and execution proceeds with step 13 a. If no condition is found to be false, control is transferred t0 the end of the PERFORM statement:
NOTE 6 After successful execution of the PERFORM statement; all induction variables contain the values they had at the completion of the last execution of the specified set of statements
d) Ifthe TEST AFTER phrase is not specified and there is no AFTER phrase, condition-1 is evaluated, and if it is true, control is transferred to the end of the PERFORM statement: If it is false, the
@ISO/IEC 2023
687
[per-copy licence stamp]


--- Page 718 ---

ISO /IEC 1989.2023 (E)
specified set of statements is executed. Then, the induction variable is incremented by the augment value, and condition-1 is evaluated again: When control is passed to the end of the PERFORM statement; the induction variable contains the value it contained when condition-1 was evaluated.
e) If the TEST AFTER phrase is not specified, and there is one or more AFTER phrase, the following occurs:
Condition-1 is evaluated,
If condition-1 is true, control is transferred to the end ofthe PERFORM statement; otherwise, the condition-2 immediately to the right becomes the current condition:
The current condition is evaluated:
If the current condition is true:
the induction variable associated with the current condition is set to its initialization value,and
the condition to the left ofthe current condition becomes the current condition, and
the induction variable associated with the new current condition is incremented by its associated augment value, and
if the current condition is condition-1 execution proceeds to step a, else execution proceeds to the beginning of step 13 b;
otherwise:
if there is another AFTER phrase to the right of the current condition:
the condition associated with that AFTER phrase becomes the current condition, and
execution proceeds to the beginning of step 13 b;
otherwise:
the specified set of statements is executed, and
the induction variable associated with the current condition is incremented by the augment value,and
execution proceeds to the beginning of step 13 b.
NOTE 7 After successful execution ofthe PERFORM statement;all induction variables contain the values they had at the completion of the last evaluation of condition-l_ With the exception of the induction variable associated with condition-1,these values are the same as they were at the last execution of the specified set of statements, or are their associated initialization values ifno statements were executed, If no statements were executed, the induction variable associated with condition-1 contains its associated
688
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 719 ---

ISO /IEC 1989.2023 (E)
initialization value; otherwise, the induction variable associated with condition-1 contains the value it contained after the last execution ofthe specified set of statements, incremented by the augment value_
During the execution of the specified set of statements associated with the PERFORM statement all changes to the induction variable; the variables associated with the augment value, and the variables associated with the initialization value have immediate effect and all subsequent references to the associated data items use the updated contents
FORMAT 3
14) If checking for exception-name-1 or exception-name-2 is not enabled for imperative-statement-1 by a TURN directive, an implicit TURN directive for exception-name-1 or exception-name-2 is assumed before the first statement in imperative-statement-1. If LOCATION is specified, that implicit TURN directive contains LOCATION. If the WHEN OTHER phrase is used, only those exception conditions that are enabled at the point at which they are detected are processed by the WHEN OTHER phrase: Within imperative-statement-1 any TURN directive that turns off checking for an exception that is specified in a WHEN phrase will prevent that WHEN phrase from being invoked if that exception condition would otherwise have been set to exist An implicit PUSH ALL followed by TURN OFF ALL is assumed at the end ofimperative-statement-1. Immediately preceding the END PERFORMphrase, there is an implicit POP ALL followed by an implicit TURN directive with OFF specified for any exception conditions that were implicitly turned on before the first statement in imperative- statement-1.
15) When the PERFORM statement is executed, control is transferred to imperative-statement-1. The specified set of statements for a format 3 PERFORM statement consists of all statements contained within imperative-statement-1. However; other statements may be executed depending on whether or not an exception was raised.
16) If the FINALLY phrase is specified, the end of the PERFORM statement begins at imperative- statement-5. There shall be no statements that include a transfer of control out of the PERFORM statement within imperative-statement-5. Any EXIT PERFORM statement within   imperative- statement-5 transfers control to an implicit CONTINUE statement following the END-PERFORM: If the FINALLY phrase is not specified, the end of the PERFORM statement is indicated by END- PERFORM:
17) If during the execution of imperative-statement-1 an exception condition associated with a WHEN phrase is raised, imperative-statement-2 is executed: The rules for  determining match are specified in General rules 3a to 3g of the USE statement At the completion of the execution of imperative-statement-2, control is passed as indicated in General rule 20 Or, if WHEN COMMON is specified, to imperative-statement-4. Any USE declarative that would normally match the exception condition is ignored.
18) If during the execution ofimperative-statement-Lany exception condition thatis notassociated with any exception condition specified in a WHEN phrase is raised and there is a WHEN OTHER phrase, imperative-statement-3 is executed. At the completion of the execution of imperative-statement-3, control is passed to the end of the PERFORM statement Or , if WHEN COMMON is specified, to imperative-statement-4 At the completion of the execution of imperative-statement-3, control is passed as indicated in General rule 14.9.29 Or, if WHEN COMMON is specified, to imperative- statement-4_ Any USE declarative that would normally match the exception condition is ignored:
@ISO/IEC 2023
689
[per-copy licence stamp]


--- Page 720 ---

ISO /IEC 1989.2023 (E)
19)If WHEN COMMON is specified, imperative-statement-4 is executed. At the completion of the execution of imperative-statement-4, control is passed as indicated in General rule 20.
20) If execution of the last statement in imperative-statement-1 completes successfully, execution proceeds to the end of the PERFORM statement: If an exception-condition was raised during the processing of imperative-statement-1 that caused a transfer to the imperative-statement in a WHEN phrase, and the processing in General rules 17, 18,and 19 is completed, return depends on whether the exception condition was fatal or nonfatal: If the exception condition was nonfatal, execution continues with an implicit CONTINUE statement immediately following the statementin imperative- statement-1 in which the exception condition occurred: If that statement was the last statement in imperative-statement-1, execution continues at the end ofthe PERFORM statement Ifthe exception condition was fatal, execution continues as specified in 14.6.13.1.3,Fatal exception conditions
NOTE 8 The end of the PERFORM statement includes the statements in a FINALLY phrase, ifit is specified.
21) Any exception  conditions raised during the execution of imperative-statement-2, imperative- statement-3,imperative-statement-4,or imperative-statement-5 will not cause transfer of controlto any ofthese imperative-statements The results are as ifthese statements were specified in a format 2 PERFORM statement
22) Atthe completion ofthe execution ofthe PERFORM statement; IfWHEN is specified and an exception condition was raised and checking for that exception condition was enabled by a TURN directive before the execution of the PERFORM statement; that checking remains enabled  If there is a TURN directive within the range of the PERFORM statement; the checking for that TURN directive is retained: Otherwise, any checking for an exception condition specified in a WHEN phrase is not enabled.
NOTE 9 If control is transferred outside of the PERFORM during WHEN processing exceptions can occur: These will be processed by whatever mechanism is normally used for this processing (such as a USE or TURN directive that can cause transfer of control to some place in the program that would never hit the exit of this PERFORM or a CALL to a program that raises an exception): The user is advised to avoid transfers outside of the PERFORM for WHEN processing:
690
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 721 ---

ISO /IEC 1989.2023 (E)
14.9.29 RAISE statement
14.9.29.1 General
The RAISE statement causes a specified exception condition to be raised:
14.9.29.2 General format
RAISE
EXCEPTION exception-name-1 identifier-1
14.9.293 Syntax rules
1) Exception-name-1 shall be a level-3 exception-name as specified in 14.6.13.1, Exception conditions_
2) Identifier-1 shall be an object reference; the predefined object references NULL and SUPER shall not be specified:
3) Identifier-1 is a sending operand.
4) Within an exception-checking PERFORM statement; the RAISE statement shall not be specified in any imperative statement other than imperative-statement-1_
14.9.29.44 General rules
1) If exception-name-1 is specified, the associated exception condition is raised, and EXCEPTION- OBJECT is set to null. Execution continues as specified in 14.6.13,Exception condition handling:
NOTE For fatal exception conditions, it is likely that the run unit will be terminated For nonfatal exception conditions where there are no applicable exception processing procedures, the RAISE statement acts a a CONTINUE statement:
2) Ifidentifier-1 is specified, EXCEPTION-OBJECT is set to reference the object referenced by identifier- 1. Ifthere is no applicable declarative, processing continues with the statement following the RAISE statement.
@ISO/IEC 2023
691
[per-copy licence stamp]


--- Page 722 ---

ISO /IEC 1989.2023 (E)
14.9.30 READ statement
14.9.30.1 General
For sequential access, the READ statement makes available the next logical record from a file. For random access, the READ statement makes available a specified record from a mass storage file:
14.9.30.2 General formats
Format 1 (sequential):
NEXT READ file-name-1 RECORD INTO identifier-1 ] PREVIOUS
ADVANCING ON LOCK IGNORING LOCK retry-phrase
WITH LOCK WITH NO LOCK
AT END imperative-statement-1 NOT AT END imperative-statement-2 END-READ ]
Format 2 (random):
READ file-name-1 RECORD [ INTO identifier-1 ]
IGNORING LOCK retry-phrase
WITH LOCK WITH NO LOCK
data-name-1 record-key-name-1
KEY IS
INVALID KEY imperative-statement-3 NOT INVALID KEY imperative-statement-4 END-READ ]
where retry-phrase is described in 14.7.9,RETRY phrase
692
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 723 ---

ISO /IEC 1989.2023 (E)
14.9.30.33 Syntax rules
ALL FORMATS
1) The INTO phrase may be specified in a READ statement:
a) If no record  description entry or   only one record description is   subordinate to the file description entry, or
b) If the data item referenced by identifier-1 and all record-names associated with file-name-1 describe an alphanumeric group item or an elementary item of category alphanumeric or category national:.
2) If identifier-1 is a strongly-typed group item,; there shall be at most one record area subordinate to the FD for file-name-1. This record area, if specified, shall bea strongly-typed group item ofthe same type as identifier-1.
3) The LOCK phrase shall not be specified in the same READ statement as the IGNORING LOCK phrase:
4) Ifautomatic locking has been specified for file-name-1, none of the phrases IGNORING LOCK, WITH LOCK, or WITH NO LOCK shall be specified.
5) If file-name-1 is subject to an APPLY COMMIT clause, none of the phrases IGNORING LOCK, WITH LOCK, or WITH NO LOCK shall be specified:
FORMAT 1
None of the phrases ADVANCING, AT END, NEXT, NOT AT END, or PREVIOUS shall be specified if ACCESS MODE RANDOM is specified in the file control entry for file-name-1_
7) The phrase PREVIOUS shall not be specified if FILE ORGANIZATION LINE SEQUENTIAL is specified in the file control entry for file-name-1_
8) Ifneither the NEXT phrase nor the PREVIOUS phrase is specified and ACCESS MODE SEQUENTIAL is specified in the file control entry for file-name-1,the NEXT phrase is implied.
9) If neither the NEXT phrase nor the PREVIOUS phrase is specified and ACCESS MODE DYNAMIC is specified in the file control entry for file-name-1,the NEXT phrase is implied ifany ofthe following phrases is specified: ADVANCING, AT END, or NOT AT END:
FORMAT 2
10) The KEY phrase may be specified only if ORGANIZATION IS INDEXED is specified in the file control entry for file-name-1.
11) Data-name-1 or record-key-name-1 shall be specified in the RECORD KEY clause or an ALTERNATE RECORD KEY clause associated with file-name-1.
12) Data-name-1 or record-key-name-1 may be qualified.
@ISO/IEC 2023
693
[per-copy licence stamp]


--- Page 724 ---

ISO /IEC 1989.2023 (E)
14.9.30.4 General rules
ALL FORMATS
1) The execution of the READ statement causes the value of the [-0 status in the file connector referenced by file-name-1 to be updated as indicated in 9.1.13,[-0 status
2) The open mode of the file connector referenced by file-name-1 shall be input or [-0.Ifit is any other value, the execution of the READ statement is unsuccessful and the [-0 status value for file-name-1 is set to '47'_
3) When the logical records of a file are described with more than one record description, the content of any data item, where any part of that data item lies beyond the range of the current record, is undefined at the completion of the execution ofthe READ statement:
4) If the execution ofa READ statement with the INTO phrase is successful, the result is equivalent to the application of the following rules in the order specified:
a) The same READ statement without the INTO phrase is executed:
b) The current record is moved from the record area to thearea specified by identifier-1 according to the rules for the MOVE statement without the CORRESPONDING phrase. The size of the current record is determined by rules specified in the RECORD clause: If the file description entry contains a RECORD IS VARYING clause, the implied move is an alphanumeric group move Item identification of the data item referenced by identifier-1 is done after the record has been read and immediately before it is moved t0 the data item: The record is available in both the record area and the data item referenced by identifier-1.
NOTE 1 14.6.10, Overlapping operands, and 14.9.25,MOVE statement; General rules, apply to any cases in which the storage area identified by identifier-1 and the record area associated with file-name-1 share any part of their storage areas: The result of execution of the READ statement is undefined ifthe result of execution ofthe implicit MOVE statement described in General rule 4b is undefined
5) If the execution of a READ statement with the INTO phrase is unsuccessful, the content of the data item referenced by identifier-1 is unchanged and item identification of the data item referenced by identifier-1 is not done_
The execution of a READ statement with the INTO phrase when there are no record description entries subordinate to the file description entry proceeds as though there were one record description entry describing an alphanumeric group item of the maximum size established by the RECORD clause:
7) Whether record locking is in effect is determined by the rules specified in 12.4.5.9, LOCK MODE clause
8) Ifrecord locking is enabled for the file connector referenced by file-name-1 and the record identified for access by the general rules for the READ statement is locked by that file connector, the record lock is ignored and the READ operation proceeds as if the record were not locked.
694
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 725 ---

ISO /IEC 1989.2023 (E)
9) Ifrecord locking is enabled for the file connector referenced by file-name-1 and the record identified for access is locked by another file connector; the result of the operation depends on the presence or absence of the RETRY phrase. If the RETRY phrase is specified, additional attempts may be made to read the recordas specified in the rules in 14.7.9,RETRY phrase. Ifthe RETRY phrase is not specified or the record is not successfully accessed as specified by the RETRY phrase, the record operation conflict condition exists. The [-0 status is set in accordance with the rules for the RETRY phrase:
10) If the record operation conflict condition exists as a result of the READ statement:
a) The file position indicator is unchanged.
b) A value is placed into the [-0 status associated with file-name-1 to indicate the record operation conflict condition:
C) The content of the associated record area is undefined_
d) The key of reference for indexed files is unchanged.
e) The READ statement is unsuccessful
11) If record locks are in effect; the following actions take place:
a) If single record locking is specified for the file connector associated with file-name-1,any prior record lock associated with that file connector is released by the execution of the READ statement
b) If multiple record locking is specified for the file connector associated with file-name-1, no record locks are released, except when the NO LOCK phrase is specified and the record accessed was already locked by that file connector; In this case, that record lock is released at the completion of the successful execution of the READ statement:
Ifthe lock mode is automatic; the record lock associated with a successfully accessed record is set
d) Iflock mode is manual, the record lock associated with a successfully accessed record is set only if the LOCK phrase is specified on the READ statement:
12) If the IGNORING LOCK phrase is specified on the READ statement; the requested record is made available; even ifit is locked.
13) If neither an at end nor an invalid key condition occurs during the execution ofa READ statement; the AT END phrase or the INVALID KEY phrase is ignored, if specified, and the following actions occur:
a) The 1-0 status associated with file-name-1 is updated and if the record operation conflict condition did not occur, the file position indicator is set:
b) If an exception condition that is not an at end or an invalid key condition exists, control is transferred according to the rules in 9.1.12, Input-output exception processing: If the exception
@ISO/IEC 2023
695
[per-copy licence stamp]


--- Page 726 ---

ISO /IEC 1989.2023 (E)
condition is not a fatal exception condition; control is then transferred to the end of the READ statement:
If no exception condition exists, the record is made available in the record area and any implicit move resulting from the presence of an INTO phrase is executed: Control is transferred t0 the end of the READ statement; Or, if the NOT AT END phrase or NOT INVALID KEY phrase is specified,to imperative-statement-2. Ifcontrol is returned from imperative-statement-2, control is then transferred to the end ofthe READ statement
14) For a record sequential file,if the number of bytes in the record that is read is less than the minimum size specified by the record description entries for file-name-1,the portion ofthe record area that is to the right of the last valid character read is undefined. If the number of bytes in the record that is read is greaterthan the maximum size specified by the record description entries for file-name-1,the record is truncated on the right to the maximum size. In either ofthese cases,the READ statement is successful, and the [-0 status value for file-name-1 is set to '04'. (9.1.13,1-0 status).
NOTE 2 It is expected that this situation will occur only when the operating environment does not check either the minimum or maximum record length as a fixed file attribute during OPEN processing or when specific physical record within a physical file violates the fixed file attributes for that physical file
15) For a line sequential file, if the number of bytes in the record that is read is less than the minimum size specified by the record description entries for file-name-1,the portion of the record area that is to the right of the last valid character read is padded with trailing spaces: If the record-area associated with file-name-1 is specified implicitly or explicitly as alphanumeric, a trailing space is defined to be the alphanumeric space character: If the record-area associated with file-name-1 is specified implicitly or explicitly as national, trailing space is defined to be the national space character;
If the number of bytes in the record that is read is greater than the maximum size specified by the record description entries for file-name-1,the record is truncated on the right to the maximum size_ In that case, the READ statement is successful and the [-0 status in the read file connector is set t06' indicating that the line delimiter or end-of-file was not detected. (9.1.13,[-0 status): After the read the file position indicator will reference the next unread character in the record.
NOTE 3 One or more subsequent READ statements can be used to read the rest of the record up to the line delimiter or until end of-file is detected:
16) If the execution of the READ statement is successful but the record area contains one or more characters not in the implementor-defined character set for a line sequential file, the [-0 status in the read file connector is set to '09'. (9.1.13,1-0 status)
17) Regardless of the method used to overlap access time with processing time, the concept ofthe READ statement is unchanged; a record is available to the runtime element prior to the execution of imperative-statement-2 or imperative-statement-4, if specified, or prior to the execution of any statement following the READ statement; if imperative-statement-2 or imperative-statement-4 is not specified.
18) Unless otherwise specified,at the completion ofany unsuccessful execution ofa READ statement;the content ofthe associated record area is undefined,the key " ofreference is undefined for indexed files, and the file position indicator is set to indicate that no valid record position has been established.
696
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 727 ---

ISO /IEC 1989.2023 (E)
FORMAT 1
19) An implicit or explicit NEXT phrase or a PREVIOUS phrase results in a sequential read: otherwise, the read is a random read and the rules for format 2 apply:
20) Ifthe PREVIOUS phrase is specified,the physical file associated with the file connector referenced by file-name-1 shall be a single reel/unit mass storage file_
21) For a sequential READ statement; if the previous READ or START statement for the file connector was unsuccessful, then the READ statement is unsuccessful and the [-0 status is set to '46' and execution proceeds as indicated in General rule 24
The setting of the file position indicator at the start of the execution of the READ statement is used in determining the record to be made available Ifthe file position indicator indicates that an optional input file is not present or that no next 0r previous logical record exists, the 1-0 status value associated with file-name-1 is set to 10', the at end condition exists, and execution proceeds as specified in General rule 24. If the file position indicator indicates that no valid record position has been established, execution of the READ statement is unsuccessful, and execution proceeds as indicated in General rule 24.
When the file is an indexed file:
a) Comparisons for records relate to the value ofthe current key of reference: The comparisons are made according to the collating sequence ofthe file.
b) If the KEY phrase is specified, the key of reference is set to the key specified in that phrase_ Otherwise, the key of reference is set to the last key of reference in the file position indicator,
If the key of reference is an alternate key, any record identified as being suppressed by the SUPPRESS WHEN phrase of the ALTERNATE RECORD KEY clause is not considered to exist:
d) Ifthe previous operation on the file was a successful OPEN or START statement; the first existing record to be made available is either:
If NEXT is specified or implied,the record to be made available is the first existing record in the physical file whose key of reference value is greater than or equal to the key value in the file position indicator.
If PREVIOUS is specified and the previous operation on the file was a START statement; the first existing record in the physical file whose key of reference value is less than or equal to the key value in the file position indicator.
Ifno such record is found or PREVIOUS is specified and the previous operation on the file was an OPEN statement; the at end condition exists and execution proceeds as indicated in General rule 24.
If the previous operation on the file was a successful READ statement and the current key of reference is not an alternate key that allows duplicates, the first existing record to be made available is either:
@ISO/IEC 2023
697
[per-copy licence stamp]


--- Page 728 ---

ISO /IEC 1989.2023 (E)
If NEXT is specified or implied, the record to be made available is the first existing record in the physical file whose key value is greater than or equal to the key value in the file position indicator_
If PREVIOUS is specified, the first existing record in the physical file whose key value is less than or equal to the key - value in the file position indicator:
Ifno such record is found,the at end condition exists and execution proceeds as indicated in General rule 24. Otherwise, the first record in the physical file whose key value is greater than the key of reference is made available:
f) If the previous operation on the file was a successful READ statement and the current key of reference is an alternate key that allows duplicates the record to be made available is one of the following:
If NEXT is specified or implied, and there exists in the physical file a record whose key value is equal to the key ofreference and whose logical position within the set of duplicates is after the record that was made available by that prior READ statement; the record within the set of duplicates thatis immediately after the record that was made available by that prior READ statement: Otherwise, the first record in the physical file whose key value is greater than the key of reference value:
If PREVIOUS is specified and there exists in the physical file a record whose key value is equal to the file position indicator and whose logical position within the set of duplicates is before the record that was made available by that prior READ statement is made available: the record within the set of duplicates that is immediately before the record that was made available by that prior READ statement; Otherwise, the last record within the set of duplicates, ifany, whose key - value is the first key value is less than the key ofreference value_
Ifno such record is found, the at end condition exists and execution proceeds as indicated in General rule 24_
g) If a record is made available, the file position indicator is set to the value of the current key of reference of the record made available and the read operation is successful.
When the file is a relative file:
a) Comparisons for records in relative files relate to the relative key number:
b) If the file position indicator was established by a prior successful OPEN or START statement; the first existing record that is selected is made available, regardless of whether NEXT or PREVIOUS is specified.
C) If the file position indicator was established by prior successful READ statement; the first existing record in the physical file whose relative key number is greater than the file position indicator if NEXT is specified or implied or is less than the file position indicator if PREVIOUS is specified is selected.
698
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 729 ---

ISO /IEC 1989.2023 (E)
d) Ifa record is found according to the above rules, the record is made available in the record area associated with file-name-1 unless the RELATIVE KEY clause is specified for file-name-land the number of significant digits in the relative key number of the selected record is larger than the size of the relative key data item. In that case,the [-0 status value associated with file-name-1 is set to '14', the at end condition exists, the file position indicator is set t0 indicate that no next or previous logical record exists, and execution proceeds as specified in General rule 24_
NOTE 4 The record made available can have a length ofzero:
If no record is found that satisfies the above rules, the at end condition exists, and execution proceeds as specified in General rule 24_
0) Ifa record is made available, the file position indicator is set to the relative record number ofthe record made available_
When the file is a sequential file:
a) Comparisons for records in sequential files relate to the record number
b) Ifthe file position indicator was established by a prior successful OPEN or START statement; the first existing record that is selected is made available, regardless ofwhether NEXT or PREVIOUS is specified.
If the file position indicator was established by prior successful READ statement; the first existing record in the physical file whose relative key number is greater than the file position indicator if NEXT is specified or implied or is less than the file position indicator if PREVIOUS is specified is selected.
d) Ifa record is found according to the above rules, the record is made available in the record area associated with file-name-1,
If no record is found that satisfies the above rules, the at end condition exists, and execution proceeds as specified in General rule 24_
0 Ifa record is made available, the file position indicator is set to the record number ofthe record made available
NOTE 5 The record made available can have a length ofzero.
22) Ifthe ADVANCING ON LOCK phrase is specified on the READ statement ofa file open for file sharing and the record to be made available is locked by another file connector, the result of this READ statement is as if the locked record were read and then the same READ statement were executed, If the record to be made available is locked by another file connector; this action is repeated until either an unlocked record is read or the end ofthe file is encountered if NEXT is specified or implied, or the beginning of file is encountered if PREVIOUS is specified_ Arecord operation conflict condition does not exist If the end of the file or beginning of the file is encountered, the file position indicator is set to indicate that no next or previous logical record exists and execution proceeds as indicated in General rule 24.
@ISO/IEC 2023
699
[per-copy licence stamp]


--- Page 730 ---

ISO /IEC 1989.2023 (E)
If the file is not open for file sharing, the ADVANCING ON LOCK phrase is ignored:
23) If, during the execution of the READ statement; the end of reel/unit is recognized or a reel/unit contains no logical records, and the logical end of the file has not been reached for a given file connector, a reel/unit swap occurs and the current volume pointer is updated to point t0 the next reel/unit existing for the physical file_
24) If, during the execution of the READ statement; the at end condition exists, the following occurs in the order specified:
a) The [-0 status ofthe file connector associated with file-name-1 is set to '10' t0 indicate the at end condition, and, if enabled, the EC-I-O-AT-END exception condition is set to exist;
b) The file position indicator is set to indicate that no next or previous logical record exists.
If the AT END phrase is specified in the READ statement causing the condition, control is transferred t0 imperative-statement-1. Any other applicable exception processing statements are not executed. If control is returned from imperative-statement-1, control is then transferred to the end of the READ statement
If the AT END phrase is not specified in the input-output statement; any applicable at end exception processing statements are executed: If there are no applicable at end exception processing statements, control is transferred to the end of the READ statement:
When the at end condition exists, execution of the READ statement is unsuccessful.
NOTE 6 The content ofthe associated record area is undefined as indicated in General rule 18.
25) For a relative file, if the RELATIVE KEY clause is specified for file-name-1,the execution ofa READ statement moves the relative record number of the record made available to the relative key data item according to the rules for the MOVE statement:
26) For an indexed file being sequentially accessed, records having the same duplicate value in an alternate record key that is the key of reference are made available in the same order; Or,in the case of PREVIOUS, in the reverse order, in which they are released by execution of WRITE statements, or by execution of REWRITE statements that create such duplicate values:
27) The [-0 status for the file connector referenced by file-name-1 is set to '02' if the execution of the READ statement is successful, an indexed file is being sequentially accessed, the key of reference is an alternate record key,and one of the following is true:
a) the NEXT phrase is specified or implied and the alternate record key in the record that follows the record that was successfully read duplicates the same key in the record that was successfully read, 0r
b) the PREVIOUS phrase is specified and the alternate record key in the record that immediately precedes the record that was successfully read duplicates the same key in the record that was successfully read:
700
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 731 ---

ISO /IEC 1989.2023 (E)
NOTE If the sharing mode of the file is sharing with all other, 1-0 status value 02 on a sequential read cannot be relied on fora subsequent sequential read The record with a duplicate key might have been deleted through another file connector between the return of [-0 status value '02' and the execution ofthe subsequent READ statement
NOTE 8 If the sharing mode of the file is sharing with all other; the lack of an [-0 status value '02 on sequential read cannot be relied on as an indication that no duplicate key will exist at the time ofa subsequent sequential read A record with a duplicate key might have been added through another file connector before the execution of that subsequent READ statement
FORMAT 2
28) If, at the time of the execution of a READ statement; the file position indicator indicates that an optional input file is not present; the invalid key condition exists and execution of the READ statement is unsuccessful. (See 9.1.14, Invalid key condition:)
29) For a relative file, execution of a READ statement sets the file position indicator to the value contained in the data item referenced by the RELATIVE KEY clause for the file,and the record whose relative record number equals the file position indicator is made available in the record area associated with file-name-1_ If the physical file does not contain such a record, the invalid key condition exists and execution of the READ statement is unsuccessful. (See 9.1.14, Invalid key condition:)
30) For an indexed file accessed through a given file connector,ifthe KEY phrase is specified, data-name- 1 or record-key-name-1 is established as the key ofreference for this retrieval. If the dynamic access mode is specified, this key of reference is also used for retrievals by any subsequent executions of sequential format READ statements for the file through the file connector until a different key of reference is established for the file through that file connector_
31) For an indexed file accessed through a given file connector, if the KEY phrase is not specified, the prime record key is established as the key ofreference for this retrieval. Ifthe dynamic access mode is specified, this key of reference is also used for retrievals by any subsequent executions of sequential format READ statements for the file through the file connector until a different key of reference is established for the file through that file connector:
32) For an indexed file accessed through a given file connector, execution of a READ statement sets the file position indicator t0 the value in the key of reference: This value is compared with the value contained in the corresponding data item ofthe stored records in the file untilthe first record having an equal value is found. In the case of an alternate key with duplicate values, the first record found is the first record in a sequence of duplicates that was released to the operating environment: The record so found is made available in the record area associated with file-name-1. If no record is SO identified,the invalid key condition exists and execution ofthe READ statement is unsuccessful. (See 9.1.14, Invalid key condition.)
@ISO/IEC 2023
701
[per-copy licence stamp]


--- Page 732 ---

ISO /IEC 1989.2023 (E)
14.9.31 RECEIVE statement
14.9.31.1 General
The RECEIVE statement receives a message from a requestor or a message server run unit_
14.9.31.2 General format
RECEIVE FROM data-name-1 GIVING identifier-1 data-name-2
arithmetic-expression-1 SECONDS CONTINUE AFTER MESSAGE RECEIVED
ON EXCEPTION imperative-statement-1 NOT ON EXCEPTION imperative-statement-2 END-RECEIVE ]
14.931.3 Syntax rules
1) Data-name-1 shall be the name of a message-tag data item:
2) The data description entries of identifier-1 or any data items subordinate to it shall not contain the ANY LENGTH clause, the BASED clause, the CONSTANT RECORD clause, the DYNAMIC-LENGTH clause; the FUNCTION-POINTER clause, the OBJECT-REFERENCE clause,the OCCURS clause with the DEPENDING ON phrase where the depending on data item is not within identifier-1, the POINTER clause, or the PROGRAM-POINTER clause
NOTE The normal case would be to define identifier-1 as an 01 level item whose data description is exactly that in the message requestor:
3) Data-name-2 shall be an integer data item of the class numeric:
14.9.31.4 General rules
1) The RECEIVE statement receives a message from a server run unit 0r, in the case where the current run unit is being activated as a server run unit; from that requestor run unit:
a) If the content of data-name-1 is the value NULL before the execution of the RECEIVE statement; the RECEIVE statement is waiting for any requestor run unitto senda messageto the current run unit; At the successful completion of the execution of the RECEIVE statement, data-name-1 will be set to an implementor-defined value that identifies the requestor run unit (the message tag) This value is used in a subsequent SEND statement to return any information requested by the sender.
b) Otherwise, the content of data-name-1 is an implementor-defined value that identifies the run unit that sent a message to the current run unit via a SEND statement: The content of identifier- 1 is the content of the message sent by that SEND statement: If the content of the message tag
702
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 733 ---

ISO /IEC 1989.2023 (E)
does not identify a requestor or is not a correct format for the MCS, the RECEIVE statement is unsuccessful and the EC-MCS-INVALID-TAG exception condition is set to exist: If the content of the message tag specifies a requestor that does not exist; the RECEIVE statement is unsuccessful and the EC-MCS-REQUESTOR-FAILED exception condition is set to exist:
NOTE The failed exception condition can happen ifthe requestor run unit aborted for some reason or if it didn't send any message or other reasons
2) Ifthe CONTINUE phrase is specified, the RECEIVE statement execution is completed either when the message or an exception is returned from the MCS or when the CONTINUE parameters are completed: In the latter case,the content of identifier-1 will be all space characters and the RECEIVE statement is assumed to be successful.
3) If the execution of the RECEIVE statement was successful, data-name-2 will contain the length, in alphanumeric characters, of the message received. If that length exceeds the length of identifier-1, the EC-MCS-MESSAGE-LENGTH exception condition is set to exist and the execution of the RECEIVE statement is unsuccessful:
4) If the execution of the RECEIVE statement is successful, the ON EXCEPTION phrase, if specified, is ignored and control is transferred to the end of the RECEIVE statement; 0r, if the NOT ON EXCEPTION phrase is specified to imperative-statement-2. If control is returned from imperative- statement-2, control is then transferred to the end of the RECEIVE statement:
5) If the execution of the RECEIVE statement is unsuccessful,then:
a) If the ON EXCEPTION phrase is specified in the RECEIVE statement; control is transferred to imperative-statement-1. If control is returned from imperative-statement-1, control is then transferred to the end of the RECEIVE statement
b) If the ON EXCEPTION phrase is not specified in the RECEIVE statement; one of the following occurs:
Ifthe RECEIVE statement is specified in a statement that is in imperative-statement-1 in an exception-checking PERFORM statementand a WHEN phrase in that statement specifies the exception condition that occurred, control is transferred to that WHEN phrase: If control is returned from the WHEN phrase, control is then transferred to the end of the RECEIVE statement:
If there is no applicable WHEN phrase and there is an applicable USE declarative, control is transferred to that declarative: If control is returned from the declarative, control is then transferred to the end ofthe RECEIVE statement
Otherwise, control is transferred to the end of the RECEIVE statement:
@ISO/IEC 2023
703
[per-copy licence stamp]


--- Page 734 ---

ISO /IEC 1989.2023 (E)
14.9.32 RELEASE statement
14.9.32.1 General
The RELEASE statement transfers records to the initial phase of a sort operation:
14.932.2 General format
identifier-1 literal-1
RELEASE record-name-1 FROM
14.9.32.3 Syntax rules
1) Record-name-1 shall be the name ofa logical record in a sort-merge file description entry and it may be qualified.
2) If identifier-1 is a function-identifier,it shall reference an alphanumeric or national function:
3) Identifier-1 or literal-1 shall be valid as a sending operand in a MOVE statement specifying record- name-1 as the receiving operand:
4) Literal-1 shall not be a zero-length literal:
14.9.32.4 General rules
1) A RELEASE statement may be executed only when it is within the range ofan input procedure being executed by a SORT statement that references the file-name associated with record-name-1. Ifit is executed at any other time, the EC-FLOW-RELEASE exception condition is set to exist:
2) The execution ofa RELEASE statement causes the record named by record-name-1 to be released to the initial phase ofa sort operation:
3) The logical record released by the execution of the RELEASE statement is no longer available in the record area unless the sort-merge file-name associated with record-name-1 is specified in a SAME RECORD AREA clause. The logical record is also available as a record of other files referenced in the same SAME RECORD AREA clause as the associated output file, as well as the file associated with record-name-1.
4) The result of the execution of a RELEASE statement with the FROM phrase is equivalent to the execution of the following statements in the order specified:
a) The statement:
MOVE identifier-1 TO record-name-1
or
704
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 735 ---

ISO /IEC 1989.2023 (E)
MOVE literal-1 TO record-name-1
according to the rules specified for the MOVE statement
b) The same RELEASE statement without the FROM phrase:
NOTE 14.6.10, Overlapping operands; and 14.9.25, MOVE statement; general rules, apply to any cases in which the storage area identified by identifier-1 and the record area associated with record-name-1 share any part of their storage areas_ The result of execution of the RELEASE statement is undefined if the result of execution of the implicit MOVE statement described in General rule 4b is undefined:
5) After the execution ofthe RELEASE statement is complete,the information in the area referenced by identifier-1 is available, even though the information in the area referenced by record-name-1 is not available except as specified by the SAME RECORD AREA clause
If the number of bytes to be released to the sort operation is greater than the number of bytes in record-name-1,the content ofthe bytes that extend beyond the end ofrecord-name-Lare undefined
@ISO/IEC 2023
705
[per-copy licence stamp]


--- Page 736 ---

ISO /IEC 1989.2023 (E)
14.9.33 RESUME statement
14.9.33.1 General
The RESUME statement transfers control to a procedure-name or to the statement following the statement that caused a declarative or an imperative-statement in a WHEN phrase of an exception- checking PERFORM statement to be executed.
14.9.33.2 General format
NEXT STATEMENT RESUME AT procedure-name-l
14.9.33.3 Syntax rules
1) The RESUME statement may be specified only in a declarative 0r in an imperative statement in a WHEN phrase of an exception-checking   PERFORM statement In the latter case, the NEXT STATEMENT phrase shall be specified:
2) The RESUME statement shall not be specified in a declarative procedure for which the GLOBAL phrase is specified in the associated USE statement:
3) Procedure-name-1 shall be a procedure-name in the nondeclarative portion ofthe function, method, or program:
14.9.33.4 General rules
1) If the RESUME statement is executed within the scope of execution of a global declarative, it is the equivalent of the execution ofa CONTINUE statement:
2) If the NEXT STATEMENT phrase is specified, control is transferred to an implicit CONTINUE statement that is determined as follows:
a) When an exception condition caused an exception processing procedure to be executed, the implicit CONTINUE statement immediately follows the end of the statement that was executing when control was transferred to the exception processing procedure unless general rules associated with the applicable statement specify otherwise The applicable statement is one of the following:
If the exception condition was raised within a statement within the runtime entity and was not a propagated exception condition, the applicable statement is the one in which the exception condition was raised:
If the exception condition was propagated from an activated runtime entity, the applicable statement is the CALL or INVOKE statement that activated the entity, Or, for an inline invocation or a function invocation, it is the statement in which the inline invocation or function invocation was specified
706
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 737 ---

ISO /IEC 1989.2023 (E)
Ifthe statement is contained in other statements, the applicable statement is the lowest level statement, not the containing statement
b) If the declarative was not executed because of an exception condition but was executed instead by a PERFORM statement in the nondeclarative portion of the source element that referenced the declarative procedure, the implicit CONTINUE statement immediately follows the last statement of the terminating procedure referenced in that PERFORM statement:
NOTE 1 Use of NEXT STATEMENT may cause a transfer of control to a statement that in the normal course ofevents would not be executed. For example IFa GO TO x ELSE GO TO y END-IF. Ifan exception condition was raised during the evaluation of 'a', transfer would be after the END-IF even though control normally would never be passed there
3) If procedure-name-1 is  specified, control is transferred to procedure-name-1 as if GO TO procedure-name-1 were executed.
NOTE 2 Use of this method of recovery can cause the flow of control for PERFORM statements to be undefined as described in 14.9.28,PERFORM statement General rule 2_
@ISO/IEC 2023
707
[per-copy licence stamp]


--- Page 738 ---

ISO /IEC 1989.2023 (E)
14.9.34 RETURN statement
14.9.34.1 General
The RETURN statement obtains either sorted records from the final phase ofa sort operation or merged records during a merge operation:
14.9.34.2 General format
RETURN file-name-1 RECORD INTO identifier-1 ] AT END imperative-statement-1 NOT AT END imperative-statement-2 ] END-RETURN
14.9.34.3 Syntax rules
1) File-name-1 shall be described by a sort-merge file description entry in the data division.
2) The INTO phrase may be specified in a RETURN statement:
a) If only one record description is subordinate to the sort-merge file description entry, or
b) If all record-names associated with file-name-1 and the data item referenced by identifier-1 describe an alphanumeric group item or an elementary item of category alphanumeric or category national
3) Ifidentifier-1 is a strongly-typed group item; there shall be exactly one record area subordinate to the SD for file-name-1. This record area shall be strongly-typed group item of the same type as identifier-1_
4) The AT END phrase and the NOT AT END phrase, when specified, may be written in reversed order:
14.9.34.4 General rules
1) A RETURN statement may be executed only when it is within the range ofan output procedure being executed by a MERGE or SORT statement that references file-name-L. If it is executed at any other time,the EC-FLOW-RETURN exception condition is set to exist;
2) When the logical records ofa file are described with more than one record description, the content of any data item, where any part of that data item lies outside the range of the current record, is undefined at the completion of the execution ofthe RETURN statement;
3) The execution of the RETURN statement causes the next existing record in the file referenced by file-name-1,as determined by thekeys listed in the SORT or MERGE statement; to be made available in the record area associated with file-name-1. If no next logical record exists in the file referenced by file-name-1, the at end condition is set to exist and control is transferred to imperative-statement-1 of the AT END phrase. If control is returned from imperative-statement-1,
708
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 739 ---

ISO /IEC 1989.2023 (E)
control is then transferred to the end of the RETURN statement; When the at end condition exists, execution of the RETURN statement is unsuccessful and the contents of the record area associated with file-name-1 are undefined. After the execution of imperative-statement-1 in the AT END phrase, no RETURN statement may be executed as part of the current output procedure. If such a RETURN statement is executed, the EC-SORT-MERGE-RETURN exception condition is set to exist and the results of the execution of the RETURN statement are undefined,
4) If an at end condition does not exist during the execution of a RETURN statement; then after the record is made available and after executing any implicit move resulting from the presence of an INTO phrase, control is transferred to imperative-statement-2, if specified; otherwise, control is transferred to the end of the RETURN statement
5) The result of the execution of a RETURN statement with the INTO phrase is equivalent to the application ofthe following rules in the order specified:
a) The same RETURN statement without the INTO phrase is executed.
b) The current record is moved from the record area to the area specified by identifier-1 according to the rules for the MOVE statement without the CORRESPONDING phrase: The size of the current record is determined by rules specified for the RECORD clause. If the file description entry contains a RECORD IS VARYING clause, the implied move is an alphanumeric group move The implied MOVE statement does not occur if the execution of the RETURN statement was unsuccessful. Item identification of the data item referenced by identifier-1 is done after the record has been read and immediately before itis moved to the data item: The record is available in both the record area and the data item referenced by identifier-1_
NOTE 14.6.10, Overlapping operands, and 14.9.25,MOVE statement; general rules, apply to any cases in which the storage area identified by identifier-1 andthe record area associated with file-name-1 share any part of their storage areas The result of execution of the RETURN statement is undefined if the result of execution ofthe implicit MOVE statement described in General rule 4b is undefined:
@ISO/IEC 2023
709
[per-copy licence stamp]


--- Page 740 ---

ISO /IEC 1989.2023 (E)
14.935 REWRITE statement
14.9.35.1 General
The REWRITE statement logically replaces a record existing in a mass storage file:
14.935.2 General format
record-name-1 FILE file-name-1
identifier-1 literal-1
REWRITE
RECORD
FROM
retry-phrase ] WITH LOCK WITH NO LOCK
INVALID KEY imperative-statement-1 NOT INVALID KEY imperative-statement-2 END-REWRITE
where retry-phrase is described in 14.7.9,RETRY phrase
14.9.35.3 Syntax rules
1) Record-name-1 is the name of a logical record in the file section of the data division and may be qualified.
2) Neither the INVALID KEY phrase nor the NOT INVALID KEY phrase shall be specified for a REWRITE statement that references a file with sequential organization or a file with relative organization and sequential access mode:
3) If record-name-1 is defined in a containing program and is referenced in a contained program; the file description entry for the file associated with record-name-1 shall contain a GLOBAL clause:
4) If automatic locking has been specified for the rewrite file, neither the WITH LOCK phrase nor the WITH NO LOCK phrase shall be specified.
5) If the rewrite file is subject to an APPLY COMMIT clause, neither the WITH LOCK phrase nor the WITH NO LOCK phrase shall be specified
6) If record-name-1 is specified, identifier-1 or literal-1 shall be valid as a sending operand in a MOVE statement specifying record-name-1as the receiving operand:
7) If identifier-1 references a bit data item other than a function and the FILE phrase is specified, identifier-1 shall be described such that:
710
OISO /IEC 2023
[per-copy licence stamp]


--- Page 741 ---

ISO /IEC 1989.2023 (E)
subscripting and reference modification in identifier-1 consist of only fixed-point numeric literals or arithmetic expressions in which all operands are fixed-point numeric literals and the exponentiation operator is not specified; and
b) it is aligned on a byte boundary:
8) If identifier-1 references a function and the FILE phrase is specified, identifier-1 shall reference an alphanumeric or national function
9) If identifier-1 references a function and the FILE phrase is not specified, identifier-1 shall reference an alphanumeric; boolean, or national function
10) If the FILE phrase is specified, the FROM phrase shall also be specified and:
a) identifier-1 shall be valid as a sending operand in a MOVE statement;
b) literal-1 shall be an alphanumeric, boolean, 0r national literal and shall not be figurative constant.
11) File-name-1 shall not reference a report file or a sort-merge file description entry.
12) If the FILE phrase is specified, the description of identifier-1, including its subordinate data items, shall not contain a data item described with a USAGE OBJECT REFERENCE clause:
14.9.35.4 General rules
1) The execution of the REWRITE statement causes the [-0 status value in the rewrite file connector to be updated as indicated in 9.1.13,1-0 status
2) The rewrite file connector is the file connector referenced by file-name-1 or the file-name associated with record-name-1.
3) The rewrite file connector shall have an open mode of [-0. If the open mode is some other value or the file is not open, the [-0 status in the rewrite file connector is set to '49' and the execution of the REWRITE statement is unsuccessful,
4) The successful execution of the REWRITE statement releases logical record to the operating environment.
5) If the rewrite file connector has an access mode of sequential, the immediately previous input- output statement executed that referenced this file connector  shall have been successfully executed READ statement If this is not true, the [-0 status in the rewrite file connector is set to '43' and the execution of the REWRITE statement is unsuccessful. For a successful REWRITE statement; the operating environment logically replaces the record that was accessed by the READ statement:
NOTE 1 Logical records in relative and sequential files can have length of zero. Logical records in an indexed file will always be long enough to contain the record keys:
@ISO/IEC 2023
711
[per-copy licence stamp]


--- Page 742 ---

ISO /IEC 1989.2023 (E)
6) The logical record released by a successful execution of the REWRITE statement is no longer available in the record area unless file-name-1 or the file-name associated with record-name-1 is specified in a SAME RECORD AREA clause. The logical record is also available as a record of other file-names referenced in the same SAME RECORD AREA clause as file-name-1 or the file-name associated with record-name-1,as well as the file associated with record-name-1.
7) The result ofthe execution ofa REWRITE statement specifying record-name-l and the FROM phrase is equivalent to the execution of the following statements in the order specified:
a) The statement:
MOVE identifier-1 TO record-name-1
or
MOVE literal-1 TO record-name-
according to the rules specified for the MOVE statement:
b) The same REWRITE statement without the FROM phrase_
NOTE 2 14.6.10, Overlapping operands, and 14.9.25, MOVE statement; general rules, apply to any cases in which the storage area identified by identifier-1 and the record area associated with file-name-1 share any part of their storage areas The result of execution of the REWRITE statement is undefined if the result of execution of the implicit MOVE statement described in General rule 7a is undefined:
8) The figurative constant SPACE when   specified in the REWRITE statement  references one alphanumeric space character:
9) The result of execution ofa REWRITE statement with the FILE phrase is equivalent to the execution of the following implicit MOVE statement and implicit REWRITE statement in the order specified:
The statement
MOVE identifier-1 TO implicit-record-1
or
MOVE literal-1 TO implicit-record-1
The statement
REWRITE implicit-record-1
where implicit-record-1 refers to the record area for file-name-1 and is treated:
a) when identifier-1 references an intrinsic function, as though implicit-record-1 were a record description entry subordinate to the file description entry having the same class, category, usage, and length as the returned value of the intrinsic function, or
712
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 743 ---

ISO /IEC 1989.2023 (E)
b) when identifier-1 does not reference an intrinsic function, as though implicit-record-1 were a record description entry subordinate to the file description entry having the same description as identifier-1,or
c) when literal-1 is specified, as though  implicit-record-1 were record   description entry subordinate to the file description entry having the same class, category, usage, and length as literal-1.
NOTE 3 14.6.10, Overlapping operands, and 14.9.25, MOVE statement; general rules, apply to any cases in which the storage area identified by identifier-1 and the record area associated with implicit-record-1 share any part of their storage areas The result of execution of the REWRITE statement is undefined if the result of execution of the implicit MOVE statement is undefined
10) After the execution of the REWRITE statement is complete, the information in the area referenced by identifier-1 is available, provided that identifier-1 is not one or part of one of the record descriptions subordinate to the file-description, even though the information in the area referenced by record-name-1 is not available except as specified for the SAME RECORD AREA clause as indicated in General rule 6.
11) If record locking is enabled for the rewrite file connector and the record identified for rewriting is locked by another file connector; the result ofthe operation depends on the presence 0r absence of the RETRY phrase. Ifthe RETRY phrase is specified,additional attempts may be made to rewrite the record as specified in the rules in 14.7.9, RETRY phrase. If the RETRY phrase is not specified or the record is not successfully rewritten as specified by the RETRY phrase, the record operation conflict condition exists. The [-0 status is set in accordance with the rules for the RETRY phrase When the record operation conflict condition exists as a result of the REWRITE statement:
a) The file position indicator is unchanged.
b) A value is placed into the [-0 status associated with the rewrite file connector to indicate the record operation conflict condition.
C) The REWRITE statement is unsuccessful,
12) If record locks are in effect; the following actions take place at the beginning or at the successful completion ofthe execution of the REWRITE statement: a) If single record locking is specified for the rewrite file connector:
If that file connector holds a record lock on the record to be logically replaced, that lock is released at completion unless the WITH LOCK phrase is specified:
If that file connector holds a record lock on a record other than the one to be logically replaced, that lock is released at the beginning:
b) If multiple record locking is specified for the rewrite file connector, and a record lock is associated with the record to be logically replaced, that record lock is released at completion only when the WITH NO LOCK phrase is specified and the record to be logically replaced was already locked by that file connector:
@ISO/IEC 2023
713
[per-copy licence stamp]


--- Page 744 ---

ISO /IEC 1989.2023 (E)
If the WITH LOCK phrase is specified, the record lock associated with the record to be replaced is set at completion.
13) The file position indicator in the rewrite file connector is not affected by the execution ofa REWRITE statement_
14) If the execution of the REWRITE statement is unsuccessful, no logical record updating takes place, the content ofthe record area is unaffected,and the [-O status in the rewrite file connector is updated as indicated in General rules 2,5,11,16,17,20,21,22,23,and 25. The transfer of control depends on other clauses and the value of [-0 status as described in 9.1.12, Input-output exception processing; 9.1.13,[-0 status,and 9.1.14, Invalid key condition.
15) When record-name-1 is specified, if the number of bytes to be written to the file is greater than the number of bytes in record-name-1, the content of the bytes that extend outside the end of record- name-1 are undefined,
SEQUENTIAL FILES
16) For a record sequential file, if the number of bytes in the data item referenced by identifier-1, the runtime representation of literal-1,or the record referenced by record-name-1 is not equal to the number of bytes in the record being replaced, the execution of the REWRITE statement is unsuccessful and the [-0 status in the rewrite file connector is set to '44'.
17) For a line sequential file
a) If the execution of the preceding READ statement results in only part of the record being transferred to the record area,the execution of the REWRITE statement is unsuccessful and the 1-0 status in the rewrite file connector is set to '44'. (9.1.13,[-0 status)
NOTE 4 A READ statement executed on a line sequential file transfers sufficient characters to fill the record area or characters up to the line delimiter: Ifthere are further characters in the record being read these are transferred by the execution of subsequent READ statements In this situation the execution of a REWRITE statement to replace the record is unsuccessful
b) Ifthe number of bytes in the data item referenced by identifier-1,the runtime representation of literal-1, or the record referenced by record-name-1 is greater than the number of bytes in the record being replaced, the execution of the REWRITE statement is unsuccessful and the [-0 status in the rewrite file connector is set to '44'. (9.1.13,[-0 status)
If the number of bytes in the data item referenced by identifier-1,the runtime representation of literal-1, or the record referenced by record-name-1 is less than the number of bytes in the record being replaced, then a sufficient number of the space character is appended to the data item referenced by identifier-1,the runtime representation of literal-1, or the record referenced by record-name-1 to increase the length of the record being transferred to the length of the record being replaced. If the data item referenced by identifier-1,the runtime representation of literall, or the record referenced by record-name-1 is specified implicitly or explicitly as alphanumeric, a space is defined to be the alphanumeric space character. If the data item referenced by identifier-1,the runtime representation of literal-1, or the record referenced by record-name-lis specified implicitly or explicitlyas national,a space is defined to be the national space character
714
@ISO /IEC 2023
[per-copy licence stamp]


--- Page 745 ---

ISO /IEC 1989.2023 (E)
If the record area contains one or more characters that are not in the implementor-defined character set defined for a line sequential file the execution of the REWRITE statement is unsuccessful and the [-0 status in the rewrite file connector is set to '71'. (9.1.13,[-0 status)
RELATIVE AND INDEXED FILES
18) The number of bytes in the record referenced by identifier-1, the runtime representation of literal- or the record referenced by record-name-1 may differ from the number of bytes in the record being replaced.
19) Transfer of control following the successful or unsuccessful execution of the REWRITE operation depends on the presence or absence of the optional INVALID KEY and NOT INVALID KEY phrases in the REWRITE statement: (See 9.1.14,Invalid key condition.)
20) The number of bytes in the runtime representation of literal-1, the data item referenced by identifier-1,0r the record referenced by record-name-1 after any changes made to the record length by the FORMAT clause shall not be larger than the largest or smaller than the smallest number of bytes allowed by the RECORD IS VARYING clause associated with file-name-1 or the file-name associated with record-name-l. If this rule is violated, the execution of the REWRITE statement is unsuccessful and the [-0 status in the rewrite file connector is set to '44'.
RELATIVE FILES
21) For a file accessed in either random or dynamic access mode, the operating environment logically replaces the record identified by the relative key data item specified for file-name-1 or the file-name associated with record-name-1.Ifthe file does not contain the record specified by thekey,the invalid key condition exists. When the invalid key condition is recognized, the execution of the REWRITE statement is unsuccessful and the [-0 status in the rewrite file connector is set to the invalid key condition '23"
INDEXED FILES
22) Ifthe access mode of the REWRITE file connector is sequential, the record to be replaced is specified by the value of the prime record key. When the REWRITE statement is executed the value of the prime record key of the record to be replaced shall be equal to the value of the prime record key of the last record read using this file connector. If it is not; the execution of the REWRITE statement is unsuccessful and the I-0 status in the rewrite file connector is set to the invalid key condition, '21'.
23) If the access mode of the rewrite file connector is random or dynamic, the record to be replaced is specified by the prime record key. If there is no existing record in the physical file with that prime record key, the execution of the REWRITE statement is unsuccessfuland the [-0 status in the rewrite file connector is set to the invalid key condition, '23'.
24) Execution ofthe REWRITE statement for a record that has an alternate record key occurs as follows:
a) When the value ofa specific alternate record key is not changed,the order of retrieval when that key is the key of reference remains unchanged
@ISO/IEC 2023
715
[per-copy licence stamp]


--- Page 746 ---

ISO /IEC 1989.2023 (E)
b) When the value ofa specific alternate record key is changed,the subsequent order of retrieval of that record may be changed when that specific alternate record key is the key = ofreference When duplicate key values are permitted, the record is logically positioned last within the set of duplicate records where the alternate record key value is equal to the same alternate key- value in one or more records in the file based on the collating sequence for the file:
NOTE 5 Iftwo or more file connectors share the physical file,a duplicate alternate key mightnot actually be positioned last at the completion ofthe REWRITE statement; because another duplicate key mighthave been created by another operation:
Ifthe SUPPRESS WHEN phrase is specified in the ALTERNATE RECORD KEY clauseand the value ofthe alternate record key is no longer equal to the literal specified in that phrase:
an access path to this record using this key of reference shall be provided, and
the record shall be logically positioned s0 that it will be found when accessed using the alternate record key:
If alternate record key suppression is specified for this alternate record key - and the value of this alternate record key is now equal to its key suppression value:
the access path to the record using this alternate record key shall no longer be provided,and
the record shall be logically repositioned so thatitwillnot be found when accessed using this alternate record key.
The comparison used for determining changes to the key is based on the collating sequence for the file according to the rules for a relation condition Any number ofrecords may have the same alternatekey value equal to its key suppression value without requiring the DUPLICATES phrase to be specified for that key: Key entries that are suppressed shall not cause a duplicate key condition to exist:
25) The comparison for equality for record keys is based on the collating sequence for the file according to the rules  for relation condition: The invalid key   condition exists under the following circumstances:
a) When the rewrite file connector is open in the sequential access mode and the value of the prime record key of the record to be replaced is not equal to the value of the prime record key of the last record read through the file connector; the I-0 status associated with the file connector is set to '21'.
b) When the rewrite file connector is open in the dynamic or random access mode and the value of the prime record key of the record to be replaced is not equal to the value of the prime record key of any record existing in that physical file, the [-0 status associated with the rewrite file connector is set to '23'_
c) When an alternate record key of the record to be replaced does not allow duplicates and the value of that alternate record key is equal to the value of the corresponding alternate record key
716
@ISO /IEC 2023
[per-copy licence stamp]


--- Page 747 ---

ISO /IEC 1989.2023 (E)
ofa record in that physical file, the I-0 status associated with the rewrite file connector is set to '22'.
When the invalid key condition is recognized, the execution of the REWRITE statement is unsuccessful, the updating operation does not take place, and the content of the record area is unaffected,
@ISO/IEC 2023
717
[per-copy licence stamp]


--- Page 748 ---

ISO /IEC 1989.2023 (E)
14.936 ROLLBACK statement
14.9.36.1 General
The ROLLBACK statement reverses all changes made to the files and data-items explicitly or implicitly referenced in all the active APPLY COMMIT clauses to the state that they were in at the latest COMMIT statement;orthe start ofthe run unit ifno COMMIT statements had previously been executed. Data items in canceled runtime elements 0r initial programs that have been exited are not restored:
14.9.36.2 General format
ROLLBACK
14.9.36.3 Syntax rules
1) This statement shall not be specified in a recursive source element
2) This statement shall not be specified in the input or output procedure of MERGE or SORT statement;
14.936.4 General rules
1) If this statement is executed when there is no active APPLY COMMIT clause, then it has the same effect as a CONTINUE statement with no additional phrases
NOTE When there is no active APPLY COMMIT clause then no files or data items will have been specified for commit and rollback
2) If this statement is attempted to be executed under the control of a recursive runtime element or a file SORT or MERGE statement; then the exception condition EC-FLOW-ROLLBACK is set to exist
3) AlL files and data-items referenced in active APPLY COMMIT clauses are restored t0 the state they were in at the execution of the last COMMIT statement or the start of the run unit if no COMMIT statements had previously been executed. This includes the setting of all file and record locks on those files It also includes the file status data items and data-items specified in the linage or record clauses of the file descriptions_
4) Those data-items whose containing item has been canceledare not restored, unless they are external items for which another defining entry subject to an active APPLY COMMIT clause still exists_
5) Data items in initial programs that have been exited are not restored, unless they are external items for which another defining entry subject to an active APPLY COMMIT clause still exists in a runtime element for which the initial attribute has not been specified or that runtime element has not yet been exited:
Any APPLY COMMIT clauses in exited initial programs or canceled runtime elements are deactivated.
718
OISO /IEC 2023
[per-copy licence stamp]


--- Page 749 ---

ISO /IEC 1989.2023 (E)
7) After a rollback; execution continues with the next logical statement (see 14.6.3,Explicit and implicit transfers of control):
@ISO/IEC 2023
719
[per-copy licence stamp]


--- Page 750 ---

ISO /IEC 1989.2023 (E)
14.937 SEARCH statement
14.9.37.1 General
The SEARCH statement is used to search a table for a table element that satisfies the specified condition and to adjust the value of the associated index to indicate that table element:
14.9.37.2 General formats
Format 1 (serial):
SEARCH identifier-1 VARYING
identifier-2 index-name-1
[AT END imperative-statement-1 ]
imperative-statement-2 NEXT SENTENCE
WHEN condition-1
END-SEARCH
NOTE 1 NEXT SENTENCE is an archaic feature. For details see F.l,Archaic language elements_
Format 2 (all):
SEARCH ALL identifier-1 [ AT END imperative-statement-1 ]
identifier-3 literal-1 arithmetic-expression-1
IS EQUAL TO
WHEN
data-name-1
condition-name-1
IS EQUAL TO data-name-2
identifier-4 literal-2 arithmetic-expression-2
AND
condition-name-2
imperative-statement-2 NEXT SENTENCE END-SEARCH
NOTE 2 NEXT SENTENCE is an archaic feature. For details see F.l,Archaic language elements
720
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 751 ---

ISO /IEC 1989.2023 (E)
14.937.3 Syntax rules
ALL FORMATS
1) Identifier-1 shall not be reference-modified:
2) The data description of identifier-1 shall contain an OCCURS clause with an INDEXED phrase and identifier-1 shall not be subscripted at the level for which the SEARCH is applicable
3) Identifier-1 may be contained within one or more other tables, for which the subscripting is still required:
4) If the END-SEARCH phrase is specified, the NEXT SENTENCE phrase shall not be specified:
FORMAT 1
5) Identifier-2 shall reference a data item whose usage is index or a data item that is an integer: Identifier-2 shallnot be subscripted by the first or only index-name specified in the INDEXED phrase in the OCCURS clause specified in the data description entry for identifier-1_
Condition-1 may be any conditional   expression   evaluated as  specified in 8.8.4, Conditional expressions__
FORMAT 2
7) The OCCURS clause associated with identifier-1 shall contain the KEY phrase:
8) Data-name-1 and all repetitions of data-name-2 shall be subscripted by the first index-name associated with identifier-1 along with any subscripts required to uniquely identify the data item, and shall be referenced in the KEY phrase in the OCCURS clause associated with identifier-1. The index-name subscript shall not be followed by a or a
9) All  referenced   condition-names   shall be defined as having   only single value and shall be subscripted by the first index-name associated with identifier-1, along with any subscripts required to uniquely identify the condition-name: The data-name associated with each condition-name shall be specified in the KEY phrase in the OCCURS clause associated with identifier-1. The index-name subscript shall not be followed by a or a
10) Identifier-3,identifier-4,identifiers specified in arithmetic-expression-1,and identifiers specified in arithmetic-expression-2 shall be neither referenced in the KEY phrase of the OCCURS clause associated with identifier-1 nor subscripted by the first index-name associated with identifier-1.
11)" When a data-name in the KEY phrase in the OCCURS clauseassociated with identifier-1 is referenced or when condition-name associated with a data-name in the KEY phrase in the OCCURS clause associated with identifier-1 is referenced, all preceding data-names in that KEY phrase or their associated condition-names shall also be referenced:
12) Data-name-1, data-name-2, identifier-3,or identifier-4 shall not specify a variable-length group:
@ISO/IEC 2023
721
[per-copy licence stamp]


--- Page 752 ---

ISO /IEC 1989.2023 (E)
13) Neither literal-1 nor literal-2 shall be zero-length literals:
14.9.37.4 General rules
ALL FORMATS
1) The SEARCH statement automatically varies the first or only index associated with identifier-1 and tests conditions specified in WHEN phrases in the SEARCH statement to determine whether a table element satisfies these conditions_ Any subscripting specified in a WHEN phrase is evaluated each time the conditions in that WHEN phrase are evaluated. For Format 1, an additional index or data item may be varied. If identifier-1 references a data item that is subordinate to a data item whose data description entry contains an OCCURS clause, only the setting of an index associated with identifier-1 (and any data item referenced by identifier-2 or any index referenced by index-name-1, if specified) is modified by the execution of the SEARCH statement: The subscript that is used to determine the occurrence of each superordinate table to search is specified by the user in the WHEN phrases. Therefore, each appropriate subscript shall be set to the desired value before the SEARCH statement is executed:
Upon completion of the search operation, one of the following occurs:
a) Ifthe search operation is successful according to the general rules that follow, then: the search operation is terminated immediately; the index being varied by the search operation remains set at the occurrence number that caused a WHEN condition to be satisfied; if the WHEN phrase contains the NEXT SENTENCE phrase; control is transferred to an implicit CONTINUE statement immediately preceding the next separator period; if the WHEN phrase contains imperative-statement-2, control is transferred to that imperative-statement-2 and execution continues according to the rules for each statement specified in that imperative-statement-2. If the execution ofa procedure branching or conditional statement results in an explicit transfer of control, control is transferred in accordance with the rules for that statement; otherwise, upon completion of the execution of that imperative-statement-2, control is transferred to the end of the SEARCH statement:
b) If the search operation is unsuccessful according to the general rules that follow, then:
If the AT END phrase is specified, control is transferred to imperative-statement-1 and execution continues according to the rules for each statement specified in imperative-statement-1. If the execution ofa procedure branching or conditional statement results in an explicit transfer of control, control is transferred in accordance with the rules for that statement; otherwise, upon completion of the execution of imperative-statement-1, control is transferred to the end of the SEARCH statement
If the AT END phrase is not specified and either the EC-RANGE-SEARCH-INDEX or EC-RANGE-SEARCH-NO-MATCH exception condition was raised during the execution of the SEARCH statement and an applicable exception processing statement associated with that exception condition exists, control is transferred according to the rules for that statement and ifcontrol is returned from that statement; control is transferredto the end ofthe SEARCH statement_
722
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 753 ---

ISO /IEC 1989.2023 (E)
Ifthe AT END phrase is not specified and neither exception condition was raised because the checking for those exception conditions was not enabled, control is transferred to the end of the SEARCH statement;
2) The comparison associated with each WHEN phrase is executed in accordance with the rules specified for conditional expressions (See 8.8.4,Conditional expressions)
FORMAT 1
3) The index to be varied by the search operation is referred to as the search index and it is determined as follows:
If the VARYING phrase is not specified, the search index is the index referenced by the first (or only) index-name specified in the INDEXED phrase in the OCCURS clause associated with identifier-1.
b) Ifthe VARYING identifier-2 phrase is specified,the search index is the same as in General rule 3a and the following also applies:
If identifier-2 references an index data item, that data item is incremented by the same amount as,and at the same time as, the search index
Ifidentifier-2 references an integer data item, that data item is incremented by the value one at the same time as the search index is incremented_
Ifthe VARYING index-name-1 phrase is specified, the search index depends on the following:
Ifindex-name-1 is specified in the INDEXED BY phrase in the OCCURS clause associated with identifier-1,the index referenced by index-name-1 is the search index:
If index-name-1 is not one of the indexes specified in the INDEXED phrase in the OCCURS clause associated with identifier-1, the search index is the same aS in General rule 3a. The index referenced by index-name-l is incremented by one occurrence number at the same time as the search index is incremented_
Only the data item and indexes indicated are varied by the search operation. All other indexes associated with identifier-1 are unchanged by the search operation_
4) The search operation is serial, starting from the occurrence number that corresponds to the value of the search index at the beginning of the execution of the SEARCH statement: If, at the start of the execution, the search index contains a value that corresponds to an occurrence number that is negative, zero, or greater than the highest permissible occurrence number for identifier-1, the search operation is unsuccessful, the EC-RANGE-SEARCH-INDEX exception condition is set to exist; and execution proceeds as indicated in General rule 1b. The number of occurrences of identifier-1, the last of which is permissible, is specified in the OCCURS clause If,at the start of the execution of the SEARCH statement; the search index contains a value that corresponds to an occurrence number that is not greater than the highest permissible occurrence number for identifier-1_ the search operation proceeds by evaluating the conditions in the order they are written: If none of the conditions is satisfied, the search index is incremented by one occurrence number: The process is
@ISO/IEC 2023
723
[per-copy licence stamp]


--- Page 754 ---

ISO /IEC 1989.2023 (E)
then repeated using the new index setting unless the new value for the search index corresponds to a table element outside the permissible range of occurrence values, in which case the search operation is unsuccessful, the EC-RANGE-SEARCH-NO-MATCH exception condition is set to exist; and execution proceeds as indicated in General rule 1b. If one of the conditions is satisfied upon its evaluation, the search operation is successful and the execution proceeds as indicated in General rule 1a.
FORMAT 2
5) At the start of the execution of a SEARCH statement with the ALL phrase specified, the following conditions shall be true:
a) The contents of each key data item referenced in the WHEN phrase shall be sequenced in the table according to the ASCENDING or DESCENDING phrase associated with that key data item: (See 13.18.38,OCCURS clause:)
b) If identifier-1 is subordinate to one 0r more data description entries that contain an OCCURS clause, the evaluation of the conditions within a WHEN phrase that reference a key data item subordinate to identifier-1 shall result in the same occurrence number for any subscripts associated with a given level ofthe superordinate tables That is,the outermost level occurrence numbers shall all be equal, the next level occurrence numbers shall all be equal down to, but not including, the innermost table
If any condition specified in General rule 5 is not satisfied:
a) If one or more settings of the search index satisfy all conditions in the WHEN phrase, one of the following occurs:
the final setting of the search index is set equal to one of those settings, but it is undefined which one; execution proceeds as in General rule la;
the final  setting of the search index is undefined, the EC-RANGE-SEARCH-NO-MATCH exception condition is set to exist, and execution proceeds as in General rule 1b.
It is undefined which of these alternatives occurs.
b) Ifno such setting of the search index exists, the final setting of the search index is undefined, the EC-RANGE-SEARCH-NO-MATCH exception condition is set to exist; and execution proceeds as in General rule 1b.
7) If both conditions specified in General rule 5 are satisfied and there is more than one setting of the search index for which all conditions in the WHEN phrase are satisfied, the search operation is successful: The final setting ofthe search index is equal to one ofthem, but it is undefined which one_
8) The search index is the index referenced by the first (or only) index-name specified in the INDEXED phrase in the OCCURS clause associated with identifier-1. Any other indexes associated with identifier-1 remain unchanged by the search operation.
724
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 755 ---

ISO /IEC 1989.2023 (E)
9) Anon serial type ofsearch operation may take place The initial setting ofthe search index is ignored: Its setting is varied during the search operation in a manner specified by the implementor: At no time is it set to a value that exceeds the value that corresponds to the last element of the table or is less than the value that corresponds to the first element ofthe table The length of the table is discussed in the OCCURS clause. If any of the conditions specified in the WHEN phrase is not satisfied for any setting of the search index within the permitted range, the final setting of the search index is undefined, the search operation is unsuccessful, the EC-RANGE-SEARCH-NO-MATCH exception condition is set to exist; and execution proceeds as indicated in General rule 1b. If all the conditions are satisfied, the search operation is successful and execution proceeds as indicated in General rule 1a.
@ISO/IEC 2023
725
[per-copy licence stamp]


--- Page 756 ---

ISO /IEC 1989.2023 (E)
14.9.38 SEND statement
14.9.38.1 General
The SEND statement sends a message to a message server run unit and optionally receives a return message from the message server run unit or sends a return message to a requestor run unit:
14.9.38.2 General formats
Format 1 (tO-message-server)
literal-1 SEND TO message-server-name-1 RETURNING data-name-1
FROM identifier-1
ON EXCEPTION imperative-statement-1 NOT ON EXCEPTION imperative-statement-2
END-SEND
Format 2 (message-server-response)
SEND TO data-name-2 FROM identifier-1
EXCEPTION exception-name-1 LAST EXCEPTION
RAISING
ON EXCEPTION imperative-statement-1 NOT ON EXCEPTION imperative-statement-2
END-SEND
14.9.38.3 Syntax rules
1) Literal-1 shall be an alphanumeric or national literal
2) Message-server-name-1 is the name ofa message server run unit
3) The data description entries of identifier-1 or any data items subordinate to it shall not contain the ANY LENGTH clause, the BASED clause, the DYNAMIC-LENGTH clause, the OBJECT-REFERENCE clause, the OCCURS clause with the DEPENDING ON phrase where the depending on data item is not within identifier-1,the POINTER clause,the FUNCTION-POINTER clause or the PROGRAM-POINTER clause.
726
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 757 ---

ISO /IEC 1989.2023 (E)
NOTE The normal case would be to define identifier-1 as an 01 level item whose data description is exactly that in the message server
4) Data-name-1 and data-name-2 shall be the name ofa message-tag data item:
14.9.38.4 General rules
ALL FORMATS
1) The number of character positions of identifier-1 shall not be zero nor larger than the message server allows: If so and checking for that exception is enabled, the EC-MCS-MESSAGE-LENGTH exception is set to exist and the execution ofthe RECEIVE statement is unsuccessful. NOTE The user is responsible for ensuring that the content ofidentifier-1 and the matching data item in the message server RECEIVE statement correspond: It is not possible for the MCS to detect any differences The message server will determine ifthe size ofthe message sent differs from the size of the message expected and will return the appropriate exception condition
2) If the execution of the SEND statement is successful, the ON EXCEPTION phrase, if specified, is ignored and control is transferred to the end of the SEND statement; r, if the NOT ON EXCEPTION phrase is specified to imperative-statement-2. If control is returned from imperative-statement-2, control is then transferred to the end of the SEND statement;
3) If the execution ofthe SEND statement is unsuccessful, then:
a) If the ON EXCEPTION phrase is specified in the SEND statement; control is transferred to imperative-statement-1 If control is returned from imperative-statement-1, control is transferred to the end of the SEND statement
b) Ifthe ON EXCEPTION phrase is not specified in the SEND statement; one ofthe following occurs:
If the SEND statement is specified in a statement that is in imperative-statement-1 in an exception-checking PERFORM statement and a WHEN phrase in that statement specifies the exception condition that occurred, control is transferred to that WHEN phrase: If control is returned from the WHEN  phrase, control is then transferred to the end of the SEND statement;
If there is no applicable WHEN phrase and there is an applicable USE declarative, control is transferred to that declarative. If control is returned from the declarative, control is then transferred to the end ofthe SEND statement:
Otherwise, control is transferred to the end ofthe SEND statement
FORMAT 1
4) Amessage is sent to the run unit identified by the content of literal-1 or message-server-name-1. The message contains the content of data-name-1 as well as any additional implementor-defined data_ This additional data is not included in the message moved to the data item in the receiving run unit The statement suspends execution until the message is received by the server run unit The server run unit does not necessarily have to respond to the message.
@ISO/IEC 2023
727
[per-copy licence stamp]


--- Page 758 ---

ISO /IEC 1989.2023 (E)
5) Message-server-name-1 or literal-1 identify a message server run unit If such a run unit is not identified, the EC-MCS-NO-SERVER exception condition is set to exist; and the execution of the SEND statement is unsuccessful.
If the execution ofthe SEND statement is successful, data-name-1 contains the implementor-defined information (the message tag) that identifies the server run unit and the message sent
FORMAT 2
7) Data-name-2 shall contain a message tag that identifies a requestor run unit that was received by the currentrun unit via a RECEIVE statement and has not yet been responded to by a SEND statement in the current run unit If data-name-2 does not identify such a run unit; the EC-MCS-NO-RECEIVER exception condition is raised,and the execution of the SEND statement is unsuccessful
8) Ifthe RAISING phrase is specified, exception-name-1 or the name of the last exception raised in the current run unit is sent to the requestor program:
728
@ISO/IEC 2023
[per-copy licence stamp]


--- Page 759 ---

ISO /IEC 1989.2023 (E)
14.9.39 SET statement
14.9.39.1 General
The SET statement provides a means for:
establishing reference points for table handling operations by setting indexes associated with table elements, altering the status of external switches, altering the value of conditional variables, assigning object references, altering the attributes associated with a screen item, assigning the address ofa data item to a data-pointer data item, assigning the address ofa based item, assigning the address of a function to a function-pointer data item, assigning the address ofa program to a program-pointer data item  setting and saving locale categories, clearing the last exception status, setting the capacity ofa dynamic capacity table, setting numeric maxima and minima, setting floating point non-numeric values,and setting the length ofa dynamic-length elementary data item: setting the content ofa message-tag data item to NULL or another message-tag
14.9.39.2 General formats
Format 1 (index-assignment):
arithmetic-expression-1 TQ index-name-2 identifier-2
index-name-1 identifier-1
SET
Format 2 (index-arithmetic):
UP BY arithmetic-expression-2 DOWN BY
SET index-name-3
Format 3 (switch-setting):
ON mnemonic-name-1 } To OFF
SET
@ISO/IEC 2023
729
[per-copy licence stamp]


--- Page 760 ---

ISO /IEC 1989.2023 (E)
Format 4 (condition-setting):
TRUE FALSE
SET
condition-name-1
TQ
Format 5 (object-reference-assignment):
SET { identifier-3
object-class-name-1 TQ identifier-4
Format 6 (attribute):
BELL BLINK HIGHLIGHT
OFF ON
SET screen-name-1 ATTRIBUTE
LOWLIGHT REVERSE-VIDEO UNDERLINE
Format (data-pointer-assignment)=
ADDRESS OF data-name-1 identifier-5
SET
TO identifier-6
Format 8 (function-pointer-assignment):
SET { identifier-12 } TO identifier-13
Format 9 (program-pointer-assignment):
SET { identifier-7 } TQ identifier-8
730
OISO /IEC 2023
[per-copy licence stamp]
