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
    public void Buff_Count_IsThree()
    {
        Assert.AreEqual(3, tables.TbBuff.DataList.Count);
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
        Assert.AreEqual("腥腥臭臭的东西在你眼里和小汉堡没区别了", buff.Tip);
        Assert.AreEqual(1, buff.Func);
        Assert.AreEqual(0, buff.Num);
        Assert.AreEqual(0, buff.Time);
        Assert.AreEqual(0, buff.Param1);
    }

    [Test]
    public void Buff_Id2_AllFieldsMatch()
    {
        var buff = tables.TbBuff.Get(2);
        Assert.AreEqual(2, buff.Id);
        Assert.AreEqual(2, buff.Type);
        Assert.AreEqual(1, buff.Showtype);
        Assert.AreEqual("憋不住了！", buff.Name);
        Assert.AreEqual(2, buff.Icon);
        Assert.AreEqual("你下次收到攻击的伤害提升", buff.Descrption);
        Assert.AreEqual("要尿了！快避免冲击，快出来了...", buff.Tip);
        Assert.AreEqual(2, buff.Func);
        Assert.AreEqual(3, buff.Num);
        Assert.AreEqual(0, buff.Time);
        Assert.AreEqual(2, buff.Param1);
    }

    [Test]
    public void Buff_Id3_AllFieldsMatch()
    {
        var buff = tables.TbBuff.Get(3);
        Assert.AreEqual(3, buff.Id);
        Assert.AreEqual(3, buff.Type);
        Assert.AreEqual(1, buff.Showtype);
        Assert.AreEqual("尿了...", buff.Name);
        Assert.AreEqual(3, buff.Icon);
        Assert.AreEqual("每回合结束的时候你会随机向一个敌人施加高额恶心伤害", buff.Descrption);
        Assert.AreEqual("出来了，已经无所谓了，尿裤裆的你放弃了所有底线，这让你所向披靡", buff.Tip);
        Assert.AreEqual(3, buff.Func);
        Assert.AreEqual(0, buff.Num);
        Assert.AreEqual(3, buff.Time);
        Assert.AreEqual(2, buff.Param1);
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
