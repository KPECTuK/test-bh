namespace BH.Components
{
	public interface IWidgetController
	{
		bool IsBusy { get; }

		void OnScreenEnter();
		void OnScreenExit();
	}
}
