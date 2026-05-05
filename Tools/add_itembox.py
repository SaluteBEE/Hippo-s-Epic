"""给 BubbleLeft.prefab 和 BubbleMiddle.prefab 添加 itemBox 结构
只删除不完整的 itemBox/Image 节点，保留主 Image
"""
import re
import os
import sys

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
PROJECT_DIR = os.path.dirname(SCRIPT_DIR)

ITEM_SPRITE_GUID = "58452070f38b29641bba627ab29d7438"
FONT_GUID = "79f46451f284c4448ad4f487a90b2054"


def generate_itembox_yaml(go_id, rt_id, cr_id, img_ui_id, le_id,
                          txt_go_id, txt_rt_id, txt_cr_id, txt_tmp_id,
                          parent_rt_file_id):
    parts = []
    
    parts.append(f"""--- !u!1 &{go_id}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {rt_id}}}
  - component: {{fileID: {cr_id}}}
  - component: {{fileID: {img_ui_id}}}
  - component: {{fileID: {le_id}}}
  m_Layer: 5
  m_Name: ItemBox
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 0""")

    parts.append(f"""--- !u!224 &{rt_id}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {{fileID: {txt_rt_id}}}
  m_Father: {{fileID: {parent_rt_file_id}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
  m_AnchorMin: {{x: 1, y: 0}}
  m_AnchorMax: {{x: 1, y: 0}}
  m_AnchoredPosition: {{x: -81, y: 1004.9}}
  m_SizeDelta: {{x: 302.8573, y: 111.6544}}
  m_Pivot: {{x: 1, y: 9}}""")

    parts.append(f"""--- !u!222 &{cr_id}
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_CullTransparentMesh: 1""")

    parts.append(f"""--- !u!114 &{img_ui_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: fe87c0e1cc204ed48ad3b37840f39efc, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_Material: {{fileID: 0}}
  m_Color: {{r: 1, g: 1, b: 1, a: 1}}
  m_RaycastTarget: 1
  m_RaycastPadding: {{x: 0, y: 0, z: 0, w: 0}}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {{fileID: 21300000, guid: {ITEM_SPRITE_GUID}, type: 3}}
  m_Type: 1
  m_PreserveAspect: 0
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1""")

    parts.append(f"""--- !u!114 &{le_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: 306cc8c2b49d7114eaa3623786fc2126, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_IgnoreLayout: 1
  m_MinWidth: -1
  m_MinHeight: -1
  m_PreferredWidth: -1
  m_PreferredHeight: -1
  m_FlexibleWidth: -1
  m_FlexibleHeight: -1
  m_LayoutPriority: 1""")

    parts.append(f"""--- !u!1 &{txt_go_id}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {txt_rt_id}}}
  - component: {{fileID: {txt_cr_id}}}
  - component: {{fileID: {txt_tmp_id}}}
  m_Layer: 5
  m_Name: Text (TMP)
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1""")

    parts.append(f"""--- !u!224 &{txt_rt_id}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {txt_go_id}}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: {rt_id}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
  m_AnchorMin: {{x: 0, y: 0}}
  m_AnchorMax: {{x: 1, y: 1}}
  m_AnchoredPosition: {{x: -1.9577026, y: 0}}
  m_SizeDelta: {{x: -54.1986, y: 0}}
  m_Pivot: {{x: 0.5, y: 0.5}}""")

    parts.append(f"""--- !u!222 &{txt_cr_id}
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {txt_go_id}}}
  m_CullTransparentMesh: 1""")

    parts.append(f"""--- !u!114 &{txt_tmp_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {txt_go_id}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: f4688fdb7df04437aeb418b961361dc5, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_Material: {{fileID: 0}}
  m_Color: {{r: 1, g: 1, b: 1, a: 1}}
  m_RaycastTarget: 1
  m_RaycastPadding: {{x: 0, y: 0, z: 0, w: 0}}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_text: "\\u63D0\\u793A\\u6587\\u5B57"
  m_isRightToLeft: 0
  m_fontAsset: {{fileID: 11400000, guid: {FONT_GUID}, type: 2}}
  m_sharedMaterial: {{fileID: -8962656103618402456, guid: {FONT_GUID}, type: 2}}
  m_fontSharedMaterials: []
  m_fontMaterial: {{fileID: 0}}
  m_fontMaterials: []
  m_fontColor32:
    serializedVersion: 2
    rgba: 4278190080
  m_fontColor: {{r: 0, g: 0, b: 0, a: 1}}
  m_enableVertexGradient: 0
  m_colorMode: 3
  m_fontColorGradient:
    topLeft: {{r: 1, g: 1, b: 1, a: 1}}
    topRight: {{r: 1, g: 1, b: 1, a: 1}}
    bottomLeft: {{r: 1, g: 1, b: 1, a: 1}}
    bottomRight: {{r: 1, g: 1, b: 1, a: 1}}
  m_fontColorGradientPreset: {{fileID: 0}}
  m_spriteAsset: {{fileID: 0}}
  m_tintAllSprites: 0
  m_StyleSheet: {{fileID: 0}}
  m_TextStyleHashCode: -1183493901
  m_overrideHtmlColors: 0
  m_faceColor:
    serializedVersion: 2
    rgba: 4294967295
  m_fontSize: 55
  m_fontSizeBase: 37.8
  m_fontWeight: 400
  m_enableAutoSizing: 1
  m_fontSizeMin: 18
  m_fontSizeMax: 55
  m_fontStyle: 0
  m_HorizontalAlignment: 2
  m_VerticalAlignment: 512
  m_textAlignment: 65535
  m_characterSpacing: 0
  m_wordSpacing: 0
  m_lineSpacing: 0
  m_lineSpacingMax: 0
  m_paragraphSpacing: 0
  m_charWidthMaxAdj: 0
  m_enableWordWrapping: 0
  m_wordWrappingRatios: 0.4
  m_overflowMode: 0
  m_linkedTextComponent: {{fileID: 0}}
  parentLinkedComponent: {{fileID: 0}}
  m_enableKerning: 1
  m_enableExtraPadding: 0
  checkPaddingRequired: 0
  m_isRichText: 1
  m_parseCtrlCharacters: 1
  m_isOrthographic: 1
  m_isCullingEnabled: 0
  m_horizontalMapping: 0
  m_verticalMapping: 0
  m_uvLineOffset: 0
  m_geometrySortingOrder: 0
  m_IsTextObjectScaleStatic: 0
  m_VertexBufferAutoSizeReduction: 0
  m_useMaxVisibleDescender: 1
  m_pageToDisplay: 1
  m_margin: {{x: 0, y: 0, z: 0, w: 0}}
  m_isUsingLegacyAnimationComponent: 0
  m_isVolumetricText: 0
  m_hasFontAssetChanged: 0
  m_baseMaterial: {{fileID: 0}}
  m_maskOffset: {{x: 0, y: 0, z: 0, w: 0}}""")

    return '\n'.join(parts)


def parse_blocks(content):
    """解析 prefab YAML 为有序块列表"""
    blocks = re.split(r'\n(?=--- !u!)', content)
    return blocks


def find_block_info(blocks, root_name):
    """找到根节点信息"""
    root_go_id = None
    root_rt_id = None
    for block in blocks:
        m = re.match(r'--- !u!1 &(\d+)', block)
        if m and f'm_Name: {root_name}' in block:
            root_go_id = m.group(1)
            for cm in re.finditer(r'component: \{fileID: (\d+)\}', block):
                root_rt_id = cm.group(1)
                break
            break
    return root_go_id, root_rt_id


def find_go_blocks_by_name(blocks, name, exclude_go_id=None):
    """找到所有指定名称的 GO block（返回列表避免误删主节点）"""
    results = []
    for block in blocks:
        m = re.match(r'--- !u!1 &(\d+)', block)
        if m:
            go_id = m.group(1)
            if go_id == exclude_go_id:
                continue
            if f'm_Name: {name}\n' in block or f'm_Name: {name}\r\n' in block:
                # 检查是否是 inactive 的不完整节点
                is_active = 'm_IsActive: 1' in block
                comp_count = len(re.findall(r'component: \{fileID:', block))
                results.append({
                    'go_id': go_id,
                    'block': block,
                    'is_active': is_active,
                    'comp_count': comp_count
                })
    return results


def collect_all_file_ids_for_go(content, go_id):
    """收集一个 GO 及其所有组件的 fileID"""
    file_ids = {go_id}
    # 找所有引用此 GO 的组件块
    for m in re.finditer(
        r'^--- !u!\d+ &(\d+)\n.*?m_GameObject: \{fileID: ' + re.escape(go_id) + r'\}',
        content, re.MULTILINE | re.DOTALL
    ):
        file_ids.add(m.group(1))
    return file_ids


def collect_children_file_ids(content, rt_file_id):
    """收集 RT 下所有子节点的 fileID"""
    file_ids = set()
    # 找 m_Father 指向 rt_file_id 的块
    for m in re.finditer(
        r'^--- !u!224 &(\d+)\n.*?m_Father: \{fileID: ' + re.escape(rt_file_id) + r'\}',
        content, re.MULTILINE | re.DOTALL
    ):
        child_rt_id = m.group(1)
        file_ids.add(child_rt_id)
        # 找这个 RT 的 GO
        go_m = re.search(r'm_GameObject: \{fileID: (\d+)\}', m.group(0))
        if go_m:
            child_go_id = go_m.group(1)
            file_ids.add(child_go_id)
            # 收集子 GO 的所有组件
            file_ids.update(collect_all_file_ids_for_go(content, child_go_id))
            # 递归收集子节点的子节点
            file_ids.update(collect_children_file_ids(content, child_rt_id))
    return file_ids


def process_prefab(prefab_path, root_name, script_guid):
    print(f'\n=== Processing {os.path.basename(prefab_path)} ===')

    with open(prefab_path, 'r', encoding='utf-8') as f:
        content = f.read()

    blocks = parse_blocks(content)
    root_go_id, root_rt_id = find_block_info(blocks, root_name)
    print(f'  Root: GO={root_go_id}, RT={root_rt_id}')

    if not root_go_id:
        print('  ERROR: root not found')
        return False

    # 找所有名为 Image 或 ItemBox 的非根、不完整节点
    stale_go_ids = set()
    for name in ['Image', 'ItemBox']:
        go_infos = find_go_blocks_by_name(blocks, name, exclude_go_id=root_go_id)
        for info in go_infos:
            # 不完整 = inactive 且组件数 <= 2（只有 RectTransform + LayoutElement）
            if not info['is_active'] and info['comp_count'] <= 2:
                stale_go_ids.add(info['go_id'])
                print(f'  Stale: name={name}, go_id={info["go_id"]}, '
                      f'active={info["is_active"]}, comps={info["comp_count"]}')

    if not stale_go_ids:
        print('  No stale nodes to remove')

    # 收集所有需要删除的 fileID
    all_stale = set()
    for go_id in stale_go_ids:
        all_stale.update(collect_all_file_ids_for_go(content, go_id))
        # 也收集此 GO 的 RT 下的子节点
        for m in re.finditer(
            r'^--- !u!224 &(\d+)\n.*?m_GameObject: \{fileID: ' + re.escape(go_id) + r'\}',
            content, re.MULTILINE | re.DOTALL
        ):
            rt_id = m.group(1)
            all_stale.update(collect_children_file_ids(content, rt_id))

    print(f'  Total stale fileIDs: {len(all_stale)}')

    # 找 stale RT 以便从根 children 中移除
    stale_rt_ids = set()
    for go_id in stale_go_ids:
        for m in re.finditer(
            r'^--- !u!224 &(\d+)\n.*?m_GameObject: \{fileID: ' + re.escape(go_id) + r'\}',
            content, re.MULTILINE | re.DOTALL
        ):
            stale_rt_ids.add(m.group(1))

    # 删除 stale 块
    for fid in all_stale:
        pattern = re.compile(
            r'\n--- !u!\d+ &' + re.escape(str(fid)) + r'\n.*?(?=\n--- !u!|\Z)',
            re.DOTALL
        )
        content = pattern.sub('', content)

    # 从根 RT 的 children 中移除 stale 引用
    for rt_id in stale_rt_ids:
        content = content.replace(f'\n  - {{fileID: {rt_id}}}', '')

    # 生成新的 itemBox fileIDs
    new_ids = {
        'go_id': '3000000000000000001',
        'rt_id': '3000000000000000002',
        'cr_id': '3000000000000000003',
        'img_ui_id': '3000000000000000004',
        'le_id': '3000000000000000005',
        'txt_go_id': '3000000000000000006',
        'txt_rt_id': '3000000000000000007',
        'txt_cr_id': '3000000000000000008',
        'txt_tmp_id': '3000000000000000009',
    }

    # 添加到根 RT 的 children
    root_rt_pattern = re.compile(
        r'(m_GameObject: \{fileID: ' + re.escape(root_go_id) +
        r'\}.*?m_Children:\n(?:  - \{fileID: \d+\}\n)*)',
        re.DOTALL
    )
    m = root_rt_pattern.search(content)
    if m:
        insert_pos = m.end()
        new_child = f'  - {{fileID: {new_ids["rt_id"]}}}\n'
        content = content[:insert_pos] + new_child + content[insert_pos:]
        print(f'  Added child ref to root RT')
    else:
        print('  WARNING: could not find root RT children block')

    # 更新脚本引用
    script_pattern = re.compile(
        r'(m_Script: \{fileID: 11500000, guid: ' + re.escape(script_guid) +
        r', type: 3\}\n  m_Name: \n  m_EditorClassIdentifier: \n  text: \{fileID: \d+\})'
    )
    m = script_pattern.search(content)
    if m:
        old = m.group(1)
        new = (old + '\n'
               f'  itemBoxRoot: {{fileID: {new_ids["go_id"]}}}\n'
               f'  itemText: {{fileID: {new_ids["txt_tmp_id"]}}}')
        content = content.replace(old, new)
        print(f'  Updated script references')
    else:
        print('  WARNING: could not find script block')

    # 追加 itemBox YAML
    itembox = generate_itembox_yaml(parent_rt_file_id=root_rt_id, **new_ids)
    content = content.rstrip() + '\n' + itembox + '\n'

    with open(prefab_path, 'w', encoding='utf-8') as f:
        f.write(content)

    print(f'  OK! ItemBox added')
    return True


# === BubbleLeft ===
BL_PATH = os.path.join(PROJECT_DIR, 'Assets', 'Prefabs', 'UI', 'BubbleLeft.prefab')
BL_SCRIPT = '6dfd326415129424bb3679e4eaba8d09'
process_prefab(BL_PATH, 'BubbleLeft', BL_SCRIPT)

# === BubbleMiddle ===
BM_PATH = os.path.join(PROJECT_DIR, 'Assets', 'Prefabs', 'UI', 'BubbleMiddle.prefab')
BM_SCRIPT = '80bd7a1f2d6925b45919f2dbba550e68'
process_prefab(BM_PATH, 'BubbleMiddle', BM_SCRIPT)

print('\nDone!')
