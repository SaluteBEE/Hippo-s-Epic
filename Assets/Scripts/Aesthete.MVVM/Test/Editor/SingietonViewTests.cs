using NUnit.Framework;
using Aesthete.MVVM.Views;
using System;


public class SingletonViewTests
{
    class TestSingletonView : SingletonView<TestSingletonView>
    {
        public override void Bind() { }
        public override void Unbind() { }
    }

    [TearDown]
    public void TearDown()
    {
        // 在每个测试后清理实例，防止影响其他测试
        try
        {
            TestSingletonView.Instance?.Dispose();
        }
        catch
        {
            // 如果实例不存在，忽略异常
        }
    }

    [Test]
    public void CreateInstance_CanGetInstanceThroughProperty()
    {
        // Arrange & Act
        var instance = new TestSingletonView();
        
        // Assert
        Assert.IsNotNull(TestSingletonView.Instance);
        Assert.AreSame(instance, TestSingletonView.Instance);
    }

    [Test]
    public void CreateMultipleInstances_ThrowsInvalidOperationException()
    {
        // Arrange
        var firstInstance = new TestSingletonView();
        
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new TestSingletonView());
    }

    [Test]
    public void GetInstanceBeforeCreation_ThrowsInvalidOperationException()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => { var _ = TestSingletonView.Instance; });
    }

    [Test]
    public void DisposeInstance_CanCreateNewInstanceAfter()
    {
        // Arrange
        var firstInstance = new TestSingletonView();
        var firstInstanceRef = TestSingletonView.Instance;
        
        // Act
        firstInstance.Dispose();
        
        // Assert - 在Dispose后，尝试访问Instance应该抛出异常
        Assert.Throws<InvalidOperationException>(() => { var _ = TestSingletonView.Instance; });
        
        // 可以创建新的实例
        var secondInstance = new TestSingletonView();
        Assert.IsNotNull(TestSingletonView.Instance);
        Assert.AreNotSame(firstInstanceRef, TestSingletonView.Instance);
        Assert.AreSame(secondInstance, TestSingletonView.Instance);
    }
}
