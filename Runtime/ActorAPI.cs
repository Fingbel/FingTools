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
        public void SetDirection(CardinalDirection direction)
        {
            if(modelController.CurrentDirection != direction)
            {
                modelController.CurrentDirection = direction;
                modelController.AnimationTick(); // Tick immediatly to avoid waiting for the next one
            }
            
        }
        /// <summary>
        /// Remove a body part from the actor, you only need to pass the ActorPartType of the part. 
        /// The Body part cannot be removed, use SetBodyPart directly instead.
        /// </summary>
        /// <param name="type"></param>
        public void RemoveBodyPart(ActorPartType type)
        {
            if(type == ActorPartType.Bodies)
            {
                Debug.LogError("ActorAPI call RemoveBodyPart : Cannot remove the body, use SetBodyPart directly instead to change it.");
            }
            else
            {
                modelController.UpdatePart(type,null);
            }
        }
        /// <summary>
        /// Equip a body part to the actor, you only need to pass the name of the part. 
        /// The easisest and safest way is to use the enums auto-generated at import. 
        /// Example : EquipBodyPart(AccessoriesAssets.Accessory_01_Ladybug_01.ToString());
        /// </summary>
        /// <param name="partName"></param>
        public void EquipBodyPart(string partName)
        {
            var spriteType = SpriteManager.Instance.GetSpriteTypeFromAssetName(partName);
            var part = SpriteManager.Instance.GetSpritePart(spriteType, partName);
            if(part != null)
            {
                modelController.UpdatePart(spriteType,part);    
            }
            else
            {
                Debug.LogError($"ActorAPI call ChangeBodyPart : Part {partName} not found or is not of type {spriteType}.");
            }
            
        }

        /// <summary>
        ///  Set the actor to a new looping animation.
        /// </summary>
        public void SetLoopingAnimation(LoopingAnimation animation)
        {            
            modelController.SetLoopingAnimation(animation);
        }   

        /// <summary>
        /// Play a one shot animation
        /// </summary>
        public void PlayOneShotAnimation(OneShotAnimation animation, bool locked = false,Action onAnimationComplete = null)
        {
            modelController.PlayOneShotAnimation(animation,locked,onAnimationComplete);
        }        
    }
}