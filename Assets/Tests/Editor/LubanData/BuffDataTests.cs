using NUnit.Framework;

class BuffDataTests
{
    private cfg.Tables tables;

    [SetUp]
    public void Setup()
    {
        tables = TestDataLoader.LoadTables();
    }

    [Test]
    public void Buff_Count_IsEighteen()
    {
        Assert.AreEqual(18, tables.TbBuff.DataList.Count);
    }

    [Test]
    public void Buff_Id1_AllFieldsMatch()
    {
        var buff = tables.TbBuff.Get(1);
        Assert.AreEqual(1, buff.Id);
        Assert.AreEqual(1, buff.Type);
        Assert.AreEqual(2, buff.Showtype);
        Assert.AreEqual("美汁汁儿", buff.Name);
        Assert.AreEqual(1, buff.Icon);
        Assert.AreEqual("从厕所里掏出来宝贝的概率提升", buff.Descrption);
        Assert.AreEqual(1, buff.Func);
        Assert.AreEqual(0, buff.Num);
        Assert.AreEqual(0, buff.Time);
        Assert.AreEqual(0, buff.Param1);
    }

    [Test]
    public void Buff_Id3_AoeDamage_FieldsMatch()
    {
        var buff = tables.TbBuff.Get(3);
        Assert.AreEqual("尿了...", buff.Name);
        // func=3：回合结束随机造成伤害
        Assert.AreEqual(3, buff.Func);
        Assert.AreEqual(3, buff.Time);
        Assert.AreEqual(2, buff.Param1);
    }

    [Test]
    public void Buff_Id29_Stun_FuncIsSix()
    {
        var buff = tables.TbBuff.Get(29);
        Assert.AreEqual("眩晕", buff.Name);
        // func=6：眩晕 → 应映射到 BuffFuncType.Stun
        Assert.AreEqual(6, buff.Func);
        Assert.AreEqual(1, buff.Time);
        Assert.AreEqual(BuffFuncType.Stun, BuffManager.MapBuffFunc(buff.Func));
    }

    [Test]
    public void Buff_MapFunc_StunMapping()
    {
        Assert.AreEqual(BuffFuncType.Stun, BuffManager.MapBuffFunc(6));
        Assert.AreEqual(BuffFuncType.Stun, BuffManager.MapBuffFunc(13));
        Assert.AreEqual(BuffFuncType.Freeze, BuffManager.MapBuffFunc(14));
        Assert.AreEqual(BuffFuncType.AoeDamage, BuffManager.MapBuffFunc(3));
        Assert.AreEqual(BuffFuncType.SelfmatainUp, BuffManager.MapBuffFunc(4));
    }

    [Test]
    public void Buff_AllNames_NotNullOrEmpty()
    {
        foreach (var buff in tables.TbBuff.DataList)
        {
            Assert.IsNotEmpty(buff.Name, $"Buff {buff.Id} Name 不应为空");
        }
    }
}
