using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Xml;

namespace ServiceHelper
{
    class ServiceHostDataLoader<TServiceType> : ServiceHost where TServiceType : class
    {
        public ServiceHostDataLoader(params Uri[] baseAddresses) : base (typeof(TServiceType), baseAddresses) { }

        public ServiceHostDataLoader(TServiceType singletonInstance, params Uri[] baseAddresses) : base(singletonInstance, baseAddresses) { }

        protected override ServiceDescription CreateDescription(out IDictionary<string, ContractDescription> implementedContracts)
        {
            ServiceDescription serviceDescription;
            if (SingletonInstance != null)
            {
                serviceDescription = ServiceDescription.GetService(SingletonInstance);
            }
            else
            {
                serviceDescription = ServiceDescription.GetService(typeof(TServiceType));
            }
            var xmlBinaryReaderSession = new XmlBinaryReaderSession();
            using (var file = File.OpenRead("ImplementedContracts.Dictionary"))
            {
                using (var br = new BinaryReader(file, Encoding.UTF8, true))
                {
                    int dictionaryId = 0;
                    while (true)
                    {
                        var str = br.ReadString();
                        if (string.IsNullOrEmpty(str)) break;
                        xmlBinaryReaderSession.Add(dictionaryId, str);
                        dictionaryId++;
                    }
                }
            }
            var serializer = new DataContractSerializer(typeof(Dictionary<string, ContractDescription>),
                new DataContractSerializerSettings
                {
                    DataContractSurrogate = new ServiceDataContractSurrogate(),
//                    PreserveObjectReferences = true
                });
            using (var file = File.OpenRead("ImplementedContracts.xml"))
            {
                var xmlDictionaryReader = XmlDictionaryReader.CreateBinaryReader(file, new XmlDictionary(), XmlDictionaryReaderQuotas.Max, xmlBinaryReaderSession);
                implementedContracts = (Dictionary<string, ContractDescription>)serializer.ReadObject(xmlDictionaryReader);
            }

            var reflectedContractCollectionType = typeof(ServiceHost).Assembly.GetType("System.ServiceModel.ServiceHost+ReflectedContractCollection");
            var reflectedContracts = Activator.CreateInstance(reflectedContractCollectionType) as KeyedCollection<Type, ContractDescription>;
            foreach(var contract in implementedContracts.Values)
            {
                reflectedContracts.Add(contract);
            }
            var reflectedContractsField = typeof(ServiceHost).GetField("reflectedContracts", BindingFlags.NonPublic | BindingFlags.Instance);
            reflectedContractsField.SetValue(this, reflectedContracts);
            return serviceDescription;
        }
    }
}
