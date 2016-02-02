namespace GVGAI

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics
open System
open System.Collections
open System.Collections.Generic

type PhysicsType =
| PHYSICS_NONE = -1
| PHYSICS_GRID = 0
| PHYSICS_CONT = 1
| PHYSICS_NON_FRICTION = 2
| PHYSICS_GRAVITY = 3

type VGDLType =
| VGDL_GAME_DEF = 0
| VGDL_SPRITE_SET = 1
| VGDL_INTERACTION_SET = 2
| VGDL_LEVEL_MAPPING = 3
| VGDL_TERMINATION_SET = 4

type MovementType =
| STILL = 0
| ROTATE = 1
| MOVE = 2

module VectorType =
    let NIL = new Vector2d(-1.0, -1.0)

    let NONE = new Vector2d(0.0, 0.0)
    let RIGHT = new Vector2d(1.0, 0.0)
    let LEFT = new Vector2d(-1.0, 0.0)
    let UP = new Vector2d(0.0, -1.0)
    let DOWN = new Vector2d(0.0, 1.0)

    let BASEDIRS = [|UP; LEFT; DOWN; RIGHT|]

type VGDLSprite =
    {
    name : string // Name of this sprite.
    is_static : bool // Indicates if this sprite is static or not.
    only_inacitve: bool // Indicates if passive movement is denied for this sprite.
    is_avatar : bool // Indicates if this sprite is the avatar (player) of the game.
    is_stochastic : bool // Indicates if the sprite has a stochastic behaviour.
    color : System.Drawing.Color // Color of this sprite.
    cooldown : int // States the pause ticks in-between two moves
    speed : float // Scalar speed of this sprite.
    mass : float // Mass of this sprite (for Continuous physics).
    physicstype_id : int // Id of the type if physics this sprite responds to.
    physicstype : string // String that represents the physics type of this sprite.
    physics : Physics // Reference to the physics object this sprite belongs to.
    shrinkfactor : float // Scale factor to draw this sprite.
    is_oriented : bool // Indicates if this sprite has an oriented behaviour.
    draw_arrow : bool // Tells if an arrow must be drawn to indicate the orientation of the sprite.
    orientation : Vector2d // Orientation of the sprite.
    rect : Rectangle // Rectangle that this sprite occupies on the screen.
    lastrect : Rectangle // Rectangle occupied for this sprite in the previous game step.
    lastmove : int // Tells how many timesteps ago was the last move
    strength : float // Strength measure of this sprite.
    singleton : bool // Indicates if this sprite is a singleton.
    is_resource : bool // Indicates if this sprite is a resource.
    portal : bool // Indicates if this sprite is a portal.
    invisible : bool // Indicates if the sprite is invisible. If it is, the effect is that it is not drawn.
    itypes : ResizeArray<int> // List of types this sprite belongs to. It contains the ids, including itself's, from this sprite up
                              // in the hierarchy of sprites defined in SpriteSet in the game definition.
    resources : SortedDictionary<int,int> // Indicates the amount of resources this sprite has, for each type defined as its int identifier.
    image : Texture2D // Image of this sprite.
    img : string // String that represents the image in VGDL.
    is_npc : bool // Indicates if this sprite is an NPC.
    spriteID : int // ID of this sprite.
    is_from_avatar : bool // Indicates if this sprite was created by the avatar.
    bucket : int // Bucket
    bucketSharp : bool // Bucket remainder.
    rotateInPlace : bool // Indicates if the sprite is able to rotate in place.
    isFirstTick : bool // Indicates if the sprite is in its first cycle of existence.
                       // Passive movement is not allowed in the first tick.
    }


and Physics =
    abstract member passiveMovement : VGDLSprite -> MovementType
    abstract member activeMovement : VGDLSprite * Vector2d * float -> MovementType // sprite * action * speed
    abstract member distance : Rectangle * Rectangle -> float

