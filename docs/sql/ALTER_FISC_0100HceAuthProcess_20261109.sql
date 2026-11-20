USE [NCARD]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
/**********************************************************************************
 * 系統名稱：COLA
 * 程式名稱：FISC_0100HceAuthProcess
 * 功能說明：FISC Messagetype =0100/0200 Process For Hce
 * 撰寫日期：2023/10/08
 * 撰寫人員：AndreaChien
 * 修改日期：20240502 Cola_MatchUpdTxAuth SP 異動 AndreaChien
 * 維護人員：20240717 修改退貨註記判斷規則 AndreaChien
 * 維護人員：20240730 修改附卡取得VIP_FLAG的判斷規則 AndreaChien
 * 修改日期：20241008 客製MCC限額參數通過不檢核限額限次 AndreaChien
 * 維護人員: 20250206 臨調區間專款專用名單檢核, MCC CODE符合6300/9311時,累加金額 AndreaChien
 * 修改日期：20251119 2025-020 客制MCC參數增加總月限額超額倍數設定 AndreaChien
 * 維護人員: 20251202 環匯特店客製判斷
 * 維護人員：20261109 F43 結尾為空白時 COUNTRY_CODE 為 NULL 導致寫入失敗(RC 05)修正
**********************************************************************************/
ALTER
    PROCEDURE [dbo].[FISC_0100HceAuthProcess]
        @tranlogKey                       	varchar(22), 	      	--tranlogKey
		@isoMessageType                 	varchar(04), 	      	--MessageType
		@isoP2CardNbr                   	varchar(16), 	      	--#2
		@isoP3ProcessCde                	varchar(06), 	      	--#3
		@isoP4Amt                       	decimal(15,3),      	--#4
		@isoP6Amt                       	decimal(15,3),      	--#6
		@isoP7TxDte                     	varchar(04),	      	--#7 MMDD
		@isoP7TxTime                    	varchar(06),	      	--#7  HHMMSS
		@isoP10ConverRte                	varchar(08),	      	--#10  Convert rate
		@isoP11TraceNo                  	varchar(06),	      	--#11 Trace no
		@isoP18MccCode                  	varchar(04),	      	--#18
		@isoP19AcqCountry               	varchar(03),	      	--#19 Acq Country Code
		@isoP22Em                       	varchar(03),	      	--#22
		@isoP32BinIca                   	varchar(11),	      	--#32  收單行代碼
		@isoP35ExpirDte                 	varchar(04),	      	--#35/#14 EXPIR_DTE 有效期限
		@isoP35Svc							varchar(03),	      	--#35 SVC
		@isoP35Pvv                      	varchar(04),	      	--#35 PVV
		@isoP35Pvki                     	varchar(01),	      	--#35 PVKI
		@isoP37RefNbr                   	varchar(12),	      	--#37 Rerefence Number
		@isoP38AuthCode                 	varchar(06)  	OUTPUT,	--#38
		@isoP39Rsp                      	varchar(02)  	OUTPUT,	--#39
		@isoP39RspAction					varchar(01)		OUTPUT,	--對映於P39的授權回覆動作 A/D/R/P(initial 時請放null即可)
		@isop41TermNbr                  	varchar(16),	      	--#41  終端機編號
		@isoP42Merchant                 	varchar(16),	      	--#42  Merchant Number
		@isoP43MerInfo                  	varchar(40),	      	--#43  Merchant Information
		@isoP48T42                      	varchar(01),	      	--#48  VISA/JCB ECI Index
		@isoP48T43                      	varchar(40),	      	--#48  CAVV Data
		@isoP48T45                      	varchar(01)  	OUTPUT,	--#48  CAVV Result
		@isoP49CurrencyCode             	varchar(04), 	      	--#49  Acq Currency code
		@isoP51BillingCurrency          	varchar(03), 	      	--#51  Billing Currency Code
		@isoP58T21LoyaltyNbr  				varchar(06),			--紅利折抵編號
		@isoP58T21LoyaltyInd  				varchar(01) 	OUTPUT,	--紅利註記
		@isoP58T21TxResp      				varchar(02) 	OUTPUT,	--紅利回覆碼
		@isoP58T21SignPoint        			varchar(01) 	OUTPUT,	--<紅利, 餘點正負項>
		@isoP58T21PointBalance     			varchar(10) 	OUTPUT,	--<紅利, 餘點>
		@isoP58T21PointRedemption  			varchar(10) 	OUTPUT,	--<紅利,折扺點數>
		@isoP58T21PaidCAmt    				varchar(12) 	OUTPUT,	--<紅利,實際支付金額>
		@isoP58T32Tpan            			varchar(20),    		--HCE 卡號
		@isoP58T32ExpDat          			varchar(04),     		--HCE 效期
		@isoP58T32AcVeryRslt				varchar(02),     		--HCE AC RESULT
		@isoP58T32FraudChkRslt				varchar(02),     		--HCE Fraud Check Result
		@isoP61VisaAcctVfy       			varchar(01),	  		--#61  Visa Acctount Verify
		@isoS95ReplaceAmt               	decimal(15,3),      	--#95  Replement Amt
		@isoS112InstTxInd               	varchar(01), 	      	--#112 分期註記
		@isoS112InstPeriod              	varchar(02), 	      	--#112 分期期數
		@isoS112DownPayment             	varchar(08)  	OUTPUT,	--#112 首期期金
		@isoS112InstPayment             	varchar(08)  	OUTPUT,	--#112 每期期金
		@isoS112InstFee              	varchar(06)  	OUTPUT,	--#112 手續費
		@isoS112InstTxResp              	varchar(02)  	OUTPUT 	--#112 分期回應碼

    AS

	  DECLARE
	            @datetime                 datetime,
	            @authCheckFlag            varchar(01)='N',
				@bu                       varchar(03)='007',
				@product                  varchar(05)='00000',
				@currency                 varchar(03)='000',
				@acctNbr                  varchar(20)='0000000',
				@cardNbr                  varchar(20)='',
				@cardProduct              varchar(05)='00000',
				@cardholderNbr            varchar(20)='0000000',
				--SYNC NCCC FIELD
				@isoP25CondCode          varchar(02)='',   --#25
				@isoP43MerName           varchar(22),	   --#43  特店名稱
				@isoP43MerCity           varchar(13),      --#43  特店城市
				@isoP43LocCountry        varchar(02),      --#43  Country Code 國別
				@isoS127Pws              varchar(06),      --#127 開卡密碼
				@isoS127Id               varchar(10),      --#127 身份証字號
				@tC5InstTxInd			 varchar(01),      --#57 TOKEN C5
				@t04ErrorFlg			 varchar(01),
				@t04CrdVeryFlg           varchar(01),
				--卡人基本資料
				@cycle                    varchar(02)='00',
				@custNbr                  varchar(20)='0000000',
				@ctlcode                  varchar(01)='',
				@delq                     int=0,
				@authClass				  varchar(10)='',   --授權等級(卡人VIP_FLAG)
				@class                    varchar(10)='',   --信用等級
				@tempClass                varchar(10)='',   --臨時信用等級
				@cashClass				  varchar(10)='',   --預現等級
				@cardholderdob            varchar(08)='',
				@cardholderdobYYY         varchar(03)='',
				@acctPermLimit            decimal(13,2)=0,
				@acctOverLimit            decimal(13,2)=0,
				@acctCshOverLim           decimal(13,2)=0,
				@acctTempLimit            decimal(13,2)=0,
				@acctCreditLimit          decimal(13,2)=0,
				@acctCreditAvail          decimal(13,2)=0,
				@acctCshLimit             decimal(13,2)=0,
				@acctCshAvail             decimal(13,2)=0,
				@acctTempLimtFlag         varchar(01)='',
				@acctExpendAmt            decimal(13,2)=0,
				@acctCshExpendAmt         decimal(13,2)=0,
				@OverPmt                  decimal(13,2)=0,
				@payInMini                varchar(01)='',
				@stmtBal				  numeric(13,2)=0,
				@cardholderCellPhone1     varchar(20)='',
				@cardholerIdNcccfy        varchar(01)='',
				--卡片基本資料
				@cardissDte               datetime,
				@cardCtl                  varchar(01)='',
				@npFlag                   varchar(01)='',
				@openFlag                 varchar(01)='',
				@expirDte                 varchar(04)='',
				@expirDteLast             varchar(04)='',
				@prevOpen                 varchar(01)='',
				@icCardConvDate           datetime,
				@pinOffset                varchar(04)='',
				@cardpinCnt               decimal(5,0)=0,
				@cardAtcCnt               decimal(13,2)=0,
				@cardActCntPrev           decimal(13,2)=0,
				@hsrcPinErr               decimal(5,0)=0,
				@cardOverLimit            decimal(13,2)=0,
				@cardTempLimit            decimal(13,2)=0,
                @cardPermLimit            decimal(13,2)=0,
				@cardCshOverLim           decimal(13,2)=0,
				@cardCreditLimit          decimal(13,2)=0,
				@cardCreditAvail          decimal(13,2)=0,
				@cardCshLimit             decimal(13,2)=0,
				@cardCshAvail             decimal(13,2)=0,
				@cardTempLimitFlag        varchar(01)='',
				@cardExpendAmt            decimal(13,2)=0,
				@cardCshExpendAmt         decimal(13,2)=0,
				@custId                   varchar(10)='',
				@frGpParm				  varchar(01)='D',   --海外分群註記
				@frGpParmLimit	          numeric(17,2)=0,   --海外分群月限額
				@crdLinkBankNbr			  varchar(20)='',    --VD連結銀行帳戶
				@crdLinkBankSeq			  varchar(02)='',    --VD連結帳戶序號
				--授權檢核參數(來源:File)
				--產品管理參數
				@icSw					  varchar(01)='',    --晶片卡註記
				@dcFlag                   varchar(01)='',    --雙幣卡註記
				@vdFlag					  varchar(01)='',    --DEBIT註記
				@businessCrdFlag		  varchar(01)='',    --公司卡註記
				@icParm                   varchar(10)='',    --晶片參數
				@openNcccSw               varchar(01)='',    --開卡是否刪除NEG
				@authSw                   varchar(01)='N',   --授權註註記
				@productVipFlag			  varchar(01)='N',   --產品VIP註記
				@rebateRate               numeric(5,4)=1,    --產品是否允許特店紅利折抵
				@caRate					  numeric(5,4)=1,    --產品是否允許預現
				@instRate                 numeric(5,4)=1,    --產品/卡人等級是否允許分期
				@productLimitParm         varchar(10)='',
				--Token資料變數
				@tokenParm                varchar(10)='',
				@foundTknCard             varchar(1)='N',
				@tknExpirDte              datetime,
				@TokenCtlCode             varchar(01)='',    --Token CTL_CODE
				@TokenStatus              varchar(02)='',    --Token STATUS
				--卡人等級參數
				@opResp                   varchar(01)='D',  --卡人等級設定:未開卡回應
				@ctlSpaceRsp              varchar(01)='',   --卡人等級設定:控制代碼為空白時授權回覆碼
				@ctlDollarRsp             varchar(01)='D',  --卡人等級設定:過Grace Day未繳足最低
				@ctlCodeRsp               varchar(40)='',   --卡人等級設定:依控制代碼(1-9,A-Z)設定授權回覆碼
				@delqrep                  varchar(08)='',   --卡人等級設定:帳齡控管
				@overLimitRate            decimal(5,4)=1,   --卡人等級設定:可超額比例
				@classLimitParm           varchar(10)='',   --卡人等級設定:限額限次參數
				@mccParm                  varchar(10)='',   --卡人等級設定:MCC參數
				@exMccParm                varchar(10)='',   --卡人等級設定:客製MCC參數
				@frequencyParm            varchar(10)='',   --卡人等級設定:交易頻設控
				@eciCheckValue            varchar(01)='0',  --卡人等級設定:ECI 檢核值
				@eciCheckAmt              numeric(13,2)=0,  --卡人等級設定:ECI 檢核金額
				@eciCheckRsp              varchar(01)='N',  --卡人等級設定:ECI 檢核回覆碼
			    --授權共用變數
				@qtrlyAffiliate           varchar(01)='',
				@todayYYYYMMDD		      varchar(10)='',
				@logDay                   int=0,
				@monetaryFlag             varchar(01)='',
				@prevAuthOvPct            decimal(3,2)=1.15, --預先授權可超額比例
				@currencyCode             varchar(03)='901',
				@onusCountryCode		  varchar(03)='',
				@onusCurrencyCode         varchar(03)='901',
				@openNewSw                varchar(01)='',
				@foundProduct             varchar(01)='N',
				@foundCardInf             varchar(01)='N',
				@foundAcctInf             varchar(01)='N',
				@foundCustInf             varchar(01)='N',
				@foundParm                varchar(01)='N',
				@cardNotFound             varchar(01)='N',
				@respForAuthRsn           varchar(50)='',
				@cardUpdate               varchar(01)='N',
				@idCheckRspCode           varchar(02)='',
				@pinCheck                 varchar(01)='N',
				@absAmt                   decimal(12,2)=0,---授權金額
				@txInd                    varchar(02)='',
			    @adjFlag                  varchar(01)='',
			    @reversalFlag             varchar(01)='',
			    @businessFlag             varchar(02)='',
				@isoP22Em_2               varchar(02)='',
				@rspFlag                  varchar(01)='N',
				@rspInner                 varchar(03)='00',
				@reason                   varchar(05)='00',
				@matchFlag                varchar(01)='',
			    @matchSeq                 varchar(22)='',
				@matchAuthEndDte          datetime,
                @ppCheckFlag              varchar(01)='N',
				@SorF                     varchar(01)='N',
                @chkCardRsn               varchar(03)='00',
				@ErrorType                nvarchar(500)='',
				@fallbackFlag             varchar(01)='',
				@limitUpdate              varchar(01)='Y',
				--檢核關卡變數(CardStatus)
				@expirDteCheck            varchar(01)='Y',
				@openFlagCheck            varchar(01)='N',
				@ctlCodeCheckSw           varchar(01)='Y',
				--檢核關卡變數(Environment)
				@txLocation               varchar(01)='',
				@acqBankNbr               varchar(10)='',
				@acqBankType              varchar(01)='',
				@merType                  varchar(01)='',
				@merStatus                varchar(02)='',
				@mccattribe				  varchar(01)='',
				@onSiteFlag				  varchar(01)='',  --Y:實體 N:非實體
				@netWorkFlag              varchar(01)='',  --Y:網路 N:非網路
				@merDecAmt                decimal(13,2)=0,
				@merRefAmt                decimal(13,2)=0,
				@idCheckRsp				  varchar(02),
				@posStatus                varchar(02)='',
				@ecFlag                   varchar(01)='N',
				@ecSafe					  varchar(01)='',
				@idCheck                  varchar(01)='N',
				@zeroRsp                  varchar(01)='N',
				@merOverLimitRate		  numeric(5,4),
				@mervipFlag				  varchar(01),
				@mccPassFlag              varchar(01)= 'N',
				@merEmLimitControl		  varchar(20)='',
				@highRiskParmUpd		  varchar(10)='N',
				@merFallbackRsp			  varchar(01)='N',
				--檢核關卡變數(CardStatus)
				@eci                      varchar(01)='',
				@ecRej2ChgPhone           varchar(01)='N',      --手機號變更且在控管期間內
				--檢核關卡(卡片需求控管客製)
				@todayPmt   			 numeric(13,2)=0,	--當日繳款
				@todayRtnPmt   			 numeric(13,2)=0,	--當日退貨還款
				@todayRtnUpdDte	         varchar(01),       --當日退貨還款日期更新註記
				@vipFlag	  			 varchar(01)='N',	--VIP註記
				@passLimitFlag			 varchar(01)='N',	--Y:略過限額限次控管
				@cashFlag 			     varchar(01)='N',	--Y:設控不能預現 N:不設控(初始值)
				@skipEcTimes 		     varchar(01)='N',	--Y:略過EC連續交易控管 N:設控EC連續交易控管(初始值)
				@skipMerchLImit			 varchar(01)='N',	--Y:略過特店金額控管 N:設控特店金額(初始值)
				@skipFallback		     varchar(01)='N',	--Y:略過境內FALLBACK控管 N:設控境內FALLBACK控管(初始值)
				@skipFrFallback		     varchar(01)='N',	--Y:略過境外FALLBACK控管 N:設控境外FALLBACK(初始值)
				@skipFrequency		     varchar(01)='N',	--Y:略過交易頻率控管 N:設控交易頻率(初始值)
				@skipPayDayCtrl	         varchar(01)='N',	--Y:略過綁卡國外交易天數控管 N:設控綁卡國外交易天數控管(初始值)
				@skipFrGroup		     varchar(01)='N',	--Y:略過海外分群控管 N:設控海外分群(初始值)
				@skipEsvcLimit	         varchar(01)='N',	--Y:略過票證限額限次 N:設控票證限額限次(初始值)
				@skipExMCC               varchar(01)='N',	--Y:略過客製MCC N:設控客製MCC(初始值)          20251119
				@mobilePay			     varchar(01)='Y',	--Y:允許行動支付 (初始值) N:不允許行動支付
				@ecUnSecurity            varchar(01)='N',	--Y:允許境內非安全性EC N:不允許境內非安全性EC(初始值)
				@frEcUnSecurity          varchar(01)='N',	--Y:允許境外非安全性EC N:不允許境外非安全性EC(初始值)
				@tempMccTxContrl         varchar(01)='',
				@mccTxTotalAmt           decimal(13,2)=0,
				--檢核關卡變數(CreditLimit)
				@txCash                   varchar(01)='',
				@vipLimitFlag             varchar(01)='N',
				@overAllPmt               decimal(13,2)=0,
				--檢核關卡變數(TxByMcCode)
				@mccGroup                 varchar(05)='0000',
				@cardOverLimitRate        decimal(5,4)=0,
				@emGroup				  varchar(05)='0000',
				@mcc4GpID                 varchar(05)='',
				@mccTxContrl			  varchar(10)='',
				@exMccParmUpd		      varchar(10)='N', --客製MCC限額控管     Y+EM+COUNTRY N:不處理
				@mcc4ParmFound			  varchar(01)='',  --MCC4日控管          Y:處理 N:不處理
				--檢核關卡(交易頻率)
				@txFrequencyEc			  varchar(01)='',     --交易頻率 EC(國外非安全性)
				@txFrequencyExcess		  varchar(01)='',     --交易頻率 大額
				@txFrequencyFreign		  varchar(01)='',     --海外分群
				--檢核關卡變數(installment)
				@matchinStdownPay         decimal(11)=0,     --首期金額
				@matchinStPayment         decimal(11)=0,     --每期金額
				@matchinStFee             decimal(06)=0,     --分期手續費
				@matchInstTxInd           varchar(01)='',
				--檢核關卡變數(Redeem Process)
				@updBonusForce            varchar(01)='N',
				@updBonusPoint            decimal(11,0)=0,
				@matchRedeemFlag          varchar(01)='',
				@matchRedeemAmt           decimal(13,2)=0,
				@matchRedeemPoint         decimal(11)=0,
				@tC5RebateAmount          varchar(12)='',
				@rebateAmount			  decimal(13,2)=0,
				@rebatePoint			  decimal(13,2)=0,
				@rebateSPChkrsp           int=0

      BEGIN TRY

        SET NOCOUNT ON;
        SET XACT_ABORT ON;
		BEGIN TRANSACTION;

        SET @datetime = GETDATE();
		IF LEN(ISNULL(@isoP22Em,'')) >=2 SET @isoP22Em_2=LEFT(@isoP22Em,2)
        SET @isoP18MccCode  = ISNULL(@isoP18MccCode,'0000');
        SET @isoS95ReplaceAmt=ISNULL(@isoS95ReplaceAmt,0)
		SET @todayYYYYMMDD=CONVERT(varchar(10),GETDATE(),112)
		SET @qtrlyAffiliate=[dbo].[FnGetQtrlyAffiliate](@isoP2CardNbr)
		SET @isoP35ExpirDte=ISNULL(@isoP35ExpirDte,'')
		SET @isoP6Amt = ISNULL(@isoP6Amt,@isoP4Amt)
		IF  @isoP6Amt =0 SET @isoP6Amt=@isoP4Amt

		 --拆解特店資料
        --IF LEN(@isoP43MerInfo) =40
        -- LEN() ignores trailing spaces, so a 40-char F43 ending in a blank (e.g. MasterCard state 'NV ')
        -- was skipped and left @isoP43LocCountry NULL. Appending a sentinel char counts every character.
        IF LEN(@isoP43MerInfo + 'X') - 1 = 40
        BEGIN
          IF SUBSTRING(@isoP43MerInfo,1,3) ='AVR'  SET @isoS127Id = SUBSTRING(@isoP43MerInfo,4,10)

          ELSE
          IF SUBSTRING(@isoP43MerInfo,1,3) ='CVR' SET @isoS127Pws = SUBSTRING(@isoP43MerInfo,10,6)
          ELSE
			  BEGIN
			     SET @isoP43MerName = SUBSTRING(@isoP43MerInfo,1,22)
			     SET @isoP43MerCity = SUBSTRING(@isoP43MerInfo,24,12)
		         SET @isoP43LocCountry = LEFT(RTRIM(LTRIM(SUBSTRING(@isoP43MerInfo,38,3))),2)
			  END
        END

		--資料驗證代碼取得 DATA_VFY_SW
		SELECT @respForAuthRsn=resp FROM dbo.FnGetAuthCheckSw(@bu,'DATA_VFY_SW',@qtrlyAffiliate)

	    --取得卡片基本資料
	    SELECT  @foundCardInf='Y',
	            @bu=a.BU,
	            @product=a.PRODUCT,
	            @currency=a.CURRENCY,
	            @acctNbr=a.ACCT_NBR,
	            @cardNbr=a.CARD_NBR,
	            @cardholderNbr=a.CARDHOLDER_NBR,
	            @custNbr=a.CUST_NBR,
				@cardProduct=a.CARD_PRODUCT,
	            @openFlag=CASE WHEN LEN(a.OPEN_FLAG)=0 THEN '0' ELSE a.OPEN_FLAG END ,
	            @prevOpen=CASE WHEN LEN(a.PREV_OPEN)=0 THEN '0' ELSE a.PREV_OPEN END ,
	            @npFlag=a.CARD_FLAG,
				@cardAtcCnt=a.ATC,
				@cardActCntPrev=a.PREV_ATC,
				@cardpinCnt=CAST (ISNULL(a.CASH_PIN_ERROR,0) AS int),
				@hsrcPinErr=CAST (ISNULL(a.HSRC_PIN_ERROR,0) AS int),
				@expirDte=SUBSTRING(CONVERT(VARCHAR(08),a.EXPIR_DTE,112),3,4),
				@expirDteLast=SUBSTRING(CONVERT(VARCHAR(08),a.EXPIR_DTE_LAST,112),3,4),
				@pinOffset=CASE WHEN LEN(a.PIN_OFFSET) >=4  THEN a.PIN_OFFSET ELSE NULL END,
				@tempClass=CASE WHEN CONVERT(varchar(08),a.TEMP_CLASS_START_DTE,112) <= @todayYYYYMMDD AND CONVERT(varchar(08),a.TEMP_CLASS_END_DTE,112) >= @todayYYYYMMDD THEN TEMP_CLASS ELSE '' END,
				@class=a.CLASS,
				@authClass=ISNULL(c.VIP_FLAG,'0000'),
				@cardCtl=a.CTL_CODE,
				@cardissDte=a.ISSUE_DTE,
				@frGpParm=a.USER_CHAR_10,
				@frGpParmLimit=a.USER_AMT_1,
				@ecRej2ChgPhone=CASE WHEN c.USER_CHAR_10 ='Y' AND c.USER_DTE_1 >= GETDATE() THEN 'Y' ELSE @ecRej2ChgPhone END,  --手機黑名單
				@cardholderdob=CONVERT(VARCHAR(08),c.DOB,112),
				@cardholderdobYYY=DATEDIFF(yyyy,c.DOB,GETDATE()),
				@cardholderCellPhone1=c.CELL_PHONE_1
		FROM  dbo.CARD_INF a WITH(NOLOCK) INNER JOIN dbo.CUST_INF c WITH(NOLOCK) ON a.CARDHOLDER_NBR = c.CUST_NBR AND a.BU = c.BU
		WHERE a.CARD_NBR = @isoP2CardNbr

		--判斷若無對映的資料存在時,拒絕交易
	    IF ( @foundCardInf = 'N' )
	    BEGIN
	        SET @rspFlag       = 'D'
			SELECT @isoP39Rsp=outRsp,@rspInner=InnerRsp FROM [dbo].[FnGetRspCode](@rspFlag,@respForAuthRsn)
			SET @reason        = '02'
			SET @chkCardRsn    = '02'
			SET @cardNotFound  = 'Y'
	    END

	    --取得卡片的產品別
        SELECT @foundProduct='Y',
               @bu=BU,
               @openNcccSw=OPEN_DEL_NEG_SW,
               @authSw =AUTH_SW,
               @qtrlyAffiliate=LEFT(QTRLY_AFFILIATE,1),
			   @productVipFlag=VIP_FLAG,
               @productLimitParm=LIMIT_PARM,@icParm=IC_PARM,
			   @dcFlag=DC_FLAG, @vdFlag=DEBIT_SW, @businessCrdFlag=BUSINESS_SW,
			   @caRate=CASE WHEN CASH_FLAG='N' THEN 0 ELSE @caRate END,
			   @instRate=CASE WHEN INSTALLMENT_FLAG='N' THEN 0 ELSE @instRate END,
			   @rebateRate=CASE WHEN REBATE_FLAG='N' THEN 0 ELSE @rebateRate END
          FROM dbo.COLA_PACARDF WITH(NOLOCK)
         WHERE BU = @bu
           AND PRODUCT = @cardProduct

        IF ( @foundProduct = 'N' )  --無此卡別
        BEGIN
	           SET @rspFlag       = 'D'
			   SELECT @isoP39Rsp=outRsp,@rspInner=InnerRsp FROM [dbo].[FnGetRspCode](@rspFlag,@respForAuthRsn)
			   SET @reason        = '01'
			   SET @chkCardRsn    = CASE WHEN @chkCardRsn ='00' THEN '01' ELSE @chkCardRsn END
			   SET @cardNotFound  = 'Y'
	    END

	  --取得帳戶的基本資料
		SELECT  @foundAcctInf='Y',
			    @tempClass=CASE WHEN @businessCrdFlag ='Y' THEN @tempClass WHEN CONVERT(varchar(08),a.TEMP_CLASS_START_DTE,112) <= @todayYYYYMMDD AND CONVERT(varchar(08),a.TEMP_CLASS_END_DTE,112) >= @todayYYYYMMDD THEN TEMP_CLASS ELSE '' END,
				@class=CASE WHEN @businessCrdFlag ='Y' THEN @class ELSE CLASS END,
				@cycle=a.CYCLE,
				@ctlcode =a.CTL_CODE,
				@delq=CASE WHEN @dcFlag ='Y' THEN CONVERT(INT,ISNULL(b.DELQ_STATUS,0)) ELSE CONVERT(INT, LEFT(ISNULL(c.DELQ_STATUS_HIST,'00'),2)) END,
				@OverPmt=c.OVER_PMT,
				@cashClass=ISNULL(a.CASH_CLASS,'0000'),
				@payInMini=CASE WHEN c.GRACE_DTE < CONVERT(DATE, GETDATE() ) AND (c.STMT_DUE > 0 OR ISNULL(b.STMT_DUE,0) > 0 ) THEN 'N' ELSE '' END,
				@stmtBal=c.STMT_BAL
	       FROM dbo.ACCT_INF a WITH(NOLOCK)
		  INNER JOIN dbo.ACCT_BOOK c WITH(NOLOCK) ON a.ACCT_NBR=c.ACCT_NBR AND a.BU=c.BU AND a.PRODUCT=c.PRODUCT AND a.CURRENCY=c.CURRENCY
		   LEFT JOIN dbo.DC_ACCT_BOOK b on a.ACCT_NBR=b.ACCT_NBR AND a.BU=b.BU
		 WHERE  a.ACCT_NBR=@acctNbr AND a.BU=@bu AND a.PRODUCT=@product AND a.CURRENCY=@currency

		--判斷若無對映的帳戶資料時,拒絕交易
	    IF ( @foundAcctInf ='N' )
	    BEGIN
	        SET @rspFlag       = 'D'
			SELECT @isoP39Rsp=outRsp,@rspInner=InnerRsp FROM [dbo].[FnGetRspCode](@rspFlag,@respForAuthRsn)
			SET @reason        = '03'
			SET @chkCardRsn    = CASE WHEN @chkCardRsn ='00' THEN '03'  ELSE @chkCardRsn END
			SET @cardNotFound  = 'Y'
	    END

		--判斷TPAN是否存在,不存在時拒絕交易
		SELECT  @foundTknCard ='Y',
		        @tknExpirDte=EXPIR_DTE,
				@TokenCtlCode=TPAN_CTL_CODE,
				@TokenStatus=[TPAN_STATUS]
		 FROM  dbo.HCE_CARD WITH(NOLOCK)
         WHERE TPAN=@isoP58T32Tpan AND CARD_NBR =@isoP2CardNbr

		IF ( @foundTknCard = 'N' )
	    BEGIN
	           SET @isoP39Rsp = '05'
			   SET @rspInner  = '012'
			   SET @rspFlag   = 'D'
			   SET @reason    = '24'
			   SET @chkCardRsn = CASE WHEN @chkCardRsn ='00' THEN @reason ELSE @chkCardRsn END
			   SET @cardNotFound  ='Y'
	    END

		--副卡取得VIP註記判斷   20240730
		IF (@npFlag='S')																								   --20240730
		BEGIN 																											   --20240730
		     SELECT @authClass=ISNULL(VIP_FLAG,'0000') FROM dbo.CUST_INF WITH(NOLOCK) WHERE CUST_NBR= @custNbr AND BU=@bu  --20240730
		END 																											   --20240730

		--判斷最後授權等級處理
		SET @class=CASE WHEN @tempClass <> '' THEN @tempClass  WHEN @authClass <> '' AND @businessCrdFlag <> 'Y' THEN @authClass ELSE @class END

		IF NOT EXISTS(SELECT CLASS FROM dbo.COLA_PARANKF WITH(NOLOCK) WHERE BU=@bu AND CLASS=@class) SET @class ='0000'
		 --授權檢核參數
        SELECT @foundParm= 'Y',
			   @txLocation= CASE WHEN LEFT(a.COUNTRY,2)=@isoP43LocCountry THEN 'Y' ELSE 'N' END,
			   @onusCountryCode=a.COUNTRY_CODE,
			   @onusCurrencyCode=a.CURRENCY_CODE,
			   @prevAuthOvPct=a.PREV_AUTH_OV_PCT,
			   @logDay=CASE WHEN @vdFlag ='Y' AND @isoP43LocCountry='JP' THEN a.AUTH_DAY_JP WHEN @isoP18MccCode='9311' THEN a.AUTH_DAY_MCC ELSE a.AUTH_DAY END,
			   @opResp=r.OP_RESP,
			   @ctlSpaceRsp=r.CTL_SPACE_RSP,
			   @ctlDollarRsp=r.CTL_DOLLAR_RSP,
			   @ctlCodeRsp=r.CTL_CODE_RSP,
			   @delqrep=r.DELQ_REP,
			   @overLimitRate=r.OVER_LIMIT_RATE,
			   @tokenParm=ISNULL(r.TOKEN_PARM,'0000'),
			   @classLimitParm=ISNULL(r.CLASS_LIMIT_PARM,'0000'),
			   @instRate=CASE WHEN @instRate=0 THEN @instRate WHEN r.INST_FLAG ='N' THEN 0 ELSE @instRate END ,
			   @mccParm=ISNULL(r.MCC_PARM,'0000'),
			   @exMccParm=ISNULL(r.EXMCC_PARM,''),
			   @frequencyParm=ISNULL(r.TX_FREQUENCY_PARM,''),
			   @eciCheckValue=CASE @qtrlyAffiliate WHEN 'M' THEN r.UCAF WHEN 'J' THEN r.ECI_JCB WHEN 'V' THEN r.ECI_VISA ELSE '0' END,
			   @eciCheckAmt=CASE @qtrlyAffiliate WHEN 'M' THEN r.UCAF_AMT WHEN 'J' THEN r.ECI_JCB_AMT WHEN 'V' THEN r.ECI_VISA_AMT ELSE 0 END,
			   @eciCheckRsp=CASE @qtrlyAffiliate WHEN 'M' THEN r.UCAF_RSP WHEN 'J' THEN r.ECI_JCB_RSP WHEN 'V' THEN r.ECI_VISA_RSP ELSE 'N' END
         FROM  dbo.COLA_PAORGNF a WITH(NOLOCK) INNER JOIN dbo.COLA_PARANKF r WITH(NOLOCK) ON a.bu=r.bu
        WHERE a.BU=@bu
		  AND r.BU=@bu AND r.CLASS=@class

		--整理紅利及分期註記
		SET @tC5InstTxInd= CASE WHEN @isoP58T21LoyaltyInd <> '' THEN @isoP58T21LoyaltyInd ELSE @isoS112InstTxInd END
		--取得交易類別
		SELECT  @authCheckFlag=rsp,
		        @reason=reason,
		        @txInd=txInd,
		        @adjFlag=adjFlag,
		        @businessFlag=businessFlag,
		        @monetaryFlag=monetaryFlag,
		        @reversalFlag=reversalFlag,
				@matchFlag=matchFlag,
				@absAmt=calAmt,
				@matchSeq=SEQ,
				@matchAuthEndDte=origExpDte,@matchinStdownPay=inStdownPay,@matchinStPayment=inStPayment,@matchinStFee=inStFee,
				@matchInstTxInd=inStallmentFlag,
				@matchRedeemFlag=redeemFlag,@matchRedeemAmt=redeemAmt,@matchRedeemPoint=RedeemPoint,
				@txFrequencyEc=ecUnsafeUpd,@txFrequencyFreign=foreignUpd,@mcc4ParmFound=mcc4ParmUpd,@exMccParmUpd=exMccParmUpd,@merEmLimitControl=merEmUpd,
				@highRiskParmUpd=highRiskUpd
				FROM [dbo].[FnGetFiscDomestic01PType]
				(@isoMessageType,@isoP2CardNbr,@isoP3ProcessCde,@isoP6Amt,@isoP18MccCode,@isoP25CondCode,@isoP38AuthCode,
				 @isoP43LocCountry,@isoS95ReplaceAmt,@isoP58T21LoyaltyInd,'',@isoP61VisaAcctVfy,@isoS112InstTxInd)
																					--@isop58t69

  		IF (@authCheckFlag <> 'N' )
		BEGIN
			SET @rspFlag    = 'D'
			SELECT @isoP39Rsp=outRsp,@rspInner=InnerRsp FROM [dbo].[FnGetRspCode](@rspFlag,@respForAuthRsn)
		END

	    --取得交易類別--預現/一般, MCC CODE屬性
		SELECT @txCash=cash,@mccAttribe=attribe FROM [dbo].[FnGetCashMcc](@bu,ISNULL(@isoP18MccCode,'0000'),@txInd)
		--實體非實體/網路判斷
	    SELECT @onSiteFlag=onSiteFlag,@emGroup=emGroup,@netWorkFlag=netWorkFlag FROM dbo.FnGetEMGroup(@bu,@isoP22Em_2)
		--取得收單銀行別
		SELECT @acqBankNbr =acqbank,@txLocation=txLocation,@acqBankType=acqBankType FROM dbo.FnGetAcqBank(@bu,LTRIM(RTRIM(@isoP32BinIca)),@txLocation)
		--取得MCC Group
	    SET @mccGroup=dbo.FnGetMccGroup(@bu,@mccParm,ISNULL(@isoP18MccCode,''))
		-- 國別碼判斷
		IF  ( LEN(@isoP19AcqCountry) = 3  AND @isoP19AcqCountry != @onusCountryCode )
        OR  ( LEN(@isoP49CurrencyCode)=3  AND @isoP49CurrencyCode != @onusCurrencyCode )
        OR  ( @txLocation ='N' )
			SET @txLocation= N'N'
		ELSE
		BEGIN
			SET @isoP49CurrencyCode=ISNULL(@isoP49CurrencyCode,@onusCurrencyCode)
			SET @txLocation = N'Y'
		END

		--判斷檔案是否存在
		EXEC Cola_InsertCheckFIl @tranLogKey,@bu,@acctNbr,@product,@currency,@custNbr,@cardNbr,@cardProduct,@cardholderNbr,@cycle,@mccGroup,@authCheckFlag OUTPUT
        IF (@authCheckFlag <> 'N')
        BEGIN
		    SET @rspFlag ='D'
			SELECT @isoP39Rsp=outRsp,@rspInner=InnerRsp FROM [dbo].[FnGetRspCode](@rspFlag,@respForAuthRsn)
			SET @reason=CASE WHEN @chkCardRsn ='00' THEN '04' ELSE @chkCardRsn END
			GOTO AuthRsp
		END

		IF (@cardNotFound ='Y')
		BEGIN
		 SET  @reason = CASE WHEN @chkCardRsn ='00' THEN @reason ELSE @chkCardRsn END
		 GOTO AuthRsp
		END

	  --判斷若不授權時,拒絕交易
	    IF ( @foundParm = 'N' OR @authSw = 'N' )
	    BEGIN
	        SET @rspFlag   = 'D'
			SET @reason    = '05'
			SELECT @isoP39Rsp=outRsp,@rspInner=InnerRsp FROM [dbo].[FnGetRspCode](@rspFlag,@respForAuthRsn)
			GOTO AuthRsp
	    END

		--公司卡不能環匯分期 20251202
		IF ( @bu ='003' AND LEFT(@isoP42Merchant,3) = '958')
		BEGIN
	   	      SELECT @rspFlag=rsp,@isoP39Rsp=rspOut,@rspInner=reason,@reason=checkInd
			    FROM [dbo].[FnCheckGlPayWithout003](@bu,@isoP42Merchant,@isoP43LocCountry,@qtrlyAffiliate)

			  IF @rspFlag IN ('D','P','R') GOTO AuthRsp
		END
		--公司卡不能環匯分期 20251202

		IF LEFT(@isoP3ProcessCde,2) = '20'
		BEGIN
		    --Redeem Check
			IF (@businessFlag ='BN')
			BEGIN
				  EXEC [dbo].[Cola_RedeemProcess] @tranlogkey,@isoMessageType,@isoP3ProcessCde,@isoS95ReplaceAmt,'FISC',@matchFlag,
			                                      @matchRedeemFlag,@matchRedeemAmt,@matchRedeemPoint,@monetaryFlag,@bu,@qtrlyAffiliate,@acqBankNbr,
											      @isop41TermNbr,@isoP42Merchant,@acctNbr,@product,@currency,@cardProduct,@npFlag,
											      @absAmt OUTPUT,@rebateRate,@updBonusPoint OUTPUT,@isoP58T21TxResp OUTPUT,@isoP58T21PointRedemption OUTPUT,@isoP58T21SignPoint OUTPUT,
											      @isoP58T21PointBalance OUTPUT,@tC5RebateAmount OUTPUT, @isoP58T21PaidCAmt OUTPUT,@isoP58T21LoyaltyInd   OUTPUT,
											      @rspFlag OUTPUT,@isoP39Rsp OUTPUT,@rspInner OUTPUT,@reason OUTPUT
				  IF @rspFlag IN ('D','P','R') GOTO AuthRsp
			END

			SELECT @todayRtnPmt=CASE WHEN CARD_LIMIT_DTE_3 >= @todayYYYYMMDD THEN CARD_LIMIT_AMT_3 ELSE 0 END,								--20240717
				   @todayRtnUpdDte=CASE WHEN CARD_LIMIT_DTE_3 >= @todayYYYYMMDD AND @todayRtnPmt > 0 THEN 'C' ELSE @todayRtnUpdDte END 	    --20240717
		      FROM dbo.CARD_LIMIT WITH(NOLOCK)																								--20240717
		     WHERE bu=@bu AND ACCT_NBR=@acctNbr AND PRODUCT=@product AND CURRENCY=@currency AND CARD_NBR=@isoP2CardNbr 						--20240717

			SET @isoP39Rsp  = '00'
			SET @rspFlag    = 'A'
			GOTO AuthRsp
		END

	    --取得特店資料(身份驗証)
		SELECT @merType=mertype, @merStatus=merStatus,@merDecAmt=decAmt,@merRefAmt=refAmt,@expirDteCheck=expirDtePass,@openFlagCheck=openFlagPass,
			   @merVipFlag=vipFlag,@zeroRsp=zeroRsp,@idCheck=idCheck,@idCheckRsp=idCheckRsp,@merOverLimitRate=overLimitRate,@merFallbackRsp=fallbackRsp
		  FROM dbo.FnGetMerInfo(@acqBankNbr,@isoP42Merchant)

		--虛擬卡設控檢核
		--SELECT @ctlCodeCheckSw=authcheck,@respForAuthRsn=resp FROM dbo.FnGetAuthCheckSw(@bu,'CTLCODE_SW',@qtrlyAffiliate)
		--IF ( @ctlCodeCheckSw = 'Y')
		--BEGIN
		--	SELECT @rspFlag=rsp,@reason=reason FROM dbo.FnVfyTokenCtlCode(@bu,@tokenParm,'HCE',@tokenCtlCode)
		--	IF @rspFlag IN ('D','P','R')
		--	BEGIN
		--		SELECT @isoP39Rsp=outRsp,@rspInner=InnerRsp FROM [dbo].[FnGetRspCode](@rspFlag,@respForAuthRsn)
		--		GOTO AuthRsp
		--	END
		--END

		EXEC [dbo].[Cola_CheckCardStatus]  @tranlogKey,@bu,@qtrlyAffiliate,@class,@cardNbr,@cardProduct,@ctlcode,@cardCtl,@isoP18MccCode,@isoP22Em_2,
									@expirDteCheck,@openFlagCheck,@stmtBal,@payInMini,@delq,
		                            @isoP35ExpirDte,@openFlag,@expirDte,@prevOpen,@expirDteLast,@absamt,@opResp,
									@ctlSpaceRsp,@ctlCodeRsp,@ctlDollarRsp,@delqrep,
									@rspFlag OUTPUT,@isoP39Rsp OUTPUT,@rspInner OUTPUT,@reason OUTPUT

		IF @rspFlag IN ('A','D','P','R') GOTO AuthRsp

		--Installment Check
		IF (@businessFlag ='IN')
		BEGIN
			  EXEC [dbo].[Cola_InstallmentProcess] @tranlogkey,@isoMessageType,@isoP3ProcessCde,@isoS95ReplaceAmt,'FISC',@matchFlag,
												   @matchinStFee,@matchInstTxInd,@matchinStdownPay,@matchinStPayment,@monetaryFlag,
												   @bu,@qtrlyAffiliate,@acqBankNbr,@isoP42Merchant,@acctNbr,@cardProduct,@absAmt OUTPUT,
												   @instRate,@isoS112InstPeriod,@tC5InstTxInd OUTPUT,@isoS112InstTxResp OUTPUT,@isoS112DownPayment OUTPUT,
												   @isoS112InstPayment OUTPUT,@isoS112InstFee OUTPUT,
												   @rspFlag OUTPUT,@isoP39Rsp OUTPUT,@rspInner OUTPUT,@reason OUTPUT

			  IF @rspFlag IN ('D','P','R') GOTO AuthRsp
		END

		--Redeem Check
		IF (@businessFlag ='BN')
		BEGIN
			  EXEC [dbo].[Cola_RedeemProcess] @tranlogkey,@isoMessageType,@isoP3ProcessCde,@isoS95ReplaceAmt,'FISC',@matchFlag,
			                                      @matchRedeemFlag,@matchRedeemAmt,@matchRedeemPoint,@monetaryFlag,@bu,@qtrlyAffiliate,@acqBankNbr,
											      @isop41TermNbr,@isoP42Merchant,@acctNbr,@product,@currency,@cardProduct,@npFlag,
											      @absAmt OUTPUT,@rebateRate, @updBonusPoint OUTPUT,@isoP58T21TxResp OUTPUT,
												  @isoP58T21PointRedemption OUTPUT,@isoP58T21SignPoint OUTPUT,
											      @isoP58T21PointBalance OUTPUT,@tC5RebateAmount OUTPUT, @isoP58T21PaidCAmt OUTPUT,@isoP58T21LoyaltyInd   OUTPUT,
											      @rspFlag OUTPUT,@isoP39Rsp OUTPUT,@rspInner OUTPUT,@reason OUTPUT

		      IF @rspFlag IN ('D','P','R') GOTO AuthRsp
		END

		--CustomizeCheck
		SET @eci= CASE WHEN @qtrlyAffiliate ='M' THEN @isoP48T42
					   WHEN @qtrlyAffiliate <> 'M' AND  @isoP48T42='0' THEN '7'
					   WHEN @qtrlyAffiliate <> 'M' AND  @isoP48T42='1' THEN '6'
					   WHEN @qtrlyAffiliate <> 'M' AND  @isoP48T42='2' THEN '5' END
	    SELECT @ecFlag=ecflag,@ecSafe=safeFalg FROM [dbo].[FnGetEcFlag](@bu,@qtrlyAffiliate,@isoP22Em_2,@isoP25CondCode,@eci)
		EXEC [dbo].[Cola_CustomizeCheck] @tranlogkey,@bu,@qtrlyAffiliate,@acctNbr,@cardNbr,@absAmt,@mccAttribe,@emGroup,@txCash,@txLocation,
										 @eci,@eciCheckValue,@eciCheckAmt,@eciCheckRsp,@ecRej2ChgPhone,'Y', --TOKEN PAY FLAG
										 @todayPmt OUTPUT,@todayRtnPmt OUTPUT,@vipFlag OUTPUT,@passLimitFlag OUTPUT,@cashFlag OUTPUT,@skipEcTimes OUTPUT,@skipMerchLImit OUTPUT,
										 @skipFallback OUTPUT, @skipFrFallback OUTPUT,@skipFrequency OUTPUT,@skipPayDayCtrl OUTPUT,@skipFrGroup OUTPUT,
										 @skipEsvcLimit OUTPUT,@skipExMCC OUTPUT,@mobilePay OUTPUT,@ecUnSecurity OUTPUT,@frEcUnSecurity OUTPUT,   --20251119
										 @rspFlag OUTPUT,@isoP39Rsp OUTPUT,@rspInner OUTPUT,@reason OUTPUT

		IF @rspFlag IN ('D','P','R') GOTO AuthRsp

		--Token Risk Check
		EXEC [dbo].[Cola_CheckHceRisk] @tranlogKey,@bu,@tokenParm,@qtrlyAffiliate,@tokenCtlCode,@isoP58T32AcVeryRslt,@isoP58T32FraudChkRslt,
	 									 @rspFlag OUTPUT,@isoP39Rsp OUTPUT,@rspInner OUTPUT,@reason OUTPUT
		IF @rspFlag IN ('A','D','P','R') GOTO AuthRsp

		--統整VIP註記判斷
	    SET @vipLimitFlag = CASE WHEN @vipFlag ='Y' THEN 'V'
		                         WHEN @mervipFlag ='Y' THEN 'M'
								 WHEN @productVipFlag ='Y' THEN 'T'
								 WHEN @passLimitFlag ='Y' THEN 'P'
								 WHEN [dbo].[FnVfyMccVipCond](@bu,@businessCrdFlag,@npFlag,@isoP18MccCode,@txLocation) ='Y' THEN 'G'
								 ELSE @vipLimitFlag END

		SELECT  @cardCreditLimit=a.CREDIT_LIMIT,
				@cardCreditAvail=a.CREDIT_AVAIL,
				@cardCshLimit=a.CASH_LIMIT,
				@cardCshAvail=a.CASH_AVAIL,
				@cardTempLimit=a.TEMP_LIMIT,
                @cardPermLimit=a.PERM_LIMIT,
				@todayRtnPmt=CASE WHEN a.CARD_LIMIT_DTE_3 >= @todayYYYYMMDD THEN a.CARD_LIMIT_AMT_3 ELSE 0 END,
				@todayRtnUpdDte=CASE WHEN a.CARD_LIMIT_DTE_3 >= @todayYYYYMMDD AND @todayRtnPmt > 0 THEN 'C' ELSE @todayRtnUpdDte END,
				@cardTempLimitFlag=CASE WHEN CONVERT(varchar(08),a.TEMP_LIMIT_START_DTE,112) <= @todayYYYYMMDD AND CONVERT(varchar(08),a.TEMP_LIMIT_END_DTE,112) >= @todayYYYYMMDD THEN 'Y' ELSE 'N' END,
				@acctCreditLimit=b.CREDIT_LIMIT,
				@acctCreditAvail=b.CREDIT_AVAIL,
				@acctTempLimit=b.TEMP_LIMIT,
				@acctCshLimit=b.CASH_LIMIT,
				@acctCshAvail=b.CASH_AVAIL,
				@acctPermLimit=b.PERM_LIMIT,
				@acctTempLimtFlag = CASE WHEN CONVERT(VARCHAR(08),b.TEMP_LIMIT_START_DTE,112) <= @todayYYYYMMDD AND CONVERT(VARCHAR(08),b.TEMP_LIMIT_END_DTE,112) >= @todayYYYYMMDD THEN 'Y' ELSE 'N' END
		  FROM  dbo.CARD_LIMIT a INNER JOIN dbo.CUST_LIMIT b ON a.BU=b.BU AND a.ACCT_NBR=b.CUST_NBR
		 WHERE  a.bu=@bu AND a.ACCT_NBR=@acctNbr AND a.PRODUCT=@product AND a.CURRENCY=@currency AND a.CARD_NBR=@isoP2CardNbr

		--溢繳 ＝ 卡人一般可用額度 - 卡人額度，若為負數則為0
		IF @acctCreditAvail - @acctCreditLimit > 0 SET @OverPmt = @acctCreditAvail - @acctCreditLimit
		ELSE SET @OverPmt = 0
		--預現比例(產品或卡片需求控管)
		SET @caRate= CASE WHEN @caRate =0 OR @cashFlag ='Y' THEN 0 ELSE @caRate END
		--可超額比例
		SET @cardOverLimitRate=isnull(@overLimitRate,1)+isnull(@merOverLimitRate,0)
		--取得帳戶信用額度(含可超額比例)
		SET @todayPmt = CASE WHEN @npFlag = 'S' THEN 0 ELSE @todayPmt END		--20231204
		SET @acctOverLimit = ROUND (@acctCreditLimit * @cardOverLimitRate,0) + (@todayPmt+@todayRtnPmt)
		----取得卡片信用額度(含可超額比例)
		SET @cardOverLimit = ROUND (@cardCreditLimit * @cardOverLimitRate,0) + (@todayPmt+@todayRtnPmt)
		--取得帳戶預現額度(不含可超額比例)
		SET @acctCshOverLim = ROUND(@acctPermLimit * @caRate,0)
		--取得卡片預現額度(不含可超額比例)
		SET @cardCshOverLim = ROUND( @cardPermLimit * @caRate ,0)
		--客戶已使用額度
		SET @acctExpendAmt = @acctCreditLimit -@acctCreditAvail
		--卡片已使用額度
		SET @cardExpendAmt = @cardCreditLimit -@cardCreditAvail
		--客戶已使用額度(預現)
		SET @acctCshExpendAmt = @acctCshLimit - @acctCshAvail
		--卡片已使用額度(預現)
		SET @cardCshExpendAmt = @cardCshLimit - @cardCshAvail

		--CreditLimit
		EXEC [dbo].[Cola_CheckCreditLimit] @tranlogkey,@bu,@qtrlyAffiliate,@product,@currency,@cardProduct,@acctNbr,@cardNbr,@txCash,@acctPermLimit,@acctOverLimit,@acctExpendAmt,
										   @cardOverLimit,@cardExpendAmt,@acctCshOverLim,@acctCshExpendAmt,@cardCshOverLim,@cardCshExpendAmt,@cardOverLimitRate,@OverPmt,@npFlag,
										   @absAmt,@txLocation,@vipLimitFlag,@cashClass,@classLimitParm,@productLimitParm,@businessFlag,@isoP18MccCode,@vdflag,
										   @rspFlag OUTPUT,@isoP39Rsp OUTPUT,@rspInner OUTPUT,@reason OUTPUT

	    IF @rspFlag IN ('A','D','P','R') GOTO AuthRsp

		IF @vdFLag ='Y' GOTO HighRisk

		--交易頻控管
		EXEC [dbo].[Cola_CheckTxFrequency] @tranlogKey,@bu,@cardNbr,@qtrlyAffiliate,@absAmt,@frequencyParm,@frGpParm,@frGpParmLimit,@cycle,@onSiteFlag,@netWorkFlag,@ecSafe,@txLocation,
										   @skipEcTimes,@skipFrequency,@skipFrGroup,
									       @txFrequencyExcess OUTPUT,@txFrequencyEc OUTPUT,@txFrequencyFreign OUTPUT,@rspFlag OUTPUT,@isoP39Rsp OUTPUT,@rspInner OUTPUT,@reason OUTPUT

		IF @rspFlag IN ('D','P','R') GOTO AuthRsp

		--TxByMcc
		IF (@skipExMCC = 'N' )                            --20251119
		BEGIN									          --20251119
			EXEC [dbo].[Cola_CheckTxByMCC] @tranlogKey,@bu,@qtrlyAffiliate,@cardNbr,@isoP18MccCode,@mccParm,@exMccParm,@isoP22Em_2,@isoP43LocCountry,@absAmt,@cardOverLimit,
												@cardCshOverLim,@cardPermLimit,@mccGroup,@txLocation,@txCash,@vipLimitFlag,	--20241008 20251119
												@mccTxContrl OUTPUT,@exMccParmUpd OUTPUT,@mcc4ParmFound OUTPUT,@rspFlag OUTPUT,@isoP39Rsp OUTPUT,@rspInner OUTPUT,@reason OUTPUT

			IF @rspFlag IN ('A','D','P','R') GOTO AuthRsp
		END										          --20251119

		--CreditLimit(客製MCC略過限額限次)  20241008
		IF (@vipLimitFlag NOT IN ('P','E') AND @mccTxContrl <> 'EX' AND @vdFlag <> 'Y')
		OR (@vipLimitFlag NOT IN ('P','E') AND @vdFlag = 'Y')
		BEGIN
			EXEC [dbo].[Cola_CheckCreditLimit_Limit] @tranlogkey,@bu,@qtrlyAffiliate,@product,@currency,@cardProduct,@acctNbr,@cardNbr,@txCash,@acctPermLimit,@acctOverLimit,@acctExpendAmt,
											   @cardOverLimit,@cardExpendAmt,@acctCshOverLim,@acctCshExpendAmt,@cardCshOverLim,@cardCshExpendAmt,@cardOverLimitRate,@OverPmt,@npFlag,
											   @absAmt,@txLocation,@vipLimitFlag,@cashClass,@classLimitParm,@productLimitParm,@businessFlag,@isoP18MccCode,@vdflag,
											   @rspFlag OUTPUT,@isoP39Rsp OUTPUT,@rspInner OUTPUT,@reason OUTPUT

			IF @rspFlag IN ('D','P','R') GOTO AuthRsp
	    END
		--20241008

		--TempLimit by MCC
		IF (@cardTempLimitFlag = 'Y' OR @acctTempLimtFlag = 'Y')
		BEGIN
		     EXEC [dbo].[Cola_CheckTempLimit] @tranlogKey,@bu,@cardNbr,@qtrlyAffiliate,@isoP18MccCode,@txLocation,@absAmt,
											 @acctPermLimit,@acctCreditLimit,@acctCreditAvail,
											 @cardPermLimit,@cardCreditLimit,@cardCreditAvail,
											 @tempMccTxContrl OUTPUT, @mccTxTotalAmt OUTPUT,
											 @rspFlag OUTPUT,@isoP39Rsp OUTPUT,@rspInner OUTPUT,@reason OUTPUT
             IF @rspFlag IN ('D','P','R') GOTO AuthRsp
		END

		HighRisk:
		--Enviroment
	    EXEC [dbo].[Cola_CheckEnvironment]  @tranlogKey,@bu,@cardNbr,@qtrlyAffiliate,@absAmt,@isoP18MccCode,@isoP22Em_2,@acqBankNbr,@isoP41TermNbr,@isoP42Merchant,@isoP43LocCountry,
		                                    @merStatus,@skipMerchLimit,@zeroRsp,@merDecAmt,@merRefAmt,
											@merEmLimitControl OUTPUT,@highRiskParmUpd OUTPUT,@posStatus,@rspFlag OUTPUT,@isoP39Rsp OUTPUT,@rspInner OUTPUT,@reason OUTPUT

	    IF @rspFlag IN ('D','P','R') GOTO AuthRsp

		SET @isoP39Rsp  = '00'
		SET @rspFlag    = 'A'

		AuthRsp:
		SET @isoP38AuthCode=CASE WHEN DATALENGTH(@isoP38AuthCode) < 6 THEN LEFT(@isoP38AuthCode+REPLICATE(' ',6),6)
		                         ELSE @isoP38AuthCode END

		IF (@rspFlag ='R') SET @isoP39Rsp=[dbo].[FnReplaceRspCode] (@isoP39Rsp,@isoP18MccCode,@ecFlag,@qtrlyAffiliate,@txLocation)

		--檔案處理代碼取得 FIL_PROC_SW
			SELECT @respForAuthRsn=resp FROM dbo.FnGetAuthCheckSw(@bu,'FIL_PROC_SW',@qtrlyAffiliate)
			SET @updBonusForce=CASE WHEN @businessFlag='BN' AND LEFT(@isoP3ProcessCde,2) = '20' AND @rspFlag ='A'
								AND @matchFlag ='Y' AND @matchSeq IS NOT NULL AND @updBonusPoint <> 0
								AND @matchRedeemPoint = ISNULL(TRY_CONVERT(NUMERIC(11,0), ISNULL(@isoP58T21PointRedemption ,0)),0) THEN 'Y'
								WHEN @monetaryFlag IN ('Y','C') AND @rspFlag ='A' AND @updBonusPoint <> 0 THEN 'Y'
								ELSE @updBonusForce END
			IF (@updBonusForce ='Y'  AND @vdFlag <> 'Y')
			BEGIN
					SET @RebateAmount=ISNULL(TRY_CONVERT(NUMERIC(13,2), ISNULL(@tC5RebateAmount/100 ,0)),0)
					SET @rebatePoint= CASE WHEN @updBonusPoint <=0  THEN @updBonusPoint * -1 ELSE @updBonusPoint END
					IF (@updBonusPoint > 0 )
						EXEC [dbo].[usp_BonusRebate] @cardNbr,@isoP42Merchant,@updBonusPoint,@RebateAmount,@tranlogKey,@isoP38AuthCode,@acqbanknbr,@rebateSPChkrsp OUTPUT, @ErrorType OUTPUT
					ELSE
						EXEC [dbo].[usp_BonusRebateCancel] @cardNbr,@isoP42Merchant,@rebatePoint,@RebateAmount,@matchSeq,@isoP38AuthCode,@acqbanknbr,@rebateSPChkrsp OUTPUT, @ErrorType OUTPUT

					IF @rebateSPChkrsp <> 0
					BEGIN
						SET @rspFlag   = 'D'
						SET @reason    = '70'
						SELECT @isoP39Rsp=outRsp,@rspInner=InnerRsp FROM [dbo].[FnGetRspCode](@rspFlag,@respForAuthRsn)
					END
			END

			SET @cardUpdate= 'N'
			SET @limitUpdate=CASE WHEN @vdFlag ='Y' THEN 'N' ELSE @limitUpdate END
			SET @todayRtnUpdDte=CASE WHEN @txInd ='RP' AND @matchFlag='Y' AND @rspFlag ='A' THEN ISNULL(@todayRtnUpdDte,'Y') ELSE 'N' END
			EXEC [dbo].[Cola_UpdateCoreFile] @bu,@acctNbr,@product,@currency,@cardNbr,Null,@npFlag,@cardProduct,@cardholderNbr,
											 @absAmt,@rspFlag,@monetaryFlag,@txCash,@cardUpdate,@cardpinCnt,@cardAtcCnt,
											 @cardActCntPrev,@hsrcPinErr,@limitUpdate,@todayRtnUpdDte,@authCheckFlag OUTPUT
			IF @authCheckFlag <> 'N'
			BEGIN
				SET @rspFlag   = 'D'
				SET @reason    = '01'
				SELECT @isoP39Rsp=outRsp,@rspInner=InnerRsp FROM [dbo].[FnGetRspCode](@rspFlag,@respForAuthRsn)
			END

			IF (@monetaryFlag IN ('Y','C'))
			BEGIN
			    SET @tempMccTxContrl = CASE WHEN @tempMccTxContrl= 'Y' THEN 'Y'														--20250206
				                            WHEN [dbo].[FnChkTmpLimitForMccList](@bu,@cardNbr,@isoP18MccCode) ='Y' THEN 'Y'		    --20250206
											ELSE @tempMccTxContrl END 															    --20250206
				EXEC dbo.Cola_UpdateCheckFIl @tranlogKey,@bu,@acctNbr,@product,@currency,@custNbr,@cardNbr,@cardProduct,@cardholderNbr,@cycle,
						@mccgroup,@absAmt,@rspFlag,@txLocation,@txCash,@reversalFlag,@acqBankNbr,@isoP42Merchant,@isoP18MccCode,@isoP43LocCountry,@vdFlag,'Y',
						@txFrequencyEc,@txFrequencyExcess,@txFrequencyFreign,@mcc4ParmFound,@exMccParmUpd,@merEmLimitControl,'N',@highRiskParmUpd,
						@tempMccTxContrl,@mccTxTotalAmt,@monetaryFlag,@ecflag,@onSiteFlag,@authCheckFlag OUTPUT                                   --@fallbackAcctFound

				IF @authCheckFlag <> 'N'
				BEGIN
					SET @rspFlag   = 'D'
					SET @reason    = '01'
					SELECT @isoP39Rsp=outRsp,@rspInner=InnerRsp FROM [dbo].[FnGetRspCode](@rspFlag,@respForAuthRsn)
				END
			END

			SET @isoP39Rsp=CASE WHEN @rspFlag <>  'A' AND ISNULL(@isoP39Rsp,'')='' THEN '05' ELSE @isoP39Rsp END
			SET @isoP39Rsp=CASE WHEN @txInd='VF' AND @rspFlag IN ('A','N') THEN '85' ELSE @isoP39Rsp END
			SET @isoP39RspAction = @rspFlag

			INSERT dbo.COLA_TX_AUTH_TODAY
			 (BU,ACCT_NBR,PRODUCT,CURRENCY,CARD_NBR,CARD_PRODUCT,CUST_NBR,SEQ
			 ,TX_DT,TX_TIME,RECEIVE_DT,RESPONSE_DT
			 ,COUNTRY_CODE,CITY,BIN_ICA,ACQ_BANK_NBR
			 ,MER_NBR,MER_STATUS,MER_NAME,POS_NBR,POS_STATUS
			 ,MCC_CODE,MCC_GROUP
			 ,ORIG_TX_AMT,PROC_AMT,AMT,ORIG_CURRENCY,ORIG_AMT,REPLACE_AMT
			 ,TRACE_NO,REF_NO,MESS_TYPE,PROCESS_CODE,POS_COND,ENTRY_MODE
			 ,[RESP],AUTH_CODE,RESP_CODE,CHECK_CODE,RSN_CODE
			 ,MONETARY_FLAG,TX_FLAG,BUSINESS_FLAG,ADJ_FLAG,REVERSAL_FLAG
			 ,CLASS,CASH_CLASS
			 ,CODE,FALLBACK_FLAG,EDC_FUNC,EC_FLAG,UCAF_FLAG
			 ,INST_TERM,INST_FEE_IND
			 ,INST_FEE,INST_TTL_AMT
			 ,INST_DOWN_PMT
			 ,INST_TERM_PMT
			 ,CASH_REBATE_FLAG
			 ,CASH_REBATE_POINT
			 ,CASH_REBATE_AMT
			 ,CASH_REBATE_PAID
			 ,CASH_REBATE_BALANCE
			 ,CASH_REBATE_RESP,CASH_REBATE_UI_FLAG
			 ,CREDIT_LIMIT,CREDIT_AVAIL,CASH_LIMIT,CASH_AVAIL,CREDIT_LIMIT_CARD
			 ,CREDIT_AVAIL_CARD,CASH_LIMIT_CARD,CASH_AVAIL_CARD,AUTH_ID
			 ,EXPIR_DT,CARD_CTL_CODE,ACCT_CTL_CODE,AUTH_END_DT,OVER_RATE,CARD_OVER_RATE
			 ,PAR_LIMIT,CARD_PAR_LIMIT,PAR_MCC,PAR_IC,PAR_EXMCC,PAR_FREQUENCY,MESS_FORMAT,EM_GROUP,ECI_SAFE
			 ,EC_UNSAFE_UPD,MCC4PARM_UPD,EXMCCPARM_UPD,MER_EM_UPD,HIGHRISK_UPD,EXCESS_UPD,FOREIGN_UPD
			 ,PID,TOKEN_FLAG,TOKEN_STATUS,PAR_TOKEN,ESVC_CTL_CODE
			 ,MATCH_FILE,MNT_DT,MNT_USER)
			 SELECT
			 @bu,@acctNbr,@product,@currency, @isoP2CardNbr,@cardProduct,@custNbr, @tranLogKey,
			 @isoP7TxDte, @isoP7TxTime, @datetime, GETDATE(),
			 ISNULL(@isoP43LocCountry,''),ISNULL(@isoP43MerCity,''), @isoP32BinIca,@acqBankNbr,
			 @isoP42Merchant,@merStatus, @isoP43MerName,@isop41TermNbr,@posStatus,
			 @isoP18MccCode,@mccGroup,
			 @isoP4Amt,CASE WHEN @monetaryFlag ='Y' THEN @absAmt ELSE 0 END, @absAmt,@isoP49CurrencyCode,@isoP4Amt, @isoS95ReplaceAmt,
			 @isoP11TraceNo,@isoP37RefNbr, @isoMessageType, @isoP3ProcessCde,@isoP25CondCode,@isoP22Em_2,
			 @rspFlag,CASE WHEN @rspFlag ='A' THEN @isoP38AuthCode ELSE N'' END,isnull(@isoP39Rsp,''),@rspInner,@reason,
			 @monetaryFlag,@txInd,@businessFlag, @adjFlag,@reversalFlag,
			 @class,@cashClass,
			 CASE WHEN @txCash ='N' THEN '0001' ELSE '0003' END,@fallbackFlag,'',CASE WHEN @qtrlyAffiliate IN ('V','J') THEN ISNULL(@eci,'') ELSE '' END ,ISNULL(@isoP48T42,''),
			 ISNULL(@isoS112InstPeriod,0), ISNULL(@isoS112InstTxInd,''),
			 ISNULL(TRY_CONVERT(NUMERIC(13,2), ISNULL(@isoS112InstFee,'0')),0),CASE WHEN @businessFlag='IN' THEN @isoP6Amt ELSE 0 END,
			 ISNULL(TRY_CONVERT(NUMERIC(13,2),ISNULL(@isoS112DownPayment,'0')),0),
			 ISNULL(TRY_CONVERT(NUMERIC(13,2),ISNULL(@isoS112InstPayment,'0')),0),
			 CASE WHEN @businessFlag='BN' AND @updBonusPoint <> 0 THEN 'Y' ELSE 'N' END ,
			 CASE WHEN @businessFlag='BN' AND @rspFlag='A' THEN @updBonusPoint ELSE 0 END,
			 CASE WHEN @businessFlag='BN' THEN ISNULL(TRY_CONVERT(NUMERIC(13,2), ISNULL(@tC5RebateAmount/100 ,0)),0) ELSE 0 END,
			 CASE WHEN @businessFlag='BN' THEN @absamt ELSE 0 END,
			 CASE WHEN @businessFlag='BN' THEN ISNULL(TRY_CONVERT(NUMERIC(13,2), ISNULL(@isoP58T21PointBalance,0)),0) ELSE 0 END,
			 ISNULL(@isoP58T21TxResp,''),
			 CASE WHEN @businessFlag='BN' AND @monetaryFlag IN ('Y','C') AND @updBonusPoint <> 0 AND @rspFlag='A' THEN 'Y' ELSE 'N' END,
			 @acctCreditLimit,@acctCreditAvail,@acctPermLimit, @acctCshAvail,@cardCreditLimit,
			 @cardCreditAvail, @cardPermLimit, @cardCshAvail,ISNULL(@isoS127Id,''),
			 @isoP35ExpirDte, @cardCtl,@ctlcode, DATEADD(DAY, dbo.FnGetAuthLogDay(@isoP42Merchant,@logDay) ,GETDATE()) ,@overLimitRate,@overLimitRate,
			 @classLimitParm,@productLimitParm,@mccParm,@icParm,@exMccParm,@frequencyParm,N'F',ISNULL(@emGroup,''),ISNULL(@ecSafe,''),
			 @txFrequencyEc,@mcc4ParmFound,@exMccParmUpd,@merEmLimitControl,@highRiskParmUpd,@txFrequencyExcess,@txFrequencyFreign,
			 @isoP58T32Tpan,'H',@TokenStatus,@tokenParm,@TokenCtlCode,
			 @matchFlag+@matchSeq,GETDATE(), '0100FiscHce'

			--增加取消調整時的異動, 故非調整Tx @adjSeq帶入空白
			IF @matchFlag ='Y'  AND @matchSeq IS NOT NULL AND @rspFlag = 'A'
     		BEGIN
     		   EXEC dbo.Cola_MatchUpdTxAuth @bu,@acctNbr,@product,@currency,@cardNbr,@cardProduct,@matchSeq,@absAmt,@adjFlag,@reversalFlag,'',0,@authCheckFlag OUTPUT   --20240502
     		END

			--寫入SMS
			EXEC [dbo].[Cola_InsertSMS] @tranLogKey,@isoP2CardNbr


        COMMIT
      END TRY
      BEGIN CATCH

		   SET @ErrorType = '0100FiscHce Fail Row:' + CONVERT(VARCHAR(10),ERROR_LINE()) +' Err-No:' + CONVERT(VARCHAR(10),ERROR_NUMBER() ) + ' Msg:' + ERROR_MESSAGE()

    	   IF (XACT_STATE()) <> 0 ROLLBACK;

		   EXEC dbo.cola_InsertTxnError @tranLogKey,@isoP3ProcessCde,@isoP2CardNbr,@isoP11TraceNo,@isoP37RefNbr,@ErrorType,@SorF OUTPUT

           SET @isoP39Rsp = '05'
		   SET @isoP39RspAction = 'D'
		   SET @isoS112InstTxResp=CASE WHEN @businessFlag = 'IN' THEN @isoP39Rsp ELSE @isoS112InstTxResp END
		   SET @isoP58T21TxResp=CASE WHEN @businessFlag = 'BN' THEN @isoP39Rsp ELSE @isoP58T21TxResp END

      END CATCH
GO
