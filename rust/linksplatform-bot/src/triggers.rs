pub trait Trigger<TContext> {
    fn Condition(context: TContext) -> bool;
    fn Action(context: TContext);
}

struct RemoveWorkflowIfRelatedFolderDoesNotExistTrigger {

}

impl Trigger<Repository> for RemoveWorkflowIfRelatedFolderDoesNotExistTrigger {
    fn Condition(context: Repository) -> bool {
        
    }
}

struct