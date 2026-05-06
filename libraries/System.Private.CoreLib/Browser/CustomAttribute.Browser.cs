using System;
using System.Text;

namespace System.Reflection
{
    [NetJs.ForcePartial(typeof(CustomAttribute))]
    internal static partial class CustomAttribute_Partial
    {
        static object? ConvertAttributeType(object? value, Type? type)
        {
            if (type == typeof(Type))
            {
                uint v = (uint)value!;
                return AppDomain.GetType(v);
            }
            return value;
        }

        static Attribute CreateAttribute(AttributeModel att, Type attType)
        {
            var args = NetJs.Script.IsDefined(att.ConstructorArguments) ? att.ConstructorArguments!.Map(a =>
            {
                var type = AppDomain.GetType(a.Type.As<uint>());
                return ConvertAttributeType(a.Value, type);
            }) : NetJs.Script.CreateArrayFromValues<object?>();
            var constructor = (ConstructorInfo)AppDomain.GetMember(att.ConstructorHandle.As<uint>())!;
            var attribute = (Attribute)Activator.CreateInstance(attType, args)!;
            if (NetJs.Script.IsDefined(att.NamedArguments))
            {
                for (int i = 0; i < att.NamedArguments!.Length; i++)
                {
                    var type = AppDomain.GetType(att.NamedArguments[i].Type.As<uint>()) ?? throw new InvalidOperationException();
                    var val = ConvertAttributeType(att.NamedArguments[i].Value, type);
                    var property = attType.GetProperty(att.NamedArguments[i].Name) ?? throw new InvalidOperationException();
                    property.SetValue(attribute, val);
                }
            }
            return attribute;
        }

        static CustomAttributeData CreateAttributeData(AttributeModel att)
        {
            var attributeType = AppDomain.GetType(att.TypeHandle.As<uint>()) ?? throw new InvalidOperationException();
            var constructor = (ConstructorInfo)AppDomain.GetMember(att.ConstructorHandle.As<uint>())!;
            return new BrowserCustomAttributeData(
                constructor,
                NetJs.Script.IsDefined(att.ConstructorArguments) ? (att.ConstructorArguments!.Map(a => new CustomAttributeTypedArgument(AppDomain.GetType(a.Type.As<uint>()) ?? throw new InvalidOperationException(), a.Value))) : [],
                NetJs.Script.IsDefined(att.NamedArguments) ? att.NamedArguments!.Map(a =>
                {
                    var member = attributeType.GetMember(a.Name).ArraySingle();
                    return new CustomAttributeNamedArgument(member, new CustomAttributeTypedArgument(AppDomain.GetType(a.Type.As<uint>()) ?? throw new InvalidOperationException(), a.Value));
                }) : NetJs.Script.CreateArrayFromValues<CustomAttributeNamedArgument>());
        }

        static AttributeModel[]? GetAttributeModel(ICustomAttributeProvider obj)
        {
            AttributeModel[]? attributesModel = null;
            if (obj is RuntimeAssembly ra)
            {
                attributesModel = ra.As<RuntimeAssembly_Partial>()._model.Attributes;
            }
            else if (obj is RuntimeType rt)
            {
                attributesModel = rt.As<RuntimeAssembly_Partial>()._model.Attributes;
            }
            else if (obj is RuntimeMethodInfo rm)
            {
                attributesModel = rm._model.Attributes;
            }
            else if (obj is RuntimePropertyInfo rp)
            {
                attributesModel = rp.As<RuntimePropertyInfo>()._model.Attributes;
            }
            else if (obj is RuntimeFieldInfo rf)
            {
                attributesModel = rf.As<RuntimeFieldInfo>()._model.Attributes;
            }
            else if (obj is RuntimeParameterInfo rpp)
            {
                attributesModel = rpp.As<RuntimeParameterInfo_Partial>()._model.Attributes;
            }
            return attributesModel;
        }

        [NetJs.MemberReplace]
        internal static Attribute[] GetCustomAttributesInternal(ICustomAttributeProvider obj, Type attributeType, bool pseudoAttrs)
        {
            var attHandle = attributeType.As<RuntimeType>()._model.Handle;
            AttributeModel[]? attributesModel = GetAttributeModel(obj);
            return (attributesModel?.Filter(a => a.TypeHandle == attHandle).Map(a => CreateAttribute(a, attributeType)) ?? []).AsNetArray();
        }

        [NetJs.MemberReplace]
        private static CustomAttributeData[] GetCustomAttributesDataInternal(ICustomAttributeProvider obj)
        {
            AttributeModel[]? attributesModel = GetAttributeModel(obj);
            return (attributesModel?.Map(a => CreateAttributeData(a)) ?? []).AsNetArray();
        }

        [NetJs.MemberReplace]
        private static bool IsDefinedInternal(ICustomAttributeProvider obj, Type AttributeType)
        {
            var attHandle = AttributeType.As<RuntimeType>()._model.Handle;
            AttributeModel[]? attributesModel = GetAttributeModel(obj);
            return attributesModel?.Some(a => a.TypeHandle == attHandle) ?? false;
        }
    }
}
