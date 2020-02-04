CREATE TABLE [dbo].[__Semaphores](
	[Name] nvarchar(50) NOT NULL,
	[Owners] [nvarchar](4000) NULL,
	[RowVersion] [rowversion] NOT NULL,
    CONSTRAINT [PK___Semaphores] PRIMARY KEY CLUSTERED([Name] ASC)
)