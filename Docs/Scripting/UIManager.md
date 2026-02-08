# UIManager API 文档

## 概述

`UIManager` 是一个单例UI管理系统，提供了完整的UI窗口生命周期管理、层级控制、导航栈管理和缓存机制。系统采用预制体加载方式，支持从Resources目录动态加载UI窗口。

## 核心组件

### UILayer 枚举

定义UI窗口的层级优先级。

```csharp
public enum UILayer
{
    Background = 0,    // 背景层
    Normal = 100,      // 常规层
    Popup = 200,       // 弹窗层
    Top = 300          // 顶层
}
```

**层级说明：**

- `Background`：背景层，层级最低，通常用于背景图片或视频
- `Normal`：常规层，主要的游戏界面，如主菜单、游戏HUD等
- `Popup`：弹窗层，用于各类弹出窗口、对话框
- `Top`：顶层，用于系统级提示、Loading界面等

### UIWindow 抽象类

所有UI窗口的基类，派生类需继承此类。

#### 虚拟属性

```csharp
public virtual UILayer Layer => UILayer.Normal;
```

**说明：** 窗口所属层级，默认为Normal层。
**用法：** 重写此属性以指定窗口层级。

```csharp
public virtual bool PushToStack => true;
```

**说明：** 打开时是否入栈，用于Back()返回功能。
**默认值：** `true`
**用法：** 对于不需要返回的窗口（如Loading界面），可设为`false`。

```csharp
public virtual bool CacheOnClose => false;
```

**说明：** 关闭后是否缓存GameObject，避免重复销毁/创建。
**默认值：** `false`
**用法：** 对于频繁开关的窗口，设为`true`可提升性能。

#### 生命周期方法

```csharp
public virtual void OnCreate(object args)
```

**说明：** 窗口首次创建后调用一次，用于初始化。
**参数：**

- `args`：传递给窗口的参数对象

```csharp
public virtual void OnOpen(object args)
```

**说明：** 每次打开窗口时调用。
**参数：**

- `args`：传递给窗口的参数对象

```csharp
public virtual void OnClose()
```

**说明：** 每次关闭窗口时调用。

```csharp
public virtual void OnDestroyWindow()
```

**说明：** 窗口真正销毁前调用一次，用于清理资源。

### UIManager 类

UI管理器单例，负责窗口的创建、打开、关闭和导航。

#### 静态属性

```csharp
public static UIManager Instance { get; private set; }
```

**说明：** 单例实例，通过此属性访问UIManager。

#### Inspector 可配置属性

```csharp
[SerializeField] private Canvas rootCanvas;
```

**说明：** 根画布，所有UI窗口将作为此Canvas的子节点。

```csharp
[SerializeField] private bool autoCreateLayers = true;
```

**说明：** 是否自动创建层级节点。若为`true`，将在根画布下自动创建各层级的父节点。

## 公共方法

### `Open<T>`

打开或创建并打开一个UI窗口。

```csharp
public T Open<T>(string prefabPath, object args = null) where T : UIWindow
```

**泛型参数：**

- `T`：窗口类型，必须继承自`UIWindow`

**参数：**

- `prefabPath`：预制体在Resources目录下的路径（不含扩展名）
- `args`：传递给窗口的参数对象（可选）

**返回值：**

- 返回窗口实例，若加载失败则返回`null`

**行为：**

1. 若窗口已存在，直接激活并调用`OnOpen`
2. 若窗口不存在，从Resources加载预制体，实例化后调用`OnCreate`和`OnOpen`
3. 根据窗口的`PushToStack`属性决定是否入栈

**示例：**

```csharp
// 打开主菜单
var mainMenu = UIManager.Instance.Open<MainMenuWindow>("UI/MainMenu");

// 打开设置界面并传递参数
var settings = UIManager.Instance.Open<SettingsWindow>("UI/Settings", new { volume = 0.8f });
```

### `Close<T>`

关闭指定类型的窗口。

```csharp
public void Close<T>() where T : UIWindow
```

**泛型参数：**

- `T`：要关闭的窗口类型

**行为：**

1. 调用窗口的`OnClose`方法
2. 若`CacheOnClose`为`true`，隐藏窗口；否则销毁窗口并调用`OnDestroyWindow`
3. 从导航栈中移除窗口

**示例：**

```csharp
// 关闭设置界面
UIManager.Instance.Close<SettingsWindow>();
```

### Close (Type重载)

通过Type关闭窗口。

```csharp
public void Close(Type type)
```

**参数：**

- `type`：窗口类型

**用法：** 当无法使用泛型时的替代方案。

### `SwitchExclusiveNormal<T>`

独占式切换：关闭所有Normal层窗口，仅打开指定窗口。

```csharp
public T SwitchExclusiveNormal<T>(string prefabPath, object args = null) where T : UIWindow
```

**泛型参数：**

- `T`：目标窗口类型

**参数：**

- `prefabPath`：预制体路径
- `args`：传递给窗口的参数对象（可选）

**返回值：**

- 返回新打开的窗口实例

**使用场景：** 适用于主界面切页，如从"主菜单"切换到"背包界面"。

**示例：**

```csharp
// 切换到背包界面，关闭其他Normal层窗口
UIManager.Instance.SwitchExclusiveNormal<InventoryWindow>("UI/Inventory");
```

### Back

返回导航栈中的上一个窗口。

```csharp
public void Back()
```

**行为：**

1. 关闭当前窗口（栈顶）
2. 激活并打开上一个窗口
3. 跳过已销毁的窗口

**使用场景：** 实现返回键功能。

**示例：**

```csharp
// 在Update中监听返回键
void Update()
{
    if (Input.GetKeyDown(KeyCode.Escape))
    {
        UIManager.Instance.Back();
    }
}
```

### `IsOpen<T>`

检查指定窗口是否处于打开状态。

```csharp
public bool IsOpen<T>() where T : UIWindow
```

**泛型参数：**

- `T`：要检查的窗口类型

**返回值：**

- `true`：窗口存在且激活
- `false`：窗口不存在或未激活

**示例：**

```csharp
if (UIManager.Instance.IsOpen<PauseMenuWindow>())
{
    Debug.Log("游戏已暂停");
}
```

## 使用指南

### 1. 场景设置

1. 在场景中创建一个GameObject，挂载`UIManager`组件
2. 在Inspector中分配`Root Canvas`（通常是场景中的主Canvas）
3. 确保`Auto Create Layers`选项勾选，系统将自动创建层级节点

### 2. 创建自定义窗口

```csharp
using UnityEngine;

public class MainMenuWindow : UIWindow
{
    // 指定窗口层级
    public override UILayer Layer => UILayer.Normal;
    
    // 频繁使用的窗口可以缓存
    public override bool CacheOnClose => true;

    // 首次创建时初始化
    public override void OnCreate(object args)
    {
        base.OnCreate(args);
        Debug.Log("主菜单窗口创建");
    }

    // 每次打开时刷新
    public override void OnOpen(object args)
    {
        base.OnOpen(args);
        Debug.Log("主菜单窗口打开");
    }

    // 关闭时保存状态
    public override void OnClose()
    {
        base.OnClose();
        Debug.Log("主菜单窗口关闭");
    }

    // 按钮点击事件
    public void OnStartButtonClick()
    {
        UIManager.Instance.Open<GameWindow>("UI/Game");
    }

    public void OnExitButtonClick()
    {
        Application.Quit();
    }
}
```

### 3. 预制体结构

窗口预制体应满足以下要求：

- 根节点带有继承自`UIWindow`的组件
- 使用RectTransform（UGUI）
- 放置在`Resources`目录下（或子目录）
- 可选：根节点带有Canvas组件（用于独立排序）

**推荐目录结构：**

```text
Assets/
└── Resources/
    └── UI/
        ├── MainMenu.prefab
        ├── GameHUD.prefab
        ├── Settings.prefab
        └── Popup/
            ├── ConfirmDialog.prefab
            └── Notification.prefab
```

### 4. 层级管理最佳实践

```csharp
// Background层：静态背景
public class BackgroundWindow : UIWindow
{
    public override UILayer Layer => UILayer.Background;
    public override bool PushToStack => false; // 背景不入栈
}

// Normal层：主界面
public class GameHUDWindow : UIWindow
{
    public override UILayer Layer => UILayer.Normal;
}

// Popup层：弹窗
public class ConfirmDialogWindow : UIWindow
{
    public override UILayer Layer => UILayer.Popup;
}

// Top层：Loading
public class LoadingWindow : UIWindow
{
    public override UILayer Layer => UILayer.Top;
    public override bool PushToStack => false; // Loading不入栈
    public override bool CacheOnClose => true; // 缓存以提高性能
}
```

### 5. 参数传递示例

```csharp
// 定义参数类
public class DialogArgs
{
    public string Title;
    public string Message;
    public System.Action OnConfirm;
}

// 窗口接收参数
public class DialogWindow : UIWindow
{
    public override UILayer Layer => UILayer.Popup;

    public override void OnOpen(object args)
    {
        if (args is DialogArgs dialogArgs)
        {
            titleText.text = dialogArgs.Title;
            messageText.text = dialogArgs.Message;
            confirmButton.onClick.AddListener(() => {
                dialogArgs.OnConfirm?.Invoke();
                UIManager.Instance.Close<DialogWindow>();
            });
        }
    }
}

// 调用方传递参数
UIManager.Instance.Open<DialogWindow>("UI/Popup/Dialog", new DialogArgs
{
    Title = "确认",
    Message = "是否退出游戏？",
    OnConfirm = () => Application.Quit()
});
```

## 注意事项

1. **单例模式**：UIManager使用DontDestroyOnLoad，确保场景切换时不被销毁
2. **Resources加载**：所有UI预制体必须放在Resources目录下
3. **层级排序**：Canvas sorting order自动设置为Layer的枚举值
4. **栈管理**：避免重复入栈同一类型窗口，系统会自动检测
5. **性能优化**：对于频繁开关的窗口，建议设置`CacheOnClose = true`
6. **线程安全**：UIManager非线程安全，所有调用应在主线程进行

## 常见问题

**Q: 如何实现模态对话框？**
A: 将窗口Layer设为Popup或Top，并在窗口下添加一个全屏透明按钮作为遮罩，阻止底层交互。

**Q: 如何实现多个窗口共存？**
A: 不使用`SwitchExclusiveNormal`，直接调用`Open`方法，系统支持同时打开多个窗口。

**Q: Back()没有效果？**
A: 检查窗口的`PushToStack`属性是否为`true`，并确保窗口已正常打开。

**Q: 如何实现窗口动画？**
A: 在`OnOpen`和`OnClose`中使用DOTween或Animation组件播放动画。

## 版本信息

- **创建日期**：2026年2月8日
- **Unity版本**：Universal Render Pipeline
- **依赖**：UnityEngine.UI (UGUI)
