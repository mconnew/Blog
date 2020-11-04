using System.Collections.Generic;
using System.Net.Security;
using System.Runtime.Serialization;
using System.ServiceModel.Description;

namespace ServiceHelper
{
    [DataContract(Name = "MessageDescription")]
    internal class MessageDescriptionSurrogated
    {
        public MessageDescriptionSurrogated(MessageDescription md)
        {
            Action = md.Action;
            Body = md.Body;
            Direction = md.Direction;
            Headers = new List<MessageHeaderDescription>(md.Headers);
            Properties = new List<MessagePropertyDescription>(md.Properties);
            ProtectionLevel = md.ProtectionLevel;
            HasProtectionLevel = md.HasProtectionLevel;
            MessageType = md.MessageType?.AssemblyQualifiedName;
        }

        [DataMember]
        public string Action { get; set; }

        [DataMember]
        public MessageBodyDescription Body { get; set; }

        [DataMember]
        public MessageDirection Direction { get; set; }

        [DataMember]
        public List<MessageHeaderDescription> Headers { get; set; }

        [DataMember]
        public List<MessagePropertyDescription> Properties { get; set; }

        [DataMember]
        public ProtectionLevel ProtectionLevel { get; set; }

        [DataMember]
        public bool HasProtectionLevel { get; set; }

        [DataMember]
        public string MessageType { get; set; }

        public object MessageDescription {
            get
            {
                var md = new MessageDescription(Action, Direction);
                CopyBody(Body, md.Body);
                foreach (var headerDescription in Headers)
                {
                    md.Headers.Add(headerDescription);
                }
                foreach(var propertyDescription in Properties)
                {
                    md.Properties.Add(propertyDescription);
                }
                if(HasProtectionLevel)
                {
                    md.ProtectionLevel = ProtectionLevel;
                }
                if (!string.IsNullOrEmpty(MessageType))
                {
                    md.MessageType = ReflectionHelper.GetType(MessageType);
                }
                return md;
            }
        }

        private void CopyBody(MessageBodyDescription src, MessageBodyDescription dest)
        {
            foreach(var messagePart in src.Parts)
            {
                dest.Parts.Add(messagePart);
            }
            dest.ReturnValue = src.ReturnValue;
            dest.WrapperName = src.WrapperName;
            dest.WrapperNamespace = src.WrapperNamespace;
        }
    }
}