// add resource before 
BeginTrans
//
insert into {/*OWNERKEY*/}RESOURCE (idClub, id, name) select distinct {/*ME.COL_idClub*/}, {/*ME.COL_idResource*/}, {/*ME.COL_lastname*/} from {/*OWNERKEY*/}ALIAS where not exists (select 1 from {/*OWNERKEY*/}RESOURCE where ID={/*ME.COL_idResource*/} ) ;
//
InsertCurrent
//
CommitTrans
