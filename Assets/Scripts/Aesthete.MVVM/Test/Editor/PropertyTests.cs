using NUnit.Framework;
using Aesthete.MVVM.Properties;
using System;

public class PropertyTests
{
    [Test]
    public void Value_WhenSet_AppliesValue()
    {
        var property = new Property<int>();
        property.Value = 42;
        Assert.AreEqual(42, property.Value);
    }

    [Test]
    public void Value_WhenSet_NotifiesSubscribers()
    {
        var property = new Property<int>();
        int notifiedValue = 0;
        property.Subscribe(value => notifiedValue = value);
        
        property.Value = 42;
        
        Assert.AreEqual(42, notifiedValue);
    }

    [Test]
    public void Value_WhenSetMultipleTimes_NotifiesSubscribersEachTime()
    {
        var property = new Property<string>();
        int callCount = 0;
        string lastValue = null;
        
        property.Subscribe(value =>
        {
            callCount++;
            lastValue = value;
        });
        
        property.Value = "first";
        property.Value = "second";
        property.Value = "third";
        
        Assert.AreEqual(3, callCount);
        Assert.AreEqual("third", lastValue);
    }

    [Test]
    public void Value_WithMultipleSubscribers_NotifiesAll()
    {
        var property = new Property<int>();
        int subscriber1Value = 0;
        int subscriber2Value = 0;
        int subscriber3Value = 0;
        
        property.Subscribe(value => subscriber1Value = value);
        property.Subscribe(value => subscriber2Value = value);
        property.Subscribe(value => subscriber3Value = value);
        
        property.Value = 99;
        
        Assert.AreEqual(99, subscriber1Value);
        Assert.AreEqual(99, subscriber2Value);
        Assert.AreEqual(99, subscriber3Value);
    }

    [Test]
    public void Value_AfterUnsubscribe_DoesNotNotifyUnsubscribedHandler()
    {
        var property = new Property<int>();
        int subscriber1Value = 0;
        int subscriber2Value = 0;
        
        Action<int> handler1 = value => subscriber1Value = value;
        Action<int> handler2 = value => subscriber2Value = value;
        
        property.Subscribe(handler1);
        property.Subscribe(handler2);
        
        property.Value = 10;
        Assert.AreEqual(10, subscriber1Value);
        Assert.AreEqual(10, subscriber2Value);
        
        // Unsubscribe handler1
        property.Unsubscribe(handler1);
        
        property.Value = 20;
        Assert.AreEqual(10, subscriber1Value); // Should not change
        Assert.AreEqual(20, subscriber2Value); // Should change
    }

    [Test]
    public void Value_WhenSubscriberThrowsException_ContinuesWithOtherSubscribers()
    {
        var property = new Property<int>();
        int subscriber1Value = 0;
        int subscriber3Value = 0;
        
        property.Subscribe(value => subscriber1Value = value);
        property.Subscribe(value => throw new Exception("Test exception"));
        property.Subscribe(value => subscriber3Value = value);
        
        // Should throw when a subscriber throws, but still continue notifying other subscribers
        Assert.Catch(() => property.Value = 42);
        
        // All subscribers should be notified even if one throws
        Assert.AreEqual(42, subscriber1Value);
        Assert.AreEqual(42, subscriber3Value);
    }

    [Test]
    public void Value_WithNoSubscribers_DoesNotThrow()
    {
        var property = new Property<int>();
        Assert.DoesNotThrow(() => property.Value = 42);
        Assert.AreEqual(42, property.Value);
    }

    [Test]
    public void Subscribe_WithNullHandler_Throws()
    {
        var property = new Property<int>();
        var ex = Assert.Throws<ArgumentNullException>(() => property.Subscribe(null));
        Assert.AreEqual("onValueChanged", ex.ParamName);
    }

    [Test]
    public void Unsubscribe_WithNullHandler_DoesNotThrow()
    {
        var property = new Property<int>();
        Assert.DoesNotThrow(() => property.Unsubscribe(null));
    }

    [Test]
    public void Value_WithReferenceType_StoresReference()
    {
        var property = new Property<string>();
        string testValue = "test string";
        property.Value = testValue;
        Assert.AreSame(testValue, property.Value);
    }

    [Test]
    public void Value_WhenSetToSameValue_DoesNotNotifySubscribers()
    {
        var property = new Property<int>();
        int callCount = 0;
        
        property.Subscribe(value => callCount++);
        
        // Set initial value
        property.Value = 42;
        Assert.AreEqual(1, callCount);
        
        // Set to the same value
        property.Value = 42;
        Assert.AreEqual(1, callCount); // Notification should not be triggered again
    }

    [Test]
    public void Dispose_WhenCalled_PreventsFutureNotifications()
    {
        var property = new Property<int>();
        int callCount = 0;
        
        property.Subscribe(value => callCount++);
        
        // Set value before dispose
        property.Value = 42;
        Assert.AreEqual(1, callCount);
        
        // Dispose and set value again
        property.Dispose();
        property.Value = 100;
        
        // Notification should not be triggered after dispose
        Assert.AreEqual(1, callCount);
    }

    [Test]
    public void Dispose_WhenCalledMultipleTimes_DoesNotThrow()
    {
        var property = new Property<int>();
        property.Subscribe(value => { });
        
        // Should not throw on first dispose
        Assert.DoesNotThrow(() => property.Dispose());
        
        // Should not throw on subsequent disposes
        Assert.DoesNotThrow(() => property.Dispose());
        Assert.DoesNotThrow(() => property.Dispose());
    }
}
