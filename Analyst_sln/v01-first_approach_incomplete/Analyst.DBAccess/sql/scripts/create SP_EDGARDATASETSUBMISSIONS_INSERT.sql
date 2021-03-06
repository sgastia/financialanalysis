-- ================================================
-- Template generated from Template Explorer using:
-- Create Procedure (New Menu).SQL
--
-- Use the Specify Values for Template Parameters 
-- command (Ctrl-Shift-M) to fill in the parameter 
-- values below.
--
-- This block of comments will not be included in
-- the definition of the procedure.
-- ================================================
/*
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
*/
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
/*
IF OBJECT_ID ( 'SP_EDGARDATASSUBMISSIONS_INSERT', 'P' ) IS NOT NULL   
    DROP PROCEDURE SP_EDGARDATASSUBMISSIONS_INSERT;  
GO
*/  
CREATE PROCEDURE dbo.SP_EDGARDATASSUBMISSIONS_INSERT
	@ADSH nvarchar(max)
	,@Period datetime
	,@Detail bit
	,@XBRLInstance nvarchar(max)
	,@NumberOfCIKs int
	,@AdditionalCIKs nvarchar(max)
	,@PublicFloatUSD real
	,@FloatDate datetime
	,@FloatAxis nvarchar(max)
	,@FloatMems int
	,@LineNumber int
	,@Form_id int
	,@Registrant_Id int
	,@EdgarDataset_Id int
AS
BEGIN
	IF(
		NOT EXISTS(
			SELECT 1
			FROM [EdgarDatasetSubmissions]
			WHERE 1=1
				AND [ADSH] =@ADSH
				AND [DatasetId] =@EdgarDataset_Id
				AND [LineNumber] = @LineNumber
		)
	)
	BEGIN

		Begin transaction;
			INSERT INTO [dbo].[EdgarDatasetSubmissions]
				   ([ADSH]
				   ,[Period]
				   ,[Detail]
				   ,[XBRLInstance]
				   ,[NumberOfCIKs]
				   ,[AdditionalCIKs]
				   ,[PublicFloatUSD]
				   ,[FloatDate]
				   ,[FloatAxis]
				   ,[FloatMems]
				   ,[LineNumber]
				   ,[SECFormId]
				   ,[RegistrantId]
				   ,[DatasetId])
			 VALUES
				   (@ADSH
					,@Period
					,@Detail
					,@XBRLInstance
					,@NumberOfCIKs
					,@AdditionalCIKs
					,@PublicFloatUSD
					,@FloatDate
					,@FloatAxis
					,@FloatMems
					,@LineNumber
					,@Form_id
					,@Registrant_Id
					,@EdgarDataset_Id);
		
			UPDATE DBO.EdgarDatasets SET ProcessedSubmissions = ProcessedSubmissions+ 1 WHERE ID= @EdgarDataset_Id
		
		Commit transaction;
	END
END
--GO


