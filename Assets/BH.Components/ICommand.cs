namespace BH.Components
{
	public interface ICommand<in T>
	{
		bool Assert(T context);
		void Execute(T context);
	}
}
