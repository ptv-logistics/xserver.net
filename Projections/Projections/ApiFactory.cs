using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace System.Reflection
{
    /// <summary>
    /// Generically provides interface implementations that access unmanaged libraries.
    /// </summary>
    /// <remarks>
    /// Code was initially taken from
    /// <a href="http://social.msdn.microsoft.com/Forums/en-US/csharpgeneral/thread/0b9c6aaa-f880-41d9-9ec0-230d7fcd4aef">http://social.msdn.microsoft.com/Forums/en-US/csharpgeneral/thread/0b9c6aaa-f880-41d9-9ec0-230d7fcd4aef</a>
    /// </remarks>
    internal static class ApiFactory
    {
        /// <summary>
        /// Defines inter operation parameters.
        /// </summary>
        /// <remarks>
        /// With <see cref="System.Runtime.InteropServices.DllImportAttribute" /> requiring
        /// 'static extern methods', this copy of the attribute class is necessary to allow the parameterization of interface methods.
        /// </remarks>
        [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
        internal class DllImportAttribute : Attribute
        {
            /// <summary>
            /// Contains the value for EntryPoint.
            /// </summary>
            public string EntryPoint;

            /// <summary>
            /// Contains the value for ExactSpelling.
            /// </summary>
            public bool ExactSpelling;

            /// <summary>
            /// Contains the value for PreserveSig.
            /// </summary>
            public bool PreserveSig = true;

            /// <summary>
            /// Contains the value for SetLastError.
            /// </summary>
            public bool SetLastError;

            /// <summary>
            /// Contains the value for CallingConvention.
            /// </summary>
            public CallingConvention CallingConvention = CallingConvention.Winapi;

            /// <summary>
            /// Contains the value for CharSet.
            /// </summary>
            public CharSet CharSet = CharSet.Ansi;

            /// <summary>
            /// Contains the value for BestFitMapping.
            /// </summary>
            public bool BestFitMapping = true;

            /// <summary>
            /// Contains the value for ThrowOnUnmappableChar.
            /// </summary>
            public bool ThrowOnUnmappableChar;
        }

        /// <summary>
        /// Get the <see cref="FieldInfo"/> from a specific object.
        /// </summary>
        /// <param name="o">Object to get the <see cref="FieldInfo"/> from.</param>
        /// <returns><see cref="FieldInfo"/> of the specified object, ordered by the type names.</returns>
        private static FieldInfo[] GetOrderedFields(object o)
        {
            FieldInfo[] fi = o.GetType().GetFields();
            Array.Sort(fi, (a, b) => String.Compare(a.Name, b.Name));
            return fi;
        }

        /// <summary>
        /// Get the field values of a specific object.
        /// </summary>
        /// <param name="o">Object to get the values from.</param>
        /// <returns>Values taken from the specified object.</returns>
        private static object[] GetFieldValues(object o)
        {
            FieldInfo[] fields = GetOrderedFields(o);
            object[] values = new object[fields.Length];
            for (int j = 0; j < fields.Length; j++)
                values[j] = fields[j].GetValue(o);
            return values;
        }

        /// <summary>
        /// Gets the <see cref="FieldInfo"/> from a specific type.
        /// </summary>
        /// <param name="o">Object to get the <see cref="FieldInfo"/> reference from.</param>
        /// <param name="other">Type to get the final <see cref="FieldInfo"/> from.</param>
        /// <returns><see cref="FieldInfo"/> read from the specified type.</returns>
        private static FieldInfo[] GetFieldInfoFromOther(object o, Type other)
        {
            FieldInfo[] fi = GetOrderedFields(o);
            for (int i = 0; i < fi.Length; i++)
                fi[i] = other.GetField(fi[i].Name);
            return fi;
        }

        /// <summary>
        /// Provides an generated and generic implementation of an interface that accesses an external library.
        /// </summary>
        /// <typeparam name="T">The type to provide an implementation for.</typeparam>
        /// <param name="dllName">The file name of the external library.</param>
        /// <returns>Returns the implementation of the specified type or null on any error.</returns>
        /// <remarks>The <see cref="DllImportAttribute"/> attribute is used on the interfaces methods 
        /// to parameterize the inter operation.</remarks>
        public static T CreateNativeApi<T>(string dllName) where T : class
        {
            Type intf = typeof(T);
            string name = intf.FullName + "Impl";

            AssemblyBuilder asm = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(name), AssemblyBuilderAccess.Run);
            TypeBuilder type = asm.DefineDynamicModule(name).DefineType(name);

            type.AddInterfaceImplementation(intf);

            foreach (MethodInfo m in intf.GetMethods())
            {
                DllImportAttribute[] dllImportAttributes = (DllImportAttribute[])m.GetCustomAttributes(typeof(DllImportAttribute), false);
                DllImportAttribute import = dllImportAttributes.Length > 0 ? dllImportAttributes[0] : new DllImportAttribute();

                if (import.EntryPoint == null)
                    import.EntryPoint = m.Name;

                ParameterInfo[] parameters = m.GetParameters();
                Type[] parameterTypes = new Type[parameters.Length];

                for (int i = 0; i < parameters.Length; i++)
                    parameterTypes[i] = parameters[i].ParameterType;

                MethodBuilder pinvoke = type.DefineMethod(m.Name, MethodAttributes.Static, m.ReturnType, parameterTypes);

                Type dllImportType = typeof(System.Runtime.InteropServices.DllImportAttribute);

                pinvoke.SetCustomAttribute(new CustomAttributeBuilder(dllImportType.GetConstructor(
                    new[] { typeof(string) }), new object[] { dllName }, GetFieldInfoFromOther(import, dllImportType), GetFieldValues(import)));

                ParameterBuilder[] pinvokeParameters = new ParameterBuilder[parameters.Length];

                for (int i = -1; i < parameters.Length; i++)
                {
                    ParameterBuilder parameter;
                    object[] marshalAsAttributes = null;
                    if (i == -1)
                    {
                        parameter = pinvoke.DefineParameter(0, m.ReturnParameter.Attributes, null);
                        marshalAsAttributes = m.ReturnParameter.GetCustomAttributes(typeof(MarshalAsAttribute), false);
                    }
                    else
                    {
                        parameter = pinvoke.DefineParameter(i + 1, parameters[i].Attributes, null);
                        marshalAsAttributes = parameters[i].GetCustomAttributes(typeof(MarshalAsAttribute), false);
                    }
                    if (marshalAsAttributes != null && marshalAsAttributes.Length > 0)
                    {
                        MarshalAsAttribute marshalAs = (MarshalAsAttribute)marshalAsAttributes[0];
                        parameter.SetCustomAttribute(new CustomAttributeBuilder(typeof(MarshalAsAttribute).GetConstructor(
                            new[] { typeof(UnmanagedType) }), new object[] { marshalAs.Value }, 
                            GetOrderedFields(marshalAs), GetFieldValues(marshalAs)));
                    }
                }


                MethodBuilder method = type.DefineMethod(m.Name, MethodAttributes.Public | MethodAttributes.Virtual, m.ReturnType, parameterTypes);
                ILGenerator gen = method.GetILGenerator();

                if (parameters.Length > 0)
                    gen.Emit(OpCodes.Ldarg_1);

                if (parameters.Length > 1)
                    gen.Emit(OpCodes.Ldarg_2);

                if (parameters.Length > 2)
                    gen.Emit(OpCodes.Ldarg_3);

                for (int i = 3; i < parameters.Length; i++)
                    gen.Emit(OpCodes.Ldarg, i + 1);

                gen.EmitCall(OpCodes.Call, pinvoke, null);
                gen.Emit(OpCodes.Ret);

                type.DefineMethodOverride(method, m);
            }

            return (T)Activator.CreateInstance(type.CreateType());
        }
    }
}
