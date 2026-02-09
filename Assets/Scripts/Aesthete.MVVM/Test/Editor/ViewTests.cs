using NUnit.Framework;
using Aesthete.MVVM.Views;
using Aesthete.MVVM.ViewModels;


public class ViewTests
{
    class TestViewModel : ViewModel
    {
        public override void Bind() { }
        public override void Unbind() { }
        public override void Dispose() { }
    }

    class TestView : View
    {
        public bool BoundFlag { get; private set; }
        public bool UnboundFlag { get; private set; }
        public bool DisposedFlag { get; private set; }

        public override void Bind()
        {
            BoundFlag = true;
        }

        public override void Unbind()
        {
            UnboundFlag = true;
        }

        public override void Dispose()
        {
            DisposedFlag = true;
        }
    }

    [Test]
    public void Bind_SetsViewModelAndFlag()
    {
        var view = new TestView();
        view.Bind();
        Assert.IsTrue(view.BoundFlag);
    }

    [Test]
    public void UnbindAndDispose_SetsFlags()
    {
        var view = new TestView();
        view.Unbind();
        Assert.IsTrue(view.UnboundFlag);
        view.Dispose();
        Assert.IsTrue(view.DisposedFlag);
    }
}
