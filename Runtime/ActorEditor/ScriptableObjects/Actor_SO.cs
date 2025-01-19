using UnityEngine;

namespace FingTools.Internal{
    public class Actor_SO : ScriptableObject {
        [Header("Sprites")]
        public ActorSpritePart_SO body;
        public ActorSpritePart_SO outfit;
        public ActorSpritePart_SO eyes;
        public ActorSpritePart_SO hairstyle;
        public ActorSpritePart_SO accessory;   

        public Portrait_SO portrait_SO;
    }
}