using System;
using Aesthete.MVVM.ViewModels;

namespace Aesthete.MVVM.ViewModels
{
    /// <summary>
    /// Base class for all Views that are Singletons
    /// </summary>
    public abstract class SingletonViewmodel<TSelf> : ViewModel where TSelf : SingletonViewmodel<TSelf>
    {
        private static TSelf _instance;

        public static TSelf Instance
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

        protected SingletonViewmodel()
        {
            if (_instance != null)
            {
                throw new InvalidOperationException("An instance of SingletonView already exists.");
            }
            _instance = (TSelf)(object)this;
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