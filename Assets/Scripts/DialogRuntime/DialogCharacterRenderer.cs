using UnityEngine;
using UnityEngine.UI;

public class DialogCharacterRenderer : MonoBehaviour
{
    [Header("渲染设置")]
    [SerializeField] private int textureWidth = 1920;
    [SerializeField] private int textureHeight = 1080;
    [SerializeField] private int depthBuffer = 24;

    private Camera _camera;
    private Transform _leftAnchor;
    private readonly Transform[] _rightSlots = new Transform[3];

    private RenderTexture _renderTexture;
    private RawImage _characterImage;

    public Transform LeftAnchor => _leftAnchor;
    public Transform GetRightSlot(int index) => index >= 0 && index < 3 ? _rightSlots[index] : null;

    private void Awake()
    {
        FindChildren();
        CreateRenderTexture();
    }

    private void FindChildren()
    {
        FindRecursive(transform);
    }

    private void FindRecursive(Transform current)
    {
        for (int i = 0; i < current.childCount; i++)
        {
            var child = current.GetChild(i);
            var cam = child.GetComponent<Camera>();
            if (cam != null)
            {
                _camera = cam;
            }
            else if (child.name == "LeftAnchor")
            {
                _leftAnchor = child;
            }
            else if (child.name == "RightSlot1")
            {
                _rightSlots[0] = child;
            }
            else if (child.name == "RightSlot2")
            {
                _rightSlots[1] = child;
            }
            else if (child.name == "RightSlot3")
            {
                _rightSlots[2] = child;
            }
            else if (child.name.Contains("Right") && child.name.Contains("Anchor"))
            {
                FindRecursive(child);
            }
        }
    }

    private void CreateRenderTexture()
    {
        _renderTexture = new RenderTexture(textureWidth, textureHeight, depthBuffer, RenderTextureFormat.ARGB32);
        _renderTexture.name = "DialogCharacterRT";
        _renderTexture.Create();

        if (_camera != null)
            _camera.targetTexture = _renderTexture;
    }

    public void SetCharacterImage(RawImage image)
    {
        _characterImage = image;
        if (_characterImage != null)
            _characterImage.texture = _renderTexture;
    }

    public void SetVisible(bool visible)
    {
        if (_characterImage != null)
            _characterImage.gameObject.SetActive(visible);
    }

    public Transform GetAnchor(SpeakerSide side)
    {
        return side == SpeakerSide.Left ? _leftAnchor : _rightSlots[1];
    }

    public void SwapToCenterSlot(int speakingSlotIndex)
    {
        if (speakingSlotIndex == 1) return;
        if (speakingSlotIndex < 0 || speakingSlotIndex >= _rightSlots.Length) return;

        var center = _rightSlots[1];
        var speaker = _rightSlots[speakingSlotIndex];
        if (center == null || speaker == null) return;

        if (speaker.childCount == 0)
        {
            Debug.LogWarning($"[DialogCharacterRenderer] SwapToCenterSlot: slot{speakingSlotIndex} 无子物体，跳过交换");
            return;
        }

        var speakerChild = speaker.GetChild(0);
        var centerChild = center.childCount > 0 ? center.GetChild(0) : null;

        speakerChild.SetParent(center, false);
        if (centerChild != null)
            centerChild.SetParent(speaker, false);
    }

    private void OnDestroy()
    {
        if (_renderTexture != null)
        {
            _renderTexture.Release();
            Destroy(_renderTexture);
            _renderTexture = null;
        }
    }
}
