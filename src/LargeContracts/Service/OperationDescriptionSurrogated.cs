using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Description;

namespace ServiceHelper
{
    [DataContract(Name = "OperationDescription")]
    [KnownType(typeof(OperationBehaviorAttribute))]
    internal class OperationDescriptionSurrogated
    {
        private static ContractDescription s_dummyContractDescription = new ContractDescription("foo");
        internal static Type OperationInvokerBehaviorType = Type.GetType("System.ServiceModel.Dispatcher.OperationInvokerBehavior, System.ServiceModel, Version = 4.0.0.0, Culture = neutral, PublicKeyToken = b77a5c561934e089");
        internal static IOperationBehavior OperationInvokerBehavior = Activator.CreateInstance(OperationInvokerBehaviorType) as IOperationBehavior;
        internal static DataContractFormatAttribute OperationFormatDocumentStyle = new DataContractFormatAttribute { Style = OperationFormatStyle.Document };
        internal static DataContractFormatAttribute OperationFormatRpcStyle = new DataContractFormatAttribute { Style = OperationFormatStyle.Rpc };

        public OperationDescriptionSurrogated(OperationDescription od)
        {
            KnownTypes = new List<string>(od.KnownTypes.Count);
            foreach(var knownType in od.KnownTypes)
            {
                KnownTypes.Add(knownType.AssemblyQualifiedName);
            }
            IsTerminating = od.IsTerminating;
            IsInitiating = od.IsInitiating;
            Faults = new List<FaultDescription>(od.Faults);
            if (od.SyncMethod != null)
            {
                MethodDeclaringType = od.SyncMethod.DeclaringType.AssemblyQualifiedName;
                SyncMethod = od.SyncMethod.Name;
                // If there's a problem with method name ambiguity, this can be modified to save MethodInfo.ToString as well, then compare that when deserializing
            }
            else if (od.TaskMethod != null)
            {
                MethodDeclaringType = od.TaskMethod.DeclaringType.AssemblyQualifiedName;
                TaskMethod = od.TaskMethod.Name;
            }
            else if (od.BeginMethod != null)
            {
                MethodDeclaringType = od.BeginMethod.DeclaringType.AssemblyQualifiedName;
                BeginMethod = od.BeginMethod.Name;
                EndMethod = od.EndMethod.Name;
            }

            HasProtectionLevel = od.HasProtectionLevel;
            ProtectionLevel = od.ProtectionLevel;
            Messages = new List<MessageDescription>(od.Messages.Count);
            for(int i = 0; i < od.Messages.Count; i++)
            {
                Messages.Add(od.Messages[i]);
            }

            Name = od.Name;
            Behaviors = new List<IOperationBehavior>();
            foreach(var behavior in od.OperationBehaviors)
            {
                var behaviorType = behavior.GetType();
                if (behaviorType.Equals(OperationInvokerBehaviorType))
                    continue;
                if (behavior is OperationBehaviorAttribute)
                    Behaviors.Add(behavior);
                else if (behavior is DataContractSerializerOperationBehavior)
                    OperationFormatStyle = ((DataContractSerializerOperationBehavior)behavior).DataContractFormatAttribute.Style;
                else if (behaviorType.Name.Equals("DataContractSerializerOperationGenerator"))
                    continue;
                else
                    Behaviors.Add(behavior);
            }
        }

        [DataMember]
        public List<string> KnownTypes { get; set; }

        [DataMember]
        public bool IsTerminating { get; set; }

        [DataMember]
        public bool IsInitiating { get; set; }

        public List<FaultDescription> Faults { get; set; }

        [DataMember]
        public string MethodDeclaringType { get; set; }

        [DataMember]
        public string SyncMethod { get; set; }

        [DataMember]
        public string TaskMethod { get; set; }

        [DataMember]
        public string BeginMethod { get; set; }

        [DataMember]
        public string EndMethod { get; set; }

        [DataMember]
        public bool HasProtectionLevel { get; set; }

        [DataMember]
        public ProtectionLevel ProtectionLevel { get; set; }

        [DataMember]
        public List<IOperationBehavior> Behaviors { get; set; }

        [DataMember]
        public OperationFormatStyle OperationFormatStyle { get; private set; }

        [DataMember]
        public List<MessageDescription> Messages { get; set; }

        [DataMember]
        public string Name { get; set; }

        public OperationDescription OperationDescription {
            get
            {
                var od = new OperationDescription(Name, s_dummyContractDescription);
                foreach(var knownTypeName in KnownTypes)
                {
                    od.KnownTypes.Add(ReflectionHelper.GetType(knownTypeName));
                }
                od.IsTerminating = IsTerminating;
                od.IsInitiating = IsInitiating;
                if (!string.IsNullOrEmpty(SyncMethod))
                {
                    od.SyncMethod = ReflectionHelper.GetMethod(MethodDeclaringType, SyncMethod);
                }
                else if (!string.IsNullOrEmpty(TaskMethod))
                {
                    od.TaskMethod = ReflectionHelper.GetMethod(MethodDeclaringType, TaskMethod);
                }
                else if (!string.IsNullOrEmpty(BeginMethod))
                {
                    od.BeginMethod = ReflectionHelper.GetMethod(MethodDeclaringType, BeginMethod);
                    od.EndMethod = ReflectionHelper.GetMethod(MethodDeclaringType, EndMethod);
                }
                if (HasProtectionLevel)
                {
                    od.ProtectionLevel = ProtectionLevel;
                }
                for(int i = 0; i < Messages.Count; i++)
                {
                    od.Messages.Insert(i, Messages[i]);
                }

                od.OperationBehaviors.Add(OperationInvokerBehavior);
                for (int i = 0; i < Behaviors.Count; i++)
                {
                    od.OperationBehaviors.Add(Behaviors[i]);
                }
                if (OperationFormatStyle == OperationFormatStyle.Document)
                {
                    od.OperationBehaviors.Add(new DataContractSerializerOperationBehavior(od, OperationFormatDocumentStyle));
                }
                else
                {
                    od.OperationBehaviors.Add(new DataContractSerializerOperationBehavior(od, OperationFormatRpcStyle));
                }

                return od;
            }
        }
    }
}
