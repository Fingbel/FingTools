using System;
using UnityEditor;
using UnityEngine;

namespace FingTools.Internal{
    public enum PortraitPartType{Accessory,Eyes,Hairstyle,Skin}
    public class Portrait_SO : ScriptableObject
    {
        public PortraitPart_SO accessory;
        public PortraitPart_SO eyes;
        public PortraitPart_SO hairstyle;
        public PortraitPart_SO body;        
    }
}