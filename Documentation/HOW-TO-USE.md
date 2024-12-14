## Table of contents
1. [Installation](#installation-)
2. [Getting Started](#getting-started-)
3. [Using the Actor Editor ](#Using-the-Actor-Editor )
4. [Using the Model Controller](#Using-the-Model-Controller)
6. [Updating Your Package](#updating-your-package-)

## Installation 📥

To include **FingTools** in your Unity project:

1. **Add the package from a Git repository:**

```shell
https://github.com/Fingbel/FingTools.git
```

2. Open your Unity project.
3. Go to **Window** > **Package Manager**.
4. Click on the **+** button in the top-left corner and select **"Add package from Git URL..."**.
5. Paste the link:

```shell
https://github.com/Fingbel/FingTools.git
```

6. Click **Add**.

Your package should now be installed and available for use in your Unity project.

## Getting Started 🚀
1. First click on the new FingTools tab and click on Importer -> Character Importer
2. Select the moderninterior.zip file you downloaded from itch.io Limezu's page
3. Select the size of sprite you want to import
4. Click Import
(This process can take a while as the tool is unzipping/slicing/importing and building a SpriteLibraryAsset for every single entry)

## Using the Actor Editor 🧩
Now that you have correctly imported all the assets you have access to the Actor Editor Window in the FingTools tab.  
Here you can create and modify actor presets to be used by actors at runtime.  
Every actor preset is saved as a scriptable object in your resource folder.

## Using the Model Controller
Now that you have some actor presets you can right click any GameObject in your scene and attach a Model Controller (in the Fingtools sub-menu).  
With the Model attach, you can now select one of the actor preset you created previously

## Updating Your Package 🔄

1. Open your **Package Manager** in Unity.
2. Find **FingTools** in the list of installed packages.
3. Use the built-in **"Update"** button to ensure your package is up-to-date.

Alternatively, you can manually pull updates from the repository:

```shell
git pull https://github.com/Fingbel/FingTools.git
```

## Credits 🙌

Special thanks to the creators of **Modern Interiors**, [Limezu](https://limezu.itch.io/)

## Support & Contributions 📣

If you experience issues, require enhancements, or want to contribute:

- Visit the **GitHub repository**: [https://github.com/Fingbel/FingTools.git](https://github.com/Fingbel/FingTools.git)
- Open an **issue** or make a **pull request** to help improve this package.