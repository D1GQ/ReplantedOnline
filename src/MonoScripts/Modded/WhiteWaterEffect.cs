using Il2CppSource.Controllers;
using MelonLoader;
using UnityEngine;

namespace ReplantedOnline.MonoScripts.Modded;

/// <summary>
/// Creates and manages an animated white water effect using a sprite sheet.
/// </summary>
[RegisterTypeInIl2Cpp]
internal sealed class WhiteWaterEffect : MonoBehaviour
{
    private readonly int _fps = 10;
    private Sprite[] _sprites;
    private SpriteRenderer _spriteRenderer;
    private float _frameTimer;
    private int _currentFrame;
    private int? sortingOrderOverride;

    /// <summary>
    /// Creates a new WhiteWaterEffect instance in the scene as a child of the specified zombie controller's renderer.
    /// </summary>
    /// <param name="zombieController">The zombie controller that will parent this effect.</param>
    /// <param name="active">Whether the effect should be active immediately after creation.</param>
    /// <returns>The newly created WhiteWaterEffect component.</returns>
    internal static WhiteWaterEffect Create(ZombieController zombieController, bool active)
    {
        var go = new GameObject("WhiteWaterEffect");
        go.transform.SetParent(zombieController.Renderer.transform);
        go.transform.localPosition = Vector3.zero;
        go.SetActive(active);
        var comp = go.AddComponent<WhiteWaterEffect>();
        comp.sortingOrderOverride = -14400 + (250 * zombieController.m_zombie.mRow);
        return comp;
    }

    private void Awake()
    {
        _sprites = ReplantedOnlineAssets.Sprites.WhiteWaterSpriteSheet.Sprites;
        _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        if (sortingOrderOverride != null)
        {
            _spriteRenderer.sortingOrder = sortingOrderOverride.Value;
        }

        if (_sprites != null && _sprites.Length > 0)
        {
            _spriteRenderer.sprite = _sprites[0];
        }
    }

    private void Update()
    {
        if (_sprites == null || _sprites.Length == 0) return;

        _frameTimer += Time.deltaTime;
        float frameTime = 1f / _fps;

        if (_frameTimer >= frameTime)
        {
            _frameTimer -= frameTime;
            _currentFrame = (_currentFrame + 1) % _sprites.Length;
            _spriteRenderer.sprite = _sprites[_currentFrame];
        }
    }

    private void OnDisable()
    {
        _currentFrame = 0;
        _frameTimer = 0f;
        if (_spriteRenderer != null && _sprites != null && _sprites.Length > 0)
        {
            _spriteRenderer.sprite = _sprites[0];
        }
    }

    private void OnDestroy()
    {
        if (_spriteRenderer != null)
        {
            Destroy(_spriteRenderer);
        }
    }
}