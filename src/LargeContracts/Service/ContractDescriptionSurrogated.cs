using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Description;

namespace ServiceHelper
{
    [DataContract(Name = "ContractDescription")]
    internal class ContractDescriptionSurrogated
    {
        internal static Type OperationSelectorBehaviorType = Type.GetType("System.ServiceModel.Dispatcher.OperationSelectorBehavior, System.ServiceModel, Version = 4.0.0.0, Culture = neutral, PublicKeyToken = b77a5c561934e089");
        internal static IContractBehavior OperationSelectorBehavior = Activator.CreateInstance(OperationSelectorBehaviorType) as IContractBehavior;
        public ContractDescriptionSurrogated()
        {
        }

        public ContractDescriptionSurrogated(ContractDescription cd)
        {
            SessionMode = cd.SessionMode;
            HasProtectionLevel = cd.HasProtectionLevel;
            ProtectionLevel = cd.ProtectionLevel;
            Operations = new List<OperationDescription>(cd.Operations);
            Namespace = cd.Namespace;
            Name = cd.Name;
            CallbackContractType = cd.CallbackContractType?.AssemblyQualifiedName;
            ContractType = cd.ContractType.AssemblyQualifiedName;
            ConfigurationName = cd.ConfigurationName;
            if (cd.ContractBehaviors.Count != 1)
            {
                throw new Exception("ContractDescription.ContractBehaviors has extra behaviors which will need more work to accomodate");
            }
            if (!cd.ContractBehaviors[0].GetType().Equals(OperationSelectorBehaviorType))
            {
                throw new Exception("ContractDescription.ContractBehaviors[0] is the wrong type (" + cd.ContractBehaviors[0].GetType().AssemblyQualifiedName + "), expected type (" + OperationSelectorBehaviorType.AssemblyQualifiedName + ")");
            }
        }

        [DataMember]
        public SessionMode SessionMode { get; set; }

        [DataMember]
        public bool HasProtectionLevel { get; set; }

        [DataMember]
        public ProtectionLevel ProtectionLevel { get; set; }

        [DataMember]
        public List<OperationDescription> Operations { get; set; }

        [DataMember]
        public string Namespace { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string CallbackContractType { get; set; }

        [DataMember]
        public string ContractType { get; set; }

        [DataMember]
        public string ConfigurationName { get; set; }

        public ContractDescription ContractDescription {
            get
            {
                var cd = new ContractDescription(Name, Namespace);
                cd.SessionMode = SessionMode;
                if (HasProtectionLevel)
                {
                    cd.ProtectionLevel = ProtectionLevel;
                }
                for(int i = 0; i < Operations.Count; i++)
                {
                    var operation = Operations[i];
                    operation.DeclaringContract = cd;
                    cd.Operations.Insert(i, Operations[i]);
                }
                if (!string.IsNullOrEmpty(CallbackContractType))
                {
                    cd.CallbackContractType = ReflectionHelper.GetType(CallbackContractType);
                }
                cd.ContractType = ReflectionHelper.GetType(ContractType);
                cd.ConfigurationName = ConfigurationName;
                cd.ContractBehaviors.Add(OperationSelectorBehavior);
                return cd;
            }
        }
    }

}
