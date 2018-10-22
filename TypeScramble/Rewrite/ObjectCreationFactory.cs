﻿using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeScramble.Rewrite {
    class ObjectCreationFactory {

        public static readonly ObjectCreationFactory Instance = new ObjectCreationFactory();

        public IEnumerable<MethodDef> CreationMethods => objectCreationFactories.Values;

        private Dictionary<int, MethodDef> objectCreationFactories;

        public void CreateMethods(ITypeService service, ModuleDef module) {
            objectCreationFactories = new Dictionary<int, MethodDef>() {
                {0,CreateFactoryMethodNoParameters(service, module) },
            };
        }

        public MethodDef GetCreationMethod(int param) => objectCreationFactories[param];

        private  MethodDef CreateFactoryMethodNoParameters(ITypeService service, ModuleDef module) {

            var instancevar = new GenericParamUser(0, GenericParamAttributes.NoSpecialConstraint, "t");
            var mvar = new GenericMVar(0);
            var typeSpec = new TypeSpecUser(mvar);

            var local = new Local(mvar);
            var rtHandle = new Local(module.Import(typeof(RuntimeTypeHandle)).ToTypeSig());

            var method = new MethodDefUser("create", new MethodSig(CallingConvention.Default, 1, mvar), MethodAttributes.Static);
            method.GenericParameters.Add(instancevar);
           

            var gettype = typeof(Type).GetMethod("GetTypeFromHandle");
            var comparetypes = typeof(Type).GetMethod("op_Equality");


            var i = new List<Instruction>();
            i.Add(Instruction.Create(OpCodes.Ldtoken, typeSpec));
            i.Add(Instruction.Create(OpCodes.Call, module.Import(gettype)));
            i.Add(Instruction.Create(OpCodes.Stloc, rtHandle));


            foreach (MemberRef mr in service.ObjectCreationRef) {
                Instruction endjump = Instruction.Create(OpCodes.Nop);

                i.Add(Instruction.Create(OpCodes.Ldloc, rtHandle));

                i.Add(Instruction.Create(OpCodes.Ldtoken, mr.DeclaringType));
                i.Add(Instruction.Create(OpCodes.Call, module.Import(gettype)));

                i.Add(Instruction.Create(OpCodes.Call, module.Import(comparetypes)));
                i.Add(Instruction.Create(OpCodes.Brfalse_S, endjump));

                i.Add(Instruction.Create(OpCodes.Newobj, mr));
                i.Add(Instruction.Create(OpCodes.Ret));

                i.Add(endjump);
            }

            i.Add(Instruction.Create(OpCodes.Ldloca_S, local));
            i.Add(Instruction.Create(OpCodes.Initobj, typeSpec));
            i.Add(Instruction.Create(OpCodes.Ldloc, local));
            i.Add(Instruction.Create(OpCodes.Ret));


            method.Body = new CilBody(true, i, new ExceptionHandler[0], new Local[] { local, rtHandle });
            return method;
        }

    }
}