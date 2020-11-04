using Sample.Services;
using ServiceHelper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    class Program
    {
        static void Main(string[] args)
        {
            //CreateContracts();
            var sw = new Stopwatch();
            sw.Restart();
            using (var host = new ServiceHost(typeof(Service), new Uri("net.pipe://localhost/Sample/Service")))
            {
                host.AddServiceEndpoint(typeof(IService), new NetNamedPipeBinding(), "");
                host.Open();
                sw.Stop();
                Console.WriteLine($"Open took {sw.ElapsedMilliseconds} ms to open using ServiceHost");
            }

            sw.Restart();
            using (var host = new ServiceHostDataSaver(typeof(Service), new Uri("net.pipe://localhost/ProductService")))
            {
                host.AddServiceEndpoint(typeof(IService), new NetNamedPipeBinding(), "");
                host.Open();
                sw.Stop();
                Console.WriteLine($"Open took {sw.ElapsedMilliseconds} ms to open when saving contract cache with ServiceHostDataSaver");
            }

            sw.Restart();
            using (var host = new ServiceHostDataLoader<Service>(new Uri("net.pipe://localhost/ProductService")))
            {
                host.AddServiceEndpoint(typeof(IService), new NetNamedPipeBinding(), "");
                //open host
                host.Open();
                sw.Stop();
                Console.WriteLine($"Open took {sw.ElapsedMilliseconds} ms to open when using contract cache with ServiceHostDataLoader");
            }
        }

        static void CreateContracts()
        {
            using (var serviceWriter = new StreamWriter("Service.cs"))
            {
                serviceWriter.WriteLine(
@"using Sample.Services;

namespace Service
{
    public class Service : IService
    {");
                for (int i = 1; i < 100; i++)
                {
                    GenerateContract(serviceWriter, i);
                }
                serviceWriter.Write(
@"    }
}");
            }
            using (var sw = new StreamWriter("IService.cs"))
            {
                sw.Write(
@"using System;
using System.ServiceModel;
namespace Sample.Services
{
    [ServiceContract()]
    public interface IService : ");
                var prefix = string.Empty;
                for (int i = 1; i < 100; i++)
                {
                    sw.Write($"{prefix}IService{i}");
                    prefix = ", ";
                }
                sw.WriteLine();
                sw.WriteLine(
@"    {
    }
}");
            }
        }

        private static void GenerateContract(StreamWriter serviceWriter, int i)
        {
            var filename = $"IService{i}.cs";
            using(var sw = new StreamWriter(filename))
            {
                sw.Write(
@"using System;
using System.ServiceModel;
namespace Sample.Services
{
    [ServiceContract()]
    public interface IService");
                sw.WriteLine(i);
                sw.WriteLine("    {");
                char operationFirstLetter = ((char)((char)((i - 1) % 26) + 'A'));
                for(int j=1;j <= 100; j++)
                {
                    WriteOperation(sw, serviceWriter, operationFirstLetter, j, i);
                }
                sw.WriteLine(
@"    }
}");
            }
        }

        private static void WriteOperation(StreamWriter sw, StreamWriter serviceWriter, char operationFirstLetter, int operationNumber, int interfaceNumber)
        {
            sw.WriteLine(
@"        [OperationContract]
        [FaultContract(typeof(Byte[]))]");
            var methodSig = $"int { operationFirstLetter}{operationNumber:D3}_{interfaceNumber}(int num)";
            var methodBody = "{ return 42; }";
            sw.WriteLine($"        {methodSig};");
            serviceWriter.WriteLine($"        public {methodSig} {methodBody}");
        }
    }
}
