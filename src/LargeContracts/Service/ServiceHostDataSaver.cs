using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Xml;

namespace ServiceHelper
{
    public class ServiceHostDataSaver : ServiceHost
    {
        public ServiceHostDataSaver(Type serviceType, params Uri[] baseAddresses) : base(serviceType, baseAddresses) { }

        public ServiceHostDataSaver(object singletonInstance, params Uri[] baseAddresses) : base(singletonInstance, baseAddresses) { }

        protected override ServiceDescription CreateDescription(out IDictionary<string, ContractDescription> implementedContracts)
        {
            var serviceDescription = base.CreateDescription(out implementedContracts);
            var serializer = new DataContractSerializer(typeof(Dictionary<string, ContractDescription>),
                new DataContractSerializerSettings
                {
                    DataContractSurrogate = new ServiceDataContractSurrogate(),
                    // Enable below property if there's a lot of repeated types used in the contract
                    // PreserveObjectReferences = true
                });
            var xmlBinaryWriterSession = new TrackingXmlBinaryWriterSession();
            using (var file = File.Create("ImplementedContracts.xml"))
            {
                var xmlDictionaryWriter = XmlDictionaryWriter.CreateBinaryWriter(file, new XmlDictionary(), xmlBinaryWriterSession, false);
                serializer.WriteObject(xmlDictionaryWriter, implementedContracts);
                xmlDictionaryWriter.Flush();
                xmlDictionaryWriter.Close();
                file.Flush();
            }
            using (var file = File.Create("ImplementedContracts.Dictionary"))
            {
                if (xmlBinaryWriterSession.HasNewStrings)
                {
                    using (var bw = new BinaryWriter(file, Encoding.UTF8, true))
                    {
                        foreach (var newString in xmlBinaryWriterSession.NewStrings)
                        {
                            bw.Write(newString.Value);
                        }
                        bw.Write(string.Empty);
                    }
                    file.Flush();
                }
            }
            return serviceDescription;
        }

        private class TrackingXmlBinaryWriterSession : XmlBinaryWriterSession
        {
            List<XmlDictionaryString> newStrings;

            public bool HasNewStrings
            {
                get { return newStrings != null && newStrings.Count > 0; }
            }

            public IList<XmlDictionaryString> NewStrings => newStrings;

            public void ClearNew()
            {
                newStrings.Clear();
            }

            public override bool TryAdd(XmlDictionaryString value, out int key)
            {
                if (base.TryAdd(value, out key))
                {
                    if (newStrings == null)
                    {
                        newStrings = new List<XmlDictionaryString>();
                    }

                    newStrings.Add(value);
                    return true;
                }

                return false;
            }
        }
    }
}
