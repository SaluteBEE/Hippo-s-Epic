namespace Aesthete.MVVM.ViewModels
{
    /// <summary>
    /// Base class for all ViewModels
    /// </summary>
    public abstract class ViewModel : IViewModel
    {
        public abstract void Bind();
        public abstract void Unbind();

        public abstract void Dispose();
    }
}