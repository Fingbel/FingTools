using UnityEngine;
using UnityEngine.U2D.Animation;

namespace FingTools.Lime
{
[System.Serializable]
public class SpritePart_SO : ScriptableObject
{
    public CharSpriteType type;
    public Sprite[] sprites;
    public SpriteLibraryAsset spriteLibraryAsset; 
} 
}