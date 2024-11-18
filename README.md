Tripo 3d to mixamo conversion

Upload character with 3-5000 (or 18000) retopology no animations or rigging as fbx file for unity

Upload fix file to blender

(Maybe rig into t-pose?)

1. Unwrap model (convert 3d surface into 2d surface so that have a surface to bake on the textures for later)
    1. Select mesh in object mode
    2. Go to edit mode, press a to highlight all mesh faces
    3. Open uv editor (this is to see the 2d unwrapped map being made, although I think trips 3d already comes in with one. Want to change this tho so have to re-wrap it)
    4. Press uv, smart project, and can set as default values (I think, maybe play around with island margin)
    5. Unwrap by pressing ok or something 
2. Baking process
    1. Shader editor
    2. Create new texture image node disconnected from rest of graph
    3. Can leave everything as default except change to 2048x2048, and check alpha if need transparent parts
    4. Go to render properties on right side of screen (camera icon)
    5. Change render engine from eevee to cycles if not already cycles
    6. Basically, we are replacing every node that goes into the principled bdsf node. 
        1. Diffuse is for base color, roughness for roughness, 
    7. In object mode, select the mesh or just the model(so that blender knows what to bake onto)
    8. In shader editor, make sure new disconnected node is selected
    9. Press bake in render properities
    10. Open uv/image editor, save the image created as a .png or .jpg
    11. Replace nodes 

Importing character and animations from mixamo to unity

https://discussions.unity.com/t/how-to-getting-mixamo-and-unity-to-work/715933

First, download just character without any animations. With skin, 30fps, T_POSE. You need t-pose because unity actually doesn’t ultimately use the skeleton rig that mixamo created for animation. It will instead re rig it based on unity’s humanoid skeleton rig, but this works nicely.

Once have model in unity, need to rig it in unity so that unity can play animations on it. Click on model in assets folder, go to rig tab, do humanoid, uncheck strip bones and optimize game object, and apply. This will re rig the model with unity’s humanoid skeleton so that animations can be applied. 

In the same folder as your model in the assets folder, create an animator controller for that model. Once you eventually drag the character model into your scene as nested into the overall parent enemy game object, the model should already have an animator component added to it from when you rigged it. If not, you can manually add it. Make sure the controller slot uses your new animator controller you j put created, and the avatar slot has the avatar in the model as well. Uncheckk apply root motion box because that will change the transform of the mesh, not the parent object. WE ARE DOING ALL TRANSFORM CHANGES ON THE PARENT OBJECT FOR LOGIC REASONS.

Next, download individual animations. Remember, humanoid animations from mixamo can be used for any humanoid rig in unity, so not character specific. import without skin, and if an option, uncheck the move character part. (e.g. walking should be standing still). Will need to rig each of these individual animations for humanoid, uncheck the two boxes again. You can then place this animation directly into the animator controller of any model that you want to use this animation.