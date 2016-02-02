//Available controllers:
let sampleRandomController = "controllers.sampleRandom.Agent"
let sampleOneStepController = "controllers.sampleonesteplookahead.Agent"
let sampleMCTSController = "controllers.sampleMCTS.Agent"
let sampleFlatMCTSController = "controllers.sampleFlatMCTS.Agent"
let sampleOLMCTSController = "controllers.sampleOLMCTS.Agent"
let sampleGAController = "controllers.sampleGA.Agent"
let tester = "controllers.Tester.Agent"

type PhysicsType =
    | PHYSICS_NONE = -1
    | PHYSICS_GRID = 0
    | PHYSICS_CONT = 1
    | PHYSICS_NON_FRICTION = 2
    | PHYSICS_GRAVITY = 3

let t = PhysicsType.PHYSICS_NON_FRICTION

match t with
| PhysicsType.PHYSICS_NONE -> printfn "None"
| PhysicsType.PHYSICS_GRID -> printfn "None"
| PhysicsType.PHYSICS_CONT -> printfn "None"
| PhysicsType.PHYSICS_NON_FRICTION -> printfn "None"
| PhysicsType.PHYSICS_GRAVITY -> printfn "None"
| _ -> printfn "Invalid"


//Available Generators
let randomLevelGenerator = "levelGenerators.randomLevelGenerator.LevelGenerator"
let geneticGenerator = "levelGenerators.geneticLevelGenerator.LevelGenerator"
let constructiveLevelGenerator = "levelGenerators.constructiveLevelGenerator.LevelGenerator"
        
//Available games:
let gamesPath = "examples/gridphysics/"
let generateLevelPath = "examples/generatedLevels/"

//My Set
let games = [|"windy_gridworld"|]

//Training Set 1 (2015; CIG 2014)
//games = [|"aliens"; "boulderdash"; "butterflies"; "chase"; "frogs";
//        "missilecommand"; "portals"; "sokoban"; "survivezombies"; "zelda"|]

//Training Set 2 (2015; Validation CIG 2014)
//games = [|"camelRace"; "digdug"; "firestorms"; "infection"; "firecaster";
//      "overload"; "pacman"; "seaquest"; "whackamole"; "eggomania"|]

//Training Set 3 (2015)
//games = [|"bait"; "boloadventures"; "brainman"; "chipschallenge";  "modality";
//                              "painter"; "realportals"; "realsokoban"; "thecitadel"; "zenpuzzle"|]

//Training Set 4 (Validation GECCO 2015; Test CIG 2014)
//games = [|"roguelike"; "surround"; "catapults"; "plants"; "plaqueattack";
//        "jaws"; "labyrinth"; "boulderchase"; "escape"; "lemmings"|]


//Training Set 5 (Validation CIG 2015; Test GECCO 2015)
//games = [| "solarfox"; "defender"; "enemycitadel"; "crossfire"; "lasers";
//                               "sheriff"; "chopper"; "superman"; "waitforbreakfast"; "cakybaky"|]

//Training Set 6 (Validation CEEC 2015)
//games = [|"lasers2"; "hungrybirds" ;"cookmepasta"; "factorymanager"; "raceBet2";
//        "intersection"; "blacksmoke"; "iceandfire"; "gymkhana"; "tercio"|]

open System

let rng = Random()
//Other settings
let visuals = true
let recordActionsFile = None //where to record the actions executed. null if not to save.
let seed = rng.Next()

//Game and level to play
let gameIdx = 0
let levelIdx = 0 //level names from 0 to 4 (game_lvlN.txt).
let game = gamesPath + games.[gameIdx] + ".txt";
let level1 = gamesPath + games.[gameIdx] + "_lvl" + string levelIdx + ".txt"
        
let recordLevelFile = generateLevelPath + "geneticLevelGenerator/" + games.[gameIdx] + "_lvl0.txt"

// 1. This starts a game, in a level, played by a human.

ArcadeMachine.playOneGame(game, level1, recordActionsFile, seed)

// 2. This plays a game in a level by the controller.
//ArcadeMachine.runOneGame(game, level1, visuals, sampleMCTSController, recordActionsFile, seed);
//ArcadeMachine.runOneGame(game, level1, visuals, sampleOneStepController, recordActionsFile, seed);
//ArcadeMachine.runOneGame(game, level1, visuals, tester, recordActionsFile, seed);

// 3. This replays a game from an action file previously recorded
//String readActionsFile = "actionsFile_aliens_lvl0.txt";  //This example is for
//ArcadeMachine.replayGame(game, level1, visuals, readActionsFile);

// 4. This plays a single game, in N levels, M times :
//String level2 = gamesPath + games[gameIdx] + "_lvl" + 1 +".txt";
//int M = 3;
//for(int i=0; i<games.length; i++){
//	game = gamesPath + games[i] + ".txt";
//	level1 = gamesPath + games[i] + "_lvl" + levelIdx +".txt";
//	ArcadeMachine.runGames(game, new String[]{level1}, 5, evolutionStrategies, null);
//}
        
//5. This starts a game, in a generated level created by a specific level generator
//if(ArcadeMachine.generateOneLevel(game, geneticGenerator, recordLevelFile)){
//	ArcadeMachine.playOneGeneratedLevel(game, recordActionsFile, recordLevelFile, seed);
//}
        
//6. This plays N games, in the first L levels, M times each. Actions to file optional (set saveActions to true).
/*int N = 10, L = 1, M = 5;
boolean saveActions = false;
String[] levels = new String[L];
String[] actionFiles = new String[L*M];
for(int i = 0; i < N; ++i)
{
    int actionIdx = 0;
    game = gamesPath + games[i] + ".txt";
    for(int j = 0; j < L; ++j){
        levels[j] = gamesPath + games[i] + "_lvl" + j +".txt";
        if(saveActions) for(int k = 0; k < M; ++k)
            actionFiles[actionIdx++] = "actions_game_" + i + "_level_" + j + "_" + k + ".txt";
    }
    ArcadeMachine.runGames(game, levels, M, kNearestNeighbour, saveActions? actionFiles:null);
}*/
