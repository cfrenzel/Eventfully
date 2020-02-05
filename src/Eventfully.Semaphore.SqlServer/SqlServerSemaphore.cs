using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Eventfully.Semaphore.SqlServer
{
    public class SqlServerSemaphore : ICountingSemaphore
    {

        private static ILogger<SqlServerSemaphore> _log = Logging.CreateLogger<SqlServerSemaphore>();

        public string Name { get; set; }
        public int TimeoutInSeconds { get; private set; } = 20;
        public int MaxConcurrentOwners { get; private set; } = 1;

        private readonly SqlConnectionFactory _dbConnection;

        public SqlServerSemaphore(string connectionString, string name, int timeoutInSeconds = 20, int maxConcurrentOwners = 1)
        {
            if(String.IsNullOrEmpty(name))
                throw new InvalidOperationException("SqlServer Semaphore requires a name.  Name can not be empty");
            Name = name;
            
            if (TimeoutInSeconds < 1)
                throw new InvalidOperationException("SqlServer Semaphore timeoutInSeconds must be >= 1");
            TimeoutInSeconds = timeoutInSeconds;

            if (maxConcurrentOwners < 1)
                throw new InvalidOperationException("SqlServer Semaphore maxConcurrentOwners must be >= 1");
            MaxConcurrentOwners = maxConcurrentOwners;

            if (String.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("SqlServer Semaphore requires a connectionString.  ConnectionString can not be null");
            _dbConnection = new SqlConnectionFactory(connectionString);

        }

        public async Task<bool> TryAcquire(string ownerId)
        {
            try
            {
                var semInfo = await GetSemaphoreInfo(true);//create if not exists
                var owners = Deserialize(semInfo.OwnersJson);
                owners.RemoveExpired();
                if (owners.Add(ownerId, this.MaxConcurrentOwners, this.TimeoutInSeconds))
                {
                    if (await _updateSemaphoreInfo(semInfo.RowVerion, owners))
                        return true;
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Exception acquiring SqlServer Semaphore: {SemaphoreName}, OwnerId: {OwnerId}", this.Name, ownerId);
            }
            return false;
        }

        public async Task<bool> TryRenew(string ownerId)
        {
            try
            {
                var semInfo = await GetSemaphoreInfo(true);//create if not exists
                var owners = Deserialize(semInfo.OwnersJson);
                owners.RemoveExpired();
                if (owners.Renew(ownerId, this.MaxConcurrentOwners, this.TimeoutInSeconds))
                {
                    if (await _updateSemaphoreInfo(semInfo.RowVerion, owners))
                        return true;
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Exception renewing SqlServer Semaphore: {SemaphoreName}, OwnerId: {OwnerId}", this.Name, ownerId);
            }
            return false;
        }

        public async Task<bool> TryRelease(string ownerId)
        {
            try
            {
                var semInfo = await GetSemaphoreInfo();
                var owners = Deserialize(semInfo.OwnersJson);
                owners.RemoveExpired();
                if (owners.Release(ownerId))
                {
                    if (await _updateSemaphoreInfo(semInfo.RowVerion, owners))
                        return true;
                    else
                        return false;
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Exception releasing SqlServer Semaphore: {SemaphoreName}, OwnerId: {OwnerId}", this.Name, ownerId);
            }
            return true;
        }



        public class SemaphoreOwners
        {
            public string Name { get; set; }
            public List<SemaphoreOwner> Owners { get; set; } = new List<SemaphoreOwner>();
            public SemaphoreOwners() { }
            public void RemoveExpired(DateTime? utcNow = null)
            {
                DateTime now = utcNow ?? DateTime.UtcNow;
                Owners.RemoveAll(x => x.ExpiresAtUtc <= now);
            }

            public bool Add(string ownerId, int maxConcurrentOwners, int timeoutInSeconds, DateTime? utcNow = null)
            {
                DateTime now = utcNow ?? DateTime.UtcNow;
                var owner = Owners.SingleOrDefault(x => x.OwnerId.Equals(ownerId));
                if (owner != null)
                    return Renew(ownerId, maxConcurrentOwners, timeoutInSeconds, now);

                if (this.Owners.Count < maxConcurrentOwners)
                {
                    this.Owners.Add(new SemaphoreOwner()
                    {
                        OwnerId = ownerId,
                        ExpiresAtUtc = now.AddSeconds(timeoutInSeconds)
                    });
                    return true;
                }
                return false;
            }

            public bool Renew(string ownerId, int maxConcurrentOwners, int timeoutInSeconds, DateTime? utcNow = null)
            {
                DateTime now = utcNow ?? DateTime.UtcNow;
                var owner = Owners.SingleOrDefault(x => x.OwnerId.Equals(ownerId));
                if (owner == null)
                    return Add(ownerId, maxConcurrentOwners, timeoutInSeconds, now);
                owner.ExpiresAtUtc = now.AddSeconds(timeoutInSeconds);
                return true;
            }

            public bool Release(string ownerId)
            {
                var owner = Owners.SingleOrDefault(x => x.OwnerId.Equals(ownerId));
                if (owner != null)
                {
                    Owners.Remove(owner);
                    return true;
                }
                return false;
            }

            public class SemaphoreOwner
            {
                public string OwnerId { get; set; }
                public string OwnerDescription { get; set; }
                public DateTime ExpiresAtUtc { get; set; }
                public SemaphoreOwner() { }
            }
        }

        public struct SemaphoreInfo
        {
            public string Name { get; set; }
            public string OwnersJson { get; set; }
            public byte[] RowVerion { get; set; }
        }

        public async Task<SemaphoreInfo> GetSemaphoreInfo(bool createIfNotExists = false)
        {
            bool exists;
            using (var conn = _dbConnection.Get())
            {
                var sql = @"SELECT [Name], [Owners], [RowVersion] 
                    FROM __Semaphores
                    WHERE [Name] = @SemaphoreName
                    ";
                conn.Open();
                DbCommand command = conn.CreateCommand();
                command.CommandText = sql;
                SqlParameter nameParam = new SqlParameter("@SemaphoreName", this.Name)
                {
                    SqlDbType = SqlDbType.NVarChar,
                    Direction = ParameterDirection.Input,
                };
                command.Parameters.Add(nameParam);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    reader.Read();
                    if (exists = reader.HasRows)
                    {
                        var name = reader.GetString(0);
                        var ownersJson = reader.GetString(1);
                        var rowVersion = reader.GetFieldValue<byte[]>(2);
                        return new SemaphoreInfo() { Name = name, OwnersJson = ownersJson, RowVerion = rowVersion };
                    }
                }
            }

            if (!exists && createIfNotExists)
                return await CreateSemaphore();

            throw new ApplicationException("Error getting semaphore info");
        }

        public async Task<SemaphoreInfo> CreateSemaphore()
        {

            using (var conn = _dbConnection.Get())
            {
                var sql = @"INSERT INTO [dbo].[__Semaphores]([Name] ,[Owners]) VALUES  (@SemaphoreName, @Owners) ";
                conn.Open();
                DbCommand command = conn.CreateCommand();
                command.CommandText = sql;
                SqlParameter nameParam = new SqlParameter("@SemaphoreName", this.Name)
                {
                    SqlDbType = SqlDbType.NVarChar,
                    Direction = ParameterDirection.Input,
                };
                command.Parameters.Add(nameParam);

                SqlParameter ownersParam = new SqlParameter("@Owners", Serialize(new SemaphoreOwners() { Name = this.Name }))
                {
                    SqlDbType = SqlDbType.NVarChar,
                    Direction = ParameterDirection.Input,
                };
                command.Parameters.Add(ownersParam);
                int rows = await command.ExecuteNonQueryAsync();
                if (rows == 1)
                    return await GetSemaphoreInfo();
                else
                    throw new ApplicationException("Error creating semaphore");
            }
        }

        /// <summary>
        /// Use optimistic concurrency to update the  semaphore owners
        /// </summary>
        /// <param name="origRowVersion"></param>
        /// <param name="owners"></param>
        /// <returns></returns>
        private async Task<bool> _updateSemaphoreInfo(byte[] origRowVersion, SemaphoreOwners owners)
        {
            using (var conn = _dbConnection.Get())
            {
                var sql = @"
                    UPDATE __Semaphores SET [Owners] = @Owners 
                    WHERE [Name] = @SemaphoreName AND 
                    [RowVersion] = @RowVersion                   
                    ";

                conn.Open();
                DbCommand command = conn.CreateCommand();
                command.CommandText = sql;
                SqlParameter nameParam = new SqlParameter("@SemaphoreName", this.Name)
                {
                    SqlDbType = SqlDbType.NVarChar,
                    Direction = ParameterDirection.Input,
                };
                SqlParameter ownersParam = new SqlParameter("@Owners", Serialize(owners))
                {
                    SqlDbType = SqlDbType.NVarChar,
                    Direction = ParameterDirection.Input,
                };
                command.Parameters.Add(ownersParam);
                command.Parameters.Add(nameParam);
                SqlParameter versionParam = new SqlParameter("@RowVersion", origRowVersion)
                {
                    SqlDbType = SqlDbType.Timestamp,
                    Direction = ParameterDirection.Input,
                };
                command.Parameters.Add(versionParam);

                int rows = await command.ExecuteNonQueryAsync();
                if (rows == 1)
                    return true;

                return false;
            }
        }

        public SemaphoreOwners Deserialize(string ownersJson)
        {
            if (String.IsNullOrEmpty(ownersJson))
                return new SemaphoreOwners() { Name = this.Name };
            return (SemaphoreOwners)JsonConvert.DeserializeObject<SemaphoreOwners>(ownersJson);
        }

        public string Serialize(SemaphoreOwners owners)
        {
            return JsonConvert.SerializeObject(owners);
        }


    }
}
