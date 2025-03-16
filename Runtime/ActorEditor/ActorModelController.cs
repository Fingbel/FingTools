using System;
using System.Collections.Generic;
using UnityEngine;

namespace FingTools.Internal
{
    public class ActorModelController : MonoBehaviour
    {
        [SerializeField] private Actor_SO actor_SO = null;
        [SerializeField] private SpritePartController bodySpriteController;
        [SerializeField] private SpritePartController hairstyleSpriteController;
        [SerializeField] private SpritePartController outfitSpriteController;
        [SerializeField] private SpritePartController eyeSpriteController;
        [SerializeField] private SpritePartController accessorySpriteController;

        private Dictionary<ActorPartType, SpritePartController> partControllers = new();
        private Dictionary<ActorPartType, SpritePart_SO> partDIct = new();
        private float maxAnimationTick = 0.13f;
        private float animationTick;
        private int currentAnimationFrame;        
        private CardinalDirection lastCurrentDirection;
        private bool isActive = true;
        private readonly int previewSpriteSouthIndex = 3;
        public string currentAnimation;
        public CardinalDirection CurrentDirection { get => lastCurrentDirection; set => lastCurrentDirection = value; }
        public float MaxAnimationTick { get => maxAnimationTick; set => maxAnimationTick = value; }
        private Dictionary<string, (int spritesPerDirection, bool fixedDirection)> animationConfigMap;
        private Action onAnimationCompleteCallback;
        private string baseAnimationName;
        public bool isLocked = false;

        
        private void Awake()
        {
            currentAnimation = "Idle";
            if (transform.parent == null)
            {
                Debug.LogError("Actor model controller must be a child of a transform!");
                return;
            }

            InitializeAnimationConfigs();
            // Map sprite controllers to sprite types
            partControllers.Add(ActorPartType.Bodies, bodySpriteController);
            partControllers.Add(ActorPartType.Hairstyles, hairstyleSpriteController);
            partControllers.Add(ActorPartType.Outfits, outfitSpriteController);
            partControllers.Add(ActorPartType.Eyes, eyeSpriteController);
            partControllers.Add(ActorPartType.Accessories, accessorySpriteController);

            if (actor_SO != null)
            {
                ApplyPrebuiltLibraries(actor_SO);
            }
            lastCurrentDirection = CardinalDirection.S;
        }
        public string GetBodyPartName(ActorPartType actorPartType)
        {
            switch(actorPartType)
            {
                case ActorPartType.Accessories: return partDIct[ActorPartType.Accessories]?.name.ToString();
                case ActorPartType.Outfits: return partDIct[ActorPartType.Outfits]?.name.ToString();
                case ActorPartType.Eyes: return partDIct[ActorPartType.Eyes]?.ToString();
                case ActorPartType.Bodies: return partDIct[ActorPartType.Bodies]?.ToString();
                case ActorPartType.Hairstyles: return partDIct[ActorPartType.Hairstyles]?.name.ToString();
            }
            return null;
        }
        private void Update()
        {
            if (actor_SO != null && isActive)
            {
                animationTick -= Time.deltaTime;
                if (animationTick <= 0)
                {
                    AnimationTick();
                }
            }
        }

        public void AnimationTick()
        {
            // Lookup animation configuration
            if (!animationConfigMap.TryGetValue(currentAnimation.ToString(), out var config))
            {
                Debug.LogWarning($"Animation config not found for: {currentAnimation}");
                return;
            }

            // Use the correct direction for fixed-direction animations
            string directionLabel = config.fixedDirection ? "S" : lastCurrentDirection.ToString();

            // Generate the sprite label for the current frame
            string label = $"{directionLabel}_{currentAnimationFrame}";

            // Resolve animations for all sprite controllers
            foreach (var controller in partControllers.Values)
            {
                controller.Resolve(currentAnimation, label);
            }

            // Reset tick
            animationTick = maxAnimationTick;

            // Update the current animation frame, wrapping around
            currentAnimationFrame = (currentAnimationFrame + 1) % config.spritesPerDirection;

            if (currentAnimationFrame >= config.spritesPerDirection - 1)
            {
                onAnimationCompleteCallback?.Invoke(); // Trigger the callback   
            }
        }

        private void InitializeAnimationConfigs()
        {
            animationConfigMap = new Dictionary<string, (int spritesPerDirection, bool fixedDirection)>();
            
            foreach (var config in animationConfigs)
            {
                animationConfigMap[config.category] = (config.spritesPerDirection, config.fixedDirection);
            }
        }

        public void UpdatePreviewSprites()
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

        public bool ApplyPrebuiltLibraries(Actor_SO actorSO)
        {
            if (actorSO == null)
            {
                Debug.LogWarning($"An Actor has not been assigned to the object: {transform.parent?.name}", this);
                return false;
            }
            // Assign prebuilt libraries to sprite controllers
            bodySpriteController?.UpdateLibrary(actorSO.body?.spriteLibraryAsset);
            hairstyleSpriteController?.UpdateLibrary(actorSO.hairstyle?.spriteLibraryAsset ?? null);
            outfitSpriteController?.UpdateLibrary(actorSO.outfit?.spriteLibraryAsset ?? null);
            eyeSpriteController?.UpdateLibrary(actorSO.eyes?.spriteLibraryAsset ?? null);
            accessorySpriteController?.UpdateLibrary(actorSO.accessory?.spriteLibraryAsset ?? null);
            partDIct.Add(ActorPartType.Accessories,actor_SO.accessory);
            partDIct.Add(ActorPartType.Hairstyles,actor_SO.hairstyle);
            partDIct.Add(ActorPartType.Outfits,actor_SO.outfit);
            partDIct.Add(ActorPartType.Eyes,actor_SO.eyes);
            partDIct.Add(ActorPartType.Bodies,actor_SO.body);
            return true;
        }

        public void UpdatePart(ActorPartType type, ActorSpritePart_SO spritePart) 
        {
            partControllers[type].UpdateLibrary(spritePart?.spriteLibraryAsset ?? null);
            switch (type)
            {
            case ActorPartType.Accessories:
                partDIct[ActorPartType.Accessories] = spritePart;
                break;
            case ActorPartType.Outfits:
                partDIct[ActorPartType.Accessories] = spritePart;
                break;
            case ActorPartType.Eyes:
                partDIct[ActorPartType.Accessories] = spritePart;
                break;
            case ActorPartType.Bodies:
                partDIct[ActorPartType.Accessories] = spritePart;
                break;
            case ActorPartType.Hairstyles:
                partDIct[ActorPartType.Accessories] = spritePart;
                break;
            }
        }

        //Maybe we should implement a state machine to handle animations
        //
        public void SetLoopingAnimation(LoopingAnimation animation)
        {
            if(isLocked) return;
            currentAnimation = animation.ToString();
        }

        public void PlayOneShotAnimation(OneShotAnimation animation, bool locked = false,Action onAnimationComplete = null)
        {
            if (isLocked) return;
            isLocked = locked;
            baseAnimationName = currentAnimation.ToString();
            currentAnimationFrame = 0;
            currentAnimation = animation.ToString();
            onAnimationCompleteCallback += onAnimationComplete;
            onAnimationCompleteCallback += () => 
            {
                currentAnimation = baseAnimationName;
                isLocked = false;
                onAnimationCompleteCallback = null;
            };
            
        }


        public readonly (string category, int spritesPerDirection, bool fixedDirection,bool isLooping)[] animationConfigs =
        {
            ("Fixed", 1, false,true),
            ("Idle", 6, false,true),
            ("Walking", 6, false,true),
            ("Sleeping", 6, true,true),
            ("Sitting", 6, false,true),
            ("Phone_Out", 3, true,false),
            ("Phoning", 6, true,true),
            ("Phone_In", 3, true,false),
            ("Reading", 6, true,true),
            ("BookTurning", 6, true,true),
            ("Pushing", 6, false,true),
            ("Picking", 12, false,false),
            ("Gifting", 10, false,false),
            ("Lifting", 14, false,false),
            ("Throwing", 14, false,false),
            ("Hitting", 6, false,false),
            ("Punching", 6, false,false),
            ("Stabbing", 6, false,false),
            ("GunGrabbing", 4, false,false),
            ("GunIdling", 6, false,true),
            ("GunShooting", 3, false,false),
            ("Hurting", 3, false,false),
        };
    }
}
