
In order to use the SqlServerSemaphore you must have the __Sempahores table in your database

CREATE TABLE [dbo].[__Semaphores](
	[Name] nvarchar(200) NOT NULL,
	[Owners] [nvarchar](max) NULL,
	[RowVersion] [rowversion] NOT NULL,
    CONSTRAINT [PK___Semaphores] PRIMARY KEY CLUSTERED([Name] ASC)
)