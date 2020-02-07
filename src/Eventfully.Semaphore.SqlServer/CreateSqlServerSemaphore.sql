CREATE TABLE [dbo].[__Semaphores](
	[Name] nvarchar(200) NOT NULL,
	[Owners] [nvarchar](max) NULL,
	[RowVersion] [rowversion] NOT NULL,
    CONSTRAINT [PK___Semaphores] PRIMARY KEY CLUSTERED([Name] ASC)
)