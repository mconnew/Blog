using System.ServiceModel;
using System.Threading.Tasks;

namespace AsyncAuthenticationTest
{
    [ServiceContract]
    internal interface ITestService
    {
        [OperationContract(Name="Test")]
        void Test();

        [OperationContract(Name="TestAsync")]
        Task TestAsync();

        [OperationContract(Name="TestThrow")]
        void TestThrow();

        [OperationContract(Name="TestThrowAsync")]
        Task TestThrowAsync();
    }
}