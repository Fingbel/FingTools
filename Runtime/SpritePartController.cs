using UnityEngine;
using UnityEngine.U2D.Animation;
using System.Collections;

namespace FingTools.Lime
{
public class SpritePartController : MonoBehaviour
{    
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private SpriteResolver spriteResolver;
    [SerializeField] private SpriteLibrary spriteLibrary;

    public void SetPreviewSprite(Sprite sprite)
{
    // Check if the GameObject is active
    if (isActiveAndEnabled)
    {
        // Use a coroutine to avoid immediate execution in OnValidate
        if (Application.isEditor && !Application.isPlaying)
        {
            StartCoroutine(DelayedSetSprite(sprite));
        }
        else
        {
            UpdateSprite(sprite);
        }
    }
    else
    {
        // Directly update the sprite without starting a coroutine
        UpdateSprite(sprite);
    }
}


    private IEnumerator DelayedSetSprite(Sprite sprite)
    {
        yield return null; // Wait for the next frame
        UpdateSprite(sprite);
    }

    private void UpdateSprite(Sprite sprite)
    {
        spriteRenderer.sprite = sprite;
    }

    public void UpdateLibrary(SpriteLibraryAsset spriteLibraryAsset)
    {
        if (spriteLibraryAsset == null)
        {
            spriteLibrary.spriteLibraryAsset = null;
        }
        else
        {
            spriteLibrary.spriteLibraryAsset = spriteLibraryAsset;        
        }
    }

    public void Resolve(string category, string label)
    {
        spriteResolver.SetCategoryAndLabel(category, label);        
    }
}
}