#r "../packages/FParsec.1.0.2/lib/net40-client/FParsecCS.dll"
#r "../packages/FParsec.1.0.2/lib/net40-client/FParsec.dll"
open FParsec

let aliens_text = """
wwwwwwwwwwwwwwwwwwwwwwwwwwwwwwww
w                              w
w1                             w
w000                           w
w000                           w
w3                             w
w                              w
w                              w
w                              w
w    000      000000     000   w
w   00000    00000000   00000  w
w   0   0    00    00   00000  w
w                A             w
wwwwwwwwwwwwwwwwwwwwwwwwwwwwwwww
"""

let aliens_spec = """
BasicGame
    SpriteSet
        base    > Immovable    color=WHITE img=base
        avatar  > FlakAvatar   stype=sam 
        missile > Missile
            sam  > orientation=UP    color=BLUE singleton=True img=spaceship
            bomb > orientation=DOWN  color=RED  speed=0.5 img=bomb
        alien   > Bomber       stype=bomb   prob=0.01  cooldown=3 speed=0.8 img=alien
        portal  >
        	portalSlow  > SpawnPoint   stype=alien  cooldown=16   total=20 img=portal
        	portalFast  > SpawnPoint   stype=alien  cooldown=12   total=20 img=portal
    
    LevelMapping
        0 > base
        1 > portalSlow
        2 > portalFast

    TerminationSet
        SpriteCounter      stype=avatar               limit=0 win=False
        MultiSpriteCounter stype1=portal stype2=alien limit=0 win=True
        
    InteractionSet
        avatar  EOS  > stepBack
        alien   EOS  > turnAround        
        missile EOS  > killSprite
        missile base > killSprite
        base bomb > killSprite
        base sam > killSprite scoreChange=1
        base   alien > killSprite
        avatar alien > killSprite scoreChange=-1
        avatar bomb  > killSprite scoreChange=-1
        alien  sam   > killSprite scoreChange=2
"""

let test_block = """BasicGame
    t
        r
            q
        w
    l
"""

let str s = pstring s
let ws = spaces
let str_ws s = pstring s .>> ws
let float_ws : Parser<_,unit> = pfloat .>> ws

//let game_class = ws >>. str_ws "BasicGame"
let indent_test_block = ws >>. skipRestOfLine true

run indent_test_block test_block

