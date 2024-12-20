using UnityEngine;
using UnityEngine.U2D.Animation;

namespace FingTools.Internal
{
[System.Serializable]
public class SpritePart_SO : ScriptableObject
{
    public ActorPartType type;
    public Sprite[] sprites;
    public SpriteLibraryAsset spriteLibraryAsset; 
} 
}