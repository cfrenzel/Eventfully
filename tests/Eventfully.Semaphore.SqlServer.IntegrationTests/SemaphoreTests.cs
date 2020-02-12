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
        }
    
        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            await ResetCheckpoint();
        }

        public class Fixture {}

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

        [Fact]
        public async Task Should_remove_expired_on_acquire()
        {
            SqlServerSemaphore sema = new SqlServerSemaphore(ConnectionString, _semaphoreName, 1, 5);
            var owner1Res = await sema.TryAcquire(_ownerId1);
            owner1Res.ShouldBeTrue();
            await Task.Delay(1500);
            var owner2Res = await sema.TryAcquire(_ownerId2);
            var semaInfo = await sema.GetSemaphoreInfo();
            owner2Res.ShouldBeTrue();
            var owners = sema.Deserialize(semaInfo.OwnersJson);
            owners.Owners.Count().ShouldBe(1);
            owners.Owners.Single().OwnerId.ShouldBe(_ownerId2);
        }

        [Fact]
        public async Task Should_renew()
        {
            SqlServerSemaphore sema = new SqlServerSemaphore(ConnectionString, _semaphoreName, 10, 5);
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

            await Task.Delay(5000);

            var nowUtc = DateTime.UtcNow;
            var owner1RenewRes = await sema.TryRenew(_ownerId1);
            var owner2RenewRes = await sema.TryRenew(_ownerId2);
            var owner3RenewRes = await sema.TryRenew(_ownerId3);
            var owner4RenewRes = await sema.TryRenew(_ownerId4);
            var owner5RenewRes = await sema.TryRenew(_ownerId5);
            var owner6RenewRes = await sema.TryRenew(_ownerId6);

            owner1RenewRes.ShouldBeTrue();
            owner2RenewRes.ShouldBeTrue();
            owner3RenewRes.ShouldBeTrue();
            owner4RenewRes.ShouldBeTrue();
            owner5RenewRes.ShouldBeTrue();
            owner6RenewRes.ShouldBeFalse();

            var semaInfo = await sema.GetSemaphoreInfo();
            var owners = sema.Deserialize(semaInfo.OwnersJson);
            owners.Owners.Count().ShouldBe(5);

            var owner1 = owners.Owners.Single(x => x.OwnerId == _ownerId1);
            owner1.ExpiresAtUtc.ShouldBeInRange(nowUtc.AddSeconds(9), nowUtc.AddSeconds(13));

            var owner2 = owners.Owners.Single(x => x.OwnerId == _ownerId2);
            owner2.ExpiresAtUtc.ShouldBeInRange(nowUtc.AddSeconds(9), nowUtc.AddSeconds(13));

            var owner3 = owners.Owners.Single(x => x.OwnerId == _ownerId3);
            owner3.ExpiresAtUtc.ShouldBeInRange(nowUtc.AddSeconds(9), nowUtc.AddSeconds(13));

            var owner4 = owners.Owners.Single(x => x.OwnerId == _ownerId4);
            owner4.ExpiresAtUtc.ShouldBeInRange(nowUtc.AddSeconds(9), nowUtc.AddSeconds(13));

            var owner5 = owners.Owners.Single(x => x.OwnerId == _ownerId5);
            owner5.ExpiresAtUtc.ShouldBeInRange(nowUtc.AddSeconds(9), nowUtc.AddSeconds(13));

        }

        [Fact]
        public async Task Should_aquire_on_renew_when_expired()
        {
            var nowUtc = DateTime.UtcNow;
            SqlServerSemaphore sema = new SqlServerSemaphore(ConnectionString, _semaphoreName, 2, 1);
            var owner1Res = await sema.TryAcquire(_ownerId1);
            owner1Res.ShouldBeTrue();
            
            await Task.Delay(2500);

            var semaInfo = await sema.GetSemaphoreInfo();
            var owners = sema.Deserialize(semaInfo.OwnersJson);
            var owner1 = owners.Owners.Single(x => x.OwnerId == _ownerId1);
            owner1.ExpiresAtUtc.ShouldBeLessThan(DateTime.UtcNow);

            var renewNow = DateTime.UtcNow;
            var owner1RenewRes = await sema.TryRenew(_ownerId1);
            owner1RenewRes.ShouldBeTrue();

            semaInfo = await sema.GetSemaphoreInfo();
            owners = sema.Deserialize(semaInfo.OwnersJson);
            owner1 = owners.Owners.Single(x => x.OwnerId == _ownerId1);
            owner1.ExpiresAtUtc.ShouldBeGreaterThan(renewNow);
        }

        [Fact]
        public async Task Should_aquire_on_renew_when_not_owner()
        {
            var nowUtc = DateTime.UtcNow;
            SqlServerSemaphore sema = new SqlServerSemaphore(ConnectionString, _semaphoreName, 20, 1);
            var owner1Res = await sema.TryRenew(_ownerId1);
            owner1Res.ShouldBeTrue();

            var semaInfo = await sema.GetSemaphoreInfo();
            var owners = sema.Deserialize(semaInfo.OwnersJson);
            owners.Owners.Count.ShouldBe(1);
            var owner1 = owners.Owners.Single(x => x.OwnerId == _ownerId1);
            owner1.ExpiresAtUtc.ShouldBeGreaterThan(DateTime.UtcNow);
        }

        [Fact]
        public async Task Should_release()
        {
            SqlServerSemaphore sema = new SqlServerSemaphore(ConnectionString, _semaphoreName, 10, 5);
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

            var nowUtc = DateTime.UtcNow;
            var owner1ReleaseRes = await sema.TryRelease(_ownerId1);
            var owner2ReleaseRes = await sema.TryRelease(_ownerId2);
            var owner3ReleaseRes = await sema.TryRelease(_ownerId3);
            var owner4ReleaseRes = await sema.TryRelease(_ownerId4);
            var owner5ReleaseRes = await sema.TryRelease(_ownerId5);

            owner1ReleaseRes.ShouldBeTrue();
            owner2ReleaseRes.ShouldBeTrue();
            owner3ReleaseRes.ShouldBeTrue();
            owner4ReleaseRes.ShouldBeTrue();
            owner5ReleaseRes.ShouldBeTrue();
       
            var semaInfo = await sema.GetSemaphoreInfo();
            var owners = sema.Deserialize(semaInfo.OwnersJson);
            owners.Owners.Count().ShouldBe(0);

                owner6Res = await sema.TryAcquire(_ownerId6);
            var owner7Res = await sema.TryAcquire(_ownerId7);
            var owner8Res = await sema.TryAcquire(_ownerId8);
            var owner9Res = await sema.TryAcquire(_ownerId9);
            var owner10Res = await sema.TryAcquire(_ownerId10);

            owner6Res.ShouldBeTrue();
            owner7Res.ShouldBeTrue();
            owner8Res.ShouldBeTrue();
            owner9Res.ShouldBeTrue();
            owner10Res.ShouldBeTrue();
        }


        [Fact]
        public async Task Should_succeed_on_release_when_not_owner()
        {
            var nowUtc = DateTime.UtcNow;
            SqlServerSemaphore sema = new SqlServerSemaphore(ConnectionString, _semaphoreName, 20, 1);
            var owner1Res = await sema.TryAcquire(_ownerId1);
            owner1Res.ShouldBeTrue();

            var owner2ReleaseRes = await sema.TryRelease(_ownerId2);
            owner2ReleaseRes.ShouldBeTrue();

            var semaInfo = await sema.GetSemaphoreInfo();
            var owners = sema.Deserialize(semaInfo.OwnersJson);
            owners.Owners.Count.ShouldBe(1);
            var owner1 = owners.Owners.Single(x => x.OwnerId == _ownerId1);
            owner1.ExpiresAtUtc.ShouldBeGreaterThan(DateTime.UtcNow);
        }


    }
}
