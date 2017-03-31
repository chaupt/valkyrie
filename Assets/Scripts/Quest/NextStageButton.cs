﻿using UnityEngine;
using System.Collections;
using Assets.Scripts.Content;

// Next stage button is used by MoM to move between investigators and monsters
public class NextStageButton
{
    private readonly StringKey PHASE_INVESTIGATOR = new StringKey("var", "PHASE_INVESTIGATOR");
    private readonly StringKey PHASE_MYTHOS = new StringKey("var", "PHASE_MYTHOS");
    private readonly StringKey MONSTER_STEP = new StringKey("var", "MONSTER_STEP");
    private readonly StringKey HORROR_STEP = new StringKey("var", "HORROR_STEP");

    // Construct and display
    public NextStageButton()
    {
        if (Game.Get().gameType.DisplayHeroes()) return;
        TextButton tb = new TextButton(
            new Vector2(UIScaler.GetHCenter(10f), UIScaler.GetBottom(-2.5f)),new Vector2(4, 2), 
            CommonStringKeys.TAB, delegate { Next(); });
        // Untag as dialog so this isn't cleared away
        tb.ApplyTag("questui");
        tb = new TextButton(
            new Vector2(UIScaler.GetHCenter(-14f), UIScaler.GetBottom(-2.5f)), new Vector2(4, 2), 
            CommonStringKeys.LOG, delegate { Log(); });
        // Untag as dialog so this isn't cleared away
        tb.ApplyTag("questui");
        tb = new TextButton(
            new Vector2(UIScaler.GetHCenter(-10f), UIScaler.GetBottom(-2.5f)), new Vector2(4, 2), 
            CommonStringKeys.SET, delegate { Set(); });
        // Untag as dialog so this isn't cleared away
        tb.ApplyTag("questui");
        Update();
    }

    public void Update()
    {
        // Clean up everything marked as 'uiphase'
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("uiphase"))
            Object.Destroy(go);

        DialogBox db;
        if (Game.Get().quest.phase == Quest.MoMPhase.horror)
        {
            db = new DialogBox(new Vector2(UIScaler.GetHCenter(-6f), UIScaler.GetBottom(-2.5f)), new Vector2(16, 2), HORROR_STEP);
            db.SetFont(Game.Get().gameType.GetHeaderFont());
        }
        else if (Game.Get().quest.phase == Quest.MoMPhase.mythos)
        {
            db = new DialogBox(new Vector2(UIScaler.GetHCenter(-6f), UIScaler.GetBottom(-2.5f)), new Vector2(16, 2), PHASE_MYTHOS);
            db.SetFont(Game.Get().gameType.GetHeaderFont());
        }
        else if (Game.Get().quest.phase == Quest.MoMPhase.monsters)
        {
            db = new DialogBox(new Vector2(UIScaler.GetHCenter(-6f), UIScaler.GetBottom(-2.5f)), new Vector2(16, 2), MONSTER_STEP);
            db.SetFont(Game.Get().gameType.GetHeaderFont());
        }
        else
        {
            db = new DialogBox(new Vector2(UIScaler.GetHCenter(-6f), UIScaler.GetBottom(-2.5f)), new Vector2(16, 2), PHASE_INVESTIGATOR);
            db.SetFont(Game.Get().gameType.GetHeaderFont());
        }
        db.ApplyTag("uiphase");
        db.textObj.GetComponent<UnityEngine.UI.Text>().fontSize = UIScaler.GetMediumFont();
        db.AddBorder();
    }

    // Button pressed
    public void Next()
    {
        if (GameObject.FindGameObjectWithTag("dialog") != null)
        {
            return;
        }

        Game game = Game.Get();

        // Add to undo stack
        game.quest.Save();

        if (game.quest.phase == Quest.MoMPhase.horror)
        {
            game.roundControl.EndRound();
        }
        else
        {
            game.roundControl.HeroActivated();
        }
    }

    public void Log()
    {
        if (GameObject.FindGameObjectWithTag("dialog") != null)
        {
            return;
        }
        new LogWindow();
    }

    public void Set()
    {
        if (GameObject.FindGameObjectWithTag("dialog") != null)
        {
            return;
        }
        new SetWindow();
    }
}
