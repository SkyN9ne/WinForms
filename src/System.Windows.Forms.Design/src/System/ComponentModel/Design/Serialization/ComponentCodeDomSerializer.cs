﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.CodeDom;
using System.Configuration;
using System.Reflection;

namespace System.ComponentModel.Design.Serialization;

internal class ComponentCodeDomSerializer : CodeDomSerializer
{
    private Type[] _containerConstructor;
    private static readonly Attribute[] _runTimeFilter = new Attribute[] { DesignOnlyAttribute.No };
    private static readonly Attribute[] _designTimeFilter = new Attribute[] { DesignOnlyAttribute.Yes };
    private static WeakReference s_defaultSerializerRef;

    private Type[] GetContainerConstructor(IDesignerSerializationManager manager)
    {
        _containerConstructor ??= new Type[]
            {
                GetReflectionTypeFromTypeHelper(manager, typeof(IContainer))
            };

        return _containerConstructor;
    }

    /// <summary>
    ///  Retrieves a default static instance of this serializer.
    /// </summary>
    internal static new ComponentCodeDomSerializer Default
    {
        get
        {
            ComponentCodeDomSerializer defaultSerializer;
            if (s_defaultSerializerRef is not null)
            {
                defaultSerializer = s_defaultSerializerRef.Target as ComponentCodeDomSerializer;
                if (defaultSerializer is not null)
                {
                    return defaultSerializer;
                }
            }

            defaultSerializer = new ComponentCodeDomSerializer();
            s_defaultSerializerRef = new WeakReference(defaultSerializer);
            return defaultSerializer;
        }
    }

    /// <summary>
    ///  Determines if we can cache the results of serializing a component.
    /// </summary>
    private static bool CanCacheComponent(IDesignerSerializationManager manager, object value, PropertyDescriptorCollection props)
    {
        if (value is IComponent comp)
        {
            if (comp.Site is not null)
            {
                if (comp.Site is INestedSite nestedSite && !string.IsNullOrEmpty(nestedSite.FullName))
                {
                    return false;
                }
            }

            props ??= TypeDescriptor.GetProperties(comp);

            foreach (PropertyDescriptor property in props)
            {
                if (typeof(IComponent).IsAssignableFrom(property.PropertyType) &&
                    !property.Attributes.Contains(DesignerSerializationVisibilityAttribute.Hidden))
                {
                    MemberCodeDomSerializer memberSerializer = (MemberCodeDomSerializer)manager.GetSerializer(property.GetType(), typeof(MemberCodeDomSerializer));

                    if (memberSerializer is not null && memberSerializer.ShouldSerialize(manager, value, property))
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    /// <summary>
    ///  This method is invoked during deserialization to obtain an instance of an object.  When this is called, an instance
    ///  of the requested type should be returned.  This implementation calls base and then tries to deserialize design
    ///  time properties for the component.
    /// </summary>
    protected override object DeserializeInstance(IDesignerSerializationManager manager, Type type, object[] parameters, string name, bool addToContainer)
    {
        object instance = base.DeserializeInstance(manager, type, parameters, name, addToContainer);

        if (instance is not null)
        {
            Trace(TraceLevel.Verbose, $"Deserializing design time properties for {manager.GetName(instance)}");
            DeserializePropertiesFromResources(manager, instance, _designTimeFilter);
        }

        return instance;
    }

    /// <summary>
    ///  Serializes the given object into a CodeDom object.
    /// </summary>
    public override object Serialize(IDesignerSerializationManager manager, object value)
    {
        CodeStatementCollection statements = null;
        PropertyDescriptorCollection props = TypeDescriptor.GetProperties(value);
        using (TraceScope("ComponentCodeDomSerializer::Serialize"))
        {
            ArgumentNullException.ThrowIfNull(manager);
            ArgumentNullException.ThrowIfNull(value);

            if (IsSerialized(manager, value))
            {
                Debug.Fail("Serialize is being called twice for the same component");
                return GetExpression(manager, value);
            }

            // If the object is being inherited, we will will not emit a variable declaration.  Also, we won't
            // do any serialization at all if the object is privately inherited.
            InheritanceLevel inheritanceLevel = InheritanceLevel.NotInherited;
            InheritanceAttribute inheritanceAttribute = (InheritanceAttribute)TypeDescriptor.GetAttributes(value)[typeof(InheritanceAttribute)];

            if (inheritanceAttribute is not null)
            {
                inheritanceLevel = inheritanceAttribute.InheritanceLevel;
            }

            // First, skip everything if we're privately inherited.  We cannot write any code that would affect this
            // component.
            TraceIf(TraceLevel.Verbose, inheritanceLevel == InheritanceLevel.InheritedReadOnly, "Skipping read only inherited component");
            if (inheritanceLevel != InheritanceLevel.InheritedReadOnly)
            {
                // Things we need to know:
                //
                // 1.  What expression should we use for the left hand side
                //      a) already given to us via GetExpression?
                //      b) a local variable?
                //      c) a member variable?
                //
                // 2.  Should we generate an init expression for this
                //     object?
                //      a) Inherited or existing expression: no
                //      b) otherwise, yes.

                statements = new CodeStatementCollection();
                CodeTypeDeclaration typeDecl = manager.Context[typeof(CodeTypeDeclaration)] as CodeTypeDeclaration;
                RootContext rootCtx = manager.Context[typeof(RootContext)] as RootContext;
                CodeExpression assignLhs = null;
                CodeExpression assignRhs;

                // Defaults for components
                bool generateLocal = false;
                bool generateField = true;
                bool generateObject = true;
                bool isComplete = false;

                assignLhs = GetExpression(manager, value);

                if (assignLhs is not null)
                {
                    Trace(TraceLevel.Verbose, "Existing expression for LHS of value");
                    generateLocal = false;
                    generateField = false;
                    generateObject = false;

                    // if we have an existing expression and this is not
                    // a sited component, do not serialize it.  We need this for Everett / 1.0
                    // backwards compat (even though it's wrong).
                    if (value is IComponent comp && comp.Site is null)
                    {
                        // We were in a serialize content
                        // property and would still serialize it.  This code reverses what the
                        // outer if block does for this specific case.  We also need this
                        // for Everett / 1.0 backwards compat.
                        if (!(manager.Context[typeof(ExpressionContext)] is ExpressionContext expCtx) || expCtx.PresetValue != value)
                        {
                            isComplete = true;
                        }
                    }
                }
                else
                {
                    Trace(TraceLevel.Verbose, "Creating LHS expression");
                    if (inheritanceLevel == InheritanceLevel.NotInherited)
                    {
                        // See if there is a "GenerateMember" property.  If so,
                        // we might want to generate a local variable.  Otherwise,
                        // we want to generate a field.
                        PropertyDescriptor generateProp = props["GenerateMember"];
                        if (generateProp is not null && generateProp.PropertyType == typeof(bool) && !(bool)generateProp.GetValue(value))
                        {
                            Trace(TraceLevel.Verbose, "Object GenerateMember property wants a local variable");
                            generateLocal = true;
                            generateField = false;
                        }
                    }
                    else
                    {
                        generateObject = false;
                    }

                    if (rootCtx is null)
                    {
                        generateLocal = true;
                        generateField = false;
                    }
                }

                // Push the component being serialized onto the stack.  It may be handy to
                // be able to discover this.
                manager.Context.Push(value);
                manager.Context.Push(statements);

                try
                {
                    string name = manager.GetName(value);

                    string typeName = TypeDescriptor.GetClassName(value);

                    // Output variable / field declarations if we need to
                    if ((generateField || generateLocal) && name is not null)
                    {
                        if (generateField)
                        {
                            if (inheritanceLevel == InheritanceLevel.NotInherited)
                            {
                                // We need to generate the field declaration.  See if there is a modifiers property on
                                // the object.  If not, look for a DefaultModifies, and finally assume it's private.
                                CodeMemberField field = new CodeMemberField(typeName, name);
                                PropertyDescriptor modifiersProp = props["Modifiers"];
                                MemberAttributes fieldAttrs;

                                modifiersProp ??= props["DefaultModifiers"];

                                if (modifiersProp is not null && modifiersProp.PropertyType == typeof(MemberAttributes))
                                {
                                    fieldAttrs = (MemberAttributes)modifiersProp.GetValue(value);
                                }
                                else
                                {
                                    Trace(TraceLevel.Warning, $"No Modifiers or DefaultModifiers property on component {name}. We must assume private.");
                                    fieldAttrs = MemberAttributes.Private;
                                }

                                field.Attributes = fieldAttrs;
                                typeDecl.Members.Add(field);
                                Trace(TraceLevel.Verbose, $"Field {fieldAttrs} {typeName} {name} created.");
                            }

                            // Next, create a nice LHS for our pending assign statement, when we hook up the variable.
                            assignLhs = new CodeFieldReferenceExpression(rootCtx.Expression, name);
                        }
                        else
                        {
                            if (inheritanceLevel == InheritanceLevel.NotInherited)
                            {
                                CodeVariableDeclarationStatement local = new CodeVariableDeclarationStatement(typeName, name);

                                statements.Add(local);
                                Trace(TraceLevel.Verbose, $"Local {typeName} {name} created.");
                            }

                            assignLhs = new CodeVariableReferenceExpression(name);
                        }
                    }

                    // Now output an object create if we need to.  We always see if there is a
                    // type converter that can provide us guidance

                    if (generateObject)
                    {
                        // Ok, now that we've decided if we have a local or a member variable, its now time to serialize the rest of the code.
                        // The first step is to create an assign statement to "new" the object.  For that, we need to know if
                        // the component wants a special IContainer constructor or not.  For that to be valid we must also know
                        // that we can get to an actual IContainer.
                        IContainer container = manager.GetService(typeof(IContainer)) as IContainer;
                        ConstructorInfo ctor = null;
                        if (container is not null)
                        {
                            ctor = GetReflectionTypeHelper(manager, value).GetConstructor(BindingFlags.ExactBinding | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, GetContainerConstructor(manager), null);
                        }

                        if (ctor is not null)
                        {
                            Trace(TraceLevel.Verbose, "Component has IContainer constructor.");
                            assignRhs = new CodeObjectCreateExpression(typeName, new CodeExpression[]
                            {
                                SerializeToExpression(manager, container)
                            });
                        }
                        else
                        {
                            // For compat reasons we ignore the isCompleteOld value here.
                            assignRhs = SerializeCreationExpression(manager, value, out bool isCompleteOld);
                            Debug.Assert(isCompleteOld == isComplete, "CCDS Differing");
                        }

                        TraceIf(TraceLevel.Error, assignRhs is null, $"No RHS code assign for object {value}");
                        if (assignRhs is not null)
                        {
                            if (assignLhs is null)
                            {
                                // We cannot do much more for this object.  If isComplete is true,
                                // then the RHS now becomes our LHS.  Otherwise, I'm afraid we have
                                // just failed to serialize this object.
                                if (isComplete)
                                {
                                    assignLhs = assignRhs;
                                }
                                else
                                {
                                    Trace(TraceLevel.Error, "Incomplete serialization of object, abandoning serialization.");
                                }
                            }
                            else
                            {
                                CodeAssignStatement assign = new CodeAssignStatement(assignLhs, assignRhs);
                                statements.Add(assign);
                            }
                        }
                    }

                    if (assignLhs is not null)
                    {
                        SetExpression(manager, value, assignLhs);
                    }

                    // It should practically be an assert that isComplete is false, but someone may
                    // have an unusual component.
                    if (assignLhs is not null && !isComplete)
                    {
                        // .NET CF needs us to verify that the ISupportInitialize interface exists
                        // (they do not support this interface and will modify their DSM to resolve the type to null).

                        bool supportInitialize = (value is ISupportInitialize);
                        if (supportInitialize)
                        {
                            string fullName = typeof(ISupportInitialize).FullName;
                            supportInitialize = manager.GetType(fullName) is not null;
                        }

                        Type reflectionType = null;
                        if (supportInitialize)
                        {
                            // Now verify that this control implements ISupportInitialize in the project target framework
                            // Don't use operator "is" but rather use IsAssignableFrom on the reflection types.
                            // We have other places where we use operator "is", for example "is IComponent" to generate
                            // specific CodeDOM objects, however we don't have cases of objects which were not an IComponent
                            // in a downlevel framework and became an IComponent in a newer framework, so I'm not replacing
                            // all instances of operator "is" by IsAssignableFrom.
                            reflectionType = GetReflectionTypeHelper(manager, value);
                            supportInitialize = GetReflectionTypeFromTypeHelper(manager, typeof(ISupportInitialize)).IsAssignableFrom(reflectionType);
                        }

                        bool persistSettings = (value is IPersistComponentSettings) && ((IPersistComponentSettings)value).SaveSettings;
                        if (persistSettings)
                        {
                            string fullName = typeof(IPersistComponentSettings).FullName;
                            persistSettings = manager.GetType(fullName) is not null;
                        }

                        if (persistSettings)
                        {
                            reflectionType ??= GetReflectionTypeHelper(manager, value);
                            persistSettings = GetReflectionTypeFromTypeHelper(manager, typeof(IPersistComponentSettings)).IsAssignableFrom(reflectionType);
                        }

                        // We implement statement caching only for the main code generation phase.  We don't implement it for other
                        // serialization managers.  How do we tell the difference?  The main serialization manager exists as a service.
                        IDesignerSerializationManager mainManager = manager.GetService(typeof(IDesignerSerializationManager)) as IDesignerSerializationManager;

                        if (supportInitialize)
                        {
                            Trace(TraceLevel.Verbose, "Object implements ISupportInitialize.");
                            SerializeSupportInitialize(manager, statements, assignLhs, value, "BeginInit");
                        }

                        SerializePropertiesToResources(manager, statements, value, _designTimeFilter);

                        // Writing out properties is expensive.  But, we're very smart and we cache the results
                        // in ComponentCache.  See if we have cached results.  If so, use 'em.  If not, generate
                        // code and then see if we can cache the results for later.
                        ComponentCache cache = manager.GetService(typeof(ComponentCache)) as ComponentCache;
                        ComponentCache.Entry entry = null;
                        if (cache is null)
                        {
                            if (manager.GetService(typeof(IServiceContainer)) is ServiceContainer sc)
                            {
                                cache = new ComponentCache(manager);
                                sc.AddService(typeof(ComponentCache), cache);
                            }
                        }
                        else
                        {
                            if (manager == mainManager && cache.Enabled)
                            {
                                entry = cache[value];
                            }
                        }

                        if (entry is null || entry.Tracking)
                        {
                            // Pushing the entry here allows it to be found by the resource code dom serializer,
                            // which will add data to the ResourceBlob property on the entry.
                            if (entry is null)
                            {
                                entry = new ComponentCache.Entry();

                                // We cache components even if they're not valid so dependencies are
                                // still tracked correctly (see comment below).  The problem is, we will create a
                                // new entry object even if there is still an existing one that is just invalid, and it
                                // might have dependencies that will be lost.
                                // we need to make sure we copy over any dependencies that are also tracked.
                                ComponentCache.Entry oldEntry = cache?.GetEntryAll(value);
                                if (oldEntry is not null && oldEntry.Dependencies is not null && oldEntry.Dependencies.Count > 0)
                                {
                                    foreach (object dependency in oldEntry.Dependencies)
                                    {
                                        entry.AddDependency(dependency);
                                    }
                                }
                            }

                            entry.Component = value;
                            // we need to link the cached entry with its corresponding component right away, before it's put in the context
                            // see CodeDomSerializerBase.cs::GetExpression for usage

                            // This entry will only be used if the valid bit is set.
                            // This is useful because we still need to setup dependency relationships
                            // between components even if they are not cached.  See VSWhidbey 263053.
                            bool correctManager = manager == mainManager;
                            entry.Valid = correctManager && CanCacheComponent(manager, value, props);

                            if (correctManager && cache is not null && cache.Enabled)
                            {
                                manager.Context.Push(cache);
                                manager.Context.Push(entry);
                            }

                            try
                            {
                                entry.Statements = new CodeStatementCollection();
                                SerializeProperties(manager, entry.Statements, value, _runTimeFilter);
                                SerializeEvents(manager, entry.Statements, value, null);

                                foreach (CodeStatement statement in entry.Statements)
                                {
                                    if (statement is CodeVariableDeclarationStatement local)
                                    {
                                        entry.Tracking = true;
                                        break;
                                    }
                                }

                                if (entry.Statements.Count > 0)
                                {
                                    // if we added some statements, insert the comments
                                    //
                                    entry.Statements.Insert(0, new CodeCommentStatement(string.Empty));
                                    entry.Statements.Insert(0, new CodeCommentStatement(name));
                                    entry.Statements.Insert(0, new CodeCommentStatement(string.Empty));

                                    //
                                    // cache the statements for future usage if possible. We only do this for the main serialization manager, not
                                    // for any other serialization managers that may be calling us for undo or clipboard functions.
                                    if (correctManager && cache is not null && cache.Enabled)
                                    {
                                        cache[value] = entry;
                                    }
                                }
                            }
                            finally
                            {
                                if (correctManager && cache is not null && cache.Enabled)
                                {
                                    Debug.Assert(manager.Context.Current == entry, "Context stack corrupted");
                                    manager.Context.Pop();
                                    manager.Context.Pop();
                                }
                            }
                        }
                        else
                        {
                            // If we got a cache entry, we will need to take all the resources out of
                            // it and apply them too.
                            if ((entry.Resources is not null || entry.Metadata is not null) && cache is not null && cache.Enabled)
                            {
                                ResourceCodeDomSerializer res = ResourceCodeDomSerializer.Default;
                                ResourceCodeDomSerializer.ApplyCacheEntry(manager, entry);
                            }
                        }

                        // Regardless, apply statements.  Either we created them or we got them
                        // out of the cache.
                        statements.AddRange(entry.Statements);

                        if (persistSettings)
                        {
                            SerializeLoadComponentSettings(manager, statements, assignLhs, value);
                        }

                        if (supportInitialize)
                        {
                            SerializeSupportInitialize(manager, statements, assignLhs, value, "EndInit");
                        }
                    }
                }
                catch (CheckoutException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    manager.ReportError(ex);
                }
                finally
                {
                    Debug.Assert(manager.Context.Current == statements, "Context stack corrupted");
                    manager.Context.Pop();
                    manager.Context.Pop();
                }
            }
        }

        return statements;
    }

    /// <summary>
    ///  This emits a method invoke to IPersistComponentSettings.LoadComponentSettings.
    /// </summary>
    private static void SerializeLoadComponentSettings(IDesignerSerializationManager manager, CodeStatementCollection statements, CodeExpression valueExpression, object value)
    {
        Trace(TraceLevel.Verbose, "Emitting LoadComponentSettings");

        CodeTypeReference type = new CodeTypeReference(typeof(IPersistComponentSettings));
        CodeCastExpression castExp = new CodeCastExpression(type, valueExpression);
        CodeMethodReferenceExpression method = new CodeMethodReferenceExpression(castExp, "LoadComponentSettings");
        CodeMethodInvokeExpression methodInvoke = new CodeMethodInvokeExpression
        {
            Method = method
        };

        CodeExpressionStatement statement = new CodeExpressionStatement(methodInvoke);
        statement.UserData["statement-ordering"] = "end";

        statements.Add(statement);
    }

    /// <summary>
    ///  This emits a method invoke to ISupportInitialize.
    /// </summary>
    private static void SerializeSupportInitialize(IDesignerSerializationManager manager, CodeStatementCollection statements, CodeExpression valueExpression, object value, string methodName)
    {
        Trace(TraceLevel.Verbose, $"Emitting {methodName}");

        CodeTypeReference type = new CodeTypeReference(typeof(ISupportInitialize));
        CodeCastExpression castExp = new CodeCastExpression(type, valueExpression);
        CodeMethodReferenceExpression method = new CodeMethodReferenceExpression(castExp, methodName);
        CodeMethodInvokeExpression methodInvoke = new CodeMethodInvokeExpression
        {
            Method = method
        };

        CodeExpressionStatement statement = new CodeExpressionStatement(methodInvoke);

        if (methodName == "BeginInit")
        {
            statement.UserData["statement-ordering"] = "begin";
        }
        else
        {
            statement.UserData["statement-ordering"] = "end";
        }

        statements.Add(statement);
    }
}
