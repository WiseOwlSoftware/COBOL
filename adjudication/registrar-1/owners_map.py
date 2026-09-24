"""Registrar-1 ownership map (kb/Work PB1522): every adjudicated row -> the kb/Work note(s) that own its mechanism.
The FIRST id is the primary owner written into the record's notes; every listed note claims the row in inventory_rows."""
D = lambda *ns: ['DOC-A.1-%d' % n for n in ns]
EXTEND = {  # existing OPEN notes
    'PB1092': D(8, 43, 186),
    'PB1369': D(23, 29, 40, 53, 54, 55, 79, 80, 111),
    'PB1375': D(80), 'PB1376': D(79),
    'PB1397': D(25, 26, 32, 44, 95),
    'PB322': D(52, 76, 77, 130, 131, 218),
    'PB1253': D(116, 159, 160),
    'PB1278': D(146, 147, 148),
    'PB1496': D(117, 156), 'PB1491': D(157),
    'PB1520': D(121), 'PB1149': D(141, 167),
    'PB160': D(97, 122, 217),
    'PB690': D(31),
    'PB1086': D(66, 67, 161, 162),
    'PB1069': D(168),
    'PB661': D(219), 'PB1402': D(219),
    'PB1422': D(14, 89, 102),
    'PB1268': D(60), 'PB1410': D(60, 61), 'PB1094': D(63), 'PB1110': D(81), 'PB1192': D(107, 108),
    'PB1393': D(9), 'PB1383': D(68), 'PB1355': D(40), 'PB643': D(164), 'PB538': D(196), 'PB1178': D(192),
    'PB1512': D(103),
    'PB1193': ['SR-13.18.27.3-3'],
    'PB1506': ['GR-16.2.1.2-1', 'GR-16.2.2.2-1'],
    'PB298': ['GR-4.2.10-2'],
    'PB1413': ['SR-5.5-2'],
    # NEEDS-OWNER-DECISION rows: their owner notes, made mechanical
    'PB1151': ['GR-14.9.1.4-24', 'GR-14.9.1.4-L2.1', 'GR-14.9.1.4-L2.2', 'GR-14.9.1.4-L3.1', 'GR-14.9.1.4-L3.2',
               'GR-14.6.11-7', 'GR-14.7.7-3'],
    'PB1099': ['GR-12.4.5.6.4-2', 'SR-12.4.5.6.3-6', 'FMT-12.4.5.12.2', 'GR-12.4.5.12.4-2', 'SR-12.4.5.12.3-5',
               'FMT-12.4.6.3.2'] + D(161, 162),
    'PB1255': ['GR-13.18.13.4-4'], 'PB1518': ['GR-9.1.13.7-5'], 'PB1517': ['GR-11.9.5.2-2'],
    'PB1198': ['GR-14.9.51.4-24', 'GR-8.4.3.10.4-4', 'GR-8.8.4.2.1-9', 'FMT-14.9.31.2'] +
              ['GR-14.9.31.4-%d' % i for i in range(1, 6)] + ['SR-14.9.31.3-%d' % i for i in range(1, 4)] +
              ['FMT-14.9.38.2'] + ['SR-14.9.38.3-%d' % i for i in range(1, 5)] + ['GR-14.9.38.4-%d' % i for i in range(1, 9)] + D(212),
    'PB579': ['SR-11.9.9.3-%d' % i for i in range(1, 7)] + D(47),
    'PB1519': ['GR-9.3.6-L5.1', 'GR-9.3.6-L5.2', 'GR-9.3.6-L5.3'],
    'PB468': ['GR-4.4-1', 'GR-4.4-2'],
}
NEW = {
    'PB1523': ['GR-13.18.27.4-1', 'GR-13.18.27.4-3'],
    'PB1524': ['GR-16.2.1.2-2'],
    'PB1525': ['GR-4.2.10-1', 'GR-4.2.10-3'],
    'PB1526': ['GR-5.5-3'],
    'PB1527': D(214),
    'PB1528': ['GR-14.9.1.4-24', 'GR-14.9.1.4-L2.1', 'GR-14.9.1.4-L2.2', 'GR-14.9.1.4-L3.1', 'GR-14.9.1.4-L3.2'],
    'PB1529': D(39),
    'PB1530': D(72),
    'PB1531': D(99, 100),
    'PB1532': D(113),
    'PB1533': D(49),
    # PB1534 (analysis: determinations fixed in code, no item-specific note) = the DOC residue, computed below
    # PB1535 (decision: the DOC-row blocker) = every batch-doc row, computed below
    'PB1536': D(149, 152, 200, 30, 34, 37, 142, 143, 88, 96, 98) + ['FMT-5.2'],
}
PRIMARY_ORDER = ['PB1523', 'PB1524', 'PB1525', 'PB1526', 'PB1527', 'PB1529', 'PB1530', 'PB1531', 'PB1532', 'PB1533',
                 'PB1193', 'PB1506', 'PB298', 'PB1413']
