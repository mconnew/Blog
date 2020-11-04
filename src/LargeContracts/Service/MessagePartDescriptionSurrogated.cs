using System.Net.Security;
using System.Runtime.Serialization;
using System.ServiceModel.Description;

namespace ServiceHelper
{
    [DataContract(Name = "MessagePartDescription", IsReference = true)]
    internal class MessagePartDescriptionSurrogated
    {
        private int _hashcode;
        private MessagePartDescription _mpd;

        public MessagePartDescriptionSurrogated(MessagePartDescription mpd)
        {
            Name = mpd.Name;
            Namespace = mpd.Namespace;
            TypeName = mpd.Type?.AssemblyQualifiedName;
            Index = mpd.Index;
            Multiple = mpd.Multiple;
            ProtectionLevel = mpd.ProtectionLevel;
            HasProtectionLevel = mpd.HasProtectionLevel;
            DeclaringType = mpd.MemberInfo?.DeclaringType?.AssemblyQualifiedName;
            MemberInfoName = mpd.MemberInfo?.Name;
        }

        public override int GetHashCode()
        {
            if (_hashcode == 0)
            {
                unchecked // Overflow is fine, just wrap
                {
                    int hash = 17;
                    // Suitable nullity checks etc, of course :)
                    hash = hash * 23 + Name.GetHashCode();
                    hash = hash * 23 + Namespace.GetHashCode();
                    if (TypeName != null)
                        hash = hash * 23 + TypeName.GetHashCode();
                    hash = hash * 23 + Index;
                    if (HasProtectionLevel)
                        hash = hash * 23 + ProtectionLevel.GetHashCode();
                    if (DeclaringType != null)
                        hash = hash * 23 + DeclaringType.GetHashCode();
                    if (MemberInfoName != null)
                        hash = hash * 23 + MemberInfoName.GetHashCode();
                    _hashcode = hash;
                }
            }

            return _hashcode;
        }

        public override bool Equals(object obj)
        {
            var other = obj as MessagePartDescriptionSurrogated;
            if (other == null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return string.Equals(Name, other.Name) &&
                   string.Equals(Namespace, other.Namespace) &&
                   string.Equals(TypeName, other.TypeName) &&
                   Index == other.Index &&
                   HasProtectionLevel == other.HasProtectionLevel &&
                   string.Equals(DeclaringType, other.DeclaringType) &&
                   string.Equals(MemberInfoName, other.MemberInfoName);
        }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Namespace { get; set; }

        [DataMember]
        public string TypeName { get; set; }

        [DataMember]
        public int Index { get; set; }

        [DataMember]
        public bool Multiple { get; set; }

        [DataMember]
        public ProtectionLevel ProtectionLevel { get; set; }

        [DataMember]
        public bool HasProtectionLevel { get; set; }

        [DataMember]
        public string DeclaringType { get; set; }

        [DataMember]
        public string MemberInfoName { get; set; }

        public object MessagePartDescription {
            get
            {
                if (_mpd == null)
                {
                    _mpd = new MessagePartDescription(Name, Namespace);
                    _mpd.Type = ReflectionHelper.GetType(TypeName);
                    _mpd.Index = Index;
                    _mpd.Multiple = Multiple;
                    if (HasProtectionLevel)
                    {
                        _mpd.ProtectionLevel = ProtectionLevel;
                    }
                    if (!string.IsNullOrEmpty(DeclaringType))
                    {
                        _mpd.MemberInfo = ReflectionHelper.GetMember(DeclaringType, MemberInfoName);
                    }
                }

                return _mpd;
            }
        }
    }
}