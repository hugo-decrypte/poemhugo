// add resource before 
BeginTrans
//
update {/*OWNERKEY*/}RESOURCE set idClub={/*ME.COL_idClub*/},name={/*ME.COL_lastname*/} where id={/*ME.COL_idResource*/};
//
update {/*OWNERKEY*/}USER set idClub={/*ME.COL_idClub*/},name={/*ME.COL_lastname*/} where idResource={/*ME.COL_idResource*/};
//
UpdateCurrent
//
CommitTrans
