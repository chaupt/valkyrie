using UnityEngine;
using System.Text;
using System.Collections.Generic;
using Assets.Scripts.Content;
using Assets.Scripts.UI;

public class EditorComponentSpawn : EditorComponentEvent
{
    private readonly StringKey POSITION_TYPE_UNUSED = new StringKey("val", "POSITION_TYPE_UNUSED");
    private readonly StringKey POSITION_TYPE_HIGHLIGHT = new StringKey("val", "POSITION_TYPE_HIGHLIGHT");
    private readonly StringKey MONSTER_UNIQUE = new StringKey("val", "MONSTER_UNIQUE");
    private readonly StringKey MONSTER_NORMAL = new StringKey("val", "MONSTER_NORMAL");

    private readonly StringKey UNIQUE_TITLE = new StringKey("val", "UNIQUE_TITLE");
    private readonly StringKey UNIQUE_INFO = new StringKey("val", "UNIQUE_INFO");
    private readonly StringKey HEALTH = new StringKey("val", "HEALTH");
    private readonly StringKey HEALTH_HERO = new StringKey("val", "HEALTH_HERO");
    private readonly StringKey TYPES = new StringKey("val", "TYPES");
    
    private readonly StringKey REQ_TRAITS = new StringKey("val", "REQ_TRAITS");
    private readonly StringKey POOL_TRAITS = new StringKey("val", "POOL_TRAITS");
    
    
    QuestData.Spawn spawnComponent;

    UIElementEditable uniqueTitleUIE;
    UIElementEditablePaneled uniqueTextUIE;
    UIElementEditable healthUIE;
    UIElementEditable healthHeroUIE;

    EditorSelectionList monsterTraitESL;
    EditorSelectionList monsterPlaceESL;

    public EditorComponentSpawn(string nameIn) : base(nameIn)
    {
    }

    override public void AddLocationType(float offset)
    {
        UIElement ui = new UIElement(Game.EDITOR, scrollArea.GetScrollTransform());
        ui.SetLocation(14, offset, 4, 1);
        ui.SetButton(delegate { PositionTypeCycle(); });
        new UIElementBorder(ui);
        if (!component.locationSpecified)
        {
            ui.SetText(POSITION_TYPE_UNUSED);
        }
        else
        {
            ui.SetText(POSITION_TYPE_HIGHLIGHT);
        }
    }
    
    override public float AddSubEventComponents(float offset)
    {
        spawnComponent = component as QuestData.Spawn;

        UIElement ui = null;

        if (game.gameType is D2EGameType)
        {
            ui = new UIElement(Game.EDITOR, scrollArea.GetScrollTransform());
            ui.SetLocation(0, offset, 6, 1);
            ui.SetText(new StringKey("val", "X_COLON", MONSTER_UNIQUE));

            if (!spawnComponent.unique)
            {
                ui = new UIElement(Game.EDITOR, scrollArea.GetScrollTransform());
                ui.SetLocation(6, offset, 3, 1);
                ui.SetText(new StringKey("val", "FALSE"));
                ui.SetButton(delegate { UniqueToggle(); });
                new UIElementBorder(ui);
                offset += 2;
            }
            else
            {
                ui = new UIElement(Game.EDITOR, scrollArea.GetScrollTransform());
                ui.SetLocation(6, offset, 3, 1);
                ui.SetText(new StringKey("val", "TRUE"));
                ui.SetButton(delegate { UniqueToggle(); });
                new UIElementBorder(ui);
                offset += 2;

                ui = new UIElement(Game.EDITOR, scrollArea.GetScrollTransform());
                ui.SetLocation(0, offset, 5, 1);
                ui.SetText(new StringKey("val", "X_COLON", UNIQUE_TITLE));

                uniqueTitleUIE = new UIElementEditable(Game.EDITOR, scrollArea.GetScrollTransform());
                uniqueTitleUIE.SetLocation(5, offset, 14.5f, 1);
                uniqueTitleUIE.SetText(spawnComponent.uniqueTitle.Translate());
                uniqueTitleUIE.SetSingleLine();
                uniqueTitleUIE.SetButton(delegate { UpdateUniqueTitle(); });
                new UIElementBorder(uniqueTitleUIE);
                offset += 2;

                ui = new UIElement(Game.EDITOR, scrollArea.GetScrollTransform());
                ui.SetLocation(0, offset++, 20, 1);
                ui.SetText(new StringKey("val", "X_COLON", UNIQUE_INFO));

                uniqueTextUIE = new UIElementEditablePaneled(Game.EDITOR, scrollArea.GetScrollTransform());
                uniqueTextUIE.SetLocation(0.5f, offset, 19, 8);
                uniqueTextUIE.SetText(spawnComponent.uniqueText.Translate());
                uniqueTextUIE.SetButton(delegate { UpdateUniqueText(); });
                new UIElementBorder(uniqueTextUIE);
                offset += 9;
            }
        }

        ui = new UIElement(Game.EDITOR, scrollArea.GetScrollTransform());
        ui.SetLocation(0, offset, 5, 1);
        ui.SetText(new StringKey("val", "X_COLON", HEALTH));

        healthUIE = new UIElementEditable(Game.EDITOR, scrollArea.GetScrollTransform());
        healthUIE.SetLocation(5, offset, 3, 1);
        healthUIE.SetText(spawnComponent.uniqueHealthBase.ToString());
        healthUIE.SetSingleLine();
        healthUIE.SetButton(delegate { UpdateHealth(); });
        new UIElementBorder(healthUIE);

        ui = new UIElement(Game.EDITOR, scrollArea.GetScrollTransform());
        ui.SetLocation(8, offset, 7, 1);
        ui.SetText(new StringKey("val", "X_COLON", HEALTH_HERO));

        healthHeroUIE = new UIElementEditable(Game.EDITOR, scrollArea.GetScrollTransform());
        healthHeroUIE.SetLocation(15, offset, 3, 1);
        healthHeroUIE.SetText(spawnComponent.uniqueHealthHero.ToString());
        healthHeroUIE.SetSingleLine();
        healthHeroUIE.SetButton(delegate { UpdateHealthHero(); });
        new UIElementBorder(healthHeroUIE);
        offset += 2;

        ui = new UIElement(Game.EDITOR, scrollArea.GetScrollTransform());
        ui.SetLocation(1.5f, offset, 15, 1);
        ui.SetText(new StringKey("val", "X_COLON", TYPES));

        ui = new UIElement(Game.EDITOR, scrollArea.GetScrollTransform());
        ui.SetLocation(16.5f, offset++, 1, 1);
        ui.SetText(CommonStringKeys.PLUS, Color.green);
        ui.SetButton(delegate { MonsterTypeAdd(0); });
        new UIElementBorder(ui, Color.green);

        int i = 0;
        for (i = 0; i < spawnComponent.mTypes.Length; i++)
        {
            int mSlot = i;
            string mName = spawnComponent.mTypes[i];
            if (mName.IndexOf("Monster") == 0)
            {
                mName = mName.Substring("Monster".Length);
            }

            if ((spawnComponent.mTypes.Length > 1) || (spawnComponent.mTraitsRequired.Length > 0) || (spawnComponent.mTraitsPool.Length > 0))
            {
                ui = new UIElement(Game.EDITOR, scrollArea.GetScrollTransform());
                ui.SetLocation(0.5f, offset, 1, 1);
                ui.SetText(CommonStringKeys.MINUS, Color.red);
                ui.SetButton(delegate { MonsterTypeRemove(mSlot); });
                new UIElementBorder(ui, Color.red);
            }

            ui = new UIElement(Game.EDITOR, scrollArea.GetScrollTransform());
            ui.SetLocation(1.5f, offset, 15, 1);
            ui.SetText(mName);
            ui.SetButton(delegate { MonsterTypeReplace(mSlot); });
            new UIElementBorder(ui);

            ui = new UIElement(Game.EDITOR, scrollArea.GetScrollTransform());
            ui.SetLocation(16.5f, offset++, 1, 1);
            ui.SetText(CommonStringKeys.PLUS, Color.green);
            ui.SetButton(delegate { MonsterTypeAdd(mSlot + 1); });
            new UIElementBorder(ui, Color.green);
        }
        offset++;

        float traitOffset = offset;
        ui = new UIElement(Game.EDITOR, scrollArea.GetScrollTransform());
        ui.SetLocation(0.5f, offset, 8, 1);
        ui.SetText(REQ_TRAITS);

        ui = new UIElement(Game.EDITOR, scrollArea.GetScrollTransform());
        ui.SetLocation(8.5f, offset++, 1, 1);
        ui.SetText(CommonStringKeys.PLUS, Color.green);
        ui.SetButton(delegate { MonsterTraitsAdd(); });
        new UIElementBorder(ui, Color.green);

        for (i = 0; i < spawnComponent.mTraitsRequired.Length; i++)
        {
            int mSlot = i;
            string mName = spawnComponent.mTraitsRequired[i];

            if ((spawnComponent.mTypes.Length > 0) || (spawnComponent.mTraitsRequired.Length > 1) || (spawnComponent.mTraitsPool.Length > 0))
            {
                ui = new UIElement(Game.EDITOR, scrollArea.GetScrollTransform());
                ui.SetLocation(0.5f, offset, 1, 1);
                ui.SetText(CommonStringKeys.MINUS, Color.red);
                ui.SetButton(delegate { MonsterTraitsRemove(mSlot); });
                new UIElementBorder(ui, Color.red);
            }

            ui = new UIElement(Game.EDITOR, scrollArea.GetScrollTransform());
            ui.SetLocation(1.5f, offset++, 8, 1);
            ui.SetText(new StringKey("val", mName));
            ui.SetButton(delegate { MonsterTraitReplace(mSlot); });
            new UIElementBorder(ui);
        }

        ui = new UIElement(Game.EDITOR, scrollArea.GetScrollTransform());
        ui.SetLocation(10.5f, traitOffset, 8, 1);
        ui.SetText(POOL_TRAITS);

        ui = new UIElement(Game.EDITOR, scrollArea.GetScrollTransform());
        ui.SetLocation(18.5f, traitOffset++, 1, 1);
        ui.SetText(CommonStringKeys.PLUS, Color.green);
        ui.SetButton(delegate { MonsterTraitsAdd(true); });
        new UIElementBorder(ui, Color.green);

        for (int j = 0; j < spawnComponent.mTraitsPool.Length; j++)
        {
            int mSlot = j;
            string mName = spawnComponent.mTraitsPool[j];

            if ((spawnComponent.mTypes.Length > 0) || (spawnComponent.mTraitsRequired.Length > 0) || (spawnComponent.mTraitsPool.Length > 1))
            {
                ui = new UIElement(Game.EDITOR, scrollArea.GetScrollTransform());
                ui.SetLocation(10.5f, traitOffset, 1, 1);
                ui.SetText(CommonStringKeys.MINUS, Color.red);
                ui.SetButton(delegate { MonsterTraitsRemove(mSlot, true); });
                new UIElementBorder(ui, Color.red);
            }

            ui = new UIElement(Game.EDITOR, scrollArea.GetScrollTransform());
            ui.SetLocation(11.5f, traitOffset++, 8, 1);
            ui.SetText(new StringKey("val", mName));
            ui.SetButton(delegate { MonsterTraitReplace(mSlot, true); });
            new UIElementBorder(ui);
        }

        if (traitOffset > offset) offset = traitOffset;

        offset++;
        if (game.gameType is D2EGameType)
        {
            offset = AddPlacementComponenets(offset);
        }

        return offset;
    }

    public float AddPlacementComponenets(float offset)
    {
        for (int heroes = 2; heroes < 5; heroes++)
        {
            int h = heroes;
            UIElement ui = new UIElement(Game.EDITOR, scrollArea.GetScrollTransform());
            ui.SetLocation(0.5f, offset, 18, 1);
            ui.SetText(new StringKey("val", "NUMBER_HEROS", heroes));

            ui = new UIElement(Game.EDITOR, scrollArea.GetScrollTransform());
            ui.SetLocation(18.5f, offset++, 1, 1);
            ui.SetText(CommonStringKeys.PLUS, Color.green);
            ui.SetButton(delegate { MonsterPlaceAdd(h); });
            new UIElementBorder(ui, Color.green);

            for (int i = 0; i < spawnComponent.placement[heroes].Length; i++)
            {
                int mSlot = i;
                string place = spawnComponent.placement[heroes][i];

                ui = new UIElement(Game.EDITOR, scrollArea.GetScrollTransform());
                ui.SetLocation(0.5f, offset, 1, 1);
                ui.SetText(CommonStringKeys.MINUS, Color.red);
                ui.SetButton(delegate { MonsterPlaceRemove(h, mSlot); });
                new UIElementBorder(ui, Color.red);

                ui = new UIElement(Game.EDITOR, scrollArea.GetScrollTransform());
                ui.SetLocation(1.5f, offset++, 18, 1);
                ui.SetText(place);
                ui.SetButton(delegate { QuestEditorData.SelectComponent(place); });
                new UIElementBorder(ui);
            }
            offset++;
        }

        return offset;
    }

    override public void PositionTypeCycle()
    {
        spawnComponent.locationSpecified = !spawnComponent.locationSpecified;
        Update();
    }

    public void UniqueToggle()
    {
        spawnComponent.unique = !spawnComponent.unique;
        if (!spawnComponent.unique)
        {
            LocalizationRead.scenarioDict.Remove(spawnComponent.uniquetitle_key);
            LocalizationRead.scenarioDict.Remove(spawnComponent.uniquetext_key);
        }
        else
        {
            LocalizationRead.updateScenarioText(spawnComponent.uniquetitle_key, spawnComponent.sectionName);
            LocalizationRead.updateScenarioText(spawnComponent.uniquetext_key, "-");
        }
        Update();
    }

    public void UpdateHealth()
    {
        float.TryParse(healthUIE.GetText(), out spawnComponent.uniqueHealthBase);
        Update();
    }

    public void UpdateHealthHero()
    {
        float.TryParse(healthHeroUIE.GetText(), out spawnComponent.uniqueHealthHero);
        Update();
    }

    public void UpdateUniqueTitle()
    {
        if (!uniqueTitleUIE.Empty() && uniqueTitleUIE.Changed())
        {
            LocalizationRead.updateScenarioText(spawnComponent.uniquetitle_key, uniqueTitleUIE.GetText());
        }
    }

    public void UpdateUniqueText()
    {
        if (!uniqueTextUIE.Empty() && uniqueTextUIE.Changed())
        {
            LocalizationRead.updateScenarioText(spawnComponent.uniquetext_key, uniqueTextUIE.GetText());
        }
    }

    public void MonsterTypeAdd(int pos)
    {
        if (GameObject.FindGameObjectWithTag(Game.DIALOG) != null)
        {
            return;
        }
        Game game = Game.Get();
        UIWindowSelectionListTraits select = new UIWindowSelectionListTraits(delegate(string s) { SelectMonsterType(s, pos); }, new StringKey("val", "SELECT", CommonStringKeys.MONSTER));

        foreach (KeyValuePair<string, QuestData.QuestComponent> kv in game.quest.qd.components)
        {
            if (kv.Value is QuestData.CustomMonster)
            {
                select.AddItem(kv.Value);
            }
            if (kv.Value is QuestData.Spawn)
            {
                select.AddItem(kv.Value);
            }
        }

        foreach (KeyValuePair<string, MonsterData> kv in game.cd.monsters)
        {
            select.AddItem(kv.Value);
        }
        select.Draw();
    }

    public void MonsterTypeReplace(int pos)
    {
        if (GameObject.FindGameObjectWithTag(Game.DIALOG) != null)
        {
            return;
        }
        Game game = Game.Get();
        UIWindowSelectionListTraits select = new UIWindowSelectionListTraits(delegate (string s) { SelectMonsterType(s, pos, true); }, new StringKey("val", "SELECT", CommonStringKeys.MONSTER));

        foreach (KeyValuePair<string, QuestData.QuestComponent> kv in game.quest.qd.components)
        {
            if (kv.Value is QuestData.CustomMonster)
            {
                select.AddItem(kv.Value);
            }
            if (kv.Value is QuestData.Spawn)
            {
                select.AddItem(kv.Value);
            }
        }

        foreach (KeyValuePair<string, MonsterData> kv in game.cd.monsters)
        {
            select.AddItem(kv.Value);
        }
        select.Draw();
    }

    public void SelectMonsterType(string type, int pos, bool replace = false)
    {
        if (replace)
        {
            spawnComponent.mTypes[pos] = type.Split(" ".ToCharArray())[0];
        }
        else
        {
            string[] newM = new string[spawnComponent.mTypes.Length + 1];

            int j = 0;
            for (int i = 0; i < newM.Length; i++)
            {
                if (j == pos && i == j)
                {
                    newM[i] = type.Split(" ".ToCharArray())[0];
                }
                else
                {
                    newM[i] = spawnComponent.mTypes[j];
                    j++;
                }
            }
            spawnComponent.mTypes = newM;
        }
        Update();
    }

    public void MonsterTypeRemove(int pos)
    {
        if ((spawnComponent.mTypes.Length == 1) && (spawnComponent.mTraitsRequired.Length == 0) && (spawnComponent.mTraitsPool.Length == 0))
        {
            return;
        }

        string[] newM = new string[spawnComponent.mTypes.Length - 1];

        int j = 0;
        for (int i = 0; i < spawnComponent.mTypes.Length; i++)
        {
            if (i != pos || i != j)
            {
                newM[j] = spawnComponent.mTypes[i];
                j++;
            }
        }
        spawnComponent.mTypes = newM;
        Update();
    }

    public void MonsterTraitReplace(int pos, bool pool = false)
    {
        Game game = Game.Get();
        HashSet<string> traits = new HashSet<string>();
        foreach (KeyValuePair<string, MonsterData> kv in game.cd.monsters)
        {
            foreach (string s in kv.Value.traits)
            {
                traits.Add(s);
            }
        }
        List<EditorSelectionList.SelectionListEntry> list = new List<EditorSelectionList.SelectionListEntry>();
        foreach (string s in traits)
        {
            list.Add(EditorSelectionList.SelectionListEntry.BuildNameKeyItem(s));
        }
        monsterTraitESL = new EditorSelectionList(CommonStringKeys.SELECT_ITEM, list, delegate { SelectMonsterTraitReplace(pos, pool); });
        monsterTraitESL.SelectItem();
    }

    public void SelectMonsterTraitReplace(int pos, bool pool = false)
    {
        if (pool)
        {
            spawnComponent.mTraitsPool[pos] = monsterTraitESL.selection;
        }
        else
        {
            spawnComponent.mTraitsRequired[pos] = monsterTraitESL.selection;
        }
        Update();
    }

    public void MonsterTraitsAdd(bool pool = false)
    {
        Game game = Game.Get();
        HashSet<string> traits = new HashSet<string>();
        foreach (KeyValuePair<string, MonsterData> kv in game.cd.monsters)
        {
            foreach (string s in kv.Value.traits)
            {
                traits.Add(s);
            }
        }

        List<EditorSelectionList.SelectionListEntry> list = new List<EditorSelectionList.SelectionListEntry>();
        foreach (string s in traits)
        {
            list.Add(EditorSelectionList.SelectionListEntry.BuildNameKeyItem(s));
        }
        monsterTraitESL = new EditorSelectionList(CommonStringKeys.SELECT_ITEM, list, delegate { SelectMonsterTrait(pool); });
        monsterTraitESL.SelectItem();
    }

    public void SelectMonsterTrait(bool pool = false)
    {
        if (pool)
        {
            string[] newM = new string[spawnComponent.mTraitsPool.Length + 1];

            int i;
            for (i = 0; i < spawnComponent.mTraitsPool.Length; i++)
            {
                newM[i] = spawnComponent.mTraitsPool[i];
            }

            newM[i] = monsterTraitESL.selection;
            spawnComponent.mTraitsPool = newM;
        }
        else
        {
            string[] newM = new string[spawnComponent.mTraitsRequired.Length + 1];

            int i;
            for (i = 0; i < spawnComponent.mTraitsRequired.Length; i++)
            {
                newM[i] = spawnComponent.mTraitsRequired[i];
            }

            newM[i] = monsterTraitESL.selection;
            spawnComponent.mTraitsRequired = newM;
        }
        Update();
    }

    public void MonsterTraitsRemove(int pos, bool pool = false)
    {
        if ((spawnComponent.mTypes.Length + spawnComponent.mTraitsPool.Length + spawnComponent.mTraitsRequired.Length) <= 1)
        {
            return;
        }
        if (pool)
        {
            string[] newM = new string[spawnComponent.mTraitsPool.Length - 1];

            int j = 0;
            for (int i = 0; i < spawnComponent.mTraitsPool.Length; i++)
            {
                if (i != pos || i != j)
                {
                    newM[j] = spawnComponent.mTraitsPool[i];
                    j++;
                }
            }
            spawnComponent.mTraitsPool = newM;
        }
        else
        {
            string[] newM = new string[spawnComponent.mTraitsRequired.Length - 1];

            int j = 0;
            for (int i = 0; i < spawnComponent.mTraitsRequired.Length; i++)
            {
                if (i != pos || i != j)
                {
                    newM[j] = spawnComponent.mTraitsRequired[i];
                    j++;
                }
            }
            spawnComponent.mTraitsRequired = newM;
        }
        Update();
    }

    public void MonsterPlaceAdd(int heroes)
    {
        Game game = Game.Get();

        List<EditorSelectionList.SelectionListEntry> mplaces = new List<EditorSelectionList.SelectionListEntry>();
        mplaces.Add(EditorSelectionList.SelectionListEntry.BuildNameKeyItem(
            new StringKey("val","NEW_X",CommonStringKeys.MPLACE).Translate(),"{NEW:MPlace}"));
        foreach (KeyValuePair<string, QuestData.QuestComponent> kv in game.quest.qd.components)
        {
            if (kv.Value is QuestData.MPlace)
            {
                mplaces.Add(new EditorSelectionList.SelectionListEntry(kv.Key));
            }
        }

        if (mplaces.Count == 0)
        {
            return;
        }
        monsterPlaceESL = new EditorSelectionList(CommonStringKeys.SELECT_ITEM, mplaces, delegate { MonsterPlaceAddSelection(heroes); });
        monsterPlaceESL.SelectItem();
    }

    public void MonsterPlaceAddSelection(int heroes)
    {
        if (monsterPlaceESL.selection.Equals("{NEW:MPlace}"))
        {
            Game game = Game.Get();
            int index = 0;

            while (game.quest.qd.components.ContainsKey("MPlace" + index))
            {
                index++;
            }
            game.quest.qd.components.Add("MPlace" + index, new QuestData.MPlace("MPlace" + index));
            monsterPlaceESL.selection = "MPlace" + index;
        }

        string[] newM = new string[spawnComponent.placement[heroes].Length + 1];
        int i;
        for (i = 0; i < spawnComponent.placement[heroes].Length; i++)
        {
            newM[i] = spawnComponent.placement[heroes][i];
        }

        newM[i] = monsterPlaceESL.selection;
        spawnComponent.placement[heroes] = newM;
        Update();
    }

    public void MonsterPlaceRemove(int heroes, int pos)
    {
        string[] newM = new string[spawnComponent.placement[heroes].Length - 1];

        int j = 0;
        for (int i = 0; i < spawnComponent.placement[heroes].Length; i++)
        {
            if (i != pos || i != j)
            {
                newM[j] = spawnComponent.placement[heroes][i];
                j++;
            }
        }
        spawnComponent.placement[heroes] = newM;
        Update();
    }
}
