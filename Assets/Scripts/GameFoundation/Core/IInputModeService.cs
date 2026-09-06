namespace GameFoundation.Core
{
    public interface IInputModeService
    {
        InputMode CurrentMode { get; }
        void SetMode(InputMode mode);
    }
}
