namespace WiseOwl.Cobol.Binding.Passes;

internal sealed record BindPass(string Name, PassPhase Requires, PassPhase Produces, Action<DataBinder> Body) : IBindPass
{
    public PassViews Views => PassViews.None;
    public void Run(DataBinder binder) => Body(binder);
}
