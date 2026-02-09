using System;
using Aesthete.MVVM.ViewModels;

namespace Aesthete.MVVM.Views
{
    /// <summary>
    /// Base class for all Views that are Singletons
    /// </summary>
    public abstract class SingletonView<T> : View where T : SingletonView<T>
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new InvalidOperationException("SingletonView instance is not created yet.");
                }
                return _instance;
            }
        }

        protected SingletonView()
        {
            if (_instance != null)
            {
                throw new InvalidOperationException("An instance of SingletonView already exists.");
            }
            _instance = (T)(object)this;
        }

        public override void Dispose()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}