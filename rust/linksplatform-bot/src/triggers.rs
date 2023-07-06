pub trait Trigger<TContext> {
    fn Condition(context: TContext) -> bool;
    fn Action(context: TContext);
}
