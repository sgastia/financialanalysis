﻿CREATE PROCEDURE dbo.SP_EDGARDATASETNUMBER_INSERT
	@DDate datetime
    ,@CountOfNumberOfQuarters int
    ,@IPRX smallint
    ,@Value float
    ,@FootNote nvarchar(512)
    ,@FootLength smallint
    ,@NumberOfDimensions smallint
    ,@CoRegistrant nvarchar(256)
    ,@durp real
    ,@datp real
    ,@Decimals int
    ,@Dimension_Id int
    ,@Submission_Id int
    ,@Tag_Id int
	,@LineNumber int
	,@EdgarDataset_Id int
AS
BEGIN

	if(
		not exists(
			select 1
			from [dbo].[EdgarDatasetNumbers] 
			where [Dimension_Id] = @Dimension_Id
			   and [Submission_Id]= @Submission_Id
			   and [Tag_Id]=@Tag_Id
			   and [EdgarDataset_Id]=@EdgarDataset_Id
		)
	) 
	begin
		Begin transaction;
			INSERT INTO [dbo].[EdgarDatasetNumbers]
			   ([DDate]
			   ,[CountOfNumberOfQuarters]
			   ,[IPRX]
			   ,[Value]
			   ,[FootNote]
			   ,[FootLength]
			   ,[NumberOfDimensions]
			   ,[CoRegistrant]
			   ,[durp]
			   ,[datp]
			   ,[Decimals]
			   ,[Dimension_Id]
			   ,[Submission_Id]
			   ,[Tag_Id]
			   ,[LineNumber]
			   ,[EdgarDataset_Id])
			 VALUES
			 (
				@DDate
				,@CountOfNumberOfQuarters
				,@IPRX
				,@Value
				,@FootNote
				,@FootLength
				,@NumberOfDimensions
				,@CoRegistrant
				,@durp
				,@datp
				,@Decimals
				,@Dimension_Id
				,@Submission_Id
				,@Tag_Id
				,@LineNumber
				,@EdgarDataset_Id);

			update DBO.EdgarDatasets SET ProcessedNumbers = ProcessedNumbers + 1 where id = @EdgarDataset_Id;
		commit transaction;
	end
END