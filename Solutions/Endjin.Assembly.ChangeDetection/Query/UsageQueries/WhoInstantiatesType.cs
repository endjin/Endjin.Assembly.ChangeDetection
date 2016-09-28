using System;
using System.Linq;
using Endjin.Assembly.ChangeDetection.Introspection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Endjin.Assembly.ChangeDetection.Query.UsageQueries
{
    public class WhoInstantiatesType : UsageVisitor
    {
        private const string ConstructorCalledContext = "Constructor called";

        private readonly TypeDefinition myType;

        public WhoInstantiatesType(UsageQueryAggregator aggregator, TypeDefinition type) : base(aggregator)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type was null");
            }

            this.myType = type;
            this.Aggregator.AddVisitScope(type.Module.Assembly.Name.Name);
        }

        public override void VisitMethodBody(Mono.Cecil.Cil.MethodBody body)
        {
            var methodDefinitions = myType.Methods.Where(x => x.IsConstructor).ToList();
            foreach (Instruction instr in body.Instructions)
            {
                if (instr.OpCode.Code == Code.Newobj)
                {
                    foreach (MethodReference method in methodDefinitions)
                    {
                        if (method.IsEqual((MethodReference)instr.Operand, false))
                        {
                            MatchContext context = new MatchContext(ConstructorCalledContext, myType.Print());
                            Aggregator.AddMatch(instr, body.Method, true, context);
                        }
                    }
                }
                else if (instr.OpCode.Code == Code.Initobj)
                {
                    TypeReference typeRef = (TypeReference)instr.Operand;
                    if (typeRef.IsEqual(myType))
                    {
                        MatchContext context = new MatchContext("Struct Constructor() called", myType.Print());
                        Aggregator.AddMatch(instr, body.Method, true, context);
                    }
                }
                else if (instr.OpCode.Code == Code.Call)
                {
                    // Value types are instantiated by directly calling the corresponding ctor
                    MethodReference methodRef = (MethodReference)instr.Operand;
                    if (methodRef.Name == ".ctor")
                    {
                        if (methodRef.DeclaringType.IsEqual(myType))
                        {
                            MatchContext context = new MatchContext(ConstructorCalledContext, myType.Print());
                            Aggregator.AddMatch(instr, body.Method, false, context);
                        }
                    }
                }
            }
        }
    }
}