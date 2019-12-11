

set nocount on

declare @SetSize int = 1000
declare @SetIndex int = 1
declare @CurrentId uniqueidentifier

while @SetIndex < @SetSize
begin
    set @CurrentId = NEWID()
    insert into  [OutboxEvents] (Id, PriorityDateUtc, TryCount, [Type], [Status], CreatedAtUtc)
		select @CurrentId, DATEADD(second, 15, GETUTCDATE()), 0,  'Test.Entity.' + CONVERT(varchar(10), @SetIndex), 0, GETUTCDATE()
     
    insert into [OutboxEventData] (Id, [Data]) 
	select @CurrentID, REPLICATE('X', RAND() * 16000)

    set @SetIndex = @SetIndex + 1
end
--delete from OutboxEvents
--select * from OutboxEvents order by PriorityDateUtc asc


DECLARE @runAt0 AS TIME = '12:06:00'
DECLARE @nextRun AS NVARCHAR(8) = CONVERT(nvarchar(8), @runAt0, 108);
DECLARE @BatchSize AS INT = 5
DECLARE @Counter AS INT =  5
DECLARE @CurrentDateUtc DateTime
	
WAITFOR TIME @nextRun

WHILE @Counter < 10
BEGIN
    SET @CurrentDateUtc = GETUTCDATE()
    SET @Counter = @Counter + 1
	SET @runAt0 = DATEADD(SECOND, 5, @runAt0);

	with NextBatch as (
		select top(@BatchSize) *
		from OutboxEvents with (rowlock, readpast)
		where [Status] = 0 and PriorityDateUtc <= @CurrentDateUtc
		order by PriorityDateUtc
	)
    update NextBatch SET [Status] = 1, TryCount = NextBatch.TryCount + 1 
	OUTPUT inserted.Id, inserted.PriorityDateUtc, inserted.[Type], inserted.TryCount, inserted.[Status], inserted.ExpiresAtUtc, inserted.CreatedAtUtc, od.Id, od.[Data]
	FROM NextBatch
	INNER JOIN OutboxEventData od
		ON NextBatch.Id = od.Id

	set @nextRun = CONVERT(nvarchar(8), @runAt0, 108);
	WAITFOR TIME @nextRun

END




CREATE TABLE [dbo].[OutboxEvents](
    [Id] [int] IDENTITY(1,1) NOT NULL,
    [PriorityDateUtc] [datetime] NOT NULL,
	[TryCount] [int] default(0) NOT NULL,
    [Title] [nvarchar](255) NOT NULL,
    [Status] [int] NOT NULL,
 CONSTRAINT [PK_OutboxEvents] PRIMARY KEY CLUSTERED
(
    [Id] ASC
))
GO

CREATE NONCLUSTERED INDEX [IX_PriorityDateUtc] ON [dbo].[OutboxEvents]
(
    [PriorityDateUtc] ASC,
    [Status] ASC,
	[TryCount] ASC
)
INCLUDE ( [Title]) 


CREATE TABLE [dbo].[OutboxEventData](
    [Id] [int] NOT NULL,
    [TextData] [nvarchar](max) NOT NULL,
) ON [PRIMARY]

GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_OutboxEventData] ON [dbo].[OutboxEventData]
(
    [Id] ASC
)
GO


