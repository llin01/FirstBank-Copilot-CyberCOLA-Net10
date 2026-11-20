USE [NCARD]   -- test database; change for other environments
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
/**********************************************************************************
 * P1 rollback: restore the original multi-statement versions of dbo.FnGetAuthCheckSw
 * and dbo.FnGetRspCode, as read from NCARD with sp_helptext on 2026-11-17.
 * Use only if P1_03_cutover.sql was applied and has to be undone. Compare with the
 * live definition on other databases before using it there.
 **********************************************************************************/
SET XACT_ABORT ON;
GO
BEGIN TRANSACTION;
GO
DROP FUNCTION IF EXISTS [dbo].[FnGetAuthCheckSw];
GO
/**********************************************************************************
 * 系統名稱：COLA
 * 程式名稱：FnGetAuthCheckSw
 * 功能說明：授權檢核及原因碼
 * 撰寫日期：2022/10/07
 * 撰寫人員：AndreaChien
 * 修改日期：20240531 變更臨時控管註記定義 AndreaChien
**********************************************************************************/
CREATE
FUNCTION [dbo].[FnGetAuthCheckSw]
(	
@bu                  VARCHAR(5),
@authCheck           VARCHAR(20),
@qtrlyAffiliate      VARCHAR(01)
)
RETURNS @AuthCheckRsp TABLE(
  authcheck      varchar(1),
  resp           varchar(50)
)
AS
BEGIN
DECLARE @datetime       varchar(20),
		@authCheckSw    varchar(1)='Y',
		@resp_#39       varchar(20)='',
		@resp_innser    varchar(20)='',
		@resp	        varchar(50)='00050701;011012013014'
   
   SELECT @datetime=CONVERT(VARCHAR(20),GETDATE(),112)
 
   SELECT  @authCheckSw = CASE WHEN AUTH_CHECK_SW = 'T' AND EFF_START_DTE <= @datetime AND EFF_END_DTE >= @datetime THEN 'Y'   --20240531
                               WHEN AUTH_CHECK_SW = 'S' AND EFF_START_DTE <= @datetime AND EFF_END_DTE >= @datetime THEN 'N'   --20240531
							   WHEN AUTH_CHECK_SW = 'S' AND ( EFF_START_DTE > @datetime OR EFF_END_DTE < @datetime ) THEN 'Y'  --20240531
							   WHEN AUTH_CHECK_SW = 'Y' THEN 'Y'
							   WHEN AUTH_CHECK_SW = 'N' THEN 'N'
			                   ELSE 'N' END,
							   @resp_#39 = CASE @qtrlyAffiliate  WHEN 'V' THEN RESP_VISA 
																 WHEN 'M' THEN RESP_MASTERCARD
																 WHEN 'J' THEN RESP_JCB
																 ELSE RESP_NCCC END, 
							   @resp_innser = RESP_INNER 
     FROM  dbo.COLA_AUTH_CHECK WITH(NOLOCK) 
    WHERE  BU=@bu AND AUTH_CHECK=@authCheck 

	SET @resp=ISNULL(@resp_#39,'00050701')+';'+ISNULL(@resp_innser,'011012013014')

    INSERT @AuthCheckRsp SELECT @authCheckSw,@resp
        
	RETURN 
	
END
GO
DROP FUNCTION IF EXISTS [dbo].[FnGetRspCode];
GO
/**********************************************************************************
 * 系統名稱：COLA
 * 程式名稱：FnGetRspCode
 * 功能說明: 取得授權內外部回應碼
 * 撰寫日期：2021/02/14
 * 撰寫人員：AndreaChien
**********************************************************************************/
CREATE
FUNCTION [dbo].[FnGetRspCode]
(	
    @inAuthRsp           varchar(01),
	@resp                varchar(50)
)
RETURNS @txRspTbl TABLE 
   (  outRsp	        varchar(02),                                                                                                                                                                                                                                                        
	  InnerRsp          varchar(03) )
AS
BEGIN
DECLARE 
        @outRsp	         varchar(02)='',                                                                                                                                                                                                                                                        
		@InnerRsp        varchar(03)='',
		@position        int=0,
		@resp_#39        varchar(20)='',
		@resp_inner      varchar(20)='',
		@interval        int =0

		SELECT @position=CHARINDEX(';', @resp, 1);  
		SET @resp_#39=SUBSTRING(@resp,1,@position-1)
		SET @resp_inner=SUBSTRING(@resp,@position+1,LEN(@resp)-@position)

		--帶入資料的呈現方式 外部回應碼 A(2)D(2)P(2)R(2) / 內部回應碼 A(3)D(3)P(3)R(3) 
	    SELECT @position=CHARINDEX(@inAuthRsp, 'ADPR', 1);  
		SET @interval=(@position-1)*2 + 1
		SET @outRsp =SUBSTRING(@resp_#39,@interval,2)
		SET @interval=(@position-1)*3 + 1
		SET @InnerRsp=SUBSTRING(@resp_inner,@interval,3)

INSERT @txRspTbl
SELECT @outRsp,@InnerRsp 

RETURN 

END
GO
IF @@TRANCOUNT > 0
BEGIN
    IF OBJECTPROPERTYEX(OBJECT_ID(N'dbo.FnGetAuthCheckSw'), 'IsTableFunction') = 1
       AND OBJECTPROPERTYEX(OBJECT_ID(N'dbo.FnGetAuthCheckSw'), 'IsInlineFunction') = 0
       AND OBJECTPROPERTYEX(OBJECT_ID(N'dbo.FnGetRspCode'), 'IsInlineFunction') = 0
        COMMIT TRANSACTION;
    ELSE
    BEGIN
        ROLLBACK TRANSACTION;
        RAISERROR(N'Rollback incomplete; rolled back to the state before this script.', 16, 1);
    END
END
GO