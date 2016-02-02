#r "../../../../../../../../Program Files (x86)/MonoGame/v3.0/Assemblies/Windows/MonoGame.Framework.dll"

// The aliens game from GVGDL library with continuous physics.

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Input
open System
open System.IO
open System.Collections
open System.Collections.Generic

let sprite_list =
    Directory.GetFiles (__SOURCE_DIRECTORY__ + @"\Sprites")
    |> Seq.toArray
    |> Array.mapi (fun i x -> @"Sprites\" + ((x.Split [|'\\'|] |> Array.last).Split [|'.'|]).[0],i)

if sprite_list.Length <> 80 then failwith "Sprites directory has changed. Change the enum."

#load "game_engine_enums.fs"
open GameEngineEnums

let globalTexure2dDict = Dictionary<int,Texture2D>()

let font_list =
    Directory.GetFiles (__SOURCE_DIRECTORY__ + @"\Fonts")
    |> Seq.toArray
    |> Array.mapi (fun i x -> @"Fonts\" + ((x.Split [|'\\'|] |> Array.last).Split [|'.'|]).[0],i)

if font_list.Length <> 1 then failwith "Fonts directory has changed. Change the enum."

let globalSpriteFontDict = Dictionary<int,SpriteFont>()

let rng = System.Random()

let BACKBUFFER_WIDTH, BACKBUFFER_HEIGHT = 800, 600
let BACKBUFFER_WIDTH_F, BACKBUFFER_HEIGHT_F = float32 BACKBUFFER_WIDTH, float32 BACKBUFFER_HEIGHT

type VGDLSprite =
    {
    mutable texture : Texture2D
    mutable prev_position : Vector2
    mutable position : Vector2
    mutable velocity : Vector2
    mutable id : SpriteEnum
    mutable effect : GameTime -> VGDLSprite -> VGDLSprite[]
    mutable draw : GameTime -> VGDLSprite -> SpriteBatch -> unit
    }

    member inline sprite.isenum (e: SpriteEnum) = sprite.id = e
    member inline sprite.rect = Rectangle(int sprite.position.X, int sprite.position.Y, sprite.texture.Width, sprite.texture.Height)

type VGDLSpriteFont =
    {
    font : SpriteFont
    mutable position : Vector2
    mutable draw : VGDLSpriteFont -> SpriteBatch -> unit
    }

/// Makes a printer using a passed by as a reference string.
let font_initializer index x y (score: string ref) =
    let font = globalSpriteFontDict.[index]
    let position = Vector2(float32 x, float32 y)
    let inline drawFont spriteFont (spriteBatch: SpriteBatch) =
        spriteBatch.DrawString(spriteFont.font,!score,position,Color.AliceBlue)
    {
    font = font
    position = position
    draw = drawFont
    }

/// Moves the sprite in the direction of its velocity.
let inline moveEffect (sprite: VGDLSprite) =
    sprite.prev_position <- sprite.position
    sprite.position <- Vector2(sprite.position.X + sprite.velocity.X,sprite.position.Y + sprite.velocity.Y)

/// Bounces the sprite back to the previous step.
let inline bounceEffect (sprite: VGDLSprite) =
    sprite.position <- sprite.prev_position

/// Moves the sprite to the other side if it touches the window's edge.
let inline wrapEffect (sprite: VGDLSprite) =
    sprite.prev_position <- sprite.position 
    sprite.position <- Vector2((sprite.position.X + BACKBUFFER_WIDTH_F) % BACKBUFFER_WIDTH_F, (sprite.position.Y + BACKBUFFER_HEIGHT_F) % BACKBUFFER_HEIGHT_F)

/// Checks if the sprite is outside the horizontal window boundary.
let inline is_sprite_outside_window_horizontal (sprite: VGDLSprite) =
    sprite.position.X < 0.0f || (sprite.position.X + (float32 <| sprite.texture.Width) > BACKBUFFER_WIDTH_F)

/// Checks if the sprite is outside the vertical window boundary.
let inline is_sprite_outside_window_vertical (sprite: VGDLSprite) =
    sprite.position.Y < 0.0f || (sprite.position.Y + (float32 <| sprite.texture.Height) > BACKBUFFER_HEIGHT_F)

/// Checks if the sprite is outside the window boundary.
let inline is_sprite_outside_window (sprite: VGDLSprite) =
    is_sprite_outside_window_horizontal sprite || is_sprite_outside_window_vertical sprite


/// Moves the sprite downwards by 20 pixels and bounces it if it touches the edge of the window.
let inline conveytorEffect (sprite: VGDLSprite) =
    if is_sprite_outside_window_horizontal sprite then
        sprite.position.Y <- sprite.position.Y + (float32 <| sprite.texture.Height)
        sprite.velocity.X <- -sprite.velocity.X

/// Combines the conveytor and the move effects.
let inline moveConveytorCombinator gametime (sprite: VGDLSprite) =
    conveytorEffect sprite 
    moveEffect sprite
    [|sprite|]

/// Combines the remove and the move effects.
let inline moveRemoveCombinator gametime (sprite: VGDLSprite) =
    moveEffect sprite
    if is_sprite_outside_window sprite then [||] else [|sprite|]

/// Makes a bomb object.
let bomb_initializer x y =
    let inline bombDraw gametime x (spriteBatch : SpriteBatch) = spriteBatch.Draw(x.texture, x.position, Color.White)

    let texture = globalTexure2dDict.[SpriteEnum.BOMB |> int]
    let position = Vector2(float32 x, float32 y)
    {
    texture = texture
    prev_position = position
    position = position
    velocity = Vector2(0.0f, 4.0f)
    id = SpriteEnum.BOMB
    effect = moveRemoveCombinator
    draw = bombDraw
    }

/// The Alien's standard behavior. It combines the conveytor, move and the bomb effects.
/// Uses a closure to keep track of time.
let inline moveWrapBombCombinator spawn_delay =
    let mutable elapsed_time = 0
    
    fun (gametime : GameTime) (sprite: VGDLSprite) ->
        elapsed_time <- elapsed_time + gametime.ElapsedGameTime.Milliseconds
        conveytorEffect sprite 
        moveEffect sprite
        if elapsed_time > spawn_delay then 
            elapsed_time <- elapsed_time - spawn_delay
            [|sprite;bomb_initializer sprite.position.X (sprite.position.Y + float32 sprite.texture.Height)|]
        else
            [|sprite|]

/// Makes an alien.
let alien_initializer x y =
    let inline alienDraw gametime x (spriteBatch : SpriteBatch) = spriteBatch.Draw(x.texture, x.position, Color.White)

    let texture = globalTexure2dDict.[SpriteEnum.ALIEN |> int]
    let position = Vector2(float32 x, float32 y)
    {
    texture = texture
    prev_position = position
    position = position
    velocity = Vector2(1.0f, 0.0f)
    id = SpriteEnum.ALIEN
    effect = moveWrapBombCombinator 3000
    draw = alienDraw
    }

/// Moves the sprite in the direction of the keyboard input.
let inline avatarMoveEffect sprite =
    let k = Keyboard.GetState()
    let x =
        if k.IsKeyDown(Keys.Left) then -1.0f*sprite.velocity.X
        else if k.IsKeyDown(Keys.Right) then 1.0f*sprite.velocity.X
        else 0.0f
    let y =
        if k.IsKeyDown(Keys.Down) then 1.0f*sprite.velocity.Y
        else if k.IsKeyDown(Keys.Up) then -1.0f*sprite.velocity.Y
        else 0.0f
    sprite.prev_position <- sprite.position
    sprite.position <- Vector2(sprite.position.X + x,sprite.position.Y + y) // Movement

/// Combines the avatarMove and the wrap effects.    
let inline avatarMoveWrapCombinator gametime sprite =
    avatarMoveEffect sprite
    wrapEffect sprite
    [|sprite|]

/// Makes a missile.
let inline spaceship_initializer x y velocity_x velocity_y =
    let inline draw gametime x (spriteBatch : SpriteBatch) = spriteBatch.Draw(x.texture, x.position, Color.White)

    let texture = globalTexure2dDict.[SpriteEnum.SPACESHIP |> int]
    let position = Vector2(x, y)
    {
    texture = texture
    prev_position = position
    position = position
    velocity = Vector2(velocity_x,velocity_y)
    effect = moveRemoveCombinator
    id = SpriteEnum.SPACESHIP
    draw = draw
    }

/// Returns a missile if space is pressed otherwise returns an empty array.
let inline spaceshipShotEffect (sprite: VGDLSprite) =
    let k = Keyboard.GetState()
    if k.IsKeyDown(Keys.Space) then
        [|spaceship_initializer sprite.position.X (sprite.position.Y - float32 sprite.texture.Height) 0.0f -4.0f|]
    else [||]

/// The avatar moves and lauches spaceships.
let avatarMoveShootCombinator spawn_delay =
    let mutable elapsed_time = 0
    
    fun (gametime : GameTime) (sprite: VGDLSprite) ->
        elapsed_time <- elapsed_time + gametime.ElapsedGameTime.Milliseconds
        avatarMoveEffect sprite
        if elapsed_time > spawn_delay then 
            let missile = spaceshipShotEffect sprite
            elapsed_time <- spawn_delay*(1-missile.Length)
            [|
            yield sprite
            yield! missile
            |]
        else
            [|sprite|]

/// Makes an avatar.
let avatar_initializer x y =
    let inline draw gametime x (spriteBatch : SpriteBatch) = spriteBatch.Draw(x.texture, x.position, Color.White)

    let texture = globalTexure2dDict.[SpriteEnum.AVATAR |> int]
    let position = Vector2(float32 x, float32 y)
    {
    texture = texture
    prev_position = position
    position = position
    velocity = Vector2(4.0f,4.0f)
    effect = avatarMoveShootCombinator 1000
    id = SpriteEnum.AVATAR
    draw = draw
    }

/// Makes a wall.
let wall_initializer x y =
    let inline draw gametime x (spriteBatch : SpriteBatch) = spriteBatch.Draw(x.texture, x.position, Color.White)

    let texture = globalTexure2dDict.[SpriteEnum.WALL |> int]
    let position = Vector2(float32 x, float32 y)
    {
    texture = texture
    prev_position = position
    position = position
    velocity = Vector2(0.0f,0.0f)
    effect = (fun gametime sprite -> [|sprite|])
    id = SpriteEnum.WALL
    draw = draw
    }

/// The avatar moves and lauches spaceships.
let baseSpawnEffect spawn_delay total_spawns =
    let mutable elapsed_time = 0
    let mutable elapsed_spawns = 0
    
    fun (gametime : GameTime) (sprite: VGDLSprite) ->
        elapsed_time <- elapsed_time + gametime.ElapsedGameTime.Milliseconds
        avatarMoveEffect sprite
        if elapsed_time > spawn_delay then 
            elapsed_time <- elapsed_time-spawn_delay
            elapsed_spawns <- elapsed_spawns+1
            [|
            if elapsed_spawns < total_spawns then yield sprite
            yield alien_initializer sprite.position.X sprite.position.Y
            |]
        else
            [|sprite|]

/// Makes a base.
let base_initializer x y spawn_delay total_spawns =
    let inline draw gametime x (spriteBatch : SpriteBatch) = spriteBatch.Draw(x.texture, x.position, Color.White)

    let texture = globalTexure2dDict.[SpriteEnum.BASE |> int]
    let position = Vector2(float32 x, float32 y)
    {
    texture = texture
    prev_position = position
    position = position
    velocity = Vector2(0.0f,0.0f)
    effect = baseSpawnEffect spawn_delay total_spawns
    id = SpriteEnum.BASE
    draw = draw
    }


/// Collision rules for the game of Aliens. Space Invaders clone from the GVGDL library.
let aliensCollisionRules (score_modify : int -> unit)=
    let inline cond (a : VGDLSprite) en_a (b: VGDLSprite) en_b =
        if a.isenum en_a && b.isenum en_b then false, true
        else if a.isenum en_b && b.isenum en_a then true, false
        else false, false
        
    let bombAvatarRule a b = 
        let t1,t2 = cond a SpriteEnum.BOMB b SpriteEnum.AVATAR
        if t1 || t2 then score_modify -1000
        t1,t2
    let spaceshipAlienRule a b = 
        let t1,t2 = cond a SpriteEnum.SPACESHIP b SpriteEnum.ALIEN
        if t1 || t2 then score_modify 10
        t1,t2
    let wallSpaceshipRule a b =
        let t1,t2 = cond a SpriteEnum.WALL b SpriteEnum.SPACESHIP
        if t1 || t2 then score_modify 1
        t1,t2
    let spaceshipWallRule a b =
        cond a SpriteEnum.SPACESHIP b SpriteEnum.WALL
    let wallBombRule a b =
        cond a SpriteEnum.WALL b SpriteEnum.BOMB
    let bombWallRule a b =
        cond a SpriteEnum.BOMB b SpriteEnum.WALL
    let wallAvatarBounceRule a b =
        let t1,t2 = cond a SpriteEnum.WALL b SpriteEnum.AVATAR
        if t1 then bounceEffect a
        if t2 then bounceEffect b
        false, false

    [|spaceshipAlienRule;wallSpaceshipRule;spaceshipWallRule;wallBombRule;bombWallRule; wallAvatarBounceRule|]

/// Enumerates all the collision rules
let inline colliderEffect (collision_rules: (VGDLSprite -> VGDLSprite -> bool * bool)[]) (gametime : GameTime) (ar : VGDLSprite[]) =
    [|
    let collider_ar = Array.zeroCreate ar.Length
    for i=0 to ar.Length-1 do 
        for j=i+1 to ar.Length-1 do
            let a,b = ar.[i], ar.[j]
            let intersect = Rectangle.Intersect(a.rect, b.rect)
            if intersect.Size <> Point(0) then 
                let t1,t2 = collision_rules |> Array.map(fun x -> x a b) |> Array.unzip
                collider_ar.[i] <- collider_ar.[i] || Array.exists (fun x -> x = true) t1; collider_ar.[j] <- collider_ar.[j] || Array.exists (fun x -> x = true) t2
    for i=0 to ar.Length-1 do if collider_ar.[i] = false then yield ar.[i]
    |]

/// The MonoGame engine class.
type Game1(loaders : Lazy<VGDLSprite []>, printer_loaders : Lazy<VGDLSpriteFont[]>, global_effects : (GameTime -> VGDLSprite [] -> VGDLSprite [])[]) as this =
    inherit Game()

    do this.Content.RootDirectory <- __SOURCE_DIRECTORY__
    do printfn "%s" this.Content.RootDirectory

    let graphics = new GraphicsDeviceManager(this)
    do graphics.PreferredBackBufferWidth <- BACKBUFFER_WIDTH
    do graphics.PreferredBackBufferHeight <- BACKBUFFER_HEIGHT
    do this.IsMouseVisible <- true

    // Create a new SpriteBatch, which can be used to draw textures.
    let mutable spriteBatch = Unchecked.defaultof<SpriteBatch> 

    // load teddy bears and build draw rectangles
    let mutable sprites = [||]
    let mutable printers = [||]

    override this.Initialize() =
        base.Initialize()

    override this.LoadContent() =
        spriteBatch <- new SpriteBatch(this.GraphicsDevice)
        for x,i in sprite_list do // Add textures to the global dict.
            globalTexure2dDict.Add(i,this.Content.Load<Texture2D>(x))
        for x,i in font_list do // Add sprite fonts to the global dict.
            globalSpriteFontDict.Add(i,this.Content.Load<SpriteFont>(x))
        sprites <- loaders.Value
        printers <- printer_loaders.Value

    override this.UnloadContent() =
        for x in globalTexure2dDict.Values do x.Dispose()
        globalTexure2dDict.Clear()
        globalSpriteFontDict.Clear()

    override this.Update(gameTime) =
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back = ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
        then this.Exit()

        sprites <- // Local Effects
            [|
            for sprite in sprites do yield! sprite.effect gameTime sprite
            |]

        sprites <- Array.fold (fun state effect -> effect gameTime state) sprites global_effects // Global Effects

        base.Update(gameTime)

    override this.Draw(gameTime) =
        this.GraphicsDevice.Clear(Color.CornflowerBlue)

        spriteBatch.Begin()

        for sprite in sprites do sprite.draw gameTime sprite spriteBatch
        for printer in printers do printer.draw printer spriteBatch

        spriteBatch.End()

        base.Draw(gameTime)

let characters = 
    lazy [|
    yield avatar_initializer 450 550
    //for i=1 to 15 do yield alien_initializer (i*50) 100 
    for i=1 to 10 do yield base_initializer (i*20) 100 2000 5
    for i=10 to 30 do
        for j = 20 to 25 do
            yield wall_initializer (i*20) (j*20)
    |] 

let score = ref 0
let score_string = ref "Score: 0"

let modify_score m =
    score := !score+m
    score_string := sprintf "Score: %i" !score

let printers =
    lazy [|
    font_initializer (FontEnum.ARIAL |> int) 200 200 score_string
    |]

let global_effects =
    [|
    colliderEffect (aliensCollisionRules modify_score)
    |]

let g = new Game1(characters,printers,global_effects)
g.Run()
