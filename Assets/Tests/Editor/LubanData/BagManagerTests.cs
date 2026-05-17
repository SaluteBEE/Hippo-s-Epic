using NUnit.Framework;
using UnityEngine;

class BagManagerTests
{
    private BagManager _bag;

    [SetUp]
    public void Setup()
    {
        _bag = new BagManager();
        ManagerRegistry.Clear();

        var go = new GameObject("[DataTableManager]");
        var dtm = go.AddComponent<DataTableManager>();
        dtm.LoadTables();
        ManagerRegistry.Register(dtm);
    }

    [TearDown]
    public void TearDown()
    {
        ManagerRegistry.Clear();
    }

    [Test]
    public void InitFromConfig_LoadsPlayerItems()
    {
        _bag.InitFromConfig();

        Assert.IsTrue(_bag.HasItem(1, 1), "应包含钱币(itemId=1)");
        Assert.AreEqual(500, _bag.GetItemCount(1), "钱币数量应为500");
    }

    [Test]
    public void AddItem_Stackable_MergesCount()
    {
        _bag.AddItem(1, 100);
        Assert.AreEqual(100, _bag.GetItemCount(1));

        _bag.AddItem(1, 50);
        Assert.AreEqual(150, _bag.GetItemCount(1));
    }

    [Test]
    public void AddItem_NonStackable_CreatesMultipleEntries()
    {
        _bag.AddItem(2, 3);
        Assert.AreEqual(3, _bag.GetItemCount(2));
        Assert.AreEqual(3, _bag.AllItems.Count);
    }

    [Test]
    public void AddItem_NewItem_SetsCount()
    {
        _bag.AddItem(2, 1);
        Assert.AreEqual(1, _bag.GetItemCount(2));
    }

    [Test]
    public void AddItem_ZeroCount_DoesNothing()
    {
        _bag.AddItem(1, 0);
        Assert.AreEqual(0, _bag.GetItemCount(1));
    }

    [Test]
    public void RemoveItem_Success_DecreasesCount()
    {
        _bag.AddItem(1, 200);
        bool result = _bag.RemoveItem(1, 50);

        Assert.IsTrue(result);
        Assert.AreEqual(150, _bag.GetItemCount(1));
    }

    [Test]
    public void RemoveItem_ExactCount_RemovesEntry()
    {
        _bag.AddItem(2, 1);
        bool result = _bag.RemoveItem(2, 1);

        Assert.IsTrue(result);
        Assert.AreEqual(0, _bag.GetItemCount(2));
        Assert.IsFalse(_bag.HasItem(2));
    }

    [Test]
    public void RemoveItem_Insufficient_ReturnsFalse()
    {
        _bag.AddItem(1, 10);
        bool result = _bag.RemoveItem(1, 20);

        Assert.IsFalse(result);
        Assert.AreEqual(10, _bag.GetItemCount(1));
    }

    [Test]
    public void RemoveItem_NotOwned_ReturnsFalse()
    {
        bool result = _bag.RemoveItem(99, 1);
        Assert.IsFalse(result);
    }

    [Test]
    public void HasItem_Sufficient_ReturnsTrue()
    {
        _bag.AddItem(1, 100);
        Assert.IsTrue(_bag.HasItem(1, 100));
        Assert.IsTrue(_bag.HasItem(1, 1));
    }

    [Test]
    public void HasItem_Insufficient_ReturnsFalse()
    {
        _bag.AddItem(1, 5);
        Assert.IsFalse(_bag.HasItem(1, 10));
    }

    [Test]
    public void HasItem_NotOwned_ReturnsFalse()
    {
        Assert.IsFalse(_bag.HasItem(99));
    }

    [Test]
    public void Clear_RemovesAllItems()
    {
        _bag.AddItem(1, 100);
        _bag.AddItem(2, 1);
        _bag.Clear();

        Assert.AreEqual(0, _bag.GetItemCount(1));
        Assert.AreEqual(0, _bag.GetItemCount(2));
        Assert.AreEqual(0, _bag.AllItems.Count);
    }

    [Test]
    public void BuildSaveData_ThenRestore_PreservesItems()
    {
        _bag.AddItem(1, 300);
        _bag.AddItem(3, 1);

        var saveData = _bag.BuildSaveData();

        var restored = new BagManager();
        restored.RestoreFromSaveData(saveData);

        Assert.AreEqual(300, restored.GetItemCount(1));
        Assert.AreEqual(1, restored.GetItemCount(3));
        Assert.AreEqual(0, restored.GetItemCount(2));
    }

    [Test]
    public void RestoreFromSaveData_Null_ClearsBag()
    {
        _bag.AddItem(1, 100);
        _bag.RestoreFromSaveData(null);

        Assert.AreEqual(0, _bag.AllItems.Count);
    }

    [Test]
    public void OnItemChanged_FiredOnAdd()
    {
        int changedItemId = -1;
        _bag.OnItemChanged += (type, id) => { changedItemId = id; };

        _bag.AddItem(1, 50);

        Assert.AreEqual(1, changedItemId);
    }

    [Test]
    public void OnItemChanged_FiredOnRemove()
    {
        _bag.AddItem(1, 100);

        int changedItemId = -1;
        _bag.OnItemChanged += (type, id) => { changedItemId = id; };

        _bag.RemoveItem(1, 30);

        Assert.AreEqual(1, changedItemId);
    }
}
