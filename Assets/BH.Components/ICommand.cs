namespace BH.Components
{
	public interface ICommand<in T>
	{
		bool Assert(T context);
		void Execute(T context);
	}

	/// <summary>
	/// brakes execution queue after that
	/// </summary>
	public interface ICommandBreak<in T> : ICommand<T> { }
}
