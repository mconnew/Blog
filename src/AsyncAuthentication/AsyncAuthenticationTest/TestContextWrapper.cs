using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AsyncAuthenticationTest
{
    public class TestContextWrapper : TestContext
    {
        private TestContext _inner;

        public TestContextWrapper(TestContext inner)
        {
            _inner = inner;
        }
        public override void WriteLine(string format, params object[] args)
        {
            _inner.WriteLine($"{DateTime.Now:h:mm:ssss} : {string.Format(format, args)}");
        }

        public override void AddResultFile(string fileName)
        {
            _inner.AddResultFile(fileName);
        }

        public override void BeginTimer(string timerName)
        {
            _inner.BeginTimer(timerName);
        }

        public override void EndTimer(string timerName)
        {
            _inner.EndTimer(timerName);
        }

        public override IDictionary Properties => _inner.Properties;
        public override System.Data.DataRow DataRow => _inner.DataRow;
        public override System.Data.Common.DbConnection DataConnection => _inner.DataConnection;
    }
}