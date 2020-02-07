using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Nito.AsyncEx;
using Microsoft.Extensions.Logging;
using Xunit.Sdk;
using Xunit.Abstractions;

namespace Eventfully.Semaphore.SqlServer.IntegrationTests
{

    public abstract class IntegrationTestBase : IAsyncLifetime
    {
        private static readonly AsyncLock Mutex = new AsyncLock();

        private static bool _initialized;

        /// <summary>
        /// Clear database once per session
        /// </summary>
        /// <returns></returns>
        public virtual async Task InitializeAsync()
        {
            if (_initialized) return;

            using (await Mutex.LockAsync())
            {
                if (_initialized) return;
                //Uncomment to clear database at beginning of session
                //await IntegrationTestFixture.ResetCheckpoint();
                _initialized = true;
            }
        }

        public virtual Task DisposeAsync() => Task.CompletedTask;
    }


}

