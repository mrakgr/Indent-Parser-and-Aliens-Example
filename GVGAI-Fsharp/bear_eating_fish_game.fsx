// The bear eating fish game from the 'Beginning Game Programming with C#' course.
// https://class.coursera.org/gameprogramming-002

// This is an eample of how one would do it in a functional language like F#.
// It is beatiful. Literally all the program code is separate from the engine and the heavy
// use of lambdas and closures ensures that the code is very readable and interchangeable.

#r "../../../../../../../../Program Files (x86)/MonoGame/v3.0/Assemblies/Windows/MonoGame.Framework.dll"

open Microsoft.Xna.Framework
open Microsoft.Xna.Framework.Graphics
open Microsoft.Xna.Framework.Input
open System

let rng = System.Random()

let BACKBUFFER_WIDTH, BACKBUFFER_HEIGHT = 800, 600
let BACKBUFFER_WIDTH_F, BACKBUFFER_HEIGHT_F = float32 BACKBUFFER_WIDTH, float32 BACKBUFFER_HEIGHT

type VGDLSprite =
    {
    texture : Texture2D
    mutable position : Vector2
    mutable speed : Vector2
    mutable effect : VGDLSprite -> bool
    mutable rect : VGDLSprite -> Rectangle
    mutable draw : VGDLSprite -> SpriteBatch -> unit
    }

    member inline t.rect'() = t |> t.rect
    member inline t.effect'() = t |> t.effect

type VGDLSpriteFont =
    {
    font : SpriteFont
    mutable position : Vector2
    mutable draw : VGDLSpriteFont -> SpriteBatch -> unit
    }

let font_initializer file x y (score: string ref) (this : Game) =
    let font = this.Content.Load<SpriteFont>(file)
    let position = Vector2(float32 x, float32 y)
    let inline drawFont spriteFont (spriteBatch: SpriteBatch) =
        spriteBatch.DrawString(spriteFont.font,!score,position,Color.AliceBlue)
    {
    font = font
    position = position
    draw = drawFont
    }


let inline moveEffect (sprite: VGDLSprite) =
    sprite.position <- Vector2(sprite.position.X + sprite.speed.X,sprite.position.Y + sprite.speed.Y)
    true

let inline wrapEffect (sprite: VGDLSprite) =
    sprite.position <- Vector2((sprite.position.X + BACKBUFFER_WIDTH_F) % BACKBUFFER_WIDTH_F, (sprite.position.Y + BACKBUFFER_HEIGHT_F) % BACKBUFFER_HEIGHT_F)
    true
    
let inline moveWrapCombinator (sprite: VGDLSprite) =
    wrapEffect sprite && moveEffect sprite

let teddy_initializer file x y (this : Game) =
    let inline teddyRect (sprite: VGDLSprite) = Rectangle(int sprite.position.X, int sprite.position.Y, sprite.texture.Width, sprite.texture.Height)
    let inline teddyDraw x (spriteBatch : SpriteBatch) = spriteBatch.Draw(x.texture, x.position, Color.White)

    let inline loadSprite file x y =
        let sprite = this.Content.Load<Texture2D>(file)
        let position = Vector2(float32 x, float32 y)
        {
        texture = sprite
        position = position
        speed = Vector2(rng.NextDouble() |> float32,rng.NextDouble() |> float32)
        effect = moveWrapCombinator
        rect = teddyRect
        draw = teddyDraw
        }
    loadSprite file x y

let inline avatarMoveWrapEffect sprite =
    let k = Keyboard.GetState()
    let x =
        if k.IsKeyDown(Keys.Left) then -1.0f*sprite.speed.X
        else if k.IsKeyDown(Keys.Right) then 1.0f*sprite.speed.X
        else 0.0f
    let y =
        if k.IsKeyDown(Keys.Down) then 1.0f*sprite.speed.Y
        else if k.IsKeyDown(Keys.Up) then -1.0f*sprite.speed.Y
        else 0.0f
    sprite.position <- Vector2(sprite.position.X + x,sprite.position.Y + y) // Movement
    sprite.position <- Vector2((sprite.position.X + BACKBUFFER_WIDTH_F) % BACKBUFFER_WIDTH_F, (sprite.position.Y + BACKBUFFER_HEIGHT_F) % BACKBUFFER_HEIGHT_F) // Wrap
    true

type Orientation =
| LEFT = 0
| RIGHT = 1

let fish_initializer file x y (this : Game) =
    let mutable orientation = Orientation.LEFT
    let fishRect (sprite: VGDLSprite) = 
        Rectangle(int sprite.position.X, int sprite.position.Y, sprite.texture.Width/2, sprite.texture.Height)
    let fishDraw x (spriteBatch: SpriteBatch) =
        let k = Keyboard.GetState()
        if k.IsKeyDown(Keys.Left) then orientation <- Orientation.RIGHT
        else if k.IsKeyDown(Keys.Right) then orientation <- Orientation.LEFT

        let framewidth = x.texture.Width/2
        let rect = Rectangle(framewidth*(int orientation),0,framewidth,x.texture.Height)
        spriteBatch.Draw(x.texture,x.position,Nullable rect,Color.AliceBlue)

    let inline loadSprite file x y =
        let sprite = this.Content.Load<Texture2D>(file)
        let position = Vector2(float32 x, float32 y)
        {
        texture = sprite
        position = position
        speed = Vector2(4.0f,4.0f)
        effect = avatarMoveWrapEffect
        rect = fishRect
        draw = fishDraw
        }
    loadSprite file x y

let inline colliderEffect (gametime : GameTime) (ar : VGDLSprite[]) =
    [|
    let collider_ar = Array.zeroCreate ar.Length
    for i=0 to ar.Length-1 do 
        for j=i+1 to ar.Length-1 do
            let a,b = ar.[i], ar.[j]
            let intersect = Rectangle.Intersect(a.rect'(), b.rect'())
            if intersect.Size <> Point(0) then collider_ar.[i] <- true; collider_ar.[j] <- true
    for i=0 to ar.Length-1 do if collider_ar.[i] = false then yield ar.[i]
    |]

let inline spawnEffect spawn_delay =
    let inline createRand t =
        let pos = Vector2((rng.NextDouble() |> float32)*BACKBUFFER_WIDTH_F, (rng.NextDouble() |> float32)*BACKBUFFER_HEIGHT_F)
        let sp = Vector2(rng.NextDouble() |> float32, rng.NextDouble() |> float32)
        {texture=t.texture;position=pos;speed=sp;effect=t.effect;rect=t.rect;draw=t.draw}

    let mutable elapsed_time = 0

    fun (gametime : GameTime) (ar : VGDLSprite[]) ->
        [|
        elapsed_time <- elapsed_time + gametime.ElapsedGameTime.Milliseconds

        for x in ar do yield x
        if elapsed_time > spawn_delay then 
            elapsed_time <- elapsed_time - spawn_delay
            yield ar.[rng.Next(1,ar.Length-1)] |> createRand
        |]

let inline avatarDevourerEffect (score : int ref) score_string = // This one assumed the avatar is the first sprite
    fun (gametime : GameTime) (ar : VGDLSprite[]) ->
        [|
        let collider_ar = Array.zeroCreate ar.Length

        for j=1 to ar.Length-1 do // This one is for the avatar
            let a,b = ar.[0], ar.[j]
            let intersect = Rectangle.Intersect(a.rect'(), b.rect'())
            if intersect.Size <> Point(0) then collider_ar.[j] <- true; score := !score+1; score_string := sprintf "Score: %i" !score

        for i=1 to ar.Length-1 do 
            for j=i+1 to ar.Length-1 do
                let a,b = ar.[i], ar.[j]
                let intersect = Rectangle.Intersect(a.rect'(), b.rect'())
                if intersect.Size <> Point(0) then collider_ar.[i] <- true; collider_ar.[j] <- true
        
        for i=0 to ar.Length-1 do if collider_ar.[i] = false then yield ar.[i]
        |]

    
type Game1(loaders : (Game -> VGDLSprite)[], printer_loaders : (Game -> VGDLSpriteFont)[], global_effects : (GameTime -> VGDLSprite [] -> VGDLSprite [])[]) as this =
    inherit Game()

    do this.Content.RootDirectory <- @"C:\!MonoDevelop\5 3 Windows Final Code\Windows Final Code\FirstXnaGameContent\bin"

    let graphics = new GraphicsDeviceManager(this)
    do graphics.PreferredBackBufferWidth <- BACKBUFFER_WIDTH
    do graphics.PreferredBackBufferHeight <- BACKBUFFER_HEIGHT

    // Create a new SpriteBatch, which can be used to draw textures.
    let mutable spriteBatch = Unchecked.defaultof<SpriteBatch> 

    // load teddy bears and build draw rectangles
    let mutable sprites = [||]
    let mutable printers = [||]

    override x.Initialize() =
        base.Initialize()

    override x.LoadContent() =
        spriteBatch <- new SpriteBatch(this.GraphicsDevice)
        sprites <- [|for x in loaders do yield x this|]
        printers <- [|for x in printer_loaders do yield x this|]

    override x.Update(gameTime) =
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back = ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
        then x.Exit()

        sprites <- // Local Effects
            [|
            for sprite in sprites do if sprite.effect'() then yield sprite
            |]

        sprites <- Array.fold (fun state effect -> effect gameTime state) sprites global_effects // Global Effects

        base.Update(gameTime)

    override x.Draw(gameTime) =
        x.GraphicsDevice.Clear(Color.CornflowerBlue)

        // draw teddy bears
        spriteBatch.Begin()

        for sprite in sprites do sprite.draw sprite spriteBatch
        for printer in printers do printer.draw printer spriteBatch

        spriteBatch.End()

        base.Draw(gameTime)

let characters = 
    [|
    yield fish_initializer @"graphics\fish" 450 500
    for i=1 to 15 do
        yield teddy_initializer @"graphics\teddybear0" (i*50) 100 
    |] 

let score = ref 0
let score_string = ref "Score: 0"

let printers =
    [|
    font_initializer @"graphics\Arial" 200 200 score_string
    |]

let global_effects =
    [|avatarDevourerEffect score score_string;
    spawnEffect 500|]

let g = new Game1(characters,printers,global_effects)
g.Run()

