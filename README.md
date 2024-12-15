## Table of contents
1. [Installation](#installation-)
2. [Getting Started](#Importing-Limezu-characters)
3. [Using the Actor Editor ](#Using-the-Actor-Editor )
4. [Using the Model Controller](#Using-the-Model-Controller)
6. [Updating Your Package](#updating-your-package-)

## Installation 📥

To include **FingTools** in your Unity project:

1. Open your Unity project.
2. Go to **Window** > **Package Manager**.
3. Click on the **+** button in the top-left corner and select **"Add package from Git URL..."**.
4. Paste the following link:
```shell
https://github.com/Fingbel/FingTools.git
```
6. Click **Add**.


## Importing Limezu characters 🚀
1. First click on the new FingTools tab and click on Importer -> Character Importer
2. Select the moderninterior.zip file you downloaded from itch.io Limezu's page
3. Select the size of sprite you want to import
4. Click Import
(This process can take a while as the tool is unzipping/slicing/importing and building a SpriteLibraryAsset for every single entry)

## Using the Actor Editor 🧩
* After you finished to import you have access to the Actor Editor window in the FingTools tab.  
* In this window you can create and modify actor presets to be used by the Model Controllers at runtime.  
* Every actor preset is saved as a scriptable object in your resource folder.

## Getting Started with the ActorAPI 🧑
* Now that you have some actor presets to use you can right click any GameObject in your scene and attach an ActorAPI to it(in the Fingtools sub-menu).  
* With the ActorAPI attached and selected, in the inspector you can select one of the actor preset to be used.

## Using the ActorAPI in code
* Once you have a reference to the ActorAPI you can make call to interact with it.
* You can find more informations about the API in the Documentation.
* To use in conjonction with the API calls, you can use the auto-generated Enums to find any asset.

## Basic PlayerController example
This is an example of a basic PlayerController making use of the ActorAPI functionalities
```C#
using UnityEngine;
using FingTools;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float _movementSpeed = 5f;

    private ActorAPI actorAPI;
    private Vector2 _movement;
    private bool movementLocked =false;

    private void Awake() {
        actorAPI = GetComponentInChildren<ActorAPI>();
    }

    private void Update()
    {
        _movement = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if(Input.GetKeyDown(KeyCode.Space))
        {
            movementLocked = true;
            actorAPI.PlayOneShotAnimation(OneShotAnimation.Punching,true,() => movementLocked = false);
        }
        if(Input.GetKeyDown(KeyCode.E))
        {
            actorAPI.EquipBodyPart(AccessoriesAssets.Accessory_01_Ladybug_01.ToString());         
        }
        if(Input.GetKeyDown(KeyCode.A))
        {
            actorAPI.RemoveBodyPart(CharSpriteType.Accessories);         
        }
    }

    private void FixedUpdate() 
    {
        if(movementLocked) return;
        if(_movement.magnitude > 0)
        {
            actorAPI.SetLoopingAnimation(LoopingAnimation.Walking);
            transform.position += new Vector3(_movement.x, _movement.y) * Time.fixedDeltaTime * _movementSpeed;
                    actorAPI.SetDirection
                    (
                        _movement.x > 0 ? CardinalDirection.E : 
                        _movement.x < 0 ? CardinalDirection.W : 
                        _movement.y > 0 ? CardinalDirection.N : 
                        _movement.y < 0 ? CardinalDirection.S : 
                        CardinalDirection.S
                    );        
        }
        else
        {
            actorAPI.SetLoopingAnimation(LoopingAnimation.Idle);
        }
    }
}

```
## Credits 🙌

Special thanks to the creators of **Modern Interiors**, [Limezu](https://limezu.itch.io/)

```
## Credits 🙌

Special thanks to the creators of **Modern Interiors**, [Limezu](https://limezu.itch.io/)
