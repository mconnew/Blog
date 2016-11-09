using System;
using System.ServiceModel;
using System.Threading.Tasks;

namespace AsyncAuthenticationTest
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall, ConcurrencyMode = ConcurrencyMode.Multiple, IncludeExceptionDetailInFaults = true)]
    internal class TestService : ITestService
    {
        public void Test()
        {
            
        }

        public async Task TestAsync()
        {
            await Task.Yield();
        }

        public void TestThrow()
        {
            throw new Exception("TestThrow called");
        }

        public async Task TestThrowAsync()
        {
            await Task.Yield();
            throw new Exception("TestThrowAsync called");
        }
    }
}