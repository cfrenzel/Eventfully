using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Nito.AsyncEx;
using Microsoft.Extensions.Logging;
using Xunit.Sdk;
using Xunit.Abstractions;

namespace Eventfully.EFCoreOutbox.IntegrationTests
{

    public abstract class IntegrationTestBase : IAsyncLifetime
    {
        private static readonly AsyncLock Mutex = new AsyncLock();

        private static bool _initialized;

        /// <summary>
        /// Clear database once per execution
        /// </summary>
        /// <returns></returns>
        public virtual async Task InitializeAsync()
        {
            if (_initialized) return;

            using (await Mutex.LockAsync())
            {
                if (_initialized) return;
                await IntegrationTestFixture.ResetCheckpoint();


                _initialized = true;
            }
        }

        public virtual Task DisposeAsync() => Task.CompletedTask;
    }


    //public class XunitLoggerProvider : ILoggerProvider
    //{
    //    private readonly ITestOutputHelper _testOutputHelper;

    //    public XunitLoggerProvider(ITestOutputHelper testOutputHelper)
    //    {
    //        _testOutputHelper = testOutputHelper;
    //    }

    //    public ILogger CreateLogger(string categoryName)
    //        => new XunitLogger(_testOutputHelper, categoryName);

    //    public void Dispose()
    //    { }
    //}

    //public class XunitLogger : ILogger
    //{
    //    private readonly ITestOutputHelper _testOutputHelper;
    //    private readonly string _categoryName;

    //    public XunitLogger(ITestOutputHelper testOutputHelper, string categoryName)
    //    {
    //        _testOutputHelper = testOutputHelper;
    //        _categoryName = categoryName;
    //    }

    //    public IDisposable BeginScope<TState>(TState state)
    //        => NoopDisposable.Instance;

    //    public bool IsEnabled(LogLevel logLevel)
    //        => true;

    //    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    //    {
    //        _testOutputHelper.WriteLine($"{_categoryName} [{eventId}] {formatter(state, exception)}");
    //        if (exception != null)
    //            _testOutputHelper.WriteLine(exception.ToString());
    //    }

    //    private class NoopDisposable : IDisposable
    //    {
    //        public static NoopDisposable Instance = new NoopDisposable();
    //        public void Dispose()
    //        { }
    //    }
    //}
}

