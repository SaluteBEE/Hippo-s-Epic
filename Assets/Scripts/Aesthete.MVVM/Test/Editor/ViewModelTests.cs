using NUnit.Framework;
using Aesthete.MVVM.ViewModels;


public class ViewModelTests
{
    class TestViewModel : ViewModel
    {
        public bool Bound { get; private set; }
        public bool Unbound { get; private set; }
        public bool Disposed { get; private set; }
        public override void Bind() => Bound = true;
        public override void Unbind() => Unbound = true;
        public override void Dispose() => Disposed = true;
    }

    [Test]
    public void BindUnbindDispose_SetsFlags()
    {
        var vm = new TestViewModel();
        Assert.IsFalse(vm.Bound);
        vm.Bind();
        Assert.IsTrue(vm.Bound);
        vm.Unbind();
        Assert.IsTrue(vm.Unbound);
        vm.Dispose();
        Assert.IsTrue(vm.Disposed);
    }
}
