using System;
using FingTools.Internal;
using UnityEngine;

namespace FingTools{

    public enum CardinalDirection { E, N, W, S }

    public enum LoopingAnimation
    {
        Fixed, Idle, Walking, Pushing,Sleeping, Sitting, Phoning,  Reading, BookTurning,GunIdling
    }

    public enum OneShotAnimation
    {
        Phone_Out,Phone_In, Picking, Gifting, Lifting, Throwing, Hitting, 
        Punching, Stabbing,GunGrabbing,GunShooting, Hurting                
    }

    public enum ActorPartType {Accessories,Bodies,Outfits,Hairstyles,Eyes}

    public class ActorAPI : MonoBehaviour
    {
        private ActorModelController modelController;

    private void Awake() {
            modelController = GetComponent<ActorModelController>();
        }

        /// <summary>
        /// Set the actor's current direction (North, South, East, West).
        /// </summary>

        public bool SetDirection(CardinalDirection direction)
        {
            if(modelController.CurrentDirection != direction)
            {
                modelController.CurrentDirection = direction;

                modelController.AnimationTick(); // Tick immediatly to avoid waiting for the next one
                return true;
            }
            else
            {                
                return false;
            }
            
        }
        /// <summary>

        /// Remove a body part from the actor, you only need to pass the ActorPartType of the part. 
        /// The Body part cannot be removed, use SetBodyPart directly instead.
        /// </summary>
        /// <param name="type"></param>
        public bool RemoveBodyPart(ActorPartType type)
        {
            if(type != ActorPartType.Bodies)
            {
                modelController.UpdatePart(type,null);
                return true;
            }
            else
            {                
                Debug.LogError("ActorAPI call RemoveBodyPart : Cannot remove the body, use SetBodyPart directly instead to change it.");
                return false;
            }
        }
        /// <summary>
        /// Equip a body part to the actor, you only need to pass the name of the part. 
        /// The easisest and safest way is to use the enums auto-generated at import. 
        /// Example : EquipBodyPart(AccessoriesAssets.Accessory_01_Ladybug_01.ToString());
        /// </summary>
        /// <param name="partName"></param>
        public bool EquipBodyPart(string partName)
        {
            var spriteType = SpriteManager.Instance.GetSpriteTypeFromAssetName(partName);
            var part = SpriteManager.Instance.GetSpritePart(spriteType, partName);
            if(part != null)
            {
                modelController.UpdatePart(spriteType,part);    
                return true;
            }
            else
            {
                Debug.LogError($"ActorAPI call ChangeBodyPart : Part {partName} not found or is not of type {spriteType}.");
                return false;
            }
            
        }

        /// <summary>
        ///  Set the actor to a new looping animation.
        /// </summary>

        public bool SetLoopingAnimation(LoopingAnimation animation)
        {            
            if(animation.ToString() != modelController.currentAnimation)
            {
                modelController.SetLoopingAnimation(animation);
                return true;
            }
            else
            {                
                return false;
            }
        }   

        /// <summary>
        /// Play a one shot animation
        /// </summary>

        public bool PlayOneShotAnimation(OneShotAnimation animation, bool locked = false,Action onAnimationComplete = null)
        {
            if(!modelController.isLocked)
            {
                modelController.PlayOneShotAnimation(animation,locked,onAnimationComplete);
                return true;
                
            }
            else
            {
                Debug.LogWarning("ActorAPI call PlayOneShotAnimation : The actor is locked for animations,skipping.");
                return false;
            }            
        }        
    }
}