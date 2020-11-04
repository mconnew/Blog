using System;
using System.CodeDom;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.Serialization;
using System.ServiceModel.Description;

namespace ServiceHelper
{
    internal class ServiceDataContractSurrogate : IDataContractSurrogate
    {
        public Type GetDataContractType(Type type)
        {
            if (typeof(ContractDescription).IsAssignableFrom(type))
            {
                return typeof(ContractDescriptionSurrogated);
            }

            if (typeof(OperationDescription).IsAssignableFrom(type))
            {
                return typeof(OperationDescriptionSurrogated);
            }

            if (typeof(MessageDescription).IsAssignableFrom(type))
            {
                return typeof(MessageDescriptionSurrogated);
            }

            if (typeof(MessagePartDescription).IsAssignableFrom(type))
            {
                return typeof(MessagePartDescriptionSurrogated);
            }

            return type;
        }

        public object GetDeserializedObject(object obj, Type targetType)
        {
            if (obj is ContractDescriptionSurrogated)
            {
                var cs = (ContractDescriptionSurrogated)obj;
                return cs.ContractDescription;
            }

            if (obj is OperationDescriptionSurrogated)
            {
                var os = (OperationDescriptionSurrogated)obj;
                return os.OperationDescription;
            }

            if (obj is MessageDescriptionSurrogated)
            {
                var os = (MessageDescriptionSurrogated)obj;
                return os.MessageDescription;
            }

            if (obj is MessagePartDescriptionSurrogated)
            {
                var os = (MessagePartDescriptionSurrogated)obj;
                return os.MessagePartDescription;
            }

            return obj;
        }

        public object GetObjectToSerialize(object obj, Type targetType)
        {
            if (obj is ContractDescription)
            {
                return new ContractDescriptionSurrogated((ContractDescription)obj);
            }

            if (obj is OperationDescription)
            {
                return new OperationDescriptionSurrogated((OperationDescription)obj);
            }

            if (obj is MessageDescription)
            {
                return new MessageDescriptionSurrogated((MessageDescription)obj);
            }

            if (obj is MessagePartDescription)
            {
                return new MessagePartDescriptionSurrogated((MessagePartDescription)obj);
            }

            return obj;
        }

        public Type GetReferencedTypeOnImport(string typeName, string typeNamespace, object customData)
        {
            if (typeName.Equals(nameof(ContractDescriptionSurrogated)))
            {
                return typeof(ContractDescription);
            }

            if (typeName.Equals(nameof(OperationDescriptionSurrogated)))
            {
                return typeof(OperationDescription);
            }

            if (typeName.Equals(nameof(MessageDescriptionSurrogated)))
            {
                return typeof(MessageDescription);
            }

            if (typeName.Equals(nameof(MessagePartDescriptionSurrogated)))
            {
                return typeof(MessagePartDescription);
            }

            return null;
        }

        public CodeTypeDeclaration ProcessImportedType(CodeTypeDeclaration typeDeclaration, CodeCompileUnit compileUnit)
        {
            return typeDeclaration;
        }

        public object GetCustomDataToExport(MemberInfo memberInfo, Type dataContractType)
        {
            return null;
        }

        public object GetCustomDataToExport(Type clrType, Type dataContractType)
        {
            return null;
        }

        public void GetKnownCustomDataTypes(Collection<Type> customDataTypes) { }
    }
}
