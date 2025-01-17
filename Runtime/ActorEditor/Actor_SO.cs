using System;
using UnityEngine;


namespace FingTools.Internal{
    public class Actor_SO : ScriptableObject {
        [Header("Sprites")]
        public SpritePart_SO body;
        public SpritePart_SO outfit;
        public SpritePart_SO eyes;
        public SpritePart_SO hairstyle;
        public SpritePart_SO accessory;   
             
    }
}