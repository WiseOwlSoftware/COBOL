// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.CodeGen.Emit;

namespace WiseOwl.Cobol.CodeGen;

/// <summary>The PER-UNIT composition root (P7 Step 9n — the phase doc's AS-BUILT PLAN): one instance per
/// emitted unit (program class, OO class half, interface unit), constructing the per-unit
/// <see cref="EmitContext"/>/renderer quadruple and EVERY collaborator emitter over it, with the cyclic
/// edges (verbs↔<see cref="StatementEmitter"/>↔<see cref="EcEmitter"/>, KeyedIo↔SequentialIo) property-wired
/// post-construction — the EC↔statement cycle means no pure bottom-up construction order exists (the
/// Step-9 coupling census <c>wf_d677d614-5fb</c>). Replaces the transitional CSharpEmitter host edges: the
/// collaborators now hold DIRECT references to each other; the former god class survives only as the
/// bind-host facade (the P6→P9 seam; the OO bind bodies live on <c>Oo/OoDriver</c> since P9 Step 4). <see cref="ProgramEmitter.BeginUnit"/> re-creates
/// this root at each unit switch — consumers that must track the switch mid-run (OoEmitter) read through
/// <see cref="ProgramEmitter.Current"/>, never a captured copy.</summary>
internal sealed class UnitEmitters
{
    public EmitContext Ctx { get; }
    public NumericRenderer Num { get; }
    public ConditionRenderer Cond { get; }
    public PlaceTable Places { get; }

    public DebugTriggerEmitter Debug { get; }
    public ReceiverBracketEmitter Brackets { get; }
    public MoveEmitter Move { get; }
    public EcEmitter Ec { get; }
    public ArithmeticEmitter Arith { get; }
    public AlterSwitchEmitter AlterSwitch { get; }
    public AcceptDisplayEmitter AcceptDisplay { get; }
    public EvaluateEmitter Evaluate { get; }
    public InitializeEmitter Initialize { get; }
    public CorrespondingEmitter Corresponding { get; }
    public InspectEmitter Inspect { get; }
    public StringEmitter Strings { get; }
    public PtrEmitter Ptr { get; }
    public SetEmitter Set { get; }
    public TableHoldEmitter TableHold { get; }
    public KeyedIoEmitter KeyedIo { get; }
    public SequentialIoEmitter SeqIo { get; }
    public SortEmitter Sort { get; }
    public TransactionEmitter Transactions { get; }
    public CommunicationEmitter Communication { get; }
    public MessagingEmitter Messaging { get; }
    public ReportWriterEmitter ReportWriter { get; }
    public ControlFlowEmitter ControlFlow { get; }
    public CallEmitter Call { get; }
    public StatementEmitter Statements { get; }
    public DispatchEmitter Dispatch { get; }

    public UnitEmitters(CodeWriter w, DataBindResult data, PlaceTable places, NameAllocator names, GroupAtomTable atoms,
        DispatchState dispatchState, EcState ecState, CallUnitState callState, OoEmitter oo,
        Lazy<string> whenCompiledStamp)
    {
        Places = places;
        Ctx = new EmitContext(w, data, names, atoms, whenCompiledStamp);
        Num = new NumericRenderer(Ctx, ecState);
        Cond = new ConditionRenderer(Num, Ctx);

        // Acyclic construction order (each ctor takes only already-built collaborators); the cycles close
        // via the property wiring below.
        Debug = new DebugTriggerEmitter(Ctx, Places, Num);   // the data-name, file-name and cd-name USE FOR DEBUGGING triggers (docs/CONTROL_FLOW_DESIGN.md D20)
        dispatchState.OwedBeforeTransfer = Debug.OwedBeforeTransferText;   // the RESUME landing runs the sections the abandoned statements owe (kb/Work PB4857)
        Brackets = new ReceiverBracketEmitter(Ctx);
        Move = new MoveEmitter(Ctx, Num, Places, Brackets, Debug);
        Ec = new EcEmitter(Ctx, ecState, dispatchState);
        Arith = new ArithmeticEmitter(Ctx, Num, ecState, Ec, Brackets, Debug);
        AlterSwitch = new AlterSwitchEmitter(Ctx, dispatchState);
        AcceptDisplay = new AcceptDisplayEmitter(Ctx, Num, Move);
        Evaluate = new EvaluateEmitter(Ctx, Cond);
        Initialize = new InitializeEmitter(Ctx, Move);
        Corresponding = new CorrespondingEmitter(Ctx, Num, Move, Arith, Ec);
        Inspect = new InspectEmitter(Ctx, Num, Arith);
        Strings = new StringEmitter(Ctx, Num, Arith, Ec, Move, Brackets);
        Ptr = new PtrEmitter(Ctx, Num, ecState, Ec);
        Set = new SetEmitter(Ctx, Num, Arith, Ptr, Move, Brackets, Debug);
        TableHold = new TableHoldEmitter(Ctx, Arith);   // the held form of a table(ALL) function argument (kb/Work PB4162)
        KeyedIo = new KeyedIoEmitter(Ctx, Num, Places, Arith, Move);
        SeqIo = new SequentialIoEmitter(Ctx, Num, Places, dispatchState, ecState, KeyedIo, Arith, Ec, Move);
        Transactions = new TransactionEmitter(Ctx, Places, SeqIo);   // APPLY COMMIT, COMMIT, ROLLBACK (kb/Work PB3338)
        Communication = new CommunicationEmitter(Ctx, Num, Places, SeqIo);   // the communication facility (kb/Work PB4677)
        Messaging = new MessagingEmitter(Ctx, Num, ecState, Ec, SeqIo, Arith);   // the asynchronous messaging facility (kb/Work PB4744)
        Sort = new SortEmitter(Ctx, Places, dispatchState, SeqIo, Move, Arith, Ec);   // no NumericRenderer since RELEASE moved to the ONE record-area channel (kb/Work PB327)
        ReportWriter = new ReportWriterEmitter(Ctx, Num, Places, Move, Cond, dispatchState, ecState, Ec);
        ControlFlow = new ControlFlowEmitter(Ctx, Num, Cond, dispatchState, Set, Brackets);
        Call = new CallEmitter(Ctx, Num, ecState, callState, Ec, Move, dispatchState, Ptr, Brackets);
        Statements = new StatementEmitter(this, oo, dispatchState);
        Dispatch = new DispatchEmitter(Ctx, dispatchState, ecState, AlterSwitch, ReportWriter, SeqIo, Ec, Statements, Transactions, Communication);

        // The cyclic edges (statement lists nest arbitrarily inside verb phrases; EcEmitChecked re-enters
        // EmitStatement; KeyedIo consumes SequentialIo's file-I/O common services although SeqIo's ctor
        // already took KeyedIo for the shared READ/REWRITE bodies).
        Ec.Statements = Statements;
        Brackets.Statements = Statements;
        Arith.Statements = Statements;
        Evaluate.Statements = Statements;
        Strings.Statements = Statements;
        KeyedIo.Statements = Statements;
        KeyedIo.SeqIo = SeqIo;
        SeqIo.Statements = Statements;
        Sort.Statements = Statements;
        Communication.Statements = Statements;
        Messaging.Statements = Statements;
        Communication.Debug = Debug;
        KeyedIo.Debug = Debug;
        SeqIo.Debug = Debug;
        ControlFlow.Debug = Debug;
        Call.Debug = Debug;
        Evaluate.Debug = Debug;
        TableHold.Statements = Statements;
        ControlFlow.Statements = Statements;
        ControlFlow.Ec = Ec;   // CA36: SEARCH with EC-RANGE checking + no AT END dispatches via EcEmitter.EcDispatchExpr
        Call.Statements = Statements;
        Cond.Calls = Call;   // BoundUdfEvaluated — the per-evaluation function-activation text (P10 Step 10)
        Cond.Statements = Statements;
        Cond.Ptr = Ptr;   // an address-identifier relation operand (kb/Work PB1021)   // …and its NON-call pre-ops (a D18 subscript temp store), captured as text
    }
}
