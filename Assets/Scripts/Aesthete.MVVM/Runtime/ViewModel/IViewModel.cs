using System;

namespace Aesthete.MVVM.ViewModels
{
    /// <summary>
    /// Interface for all ViewModels
    /// </summary>
    public interface IViewModel : IDisposable
    {
        public void Bind();
        public void Unbind();
    }
}