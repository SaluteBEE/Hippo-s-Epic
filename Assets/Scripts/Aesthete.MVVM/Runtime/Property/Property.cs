using System;
using System.Collections.Generic;

namespace Aesthete.MVVM.Properties
{
    /// <summary>
    /// Interface for all Properties
    /// </summary>
    public interface IReadonlyProperty<T>
    {
        T Value { get; }
        void Subscribe(Action<T> onValueChanged);
        void Unsubscribe(Action<T> onValueChanged);
    }

    public interface IProperty<T> : IReadonlyProperty<T>
    {
        new T Value { get; set; }
    }

    public sealed class Property<T> : IProperty<T>, IDisposable
    {
        private T _value;

        public T Value
        {
            get => _value;
            set
            {
                if (!EqualityComparer<T>.Default.Equals(value, _value))
                {
                    _value = value;
                    var handlerSnapshot = _valueChangedHandler;
                    if (handlerSnapshot != null)
                    {
                        List<Exception> exceptions = null;
                        foreach (var handler in handlerSnapshot.GetInvocationList())
                        {
                            try
                            {
                                ((Action<T>)handler)(value);
                            }
                            catch (Exception ex) when (ex is not OutOfMemoryException)
                            {
                                exceptions ??= new List<Exception>();
                                exceptions.Add(ex);
                            }
                        }
                        if (exceptions != null)
                        {
                            throw new AggregateException(exceptions);
                        }
                    }
                }
            }
        }

        private Action<T> _valueChangedHandler;

        public void Subscribe(Action<T> onValueChanged)
        {
            if (onValueChanged == null)
            {
                throw new ArgumentNullException(nameof(onValueChanged));
            }
            _valueChangedHandler += onValueChanged;
        }

        public void Unsubscribe(Action<T> onValueChanged)
        {
            _valueChangedHandler -= onValueChanged;
        }

        public void Dispose()
        {
            _valueChangedHandler = null;
        }

        public Property(T value = default)
        {
            _value = value;
        }
    }
}