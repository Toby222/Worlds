﻿using System.Collections.Generic;
using UnityEngine.Profiling;

/// <summary>
/// Player accessible scriptable action
/// </summary>
public class ModAction : Context, IDebugLogger, IEffectTrigger
{
    public static HashSet<string> CategoryIds;

    public const string FactionTargetType = "faction";

    public const string TargetEntityId = "target";

    public static Dictionary<string, ModAction> Actions;
    public readonly FactionEntity Target;

    /// <summary>
    /// Global UId counter
    /// </summary>
    public static long CurrentUId = StartUId;

    /// <summary>
    /// The UId to use for this action type
    /// </summary>
    public long UId;

    /// <summary>
    /// Name to use in the UI for this action type
    /// </summary>
    public string Name;

    /// <summary>
    /// Category to use when sorting this into the action toolbar sections
    /// </summary>
    public string Category;

    /// <summary>
    /// Hash to use for RNGs that use this action type
    /// </summary>
    public int IdHash;

    /// <summary>
    /// Conditions that decide if this action can be accessed with the target
    /// </summary>
    public IValueExpression<bool>[] AccessConditions;

    /// <summary>
    /// Conditions that decide if this action can be used with the target
    /// </summary>
    public JustifiedCondition[] ExecuteConditions;

    /// <summary>
    /// Effects to occur after the action triggers
    /// </summary>
    public IEffectExpression[] Effects;

#if DEBUG
    private Dictionary<IEffectExpression, long> _lastUseDates = new Dictionary<IEffectExpression, long>();

    public long GetLastUseDate(IEffectExpression expression)
    {
        if (_lastUseDates.ContainsKey(expression))
        {
            return _lastUseDates[expression];
        }

        return -1;
    }

    public void SetLastUseDate(IEffectExpression expression, long date)
    {
        _lastUseDates[expression] = date;
    }
#endif

    private long _lastSetDate = -1;
    private Faction _guidedFaction = null;

    /// <summary>
    /// First UId to use for actions loaded from mods
    /// </summary>
    protected const long StartUId = 0;

    public static void ResetActions()
    {
        Actions = new Dictionary<string, ModAction>();
        CategoryIds = new HashSet<string>();
    }

    public static void LoadActionFile(string filename)
    {
        foreach (ModAction action in ActionLoader.Load(filename))
        {
            if (Actions.ContainsKey(action.Id))
            {
                Actions[action.Id] = action;
            }
            else
            {
                Actions.Add(action.Id, action);
            }
        }
    }

    public ModAction()
    {
        DebugType = "Action";

        Target = new FactionEntity(this, TargetEntityId, null);

        // Add the target to the context's entity map
        AddEntity(Target);
    }

    public static ModAction GetAction(string id)
    {
        return !Actions.TryGetValue(id, out ModAction a) ? null : a;
    }

    public bool CanAccess()
    {
        OpenDebugOutput("Evaluating Access Conditions:");

        if (AccessConditions != null)
        {
            foreach (IValueExpression<bool> exp in AccessConditions)
            {
                bool value = exp.Value;

                AddExpDebugOutput("Condition", exp);

                if (!value)
                {
                    CloseDebugOutput("Access Result: False");
                    return false;
                }
            }
        }

        CloseDebugOutput("Access Result: True");
        return true;
    }

    public bool CanExecute()
    {
        OpenDebugOutput("Evaluating Execute Conditions:");

        // Always check that the target is still valid
        if (!CanAccess())
        {
            CloseDebugOutput("Execute Result: False");
            return false;
        }

        if (ExecuteConditions != null)
        {
            foreach (var exp in ExecuteConditions)
            {
                bool value = exp.Condition.Value;

                AddExpDebugOutput("Condition", exp.Condition);

                if (!value)
                {
                    CloseDebugOutput("Execute Result: False");
                    return false;
                }
            }
        }

        CloseDebugOutput("Use Result: True");
        return true;
    }

    public string BuildExecuteInfoText()
    {
        string text = string.Empty;

        // Always check that the target is still valid
        if (!CanAccess())
        {
            return text;
        }

        bool first = true;
        if (ExecuteConditions != null)
        {
            foreach (var exp in ExecuteConditions)
            {
                if (first)
                {
                    text = $"• {exp.Info.GetFormattedString()}";
                    first = false;
                    continue;
                }

                text += $"\n• {exp.Info.GetFormattedString()}";
            }
        }

        return text;
    }

    public void SetEffectsToResolve()
    {
        World world = Target.Faction.World;

        foreach (IEffectExpression exp in Effects)
        {
            exp.Trigger = this;
            world.AddEffectToResolve(exp);
        }
    }

    public void SetTarget(Faction faction)
    {
        if (faction == null)
        {
            throw new System.ArgumentNullException("faction is set to null");
        }

        if ((_lastSetDate == Manager.CurrentWorld.CurrentDate) &&
            (_guidedFaction == faction) &&
            Manager.ResolvingPlayerInvolvedDecisionChain)
        {
            // We shouldn't reset the target if we are in the middle of resolving a
            // decision chain and neither the target nor the current date have changed
            return;
        }

        _lastSetDate = Manager.CurrentWorld.CurrentDate;
        _guidedFaction = faction;

        Reset();

        Target.Set(_guidedFaction);
    }

    public override int GetNextRandomInt(int iterOffset, int maxValue) =>
        Target.Faction.GetNextLocalRandomInt(iterOffset, maxValue);

    public override float GetNextRandomFloat(int iterOffset) =>
        Target.Faction.GetNextLocalRandomFloat(iterOffset);

    public override int GetBaseOffset() => Target.Faction.GetHashCode();
}
