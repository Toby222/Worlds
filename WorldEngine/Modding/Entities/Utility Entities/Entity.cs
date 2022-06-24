﻿using System.Collections.Generic;
using System;

public abstract class Entity : IEntity
{
    public string Id { get; private set; }

    public IEntity Parent { get; protected set; }

    public Context Context { get; private set; }

    protected abstract object _reference { get; }

    protected IValueExpression<IEntity> _expression = null;

    protected EntityAttribute _thisAttribute;

    public bool RequiresInput => RequiresInputIgnoreParent || (Parent?.RequiresInput ?? false);

    protected virtual bool RequiresInputIgnoreParent => false;

    public bool HasDefaultValue { get; private set; }

    public Entity(Context context, string id, IEntity parent, bool hasDefaultValue = false)
    {
        if (string.IsNullOrEmpty(id))
        {
            throw new ArgumentException("'id' can't be null or empty");
        }

        Id = id;

        Context = context;

        HasDefaultValue = hasDefaultValue;

        Parent = parent;
    }

    public string BuildAttributeId(string attrId)
    {
        return Id + "." + attrId;
    }

    public virtual EntityAttribute GetParametricAttribute(
        string attributeId,
        ParametricSubcontext subcontext,
        string[] paramIds,
        IExpression[] arguments)
    {
        throw new System.ArgumentException(
            $"{Id}: Unable to get parametric attribute {attributeId} from entity of type {GetType()}");
    }

    public abstract EntityAttribute GetAttribute(
        string attributeId,
        IExpression[] arguments = null);

    public abstract string GetFormattedString();

    public abstract string GetDebugString();

    public override string ToString()
    {
        return Id;
    }

    public override bool Equals(object obj)
    {
        return obj is Entity entity &&
               EqualityComparer<object>.Default.Equals(_reference, entity._reference);
    }

    public override int GetHashCode()
    {
        return -417141133 + EqualityComparer<object>.Default.GetHashCode(_reference);
    }

    public int CompareTo(object other)
    {
        throw new NotImplementedException();
    }

    public static bool operator ==(Entity left, Entity right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Entity left, Entity right)
    {
        return !left.Equals(right);
    }

    public virtual IValueExpression<IEntity> Expression
    {
        get
        {
            _expression = _expression ?? new EntityExpression(this);

            return _expression;
        }
    }

    public abstract void Set(object o);

    public virtual void UseDefaultValue()
    {
        throw new NotImplementedException($"Entity type {GetType()} does not support default values");
    }

    public virtual object GetDefaultValue()
    {
        throw new NotImplementedException($"Entity type {GetType()} does not support default values");
    }

    public virtual void Set(
        object o,
        PartiallyEvaluatedStringConverter converter)
    {
        Set(o);
    }

    public virtual ParametricSubcontext BuildParametricSubcontext(
        Context parentContext,
        string attributeId,
        string[] paramIds)
    {
        throw new System.ArgumentException(
            $"{Id}: Unable to build parametric subcontext for attribute: {attributeId} in entity of type {GetType()}");
    }

    public virtual EntityAttribute GetThisEntityAttribute()
    {
        _thisAttribute =
            _thisAttribute ?? new EntityValueEntityAttribute(this, Id, Parent);

        return _thisAttribute;
    }

    public virtual string ToPartiallyEvaluatedString(int depth)
    {
        return GetDebugString();
    }

    public virtual bool TryGetRequest(out InputRequest request)
    {
        request = null;

        return Parent?.TryGetRequest(out request) ?? false;
    }
}
