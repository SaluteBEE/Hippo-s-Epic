using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using cfg.cfg.slotstate;

public class SlotDataFieldsTests
{
    [Test]
    public void GetSlotDataFields_ExcludesFixedFields()
    {
        var fields = InvokeGetSlotDataFields();
        var nameSet = new HashSet<string>();
        foreach (var f in fields)
            nameSet.Add(f.Name);

        Assert.IsFalse(nameSet.Contains("Id"), "不应包含 Id");
        Assert.IsFalse(nameSet.Contains("Personid"), "不应包含 Personid");
        Assert.IsFalse(nameSet.Contains("Personid_Ref"), "不应包含 Personid_Ref");
        Assert.IsFalse(nameSet.Contains("Prefabtype"), "不应包含 Prefabtype");
        Assert.IsFalse(nameSet.Contains("__ID__"), "不应包含 __ID__");
    }

    [Test]
    public void GetSlotDataFields_ContainsSlotFields()
    {
        var fields = InvokeGetSlotDataFields();
        var nameSet = new HashSet<string>();
        foreach (var f in fields)
            nameSet.Add(f.Name);

        Assert.IsTrue(nameSet.Contains("嘴"), "应包含 嘴 插槽字段");
        Assert.IsTrue(nameSet.Contains("头"), "应包含 头 插槽字段");
        Assert.IsTrue(nameSet.Contains("左眼"), "应包含 左眼 插槽字段");
        Assert.IsTrue(nameSet.Contains("身体"), "应包含 身体 插槽字段");
    }

    [Test]
    public void GetSlotDataFields_AllFieldsAreInt()
    {
        var fields = InvokeGetSlotDataFields();
        foreach (var f in fields)
        {
            Assert.AreEqual(typeof(int), f.FieldType,
                $"字段 {f.Name} 应为 int 类型，实际为 {f.FieldType.Name}");
        }
    }

    [Test]
    public void GetSlotDataFields_CountMatchesExpected()
    {
        var allFields = typeof(Slotstate).GetFields();
        int fixedCount = 0;
        var fixedNames = new HashSet<string> { "Id", "Personid", "Personid_Ref", "Prefabtype", "__ID__" };
        foreach (var f in allFields)
        {
            if (fixedNames.Contains(f.Name))
                fixedCount++;
        }

        var dataFields = InvokeGetSlotDataFields();
        Assert.AreEqual(allFields.Length - fixedCount, dataFields.Length,
            $"数据字段数 = 总字段数({allFields.Length}) - 固定字段数({fixedCount})");
    }

    private static FieldInfo[] InvokeGetSlotDataFields()
    {
        var method = typeof(AnimationStateManager).GetMethod(
            "GetSlotDataFields",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(method, "GetSlotDataFields 方法应存在");

        var resetField = typeof(AnimationStateManager).GetField(
            "_slotDataFields",
            BindingFlags.NonPublic | BindingFlags.Static);
        resetField.SetValue(null, null);

        return (FieldInfo[])method.Invoke(null, null);
    }
}
