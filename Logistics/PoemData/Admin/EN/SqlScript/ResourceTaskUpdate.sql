//
// add resource before 
BeginTrans
//
//update {/*OWNERKEY*/}RESOURCE_TASK set selected='0' where idProject<>{/*ME.COL_idProject*/} or idProcessJob<>{/*ME.COL_idProcessJob*/} or idProcess<>{/*ME.COL_idProcess*/} or idResource<>{/*ME.COL_idResource*/} ;
update {/*OWNERKEY*/}RESOURCE_TASK set selected='0' ;
update {/*OWNERKEY*/}RESOURCE_TASK set selected='1' where idProject={/*ME.COL_idProject*/} and idProcessJob={/*ME.COL_idProcessJob*/} and idProcess={/*ME.COL_idProcess*/} and idResource={/*ME.COL_idResource*/} ;
//
CommitTrans
