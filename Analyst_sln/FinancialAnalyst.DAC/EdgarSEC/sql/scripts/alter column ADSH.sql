﻿
ALTER TABLE EdgarDatasetSubmissions ALTER COLUMN ADSH VARCHAR(20) COLLATE SQL_Latin1_General_CP1_CS_AS NOT NULL

CREATE UNIQUE NONCLUSTERED INDEX [IX_ADSH] ON [dbo].[EdgarDatasetSubmissions]
(
	[ADSH] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]


