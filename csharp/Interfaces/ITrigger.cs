
namespace Interfaces
{
    public interface ITrigger<TContext>
    {
        public bool Condition(TContext obj);

        public void Action(TContext obj);
    }
}
