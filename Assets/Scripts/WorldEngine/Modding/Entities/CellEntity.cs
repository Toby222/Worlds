﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class CellEntity : DelayedSetEntity<TerrainCell>
{
    public const string BiomeTraitPresenceAttributeId = "biome_trait_presence";

    public virtual TerrainCell Cell
    {
        get => Setable;
        private set => Setable = value;
    }

    protected override object _reference => Cell;

    private class BiomeTraitPresenceAttribute : ValueEntityAttribute<float>
    {
        private CellEntity _cellEntity;

        private readonly IValueExpression<string> _argument;

        public BiomeTraitPresenceAttribute(CellEntity cellEntity, IExpression[] arguments)
            : base(BiomeTraitPresenceAttributeId, cellEntity, arguments)
        {
            _cellEntity = cellEntity;

            if ((arguments == null) || (arguments.Length < 1))
            {
                throw new System.ArgumentException("Number of arguments less than 1");
            }

            _argument = ValueExpressionBuilder.ValidateValueExpression<string>(arguments[0]);
        }

        public override float Value => _cellEntity.Cell.GetBiomeTraitPresence(_argument.Value);
    }

    public CellEntity(Context c, string id, IEntity parent) : base(c, id, parent)
    {
    }

    public CellEntity(
        ValueGetterMethod<TerrainCell> getterMethod, Context c, string id, IEntity parent)
        : base(getterMethod, c, id, parent)
    {
    }

    public override EntityAttribute GetAttribute(string attributeId, IExpression[] arguments = null)
    {
        switch (attributeId)
        {
            case BiomeTraitPresenceAttributeId:
                return new BiomeTraitPresenceAttribute(this, arguments);
        }

        throw new System.ArgumentException("Cell: Unable to find attribute: " + attributeId);
    }

    public override string GetDebugString()
    {
        return "cell:" + Cell.Position.ToString();
    }

    public override string GetFormattedString()
    {
        return Cell.Position.ToBoldString();
    }
}
