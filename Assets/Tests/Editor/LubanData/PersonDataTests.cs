using NUnit.Framework;
using System.Collections.Generic;

class PersonDataTests
{
    private cfg.Tables tables;

    [SetUp]
    public void Setup()
    {
        tables = TestDataLoader.LoadTables();
    }

    [Test]
    public void Person_Count_IsTwo()
    {
        Assert.AreEqual(2, tables.TbPerson.DataList.Count);
    }

    [Test]
    public void Person_Id1_AllFieldsMatch()
    {
        var person = tables.TbPerson.Get(1);
        Assert.AreEqual(1, person.Id);
        Assert.AreEqual("河马", person.Name);
        Assert.AreEqual(100, person.Hp);
        Assert.AreEqual(100, person.Hpmax);
        Assert.AreEqual(1, person.Animstate);
        Assert.AreEqual("Assets\\Prefabs\\player\\Hippo", person.Prefab1);
        Assert.AreEqual("Assets\\Prefabs\\player\\homo_talk", person.Prefab2);
        Assert.IsNotNull(person.BuffIds);
        CollectionAssert.AreEqual(new List<int> { 1 }, person.BuffIds);
    }

    [Test]
    public void Person_Id2_AllFieldsMatch()
    {
        var person = tables.TbPerson.Get(2);
        Assert.AreEqual(2, person.Id);
        Assert.AreEqual("教练", person.Name);
        Assert.AreEqual(100, person.Hp);
        Assert.AreEqual(100, person.Hpmax);
        Assert.AreEqual(1, person.Animstate);
        CollectionAssert.AreEqual(new List<int> { 2 }, person.BuffIds);
    }

    [Test]
    public void Person_BuffIds_RefResolved()
    {
        var hippo = tables.TbPerson.Get(1);
        Assert.IsNotNull(hippo.BuffIds_Ref);
        Assert.AreEqual(1, hippo.BuffIds_Ref.Count);
        Assert.IsNotNull(hippo.BuffIds_Ref[0]);
        Assert.AreEqual("美汁汁儿", hippo.BuffIds_Ref[0].Name);

        var coach = tables.TbPerson.Get(2);
        Assert.IsNotNull(coach.BuffIds_Ref);
        Assert.AreEqual(1, coach.BuffIds_Ref.Count);
        Assert.AreEqual("憋不住了！", coach.BuffIds_Ref[0].Name);
    }

    [Test]
    public void Person_PrefabPaths_ContainBackslash()
    {
        var person = tables.TbPerson.Get(1);
        Assert.IsTrue(person.Prefab1.Contains("\\"));
    }
}
