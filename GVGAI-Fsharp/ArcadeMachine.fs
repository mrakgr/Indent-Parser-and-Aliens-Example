namespace VGVAI

open System
open System.IO

type ArcadeMachine() =
    let VERBOSE = false;
    /// Reads and launches a game for a human to be played. Graphics always on.
    /// @param game_file game description file.
    /// @param level_file file with the level to be played.
    static member playOneGame(game_file, level_file, actionFile, randomSeed) =
        let agentName = "controllers.human.Agent"
        let visuals = true
        runOneGame(game_file, level_file, visuals, agentName, actionFile, randomSeed)
  
    /// Reads game description then generate level using the supplied generator.
    /// It also launches the game for a human to be played. Graphics always on. 
    /// @param gameFile			the game description file
    /// @param levelGenerator	the level generator name
    /// @param levelFile		a file to save the generated level
    static member playOneGeneratedLevel(gameFile, actionFile, levelFile, randomSeed) =
    	let agentName = "controllers.human.Agent"
        let visuals = true
    	runOneGeneratedLevel(gameFile, visuals, agentName, actionFile, levelFile, randomSeed)
    
    /// Reads and launches a game for a bot to be played. Graphics can be on or off.
    /// @param game_file game description file.
    /// @param level_file file with the level to be played.
    /// @param visuals true to show the graphics, false otherwise.
    /// @param agentName name (inc. package) where the controller is otherwise.
    /// @param actionFile filename of the file where the actions of this player, for this game, should be recorded.
    /// @param randomSeed sampleRandom seed for the sampleRandom generator.
    static member runOneGame(game_file, level_file, visuals, agentName, actionFile, randomSeed) =
        VGDLFactory.GetInstance().init() //This always first thing to do.
        VGDLRegistry.GetInstance().init()

        printfn (" ** Playing game " + game_file + ", level " + level_file + " **")

        // First, we create the game to be played..
        let toPlay = new VGDLParser().parseGame(game_file)
        toPlay.buildLevel(level_file)

        //Warm the game up.
        ArcadeMachine.warmUp(toPlay, CompetitionParameters.WARMUP_TIME)

        //Create the player.
        let player = ArcadeMachine.createPlayer(agentName, actionFile, toPlay.getObservation(), randomSeed)

        match player with
        | None ->
            //Something went wrong in the constructor, controller disqualified
            toPlay.disqualify()

            //Get the score for the result.
            toPlay.handleResult()
        | Some player ->
            //Then, play the game.
            double score = 0.0;
            if visuals then
                score = toPlay.playGame(player, randomSeed)
            else
                score = toPlay.runGame(player, randomSeed)

            //Finally, when the game is over, we need to tear the player down.
            ArcadeMachine.tearPlayerDown(player)

            score
    
    /// Generate a level for a certain described game and test it against a supplied agent
    /// @param gameFile			game description file.
    /// @param levelGenerator	level generator class path.
    /// @param levelFile		file to save the generated level in it
    static member generateOneLevel(gameFile, levelGenerator, levelFile) =
        VGDLFactory.GetInstance().init(); //This always first thing to do.
        VGDLRegistry.GetInstance().init();

        printfn (" ** Generating a level for " + gameFile + ", using level generator " + levelGenerator + " **")

        // First, we create the game to be played..
        let toPlay = new VGDLParser().parseGame(gameFile)
        let description = new GameDescription(toPlay)
        let generator = createLevelGenerator(levelGenerator, description)
        let level = getGeneratedLevel(description, toPlay, generator)

        match level with 
        | Some "" | None ->
            printfn "Empty Level Disqualified"
            toPlay.disqualify()

            //Get the score for the result.
            toPlay.handleResult()
            false
        | Some level ->
            let charMapping = generator.getLevelMapping()

            match charMapping with 
            | Some charMapping ->
                toPlay.setCharMapping(charMapping)
        
            try
                toPlay.buildstringLevel(level.Split [|'\n'|])
            with
            | :? Exception as e ->
                printfn "Undefined symbols or wrong number of avatars Disqualified "
                toPlay.disqualify();

                //Get the score for the result.
                toPlay.handleResult();
                reraise()
        
            match levelFile with
            | Some levelFile ->
                saveLevel(level, levelFile, toPlay.getCharMapping());

    
    static member runOneGeneratedLevel(gameFile, visuals, agentName, actionFile, levelFile, randomSeed) =
    	VGDLFactory.GetInstance().init() //This always first thing to do.
        VGDLRegistry.GetInstance().init()

        printfn(" ** Playing game " + gameFile + ", using generate level file " + levelFile + " **")

        // First, we create the game to be played..
        let toPlay = new VGDLParser().parseGame(gameFile)
        let level = loadGeneratedFile(toPlay, levelFile)
        let levelLines = level.Split [|'\n'|]
        
        toPlay.reset()
        toPlay.buildstringLevel(levelLines)

        //Warm the game up.
        ArcadeMachine.warmUp(toPlay, CompetitionParameters.WARMUP_TIME)
        
        //Create the player.
        let player = ArcadeMachine.createPlayer(agentName, actionFile, toPlay.getObservation(), randomSeed)

        match player with
        | None ->
            //Something went wrong in the constructor, controller disqualified
            toPlay.disqualify()

            //Get the score for the result.
            toPlay.handleResult()
        | Some player ->
            //Then, play the game.
            double score = 0.0
            if(visuals) then
                score = toPlay.playGame(player, randomSeed)
            else
                score = toPlay.runGame(player, randomSeed)

            //Finally, when the game is over, we need to tear the player down.
            ArcadeMachine.tearPlayerDown(player)

            score
    
    /// Runs a replay given a game, level and file with the actions to execute.
    /// @param game_file game description file.
    /// @param level_file file with the level to be played.
    /// @param visuals true to show the graphics, false otherwise.
    /// @param actionFile name of the file where the actions of this player, for this game, must be read from.
    (*
    static member replayGame(game_file, level_file, visuals, actionFile) =
        let agentName = "controllers.replayer.Agent";
        VGDLFactory.GetInstance().init()  //This always first thing to do.
        VGDLRegistry.GetInstance().init()

        // First, we create the game to be played..
        let toPlay = new VGDLParser().parseGame(game_file)
        toPlay.buildLevel(level_file)

        //Second, create the player. Note: null as action_file and -1 as sampleRandom seed
        // (we don't want to record anything from this execution).
        let player = ArcadeMachine.createPlayer(agentName, null, toPlay.getObservation(), -1)

        match player with
        | None ->
            //Something went wrong in the constructor, controller disqualified
            toPlay.disqualify()

            //Get the score for the result.
            toPlay.handleResult()

        // TODO: This is a more complex piece that I will need to go over more finely.
        try
            use stream_data = File.OpenRead(actionFile)
            use br = new StreamReader(stream_data)

            //First line should be the sampleRandom seed.
            let seed = Int32.Parse(br.ReadLine())
            System.out.println("Replaying game in " + game_file + ", " + level_file + " with seed " + seed);

            //The rest are the actions:
            string line = br.readLine();
            while(line != null)
            {
                Types.ACTIONS nextAction = Types.ACTIONS.fromstring(line);
                actions.add(nextAction);

                //next!
                line = br.readLine();
            }

        }catch(Exception e)
        {
            e.printStackTrace();
            System.exit(1);
        }

        //Assign the actions to the player:
        ((controllers.replayer.Agent)player).setActions(actions);

        //Then, (re-)play the game.
        double score = 0.0;
        if(visuals)
            score = toPlay.playGame(player, seed);
        else
            score = toPlay.runGame(player, seed);

        //Finally, when the game is over, we need to tear the player down. Actually in this case this might never do anything.
        ArcadeMachine.tearPlayerDown(player);

        return score;
    }
    *)

    // Reads and launches a game for a bot to be played. It specifies which levels to play and how many times.
    // Filenames for saving actions can be specified. Graphics always off.
    // @param game_file game description file.
    // @param level_files array of level file names to play.
    // @param level_times how many times each level has to be played.
    // @param actionFiles names of the files where the actions of this player, for this game, should be recorded. Accepts
    //                    null if no recording is desired. If not null, this array must contain as much string objects as
    //                    level_files.length*level_times.
    static member runGames(game_file, level_files, level_times, agentName, actionFiles) =
        VGDLFactory.GetInstance().init(); //This always first thing to do.
        VGDLRegistry.GetInstance().init();

        let recordActions = false
        match actionFiles with
        | Some actionFiles ->
            recordActions = true;
            if actionFiles.length >= level_files.length*level_times then
                    failwith "runGames (actionFiles.length<level_files.length*level_times): " +
                             "you must supply an action file for each game instance to be played, or null."

        let scores = new StatSummary()

        let toPlay = new VGDLParser().parseGame(game_file)
        let levelIdx = 0
        let rng = System.Random()

        for level_file in level_files do
            for i=0 to level_times-1 do
                printfn(" ** Playing game " + game_file + ", level " + level_file + " ("+(i+1)+"/"+level_times+") **")

                //build the level in the game.
                toPlay.buildLevel(level_file);

                let filename = if recordActions then Some actionFiles.[levelIdx*level_times + i] else None

                //Warm the game up.
                ArcadeMachine.warmUp(toPlay, CompetitionParameters.WARMUP_TIME);

                //Determine the random seed, different for each game to be played.
                let randomSeed = rng.Next()

                //Create the player.
                let player = ArcadeMachine.createPlayer(agentName, filename, toPlay.getObservation(), randomSeed);

                let score = -1

                match player with
                | None ->
                    //Something went wrong in the constructor, controller disqualified
                    toPlay.disqualify()

                    //Get the score for the result.
                    score = toPlay.handleResult()

                | Some player ->
                    //Then, play the game.
                    score = toPlay.runGame(player, randomSeed)

                scores.add(score)

                //Finally, when the game is over, we need to tear the player down.
                match player with
                | Some player ->
                    ArcadeMachine.tearPlayerDown(player);

                //reset the game.
                toPlay.reset();

        printfn(" *** Results in game " + game_file + " *** ");
        printfn(scores);
        printfn(" *********");

    /**
     * Generate multiple levels for a certain game
     * @param gameFile			The game description file path
     * @param levelGenerator	The current used level generator
     * @param levelFile			array of level files to save the generated levels
     */
    public static void generateLevels(string gameFile, string levelGenerator, string[] levelFile){
    	VGDLFactory.GetInstance().init(); //This always first thing to do.
        VGDLRegistry.GetInstance().init();

        // First, we create the game to be played..
        Game toPlay = new VGDLParser().parseGame(gameFile);
        GameDescription description = new GameDescription(toPlay);
        AbstractLevelGenerator generator = createLevelGenerator(levelGenerator, description);
        HashMap<Character, ArrayList<string>> originalMapping = toPlay.getCharMapping();
        
    	for(int i=0;i<levelFile.length;i++){
    		System.out.println(" ** Generating a level " + (i + 1) +  " for " + gameFile + ", using level generator " + levelGenerator + " **");
    		toPlay.reset();
    		description.reset(toPlay);
    		
    		string level = getGeneratedLevel(description, toPlay, generator);
            if(level == "" || level == null){
            	toPlay.disqualify();

                //Get the score for the result.
                toPlay.handleResult();
            }
            
            HashMap<Character, ArrayList<string>> charMapping = generator.getLevelMapping();
            if(charMapping != null){
            	toPlay.setCharMapping(charMapping);
            }
            try{
            	toPlay.buildstringLevel(level.split("\n"));
            }
            catch(Exception e){
            	System.out.println("Undefined symbols or wrong number of avatars Disqualified ");
            	toPlay.disqualify();

                //Get the score for the result.
                toPlay.handleResult();
            }
            if(levelFile != null){
            	saveLevel(level, levelFile[i], toPlay.getCharMapping());
            }
            toPlay.setCharMapping(originalMapping);
    	}
    }
    
    /**
     * play a couple of generated levels for a certain game
     * @param gameFile
     * @param actionFile
     * @param levelFile
     * @param randomSeed
     */
    public static void playGeneratedLevels(string gameFile, string[] actionFile, string[] levelFile){
    	string agentName = "controllers.human.Agent";
    	
    	VGDLFactory.GetInstance().init(); //This always first thing to do.
        VGDLRegistry.GetInstance().init();

        boolean recordActions = false;
        if(actionFile != null)
        {
            recordActions = true;
            assert actionFile.length >= levelFile.length :
                    "runGames (actionFiles.length<level_files.length*level_times): " +
                    "you must supply an action file for each game instance to be played, or null.";
        }

        StatSummary scores = new StatSummary();

        Game toPlay = new VGDLParser().parseGame(gameFile);
        int levelIdx = 0;
        for(string file : levelFile){
        	System.out.println(" ** Playing game " + gameFile + ", level " + file +" **");
        	
            //build the level in the game.
        	string level = loadGeneratedFile(toPlay, file);
            string[] levelLines = level.split("\n");
            
            toPlay.buildstringLevel(levelLines);

            string filename = recordActions ? actionFile[levelIdx] : null;

            //Determine the random seed, different for each game to be played.
            int randomSeed = new Random().nextInt();

            //Create the player.
            AbstractPlayer player = ArcadeMachine.createPlayer(agentName, filename, toPlay.getObservation(), randomSeed);

            double score = -1;
            if(player == null)
            {
            	//Something went wrong in the constructor, controller disqualified
                toPlay.disqualify();
                
                //Get the score for the result.
                score = toPlay.handleResult();

            }else{

            	//Then, play the game.
                score = toPlay.playGame(player, randomSeed);
            }

            scores.add(score);

            //Finally, when the game is over, we need to tear the player down.
            if(player != null) ArcadeMachine.tearPlayerDown(player);
            
            //reset the game.
            toPlay.reset();
                
            levelIdx += 1;
        }

        System.out.println(" *** Results in game " + gameFile + " *** ");
        System.out.println(scores);
        System.out.println(" *********");
    }
    
    /**
     * Creates a player given its name with package. This class calls the constructor of the agent
     * and initializes the action recording procedure.
     * @param playerName name of the agent to create. It must be of the type "<agentPackage>.Agent".
     * @param actionFile filename of the file where the actions of this player, for this game, should be recorded.
     * @param so Initial state of the game to be played by the agent.
     * @param randomSeed Seed for the sampleRandom generator of the game to be played.
     * @return the player, created and initialized, ready to start playing the game.
     */
    private static AbstractPlayer createPlayer(string playerName, string actionFile, StateObservation so, int randomSeed)
    {
        AbstractPlayer player = null;

        try{
            //create the controller.
            player = createController(playerName, so);
            if(player != null)
                player.setup(actionFile, randomSeed);

        }catch (Exception e)
        {
            //This probably happens because controller took too much time to be created.
            e.printStackTrace();
            System.exit(1);
        }

        return player;
    }

    /**
     * Creates and initializes a new controller with the given name. Takes into account the initialization time,
     * calling the appropriate constructor with the state observation and time due parameters.
     * @param playerName Name of the controller to instantiate.
     * @param so Initial state of the game to be played by the agent.
     * @return the player if it could be created, null otherwise.
     */
    protected static AbstractPlayer createController(string playerName, StateObservation so) throws RuntimeException
    {
        AbstractPlayer player = null;
        try
        {
            //Get the class and the constructor with arguments (StateObservation, long).
            Class<? extends AbstractPlayer> controllerClass = Class.forName(playerName).asSubclass(AbstractPlayer.class);
            Class[] gameArgClass = new Class[]{StateObservation.class, ElapsedCpuTimer.class};
            Constructor controllerArgsConstructor = controllerClass.getConstructor(gameArgClass);

            //Determine the time due for the controller creation.
            ElapsedCpuTimer ect = new ElapsedCpuTimer(CompetitionParameters.TIMER_TYPE);
            ect.setMaxTimeMillis(CompetitionParameters.INITIALIZATION_TIME);

            //Call the constructor with the appropriate parameters.
            Object[] constructorArgs = new Object[] {so, ect.copy()};
            player = (AbstractPlayer) controllerArgsConstructor.newInstance(constructorArgs);

            //Check if we returned on time, and act in consequence.
            long timeTaken = ect.elapsedMillis();
            if(ect.exceededMaxTime())
            {
                long exceeded =  - ect.remainingTimeMillis();
                System.out.println("Controller initialization time out (" + exceeded + ").");

                return null;
            }
            else
            {
                System.out.println("Controller initialization time: " + timeTaken + " ms.");
            }

        //This code can throw many exceptions (no time related):

        }catch(NoSuchMethodException e)
        {
            e.printStackTrace();
            System.err.println("Constructor " + playerName + "(StateObservation,long) not found in controller class:");
            System.exit(1);

        }catch(ClassNotFoundException e)
        {
            System.err.println("Class " + playerName + " not found for the controller:");
            e.printStackTrace();
            System.exit(1);

        }catch(InstantiationException e)
        {
            System.err.println("Exception instantiating " + playerName + ":");
            e.printStackTrace();
            System.exit(1);

        }catch(IllegalAccessException e)
        {
            System.err.println("Illegal access exception when instantiating " + playerName + ":");
            e.printStackTrace();
            System.exit(1);
        }catch(InvocationTargetException e)
        {
            System.err.println("Exception calling the constructor " + playerName + "(StateObservation,long):");
            e.printStackTrace();
            System.exit(1);
        }

        return player;
    }
    
    /**
     * Generate AbstractLevelGenerator object to generate levels 
     * for the game using the supplied class path.
     * @param levelGenerator	class path for the supplied level generator
     * @param gd				abstract object describes the game
     * @return					AbstractLevelGenerator object.	
     */
    protected static AbstractLevelGenerator createLevelGenerator(string levelGenerator, GameDescription gd) throws RuntimeException
    {
        AbstractLevelGenerator generator = null;
        try
        {
            //Get the class and the constructor with arguments (StateObservation, long).
            Class<? extends AbstractLevelGenerator> controllerClass = Class.forName(levelGenerator).asSubclass(AbstractLevelGenerator.class);
            Class[] gameArgClass = new Class[]{GameDescription.class, ElapsedCpuTimer.class};
            Constructor controllerArgsConstructor = controllerClass.getConstructor(gameArgClass);

            //Determine the time due for the controller creation.
            ElapsedCpuTimer ect = new ElapsedCpuTimer(CompetitionParameters.TIMER_TYPE);
            ect.setMaxTimeMillis(CompetitionParameters.LEVEL_INITIALIZATION_TIME);

            //Call the constructor with the appropriate parameters.
            Object[] constructorArgs = new Object[] {gd, ect.copy()};
            generator = (AbstractLevelGenerator) controllerArgsConstructor.newInstance(constructorArgs);

            //Check if we returned on time, and act in consequence.
            long timeTaken = ect.elapsedMillis();
            if(ect.exceededMaxTime())
            {
                long exceeded =  - ect.remainingTimeMillis();
                System.out.println("Generator initialization time out (" + exceeded + ").");

                return null;
            }
            else
            {
                System.out.println("Generator initialization time: " + timeTaken + " ms.");
            }

        //This code can throw many exceptions (no time related):

        }catch(NoSuchMethodException e)
        {
            e.printStackTrace();
            System.err.println("Constructor " + levelGenerator + "(StateObservation,long) not found in controller class:");
            System.exit(1);

        }catch(ClassNotFoundException e)
        {
            System.err.println("Class " + levelGenerator + " not found for the controller:");
            e.printStackTrace();
            System.exit(1);

        }catch(InstantiationException e)
        {
            System.err.println("Exception instantiating " + levelGenerator + ":");
            e.printStackTrace();
            System.exit(1);

        }catch(IllegalAccessException e)
        {
            System.err.println("Illegal access exception when instantiating " + levelGenerator + ":");
            e.printStackTrace();
            System.exit(1);
        }catch(InvocationTargetException e)
        {
            System.err.println("Exception calling the constructor " + levelGenerator + "(StateObservation,long):");
            e.printStackTrace();
            System.exit(1);
        }

        return generator;
    }


    /**
     * Generate a level for the described game using the supplied level generator.
     * @param gd		Abstract description of game elements
     * @param game		Current game object.
     * @param generator Current level generator.
     * @return			string of symbols contains the generated level. Same as Level Description File string.
     */
    private static string getGeneratedLevel(GameDescription gd, Game game, AbstractLevelGenerator generator){
    	ElapsedCpuTimer ect = new ElapsedCpuTimer(CompetitionParameters.TIMER_TYPE);
        ect.setMaxTimeMillis(CompetitionParameters.LEVEL_ACTION_TIME);

        string level = generator.generateLevel(gd, ect.copy());

        if(ect.exceededMaxTime())
        {
            long exceeded =  - ect.remainingTimeMillis();

            if(ect.elapsedMillis() > CompetitionParameters.LEVEL_ACTION_TIME_DISQ)
            {
                //The agent took too long to replay. The game is over and the agent is disqualified
                System.out.println("Too long: " + "(exceeding "+(exceeded)+"ms): controller disqualified.");
                level = "";
            }else{
                System.out.println("Overspent: " + "(exceeding "+(exceeded)+"ms): applying Empty Level.");
                level = " ";
            }
        }
        
        return level;
    }
    
    /**
     * Saves a level string to a file
     * @param level		current level to save
     * @param levelFile	saved file
     */
    private static void saveLevel(string level, string levelFile, HashMap<Character, ArrayList<string>> charMapping){
    	try{
    		if(levelFile != null){
    			BufferedWriter writer = new BufferedWriter(new FileWriter(levelFile));
    			writer.write("LevelMapping");
    			writer.newLine();
    			for(Entry<Character, ArrayList<string>> e:charMapping.entrySet()){
    				writer.write("    " + e.getKey() + " > ");
    				for(string s:e.getValue()){
    					writer.write(s + " ");
    				}
    				writer.newLine();
    			}
    			writer.newLine();
    			writer.write("LevelDescription");
    			writer.newLine();
    			writer.write(level);
    			writer.close();
    		}
    	}
    	catch(IOException e){
    		e.printStackTrace();
    	}
    }
    
    /**
     * Load a generated level file
     * @param currentGame	Current Game object to se the Level Mapping
     * @param levelFile		The generated level file path
     * @return				Level string to be loaded
     */
    public static string loadGeneratedFile(Game currentGame, string levelFile){
    	HashMap<Character, ArrayList<string>> levelMapping = new HashMap<Character, ArrayList<string>>();
    	string level = "";
    	int mode = 0;
    	string[] lines = new IO().readFile(levelFile);
    	for(string line:lines){
    		if(line.equals("LevelMapping")){
    			mode = 0;
    		}
    		else if(line.equals("LevelDescription")){
    			mode = 1;
    		}
    		else{
    			switch(mode){
    			case 0:
    				if(line.trim().length() == 0){
        				continue;
        			}
    				string[] sides = line.split(">");
    				ArrayList<string> sprites = new ArrayList<string>();
    				for(string sprite:sides[1].trim().split(" ")){
    					if(sprite.trim().length() == 0){
    						continue;
    					}
    					else{
    						sprites.add(sprite.trim());
    					}
    				}
    				levelMapping.put(sides[0].trim().charAt(0), sprites);
    				break;
    			case 1:
    				level += line + "\n";
    				break;
    			}
    		}
    	}
    	currentGame.setCharMapping(levelMapping);
    	return level;
    }
    
    /**
     * This methods takes the game and warms it up. This allows Java to finish the runtime compilation
     * process and optimize the code before the proper game starts.
     * @param toPlay game to be warmed up.
     * @param howLong for how long the warming up process must last (in milliseconds).
     */
    public static void warmUp(Game toPlay, long howLong)
    {
        StateObservation stateObs = toPlay.getObservation();
        ElapsedCpuTimer ect = new ElapsedCpuTimer(CompetitionParameters.TIMER_TYPE);
        ect.setMaxTimeMillis(howLong);

        int playoutLength = 10;
        ArrayList<Types.ACTIONS> actions = stateObs.getAvailableActions();
        int copyStats = 0;
        int advStats = 0;

        StatSummary ss1 = new StatSummary();
        StatSummary ss2 = new StatSummary();


        boolean finish = ect.exceededMaxTime() || (copyStats>CompetitionParameters.WARMUP_CP && advStats>CompetitionParameters.WARMUP_ADV);

        //while(!ect.exceededMaxTime())
        while(!finish)
        {
            for (Types.ACTIONS action : actions)
            {
                StateObservation stCopy = stateObs.copy();
                ElapsedCpuTimer ectAdv = new ElapsedCpuTimer();
                stCopy.advance(action);
                copyStats++;
                advStats++;

                if( ect.remainingTimeMillis() < CompetitionParameters.WARMUP_TIME*0.5)
                {
                    ss1.add(ectAdv.elapsedNanos());
                }

                for (int i = 0; i < playoutLength; i++) {

                    int index = new Random().nextInt(actions.size());
                    Types.ACTIONS actionPO = actions.get(index);

                    ectAdv = new ElapsedCpuTimer();
                    stCopy.advance(actionPO);
                    advStats++;

                    if( ect.remainingTimeMillis() < CompetitionParameters.WARMUP_TIME*0.5)
                    {
                        ss2.add(ectAdv.elapsedNanos());
                    }
                }
            }

            finish = ect.exceededMaxTime() || (copyStats>CompetitionParameters.WARMUP_CP && advStats>CompetitionParameters.WARMUP_ADV);

            //if(VERBOSE)
            //System.out.println("[WARM-UP] Remaining time: " + ect.remainingTimeMillis() +
            //        " ms, copy() calls: " + copyStats + ", advance() calls: " + advStats);
        }

        if(VERBOSE)
        {
            System.out.println("[WARM-UP] Finished, copy() calls: " + copyStats + ", advance() calls: " + advStats + ", time (s): " + ect.elapsedSeconds());
            //System.out.println(ss1);
            //System.out.println(ss2);
        }


        //Reset input to delete warm-up effects.
        Game.ki.reset();
    }


    /**
     * Tears the player down. This initiates the saving of actions to file.
     * It should be called when the game played is over.
     * @param player player to be closed.
     */
    private static void tearPlayerDown(AbstractPlayer player)
    {
        player.teardown();
    }


}
