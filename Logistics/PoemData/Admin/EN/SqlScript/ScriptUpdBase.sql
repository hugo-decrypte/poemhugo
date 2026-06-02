// 2024.12.23 update DB USER
BeginTrans
//
// USER
alter table [USER] add idResource VARCHAR(30) ;
//
alter table [USER] drop column IDPRIMARY ;
alter table [USER] ADD CONSTRAINT PK_USER PRIMARY KEY (id) ;
//
CommitTrans
d
// 2024.12.20 update DB
BeginTrans
//
// Agency
alter table agency drop column idClub ;
//
// Alias
alter table ALIAS drop column idClub ;
//
// CALENDAR
alter table CALENDAR drop column IDPRIMARY ;
alter table CALENDAR ADD CONSTRAINT PK_CALENDAR PRIMARY KEY (id) ;
//
// CATEGORYRESOURCE
alter table CATEGORYRESOURCE drop column IDPRIMARY ;
alter table CATEGORYRESOURCE alter column id VARCHAR(20) NOT NULL ;
alter table CATEGORYRESOURCE ADD CONSTRAINT PK_CATEGRES PRIMARY KEY (id) ;
alter table CATEGORYRESOURCE add  comments  VARCHAR(200) ;
alter table CATEGORYRESOURCE alter column [color]  VARCHAR(15) ;
alter table CATEGORYRESOURCE alter column [state]  VARCHAR(15) ;
//
// CLIENT
alter table CLIENT drop column IDPRIMARY ;
alter table CLIENT ADD CONSTRAINT PK_CLIENT PRIMARY KEY (id) ;
//
// CLUB
alter table CLUB drop column idAgency ;
alter table CLUB add  router  VARCHAR(2);
//
// CONTRACT
alter table CONTRACT drop column dayTime, weekTime, maxWeekOvertime, maxYearOvertime ;
alter table CONTRACT add  timeDay DECIMAL(4,0), timeWeek DECIMAL(4,0), reservedWeek DECIMAL(4,0), reservedYear DECIMAL(4,0), nbDayYear DECIMAL(4,0) ;
alter table CONTRACT drop column IDPRIMARY ;
alter table CONTRACT ADD CONSTRAINT PK_CONTRACT PRIMARY KEY (idResource) ;
//
// EVENT
alter table EVENT drop column  datetime, IDPRIMARY ;
alter table EVENT ADD CONSTRAINT PK_EVENT PRIMARY KEY (idResource, dateResource, hourResource) ;
//
// PRODUCT
alter table PRODUCT drop column IDPRIMARY ;
alter table PRODUCT ADD CONSTRAINT PK_PRODUCT PRIMARY KEY (id) ;
alter table PRODUCT add  [state]  VARCHAR(2);
//
// PROJECT
alter table PROJECT drop column IDPRIMARY ;
alter table PROJECT ADD CONSTRAINT PK_PROJECT PRIMARY KEY (id) ;
alter table PROJECT add  [state]  VARCHAR(2);
alter table PROJECT drop column idState, DateImport ;
//
// RESOURCE
//alter table [RESOURCE] drop column IDPRIMARY ;
//alter table [RESOURCE] ADD CONSTRAINT PK_RESOURCE PRIMARY KEY (id) ;
alter table [RESOURCE] drop column IdSub, IdSite, IdSiteGroupResource, IdCoupled ;
alter table [RESOURCE] drop column DateForbidden, Slot, NameShift  ;
// RESOURCE_ABSENCE
alter table RESOURCE_ABSENCE alter column dateStart	VARCHAR(15) NOT NULL ;
alter table RESOURCE_ABSENCE alter column hourStart	VARCHAR(15) NOT NULL ;
alter table RESOURCE_ABSENCE alter column dateEnd	VARCHAR(15) NOT NULL ;
alter table RESOURCE_ABSENCE alter column hourEnd	VARCHAR(15) NOT NULL ;
alter table RESOURCE_ABSENCE add  idTypeAbsence  VARCHAR(30);
alter table RESOURCE_ABSENCE drop column  [type]  ;
//
alter table RESOURCE_ABSENCE drop column IDPRIMARY ;
alter table RESOURCE_ABSENCE ADD CONSTRAINT PK_RES_ABS PRIMARY KEY (idResource,dateStart,hourStart) ;
//
// RESOURCE_ACTIVITY
//alter table RESOURCE_ACTIVITY drop column overTime, nbWorkDay ;
alter table RESOURCE_ACTIVITY add excessTime DECIMAL(6,0) ;
alter table RESOURCE_ACTIVITY add reservedYear	DECIMAL(6,0) ;
alter table RESOURCE_ACTIVITY add nbDay	DECIMAL(4,1) ;
alter table RESOURCE_ACTIVITY add advance	DECIMAL(8,2);
alter table RESOURCE_ACTIVITY add reimbursement	DECIMAL(8,2);
alter table RESOURCE_ACTIVITY add bonus	DECIMAL(8,2);
alter table RESOURCE_ACTIVITY add bonusYear	DECIMAL(8,2);
alter table RESOURCE_ACTIVITY add garnishment	DECIMAL(8,2);
alter table RESOURCE_ACTIVITY add deduction	DECIMAL(8,2);
//
alter table RESOURCE_ACTIVITY drop column IDPRIMARY ;
alter table RESOURCE_ACTIVITY ADD CONSTRAINT PK_RES_ACT PRIMARY KEY (idResource,dateStart) ;
//
// RESOURCE_DATE
alter table RESOURCE_DATE alter column dateResource	VARCHAR(15) NOT NULL ;
alter table RESOURCE_DATE alter column hourStart	VARCHAR(15) ;
alter table RESOURCE_DATE alter column hourEnd	VARCHAR(15);
//alter table RESOURCE_DATE drop column overTime ;
//alter table RESOURCE_DATE add overtime DECIMAL(6,0) ;
alter table RESOURCE_DATE add idProject VARCHAR(30) ;
//
alter table RESOURCE_DATE drop column IDPRIMARY ;
alter table RESOURCE_DATE ADD CONSTRAINT PK_RES_DATE PRIMARY KEY (idResource,dateResource) ;
//
// RESOURCE_TASK
alter table RESOURCE_TASK add  [state]  VARCHAR(2);
// SHIFT
alter table SHIFT drop column Slot ;
// TYPEABSENCE
CREATE TABLE TYPEABSENCE (id VARCHAR(30) NOT NULL, name VARCHAR(100), [index] DECIMAL(2,0), title VARCHAR(100), timeReduced DECIMAL(8,2), duration DECIMAL(8,2), comments VARCHAR(200), color VARCHAR(15), [state] VARCHAR(2) ) ;
ALTER TABLE TYPEABSENCE ADD CONSTRAINT PK_TYPEABSENCE PRIMARY KEY (id) ;
// TYPEPROCESS
alter table TYPEPROCESS add  [state]  VARCHAR(2);
alter table TYPEPROCESS alter column id VARCHAR(5) NOT NULL ;
ALTER TABLE TYPEPROCESS ADD CONSTRAINT PK_TYPEPROCESS PRIMARY KEY (id) ;
// TYPERESOURCE
alter table TYPERESOURCE add  [state]  VARCHAR(2);
alter table TYPERESOURCE drop column IDPRIMARY ;
ALTER TABLE TYPERESOURCE ADD CONSTRAINT PK_TYPERESOURCE PRIMARY KEY (id) ;
// TYPESHEET
alter table TYPESHEET drop column [table] ;
alter table TYPESHEET add  [state]  VARCHAR(2);
alter table TYPESHEET add  color	CHAR(15);
alter table TYPESHEET add  alignment	CHAR(30);
alter table TYPESHEET add  tableName	CHAR(30);
alter table TYPESHEET drop column idClub ;
// TYPETRIP
alter table TYPETRIP add [state]  VARCHAR(2);
alter table TYPETRIP add [index]	DECIMAL(2,0);
alter table TYPETRIP add title	VARCHAR(50);
alter table TYPETRIP add principal	VARCHAR(2);
alter table TYPETRIP add costLunch	DECIMAL(8,2);
alter table TYPETRIP add costNight	DECIMAL(8,2);
alter table TYPETRIP drop column priceLunch,priceNight ;
//dd
CommitTrans
dd
BeginTrans
alter table resource ADD CONSTRAINT PK_RESOURCE PRIMARY KEY (id) ;
alter table agency add  codeTax VARCHAR(30), codeBusiness VARCHAR(30), codeActivity Varchar(30) ;
alter table agency drop column Taxcode, budinessCode, activityCode ;
CommitTrans
