using System;
using Aesthete.MVVM.ViewModels;

namespace Aesthete.MVVM.Views
{
    public interface IView : IDisposable
    {
        void Bind();
        void Unbind();
    }
    /// <summary>
    /// Interface for all Views
    /// </summary>
    public interface IView<TViewModel> : IView where TViewModel : IViewModel
    {
        public void SetViewModel(TViewModel viewModel);

    }
}

