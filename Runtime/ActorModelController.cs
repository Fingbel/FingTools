using System.Collections.Generic;
using UnityEngine;
using FingTools.Lime;

namespace FingTools
{
    public enum CardinalDirection { E, N, W, S }
    public enum ActorAnimation
    {
        Idle, Walking, Sleeping, Sitting, Phone_Out, Phoning, Phone_In, Reading, BookTurning,
        Pushing, Picking, Gifting, Lifting, Throwing, Hitting, Punching, Stabbing,
        GunGrabbing, GunIdling, GunShooting, Hurting
    }

    public class ActorModelController : MonoBehaviour
    {
        [SerializeField] private Actor_SO actor_SO = null;
        [SerializeField] private SpritePartController bodySpriteController;
        [SerializeField] private SpritePartController hairstyleSpriteController;
        [SerializeField] private SpritePartController outfitSpriteController;
        [SerializeField] private SpritePartController eyeSpriteController;
        [SerializeField] private SpritePartController accessorySpriteController;

        private Dictionary<CharSpriteType, SpritePartController> partControllers = new();
        private float maxAnimationTick = 0.13f;
        private float animationTick;
        private int currentAnimationFrame;
        private CardinalDirection lastCurrentDirection;
        private bool isWalking;
        private bool isActive = true;
        private readonly int previewSpriteSouthIndex = 3;

        public bool IsActive { get => isActive; set => isActive = value; }
        public CardinalDirection CurrentDirection { get => lastCurrentDirection; set => lastCurrentDirection = value; }
        public bool IsWalking { get => isWalking; set => isWalking = value; }
        public float MaxAnimationTick { get => maxAnimationTick; set => maxAnimationTick = value; }

        private void Awake()
        {
            if (transform.parent == null)
            {
                Debug.LogError("Actor model controller must be a child of a transform!");
                return;
            }

            // Map sprite controllers to sprite types
            partControllers.Add(CharSpriteType.Bodies, bodySpriteController);
            partControllers.Add(CharSpriteType.Hairstyles, hairstyleSpriteController);
            partControllers.Add(CharSpriteType.Outfits, outfitSpriteController);
            partControllers.Add(CharSpriteType.Eyes, eyeSpriteController);
            partControllers.Add(CharSpriteType.Accessories, accessorySpriteController);

            if (actor_SO != null)
            {
                ApplyPrebuiltLibraries(actor_SO);
            }

            lastCurrentDirection = CardinalDirection.S;
        }

        private void Update()
        {
            if (actor_SO != null && isActive)
            {
                animationTick -= Time.deltaTime;
                if (animationTick <= 0)
                {
                    // Determine animation category and label for the next animation frame
                    string label = lastCurrentDirection.ToString();
                    string category = isWalking ? "Walking" : "Idle";

                    // Update animation category and label
                    label = $"{label}_{currentAnimationFrame}";

                    // And now we resolve
                    foreach (var controller in partControllers.Values)
                    {
                        controller.Resolve(category, label);
                    }
                    animationTick = maxAnimationTick;
                    currentAnimationFrame = currentAnimationFrame == 5 ? 0 : currentAnimationFrame + 1;
                }
            }
        }
        
        private void OnValidate()
        {
            SetPreviewSprites();
        }

        public void SetPreviewSprites()
        {
            if (Application.isPlaying) return;
            if (actor_SO != null)
            {
                bodySpriteController.SetPreviewSprite(actor_SO.body?.sprites[previewSpriteSouthIndex]);
                outfitSpriteController.SetPreviewSprite(actor_SO.outfit?.sprites[previewSpriteSouthIndex]);
                eyeSpriteController.SetPreviewSprite(actor_SO.eyes?.sprites[previewSpriteSouthIndex]);
                hairstyleSpriteController.SetPreviewSprite(actor_SO.hairstyle?.sprites[previewSpriteSouthIndex]);
                accessorySpriteController.SetPreviewSprite(actor_SO.accessory?.sprites[previewSpriteSouthIndex]);
            }
            else
            {
                ClearPreviewSprites();
            }
        }

        public void ClearPreviewSprites()
        {
            foreach (var controller in partControllers.Values)
            {
                controller.SetPreviewSprite(null);
            }
        }

        public void ApplyPrebuiltLibraries(Actor_SO actorSO)
        {
            if (actorSO == null)
            {
                Debug.LogWarning($"An Actor has not been assigned to the object: {transform.parent?.name}", this);
                return;
            }
            // Assign prebuilt libraries to sprite controllers
            bodySpriteController?.UpdateLibrary(actorSO.body?.spriteLibraryAsset);
            hairstyleSpriteController?.UpdateLibrary(actorSO.hairstyle?.spriteLibraryAsset);
            outfitSpriteController?.UpdateLibrary(actorSO.outfit?.spriteLibraryAsset);
            eyeSpriteController?.UpdateLibrary(actorSO.eyes?.spriteLibraryAsset);
            accessorySpriteController?.UpdateLibrary(actorSO.accessory?.spriteLibraryAsset);
            
        }
    }
}
