using System;
using System.Collections.Generic;
using Endjin.Assembly.ChangeDetection.Infrastructure;
using Endjin.Assembly.ChangeDetection.Introspection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace Endjin.Assembly.ChangeDetection.Query.UsageQueries
{
    /// <summary>
    ///     Search for users of type
    ///     - as base type
    ///     - as base interface
    ///     - as generic argument to base type or base interface
    ///     - as field type            (including type used as generic argument) .e.g Func&lt;type&gt; myField
    ///     - as return type of method (including type used as generic argument)
    ///     - as parameter of a method (including type used as generic argument)
    ///     - as type argument to a generic function call ( xxx.CallFunc&lt;type&gt;(...) )
    ///     - who calls type methods
    ///     - who accesses type fields
    ///     - as local variable type
    ///     - casts to this type
    ///     - calls typeof(type)
    /// </summary>
    public class WhoUsesType : UsageVisitor
    {
        private const MethodPrintOption myMethodDisplayMode = MethodPrintOption.Parameters | MethodPrintOption.ReturnType | MethodPrintOption.ShortNames;

        internal const string InitObjCalledReason = "Struct Constructor() called";

        internal const string InterfaceCall = "Interface Call";

        internal const string MethodFromTypeCalledReason = "Method Call";

        internal const string CtorCalledReason = "Constructor called";

        internal const string UsedInGenericMethodReason = "Used in Generic method";

        internal const string UsedAsMethodParameterReason = "Method parameter";

        internal const string UsedAsMethodReturnType = "Return Type";

        internal const string InheritsFromReason = "Inherits from";

        internal const string ImplementsInterfaceReason = "Implements interface";

        internal const string FieldTypeReason = "Used as field type";

        internal const string LocalVariableReason = "Local variable";

        internal const string CastReason = "Cast to type";

        internal const string TypeOfReason = "typeof(xxx)";

        private static readonly FieldQuery notConst = new FieldQuery("!const * *");

        private static readonly TypeHashes myType = new TypeHashes(typeof(WhoUsesType));

        private readonly HashSet<string> myArgSearchTypeNames = new HashSet<string>();

        private readonly List<TypeDefinition> mySearchArgTypes;

        public WhoUsesType(UsageQueryAggregator aggregator, TypeDefinition funcArgType) : this(aggregator, new List<TypeDefinition>
        {
            ThrowIfNull("funcArgType", funcArgType)
        })
        {
        }

        public WhoUsesType(UsageQueryAggregator aggregator, List<TypeDefinition> funcArgTypes) : base(aggregator)
        {
            using (var t = new Tracer(Level.L5, myType, "WhoUsesType"))
            {
                if (funcArgTypes == null)
                {
                    throw new ArgumentNullException("funcArgTypes");
                }

                this.mySearchArgTypes = funcArgTypes;

                foreach (var funcArgType in funcArgTypes)
                {
                    t.Info("Adding search type {0}", new LazyFormat(() => funcArgType.Print()));
                    this.myArgSearchTypeNames.Add(funcArgType.Name);
                    this.Aggregator.AddVisitScope(funcArgType.Module.Assembly.Name.Name);
                    new WhoAccessesField(aggregator, notConst.GetMatchingFields(funcArgType));
                }
            }
        }

        public override void VisitType(TypeDefinition type)
        {
            Tracer.Info(Level.L5, myType, "VisitType", "Visiting type {0}", new LazyFormat(() => type.Print()));

            TypeDefinition matchingType = null;
            // Find type float in class Class : IDictionary<int,float>
            foreach (TypeReference typeRef in type.Interfaces)
            {
                if (this.IsMatching(this.myArgSearchTypeNames, this.mySearchArgTypes, typeRef, out matchingType))
                {
                    var context = new MatchContext(ImplementsInterfaceReason, matchingType.Print());
                    this.Aggregator.AddMatch(type, context);
                }
            }

            // find type float in class Class : KeyValuePair<int,float>
            if (type.BaseType != null)
            {
                if (this.IsMatching(this.myArgSearchTypeNames, this.mySearchArgTypes, type.BaseType, out matchingType))
                {
                    var context = new MatchContext(InheritsFromReason, matchingType.Print());
                    this.Aggregator.AddMatch(type, context);
                }
            }
        }

        public override void VisitMethod(MethodDefinition method)
        {
            TypeDefinition matchingType = null;
            // Check return type (recursively) to find all generic parameters
            if (IsMatching(myArgSearchTypeNames, mySearchArgTypes, method.ReturnType, out matchingType))
            {
                MatchContext context = new MatchContext(UsedAsMethodReturnType, matchingType.Print());
                Aggregator.AddMatch(method, context);
                return;
            }

            // check method paramters
            foreach (ParameterDefinition param in method.Parameters)
            {
                if (this.IsMatching(this.myArgSearchTypeNames, this.mySearchArgTypes, param.ParameterType, out matchingType))
                {
                    var context = new MatchContext(UsedAsMethodParameterReason, matchingType.Print());
                    this.Aggregator.AddMatch(method, context);
                    break;
                }
            }
        }

        public override void VisitMethodBody(MethodBody body)
        {
            var bSkipNext = false;

            foreach (Instruction instr in body.Instructions)
            {
                if (bSkipNext)
                {
                    bSkipNext = false;
                    continue;
                }

                TypeDefinition matchingType = null;

                if (instr.OpCode.Code == Code.Newobj || instr.OpCode.Code == Code.Call || instr.OpCode.Code == Code.Calli || instr.OpCode.Code == Code.Callvirt)
                {
                    var genericMethodRef = instr.Operand as GenericInstanceMethod;
                    var method = instr.Operand as MethodReference;

                    TypeReference methodDeclaringType = null;
                    if (genericMethodRef != null)
                    {
                        methodDeclaringType = genericMethodRef.DeclaringType;

                        // Search for method references with type arguments like
                        // cl.GenericMethod<decimal, sbyte>(0, 1);
                        foreach (TypeReference generic in genericMethodRef.GenericArguments)
                        {
                            if (this.IsMatching(this.myArgSearchTypeNames, this.mySearchArgTypes, generic, out matchingType))
                            {
                                var context = new MatchContext(UsedInGenericMethodReason, matchingType.Print());
                                this.Aggregator.AddMatch(instr, body.Method, true, context);
                            }
                        }
                    }
                    else if (method != null)
                    {
                        methodDeclaringType = method.DeclaringType;
                    }
                    else
                    {
                        // be robust. The type System.Web.Util.CalliHelper for example has strange opcodes inside it:
                        // .method assembly static void ArglessFunctionCaller(native int fp, object o) cil managed
                        // calli method instance void *()
                        continue;
                    }

                    if (methodDeclaringType != null)
                    {
                        if (this.IsMatching(this.myArgSearchTypeNames, this.mySearchArgTypes, methodDeclaringType, out matchingType))
                        {
                            MatchContext context;
                            if (instr.OpCode.Code == Code.Newobj || method.Name == ".ctor")
                            {
                                context = new MatchContext(CtorCalledReason, method.Print(MethodPrintOption.Parameters | MethodPrintOption.ShortNames));
                            }
                            else
                            {
                                context = new MatchContext(MethodFromTypeCalledReason, method.Print(myMethodDisplayMode));
                            }
                            this.Aggregator.AddMatch(instr, body.Method, false, context);
                        }
                    }
                }

                // resolve using statements where IDisposable on a known type is called
                else if (instr.OpCode.Code == Code.Constrained || instr.OpCode.Code == Code.Initobj)
                {
                    // e.g.    using (new AsyncFlowControl()) {} calls Dispose via the IDisposable interface with a type constraint
                    // constrained [mscorlib]System.Threading.AsyncFlowControl
                    // callvirt instance void [mscorlib]System.IDisposable::Dispose()

                    var typeRef = (TypeReference)instr.Operand;

                    if (this.IsMatching(this.myArgSearchTypeNames, this.mySearchArgTypes, typeRef, out matchingType))
                    {
                        MethodReference calledMethod = null;
                        if (instr.Next.OpCode.Code == Code.Callvirt)
                        {
                            calledMethod = (MethodReference)instr.Next.Operand;
                            bSkipNext = true;
                        }

                        MatchContext context;
                        if (instr.OpCode.Code == Code.Initobj)
                        {
                            context = new MatchContext(InitObjCalledReason, typeRef.FullName);
                        }
                        else
                        {
                            context = new MatchContext(InterfaceCall, string.Format("{0} on type {1}", calledMethod == null ? "" : calledMethod.Print(myMethodDisplayMode), typeRef.FullName));
                        }

                        this.Aggregator.AddMatch(instr, body.Method, true, context);
                    }
                }
                else if (instr.OpCode.Code == Code.Castclass)
                {
                    // Find type casts which are represented by the castclass opcode
                    // castclass [mscorlib]System.IDisposable
                    var typeRef = (TypeReference)instr.Operand;
                    if (this.IsMatching(this.myArgSearchTypeNames, this.mySearchArgTypes, typeRef, out matchingType))
                    {
                        this.Aggregator.AddMatch(instr, body.Method, false, new MatchContext(CastReason, matchingType.Print()));
                    }
                }
                else if (instr.OpCode.Code == Code.Ldtoken && instr.Next.OpCode.Code == Code.Call)
                {
                    // Find typeof(Type) occurances which are represented in IL code as
                    // ldtoken [mscorlib]System.IDisposable
                    // call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
                    var typeRef = instr.Operand as TypeReference;

                    if (typeRef != null)
                    {
                        if (this.IsMatching(this.myArgSearchTypeNames, this.mySearchArgTypes, typeRef, out matchingType))
                        {
                            var method = instr.Next.Operand as MethodReference;
                            if (method != null)
                            {
                                if (method.DeclaringType.FullName == "System.Type" && method.Name == "GetTypeFromHandle")
                                {
                                    this.Aggregator.AddMatch(instr.Next, body.Method, false, new MatchContext(TypeOfReason, matchingType.Print()));
                                }
                            }
                        }
                    }
                }
            }
        }

        public override void VisitLocals(Collection<VariableDefinition> locals, MethodDefinition declaringMethod)
        {
            base.VisitLocals(locals, declaringMethod);

            foreach (VariableDefinition variable in locals)
            {
                TypeDefinition matchingType = null;
                if (this.IsMatching(this.myArgSearchTypeNames, this.mySearchArgTypes, variable.VariableType, out matchingType))
                {
                    var context = new MatchContext(LocalVariableReason, matchingType.Print());
                    this.Aggregator.AddMatch(declaringMethod, context);
                }
            }
        }

        public override void VisitField(FieldDefinition field)
        {
            TypeDefinition matchingType = null;
            // exclude enums which would be marked as self users otherwise
            if (this.IsMatching(this.myArgSearchTypeNames, this.mySearchArgTypes, field.FieldType, out matchingType) && field.FieldType.FullName != field.DeclaringType.FullName)
            {
                var context = new MatchContext(FieldTypeReason, matchingType.Print());
                this.Aggregator.AddMatch(field, context);
            }
        }
    }
}