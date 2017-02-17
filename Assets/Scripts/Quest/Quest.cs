﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

// Class to manage all data for the current quest
public class Quest
{
    // QuestData
    public QuestData qd;

    // components on the board (tiles, tokens, doors)
    public Dictionary<string, BoardComponent> boardItems;

    // A list of flags that have been set during the quest
    public HashSet<string> flags;

    // A dictionary of heros that have been selected in events
    public Dictionary<string, List<Quest.Hero>> heroSelection;

    // Event manager handles the events
    public EventManager eManager;

    // A list of events to be triggered at the end of a later round
    public List<QuestData.Event.DelayedEvent> delayedEvents;

    // List of heros and their status
    public List<Hero> heroes;

    // List of active monsters and their status
    public List<Monster> monsters;

    // Stack of saved game state for undo
    public Stack<string> undo;

    // game state variables
    public int round = 1;
    public int morale = 0;
    public float threat = 0;
    public bool horrorPhase = false;

    // This is true once heros are selected and the quest is started
    public bool heroesSelected = false;

    // Have the perils been triggered?
    public bool minorPeril = false;
    public bool majorPeril = false;
    public bool deadlyPeril = false;

    // Reference back to the game object
    public Game game;

    // Construct a new quest from quest data
    public Quest(QuestLoader.Quest q)
    {
        game = Game.Get();

        // This happens anyway but we need it to be here before the following code is executed
        game.quest = this;

        // Static data from the quest file
        qd = new QuestData(q);

        // Initialise data
        boardItems = new Dictionary<string, BoardComponent>();
        flags = new HashSet<string>();
        monsters = new List<Monster>();
        heroSelection = new Dictionary<string, List<Quest.Hero>>();
        eManager = new EventManager();
        delayedEvents = new List<QuestData.Event.DelayedEvent>();
        undo = new Stack<string>();

        // Populate null hero list, these can then be selected as hero types
        heroes = new List<Hero>();
        for (int i = 1; i <= game.gameType.MaxHeroes(); i++)
        {
            heroes.Add(new Hero(null, i));
        }

        // Set quest flags for selected expansions
        Dictionary<string, string> packs = game.config.data.Get(game.gameType.TypeName() + "Packs");
        if (packs != null)
        {
            foreach (KeyValuePair<string, string> kv in packs)
            {
                flags.Add("#" + kv.Key);
            }
        }
    }

    // Construct a quest from save data string
    // Used for undo
    public Quest(string save)
    {
        LoadQuest(IniRead.ReadFromString(save));
    }

    // Construct a quest from save iniData
    public Quest(IniData saveData)
    {
        LoadQuest(saveData);
    }

    // Read save data
    public void LoadQuest(IniData saveData)
    {
        game = Game.Get();

        // This happens anyway but we need it to be here before the following code is executed (also needed for loading saves)
        game.quest = this;

        // Get state
        int.TryParse(saveData.Get("Quest", "round"), out round);
        int.TryParse(saveData.Get("Quest", "morale"), out morale);
        bool.TryParse(saveData.Get("Quest", "heroesSelected"), out heroesSelected);
        bool.TryParse(saveData.Get("Quest", "horror"), out horrorPhase);
        bool.TryParse(saveData.Get("Quest", "minorPeril"), out minorPeril);
        bool.TryParse(saveData.Get("Quest", "majorPeril"), out majorPeril);
        bool.TryParse(saveData.Get("Quest", "deadlyPeril"), out deadlyPeril);

        // Populate DelayedEvents
        delayedEvents = new List<QuestData.Event.DelayedEvent>();
        string[] saveDelayed = saveData.Get("Quest", "DelayedEvents").Split(" ".ToCharArray(), System.StringSplitOptions.RemoveEmptyEntries);
        foreach (string de in saveDelayed)
        {
            delayedEvents.Add(new QuestData.Event.DelayedEvent(de));
        }

        // Set static quest data
        string questPath = saveData.Get("Quest", "path");
        qd = new QuestData(questPath);

        // Clear all tokens
        game.tokenBoard.Clear();
        // Clean up everything marked as 'board'
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("board"))
            Object.Destroy(go);

        // Repopulate items on the baord
        boardItems = new Dictionary<string, BoardComponent>();
        Dictionary<string, string> saveBoard = saveData.Get("Board");
        foreach (KeyValuePair<string, string> kv in saveBoard)
        {
            if (kv.Key.IndexOf("Door") == 0)
            {
                boardItems.Add(kv.Key, new Door(qd.components[kv.Key] as QuestData.Door, game));
            }
            if (kv.Key.IndexOf("Token") == 0)
            {
                boardItems.Add(kv.Key, new Token(qd.components[kv.Key] as QuestData.Token, game));
            }
            if (kv.Key.IndexOf("Tile") == 0)
            {
                boardItems.Add(kv.Key, new Tile(qd.components[kv.Key] as QuestData.Tile, game));
            }
        }

        // Set flags
        flags = new HashSet<string>();
        Dictionary<string, string> saveFlags = saveData.Get("Flags");
        foreach (KeyValuePair<string, string> kv in saveFlags)
        {
            flags.Add(kv.Key);
        }

        // Restart event EventManager
        eManager = new EventManager();

        // Clean undo stack (we don't save undo stack)
        // When performing undo this is replaced later
        undo = new Stack<string>();

        // Fetch hero state
        heroes = new List<Hero>();
        monsters = new List<Monster>();
        foreach (KeyValuePair<string, Dictionary<string, string>> kv in saveData.data)
        {
            if (kv.Key.IndexOf("Hero") == 0 && kv.Key.IndexOf("HeroSelection") != 0)
            {
                heroes.Add(new Hero(kv.Value));
            }
        }

        // Monsters must be after heros because some activations refer to heros
        foreach (KeyValuePair<string, Dictionary<string, string>> kv in saveData.data)
        {
            if (kv.Key.IndexOf("Monster") == 0)
            {
                monsters.Add(new Monster(kv.Value));
            }
        }

        // Restore hero selections
        heroSelection = new Dictionary<string, List<Hero>>();
        Dictionary<string, string> saveSelection = saveData.Get("HeroSelection");
        foreach (KeyValuePair<string, string> kv in saveSelection)
        {
            // List of selected heroes
            string[] selectHeroes = kv.Value.Split(" ".ToCharArray(), System.StringSplitOptions.RemoveEmptyEntries);
            List<Hero> heroList = new List<Hero>();

            foreach (string s in selectHeroes)
            {
                foreach (Hero h in heroes)
                {
                    // Match hero id
                    int id;
                    int.TryParse(s, out id);
                    if (id == h.id)
                    {
                        heroList.Add(h);
                    }
                }
            }
            // Add this selection
            heroSelection.Add(kv.Key, heroList);
        }
        // Update the screen
        game.monsterCanvas.UpdateList();
        game.heroCanvas.UpdateStatus();
    }

    // Save to the undo stack
    // This is called on user actions (such as defeated monsters, heros activated)
    public void Save()
    {
        undo.Push(ToString());
    }

    // Load from the undo stack
    public void Undo()
    {
        // Nothing to undo
        if (undo.Count == 0) return;
        // Load the old state.  This will also set game.quest
        Quest oldQuest = new Quest(undo.Pop());
        // Transfer the undo stack to the loaded state
        oldQuest.undo = undo;
    }

    // This function adjusts morale.  We don't write directly so that NoMorale can be triggered
    public void AdjustMorale(int m)
    {
        Game game = Game.Get();
        morale += m;
        // Test for no morale ending
        if (morale < 0)
        {
            morale = 0;
            game.moraleDisplay.Update();
            eManager.EventTriggerType("NoMorale");
        }
        game.moraleDisplay.Update();
    }

    // Function to return a hero at random
    public Hero GetRandomHero()
    {
        // We have to create a list with the null heroes trimmed
        List<Hero> hList = new List<Hero>();
        foreach (Hero h in heroes)
        {
            if (h.heroData != null)
            {
                hList.Add(h);
            }
        }
        return hList[Random.Range(0, hList.Count)];
    }

    // Add a list of scomponents (token, tile, etc)
    public void Add(string[] names)
    {
        foreach (string s in names)
        {
            Add(s);
        }
    }

    // Add a component to the board
    public void Add(string name)
    {
        // Check that the component is valid
        if (!game.quest.qd.components.ContainsKey(name))
        {
            Debug.Log("Error: Unable to create missing quest component: " + name);
            Application.Quit();
        }
        // Add to active list
        QuestData.QuestComponent qc = game.quest.qd.components[name];

        if (boardItems.ContainsKey(name)) return;

        // Add to board
        if (qc is QuestData.Tile)
        {
            boardItems.Add(name, new Tile((QuestData.Tile)qc, game));
        }
        if (qc is QuestData.Door)
        {
            boardItems.Add(name, new Door((QuestData.Door)qc, game));
        }
        if (qc is QuestData.Token)
        {
            boardItems.Add(name, new Token((QuestData.Token)qc, game));
        }
    }

    // Remove a list of active components
    public void Remove(string[] names)
    {
        foreach (string s in names)
        {
            Remove(s);
        }
    }

    // Remove an activate component
    public void Remove(string name)
    {
        if (!boardItems.ContainsKey(name)) return;

        boardItems[name].Remove();
        boardItems.Remove(name);
    }

    // Remove all active components
    public void RemoveAll()
    {
        foreach (KeyValuePair<string, BoardComponent> kv in boardItems)
        {
            kv.Value.Remove();
        }

        boardItems.Clear();
    }

    // Change the transparency of a component on the board
    public void ChangeAlpha(string name, float alpha)
    {
        // Check is component is active
        if (!boardItems.ContainsKey(name)) return;
        boardItems[name].SetVisible(alpha);
    }

    // Change the transparency of all baord components
    public void ChangeAlphaAll(float alpha)
    {
        foreach (KeyValuePair<string, BoardComponent> kv in boardItems)
        {
            kv.Value.SetVisible(alpha);
        }
    }

    // Save the quest state to a string for save games and undo
    override public string ToString()
    {
        string nl = System.Environment.NewLine;
        // General quest state block
        string r = "[Quest]" + nl;

        r += "path=" + qd.questPath + nl;
        r += "round=" + round+ nl;
        r += "morale=" + morale + nl;
        r += "threat=" + threat + nl;
        r += "horror=" + horrorPhase + nl;
        r += "heroesSelected=" + heroesSelected + nl;
        r += "minorPeril=" + minorPeril + nl;
        r += "majorPeril=" + majorPeril + nl;
        r += "deadlyPeril=" + deadlyPeril + nl;
        r += "DelayedEvents=";

        // Delayed events have a delay and a name ':' separated
        foreach (QuestData.Event.DelayedEvent de in delayedEvents)
        {
            r += de.delay + ":" + de.eventName + " ";
        }
        r += nl;

        r += "[Packs]" + nl;
        foreach (string pack in game.cd.GetEnabledPackIDs())
        {
            r += pack + nl;
        }

        r += "[Board]" + nl;
        foreach (KeyValuePair<string, BoardComponent> kv in boardItems)
        {
            r += kv.Key + nl;
        }

        r += "[Flags]" + nl;
        foreach (string s in flags)
        {
            r += s + nl;
        }

        // Hero selection is a list of selections, each with space separated hero lists
        r += "[HeroSelection]" + nl;
        foreach (KeyValuePair<string, List<Quest.Hero>> kv in heroSelection)
        {
            r += kv.Key + "=";
            foreach (Quest.Hero h in kv.Value)
            {
                r += h.id + " ";
            }
            r = r.Substring(0, r.Length - 1) + nl;
        }

        // Save hero state
        foreach (Hero h in heroes)
        {
            r += h.ToString();
        }

        // Save monster state
        foreach (Monster m in monsters)
        {
            r += m.ToString();
        }

        return r;
    }

    // Class for Tile components (use TileSide content data)
    public class Tile : BoardComponent
    {
        // This is the quest information
        public QuestData.Tile qTile;
        // This is the component information
        public TileSideData cTile;

        // Construct with data from the quest, pass Game for speed
        public Tile(QuestData.Tile questTile, Game gameObject) : base(gameObject)
        {
            qTile = questTile;

            // Search for tile in content
            if (game.cd.tileSides.ContainsKey(qTile.tileSideName))
            {
                cTile = game.cd.tileSides[qTile.tileSideName];
            }
            else if (game.cd.tileSides.ContainsKey("TileSide" + qTile.tileSideName))
            {
                cTile = game.cd.tileSides["TileSide" + qTile.tileSideName];
            }
            else
            {
                // Fatal if not found
                Debug.Log("Error: Failed to located TileSide: " + qTile.tileSideName + " in quest component: " + qTile.name);
                Application.Quit();
            }

            // Attempt to load image
            Texture2D newTex = ContentData.FileToTexture(game.cd.tileSides[qTile.tileSideName].image);
            if (newTex == null)
            {
                // Fatal if missing
                Debug.Log("Error: cannot open image file for TileSide: " + game.cd.tileSides[qTile.tileSideName].image);
                Application.Quit();
            }

            // Create a unity object for the tile
            unityObject = new GameObject("Object" + qTile.name);
            unityObject.tag = "board";
            unityObject.transform.parent = game.boardCanvas.transform;

            // Add image to object
            image = unityObject.AddComponent<UnityEngine.UI.Image>();
            // Create sprite from texture
            Sprite tileSprite = Sprite.Create(newTex, new Rect(0, 0, newTex.width, newTex.height), Vector2.zero, 1);
            // Set image sprite
            image.sprite = tileSprite;
            // Move to get the top left square corner at 0,0
            float vPPS = game.cd.tileSides[qTile.tileSideName].pxPerSquare;
            float hPPS = vPPS;
            // manual aspect control
            // We need this for the 3x2 MoM tiles because they don't have square pixels!!
            if (game.cd.tileSides[qTile.tileSideName].aspect != 0)
            {
                hPPS = (vPPS * newTex.width / newTex.height) / game.cd.tileSides[qTile.tileSideName].aspect;
            }

            // Perform alignment move
            unityObject.transform.Translate(Vector3.right * ((newTex.width / 2) - cTile.left) / hPPS, Space.World);
            unityObject.transform.Translate(Vector3.down * ((newTex.height / 2) - cTile.top) / vPPS, Space.World);
            // Move to get the middle of the top left square at 0,0
            // We don't do this for MoM because it spaces differently
            if (game.gameType.TileOnGrid())
            {
                unityObject.transform.Translate(new Vector3(-(float)0.5, (float)0.5, 0), Space.World);
            }
            // Set the size to the image size
            image.rectTransform.sizeDelta = new Vector2((float)newTex.width / hPPS, (float)newTex.height / vPPS);

            // Rotate around 0,0 rotation amount
            unityObject.transform.RotateAround(Vector3.zero, Vector3.forward, qTile.rotation);
            // Move tile into target location (Space.World is needed because tile has been rotated)
            unityObject.transform.Translate(new Vector3(qTile.location.x, qTile.location.y, 0), Space.World);
        }

        // Remove this tile
        public override void Remove()
        {
            Object.Destroy(unityObject);
        }

        // Tiles are not interactive, no event
        public override QuestData.Event GetEvent()
        {
            return null;
        }
    }

    // Tokens are events that are tied to a token placed on the board
    public class Token : BoardComponent
    {
        // Quest info on the token
        public QuestData.Token qToken;

        // Construct with quest info and reference to Game
        public Token(QuestData.Token questToken, Game gameObject) : base(gameObject)
        {
            qToken = questToken;

            string tokenName = qToken.tokenName;
            // Check that token exists
            if (!game.cd.tokens.ContainsKey(tokenName))
            {
                Debug.Log("Warning: Quest component " + qToken.name + " is using missing token type: " + tokenName);
                // Catch for older quests with different types (0.4.0 or older)
                if (game.cd.tokens.ContainsKey("TokenSearch"))
                {
                    tokenName = "TokenSearch";
                }
            }

            // Get texture for token
            Vector2 texPos = new Vector2(game.cd.tokens[tokenName].x, game.cd.tokens[tokenName].y);
            Vector2 texSize = new Vector2(game.cd.tokens[tokenName].width, game.cd.tokens[tokenName].height);
            Texture2D newTex = ContentData.FileToTexture(game.cd.tokens[tokenName].image, texPos, texSize);

            // Create object
            unityObject = new GameObject("Object" + qToken.name);
            unityObject.tag = "board";

            unityObject.transform.parent = game.tokenCanvas.transform;

            // Create the image
            image = unityObject.AddComponent<UnityEngine.UI.Image>();
            Sprite tileSprite = Sprite.Create(newTex, new Rect(0, 0, newTex.width, newTex.height), Vector2.zero, 1);
            image.color = Color.white;
            image.sprite = tileSprite;
            image.rectTransform.sizeDelta = new Vector2(1f, 1f);
            // Move to square
            unityObject.transform.Translate(new Vector3(qToken.location.x, qToken.location.y, 0), Space.World);

            game.tokenBoard.Add(this);
        }

        // Tokens have an associated event to start on press
        public override QuestData.Event GetEvent()
        {
            return qToken;
        }

        // Clean up
        public override void Remove()
        {
            Object.Destroy(unityObject);
        }
    }

    // Doors are like tokens but placed differently and have different defaults
    // Note that MoM Explore tokens are tokens and do not use this
    public class Door : BoardComponent
    {
        public QuestData.Door qDoor;

        // Constuct with quest data and reference to Game
        public Door(QuestData.Door questDoor, Game gameObject) : base(gameObject)
        {
            qDoor = questDoor;

            // Load door texture, should be game specific
            Texture2D newTex = Resources.Load("sprites/door") as Texture2D;
            // Check load worked
            if (newTex == null)
            {
                Debug.Log("Error: Cannot load door image");
                Application.Quit();
            }

            // Create object
            unityObject = new GameObject("Object" + qDoor.name);
            unityObject.tag = "board";

            unityObject.transform.parent = game.tokenCanvas.transform;

            // Create the image
            image = unityObject.AddComponent<UnityEngine.UI.Image>();
            Sprite tileSprite = Sprite.Create(newTex, new Rect(0, 0, newTex.width, newTex.height), Vector2.zero, 1);
            // Set door colour
            image.sprite = tileSprite;
            image.rectTransform.sizeDelta = new Vector2(0.4f, 1.6f);
            // Rotate as required
            unityObject.transform.RotateAround(Vector3.zero, Vector3.forward, qDoor.rotation);
            // Move to square
            unityObject.transform.Translate(new Vector3(-(float)0.5, (float)0.5, 0), Space.World);
            unityObject.transform.Translate(new Vector3(qDoor.location.x, qDoor.location.y, 0), Space.World);

            // Set the texture colour from quest data
            SetColor(qDoor.colourName);

            game.tokenBoard.Add(this);
        }

        // Function to set door texture colour
        public void SetColor(string colorName)
        {
            // Translate from name to #RRGGBB, will return input if already #RRGGBB
            string colorRGB = ColorUtil.FromName(colorName);
            // Check format is valid
            if ((colorRGB.Length != 7) || (colorRGB[0] != '#'))
            {
                Debug.Log("Warning: Door color must be in #RRGGBB format or a known name in: " + qDoor.name);
            }

            // State with white (used for alpha)
            Color colour = Color.white;
            // Hexadecimal to float convert (0x00-0xFF -> 0.0-1.0)
            colour[0] = (float)System.Convert.ToInt32(colorRGB.Substring(1, 2), 16) / 255f;
            colour[1] = (float)System.Convert.ToInt32(colorRGB.Substring(3, 2), 16) / 255f;
            colour[2] = (float)System.Convert.ToInt32(colorRGB.Substring(5, 2), 16) / 255f;
            image.color = colour;
        }

        // Clean up
        public override void Remove()
        {
            Object.Destroy(unityObject);
        }

        // Doors have events that start when pressed
        public override QuestData.Event GetEvent()
        {
            return qDoor;
        }
    }


    // Super class for all quest components
    abstract public class BoardComponent
    {
        // image for display
        public UnityEngine.UI.Image image;

        // Game object
        public Game game;

        public GameObject unityObject;

        public BoardComponent(Game gameObject)
        {
            game = gameObject;
        }

        abstract public void Remove();

        abstract public QuestData.Event GetEvent();

        // Set visible can control the transparency level of the component
        virtual public void SetVisible(float alpha)
        {
            if (image == null)
                return;
            image.color = new Color(image.color.r, image.color.g, image.color.b, alpha);
        }
    }

    // Class for holding current hero status
    public class Hero
    {
        // This can be null if not selected
        public HeroData heroData;
        public bool activated = false;
        public bool defeated = false;
        //  Heros are in a list so they need ID... maybe at some point this can move to an array
        public int id = 0;
        // Used for events that can select or highlight heros
        public bool selected;

        // Constuct with content hero data and an index for hero
        public Hero(HeroData h, int i)
        {
            heroData = h;
            id = i;
        }

        // Construct with saved data
        public Hero(Dictionary<string, string> data)
        {
            bool.TryParse(data["activated"], out activated);
            bool.TryParse(data["defeated"], out defeated);
            int.TryParse(data["id"], out id);

            Game game = Game.Get();
            // Saved content reference, look it up
            if (data.ContainsKey("type"))
            {
                foreach (KeyValuePair<string, HeroData> hd in game.cd.heros)
                {
                    if (hd.Value.sectionName.Equals(data["type"]))
                    {
                        heroData = hd.Value;
                    }
                }
            }
        }

        // Save hero to string for saves/undo
        override public string ToString()
        {
            string nl = System.Environment.NewLine;

            string r = "[Hero" + id + "]" + nl;
            r += "id=" + id + nl;
            r += "activated=" + activated + nl;
            r += "defeated=" + defeated + nl;
            if (heroData != null)
            {
                r += "type=" + heroData.sectionName + nl;
            }

            return r;
        }
    }

    // Class for holding current monster status
    public class Monster
    {
        // Content Data
        public MonsterData monsterData;
        // What dulpicate number is the monster?
        public int duplicate = 0;

        // State
        public bool activated = false;
        public bool minionStarted = false;
        public bool masterStarted = false;
        // Accumulated damage
        public int damage = 0;

        // Quest specific info
        public bool unique = false;
        public string uniqueText = "";
        public string uniqueTitle = "";
        // Activation is reset each round so that master/minion use the same data and forcing doesn't re roll
        // Note that in RtL forcing activation WILL reroll the selected activation
        public ActivationInstance currentActivation;

        // Initialise from monster event
        // When an event adds a monster group this is called
        public Monster(EventManager.MonsterEvent monsterEvent)
        {
            monsterData = monsterEvent.cMonster;
            unique = monsterEvent.qMonster.unique;
            uniqueTitle = monsterEvent.GetUniqueTitle();
            uniqueText = monsterEvent.qMonster.uniqueText;

            Game game = Game.Get();
            HashSet<int> dupe = new HashSet<int>();
            foreach (Monster m in game.quest.monsters)
            {
                if (m.monsterData == monsterData || m.duplicate != 0)
                {
                    dupe.Add(m.duplicate);
                }
            }

            while (dupe.Contains(duplicate))
            {
                duplicate++;
            }
        }

        // Construct from save data
        public Monster(Dictionary<string, string> data)
        {
            bool.TryParse(data["activated"], out activated);
            bool.TryParse(data["minionStarted"], out minionStarted);
            bool.TryParse(data["masterStarted"], out masterStarted);
            bool.TryParse(data["unique"], out unique);
            int.TryParse(data["damage"], out damage);
            int.TryParse(data["duplicate"], out duplicate);
            uniqueText = data["uniqueText"];
            uniqueTitle = data["uniqueTitle"];

            // Find base type
            Game game = Game.Get();
            if (game.cd.monsters.ContainsKey(data["type"]))
            {
                monsterData = game.cd.monsters[data["type"]];
            }
            // Check if type is a special quest specific type
            if (game.quest.qd.components.ContainsKey(data["type"]) && game.quest.qd.components[data["type"]] is QuestData.UniqueMonster)
            {
                monsterData = new QuestMonster(game.quest.qd.components[data["type"]] as QuestData.UniqueMonster);
            }

            // If we have already selected an activation find it
            if (data.ContainsKey("activation"))
            {
                ActivationData saveActivation = null;
                if (game.cd.activations.ContainsKey(data["activation"]))
                {
                    saveActivation = game.cd.activations[data["activation"]];
                }
                // Activations can be specific to the quest
                if (game.quest.qd.components.ContainsKey(data["activation"]))
                {
                    saveActivation = new QuestActivation(game.quest.qd.components[data["activation"]] as QuestData.Activation);
                }
                currentActivation = new ActivationInstance(saveActivation, monsterData.name);
            }
        }

        // Create new activation
        public void NewActivation(ActivationData contentActivation)
        {
            currentActivation = new ActivationInstance(contentActivation, monsterData.name);
        }

        // Activation instance is requresd to track variables in the activation
        public class ActivationInstance
        {
            public ActivationData ad;
            // String is populated on creation of the activation
            public string effect;

            // Construct activation
            public ActivationInstance(ActivationData contentActivation, string monsterName)
            {
                ad = contentActivation;
                // Fill in hero, monster names
                // Note: Random hero selection is NOT kept on load/undo FIXME
                effect = ad.ability.Replace("{0}", Game.Get().quest.GetRandomHero().heroData.name);
                effect = effect.Replace("{1}", monsterName);
                // Fix new lines
                effect.Replace("\\n", "\n");
            }
        }

        // Save monster data to string
        override public string ToString()
        {
            string nl = System.Environment.NewLine;

            // Section name must be unique
            string r = "[Monster" + monsterData.sectionName + "]" + nl;
            r += "activated=" + activated + nl;
            r += "type=" + monsterData.sectionName + nl;
            r += "minionStarted=" + minionStarted + nl;
            r += "masterStarted=" + masterStarted + nl;
            r += "unique=" + unique + nl;
            r += "uniqueText=" + uniqueText + nl;
            r += "uniqueTitle=" + uniqueTitle + nl;
            r += "damage=" + damage + nl;
            r += "duplicate=" + duplicate + nl;
            // Save the activation (currently doesn't save the effect string)
            if (currentActivation != null)
            {
                r += "activation=" + currentActivation.ad.sectionName + nl;
            }
            return r;
        }
    }
}

