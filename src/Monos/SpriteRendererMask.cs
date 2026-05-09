using UnityEngine;

namespace ReplantedOnline.Monos;

/// <summary>
/// Manages a sprite mask that follows a target SpriteRenderer and applies masking to another SpriteRenderer.
/// </summary>
internal sealed class SpriteRendererMask : MonoBehaviour
{
    private SpriteRenderer _spriteRenderer;
    private SpriteMask _spriteMask;
    private SpriteRenderer _spriteRendererMask;
    private SpriteMaskInteraction _interaction = SpriteMaskInteraction.None;

    private void Awake()
    {
        _spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        if (_spriteRenderer == null)
        {
            Destroy(this);
            return;
        }

        GameObject spriteMaskGo = new(nameof(SpriteRendererMask));
        _spriteMask = spriteMaskGo.AddComponent<SpriteMask>();
        _spriteRenderer.maskInteraction = _interaction;
    }

    private void Update()
    {
        if (_spriteRenderer == null) return;
        if (_spriteRendererMask == null) return;
        if (_spriteMask == null)
        {
            Destroy(this);
            return;
        }

        _spriteRenderer.maskInteraction = _interaction;
        _spriteMask.sprite = _spriteRendererMask.sprite;
        _spriteMask.transform.position = _spriteRendererMask.transform.position;
        _spriteMask.transform.rotation = _spriteRendererMask.transform.rotation;
        _spriteMask.transform.localScale = _spriteRendererMask.transform.lossyScale;
        int sortingOrder = _spriteRenderer.sortingOrder;
        _spriteMask.frontSortingOrder = sortingOrder + 1;
        _spriteMask.backSortingOrder = sortingOrder - 1;
        _spriteMask.frontSortingLayerID = _spriteRenderer.sortingLayerID;
        _spriteMask.backSortingLayerID = _spriteRenderer.sortingLayerID;
    }

    private void OnDestroy()
    {
        if (_spriteMask != null)
        {
            Destroy(_spriteMask.gameObject);
        }
    }

    /// <summary>
    /// Sets the SpriteRenderer that will be used as the source for the mask's sprite and transform.
    /// </summary>
    /// <param name="spriteRendererMask">The SpriteRenderer to use as the mask source.</param>
    internal void SetMask(SpriteRenderer spriteRendererMask)
    {
        _spriteRendererMask = spriteRendererMask;
    }

    /// <summary>
    /// Sets how the mask interacts with the target SpriteRenderer.
    /// </summary>
    /// <param name="spriteMaskInteraction">The mask interaction mode.</param>
    internal void SetInteraction(SpriteMaskInteraction spriteMaskInteraction)
    {
        _interaction = spriteMaskInteraction;
    }
}