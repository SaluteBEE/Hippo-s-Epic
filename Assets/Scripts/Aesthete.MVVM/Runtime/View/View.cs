using System;
using Aesthete.MVVM.ViewModels;
using Codice.Client.Commands.CheckIn;

namespace Aesthete.MVVM.Views
{
    /// <summary>
    /// Base class for all Views
    /// </summary>
    public abstract class View : IView
    {
        public abstract void Bind();
        public abstract void Unbind();

        public abstract void Dispose();
    }
}