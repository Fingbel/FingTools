using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace FingTools.Internal{
    public enum PortraitPartType{Accessory,Eyes,Hairstyle,Skin}
    public class Portrait_SO : ScriptableObject
    {
        public PortraitPart_SO accessory;
        public PortraitPart_SO eyes;
        public PortraitPart_SO hairstyle;
        public PortraitPart_SO body;        
        public void RefreshPortrait(Actor_SO actor_SO)
        {            
            if(actor_SO.accessory != null) ResolvePortraitPart(PortraitPartType.Accessory,actor_SO.accessory.name);                
            else accessory = null;
                
            if(actor_SO.eyes != null) ResolvePortraitPart(PortraitPartType.Eyes,actor_SO.eyes.name);
            else eyes = null;

            if(actor_SO.hairstyle != null) ResolvePortraitPart(PortraitPartType.Hairstyle,actor_SO.hairstyle.name);
            else hairstyle = null;

            if(actor_SO.body != null) ResolvePortraitPart(PortraitPartType.Skin,actor_SO.body.name);
            else body = null;
        }

        private void ResolvePortraitPart(PortraitPartType portraitPartType, string actorPartName)
        {
            // Step 1: Add the "PG_" prefix to the actorPartName
            string expectedPortraitPartName = "PG_" + actorPartName;

            // Now process the PortraitPartType
            switch (portraitPartType)
            {
                case PortraitPartType.Accessory:
                    expectedPortraitPartName = Regex.Replace(expectedPortraitPartName, @"(_0[1-9])$", match =>
                    {
                        return match.Value.Replace("0", "");
                    });
                    var newAccessory = SpriteManager.Instance.accessoryPortraitParts.Where(x => x.name == expectedPortraitPartName).FirstOrDefault();
                    if (newAccessory != null)
                    {
                        accessory = newAccessory;
                    }
                    break;

                case PortraitPartType.Eyes:
                    var newEyes = SpriteManager.Instance.eyePortraitParts.Where(x => x.name == expectedPortraitPartName).FirstOrDefault();
                    if (newEyes != null)
                    {
                        eyes = newEyes;
                    }
                    break;

                case PortraitPartType.Hairstyle:
                    expectedPortraitPartName = Regex.Replace(expectedPortraitPartName, @"(_0[1-9])$", match =>
                    {
                        return match.Value.Replace("0", "");
                    });
                    var newHairstyle = SpriteManager.Instance.hairstylePortraitParts.Where(x => x.name == expectedPortraitPartName).FirstOrDefault();
                    if (newHairstyle != null)
                    {
                        hairstyle = newHairstyle;
                    }
                    break;

                case PortraitPartType.Skin:
                    expectedPortraitPartName = "PG_Skin" + actorPartName.Substring(4);
                    expectedPortraitPartName = Regex.Replace(expectedPortraitPartName, @"(_0[1-9])$", match =>
                    {
                        return match.Value.Replace("0", "");
                    });
                    var newSkin = SpriteManager.Instance.bodyPortraitParts.Where(x => x.name == expectedPortraitPartName).FirstOrDefault();
                    if (newSkin != null)
                    {
                        body = newSkin;                    
                    }
                    break;
            }
        }
    }
}