using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Shouldly;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Linq;
using FakeItEasy;
using System.Data.SqlClient;
using Xunit.Abstractions;

namespace Eventfully.Semaphore.SqlServer.IntegrationTests
{
    using static IntegrationTestFixture;
    
    [Collection("Sequential")]
    public class SemaphoreTests : IntegrationTestBase, IClassFixture<SemaphoreTests.Fixture>
    {
        private readonly Fixture _fixture;
        private readonly ITestOutputHelper _log;
        private readonly string _semaphoreName = "test.outbox.consumer";
        private readonly string _ownerId1 = "test.owner.1";
        private readonly string _ownerId2 = "test.owner.2";
        private readonly string _ownerId3 = "test.owner.3";
        private readonly string _ownerId4 = "test.owner.4";
        private readonly string _ownerId5 = "test.owner.5";
        private readonly string _ownerId6 = "test.owner.6";
        private readonly string _ownerId7 = "test.owner.7";
        private readonly string _ownerId8 = "test.owner.8";
        private readonly string _ownerId9 = "test.owner.9";
        private readonly string _ownerId10 = "test.owner.10";

        public SemaphoreTests(Fixture fixture, ITestOutputHelper log)
        {
            this._fixture = fixture;
            this._log = log;
            ResetCheckpoint();//reset after every run
        }

        public class Fixture
        {
            public Fixture()
            {
            
            }
        }

        [Fact]
        public async Task Should_create_semaphore()
        {
            SqlServerSemaphore sema = new SqlServerSemaphore(ConnectionString, _semaphoreName, 120, 1);
            var semaInfo = await sema.CreateSemaphore();
            semaInfo.Name.ShouldBe(_semaphoreName);

            //should have a non empty RowVersion
            semaInfo.RowVerion.Any(x => x != default(byte)).ShouldBeTrue();
            var owners = sema.Deserialize(semaInfo.OwnersJson);
            owners.Name.ShouldBe(_semaphoreName);
            owners.Owners.ShouldNotBeNull();
            owners.Owners.Count().ShouldBe(0);
        }

        [Fact]
        public async Task Should_create_semaphore_on_aquire_if_not_exists()
        {
            SqlServerSemaphore sema = new SqlServerSemaphore(ConnectionString, _semaphoreName, 120, 1);
            var owner1Res = await sema.TryAcquire(_ownerId1);
            owner1Res.ShouldBeTrue();
            var semaInfo = await sema.GetSemaphoreInfo();
            semaInfo.Name.ShouldBe(_semaphoreName);
        }

        [Fact]
        public async Task Should_throw_exception_when_create_already_exists()
        {
            SqlServerSemaphore sema = new SqlServerSemaphore(ConnectionString, _semaphoreName, 120, 1);
            var semaInfo = await sema.CreateSemaphore();
            Should.Throw<Microsoft.Data.SqlClient.SqlException>( sema.CreateSemaphore());
        }

        [Fact]
        public async Task Should_add_first_owner()
        {
            var nowUtc = DateTime.UtcNow;
            SqlServerSemaphore sema = new SqlServerSemaphore(ConnectionString, _semaphoreName, 120, 1);
            var owner1Res = await sema.TryAcquire(_ownerId1);
            owner1Res.ShouldBeTrue();
            var semaInfo = await sema.GetSemaphoreInfo();
            semaInfo.Name.ShouldBe(_semaphoreName);
            var owners = sema.Deserialize(semaInfo.OwnersJson);
            owners.Name.ShouldBe(_semaphoreName);
            owners.Owners.ShouldNotBeNull();
            owners.Owners.Count().ShouldBe(1);
            owners.Owners.First().OwnerId.ShouldBe(_ownerId1);
            owners.Owners.First().ExpiresAtUtc.ShouldBeInRange(nowUtc.AddSeconds(120), nowUtc.AddSeconds(125));
        }

        [Fact]
        public async Task Should_add_owners_up_to_max()
        {
            SqlServerSemaphore sema = new SqlServerSemaphore(ConnectionString, _semaphoreName, 120, 5);
            var owner1Res = await sema.TryAcquire(_ownerId1);
            var owner2Res = await sema.TryAcquire(_ownerId2);
            var owner3Res = await sema.TryAcquire(_ownerId3);
            var owner4Res = await sema.TryAcquire(_ownerId4);
            var owner5Res = await sema.TryAcquire(_ownerId5);
            var owner6Res = await sema.TryAcquire(_ownerId6);

            owner1Res.ShouldBeTrue();
            owner2Res.ShouldBeTrue();
            owner3Res.ShouldBeTrue();
            owner4Res.ShouldBeTrue();
            owner5Res.ShouldBeTrue();
            owner6Res.ShouldBeFalse();
        }

    }
}
