--[beginscript]

create table CK.tActorEMail
(
	ActorId int not null,
	EMail nvarchar( 255 ) collate Latin1_General_100_CI_AS not null,
	IsPrimary bit not null,
	ValTime datetime2(2) not null,
	constraint PK_CK_ActorEMail primary key (ActorId,EMail),
	constraint FK_CK_ActorEMail_ActorId foreign key (ActorId) references CK.tActor(ActorId)
);

-- This CK.DB.Actor.ActorEMail package has been designed so that the same EMail address MAY
-- be associated to different actors: by deleting this unique constraint, it is possible to support
-- such scenarii where a mail can be shared by different actors.
-- Here, we restrict this: by default a mail is bound to one and only one user.
if  COL_LENGTH('CKCore.tSystem', 'ActorEMailUnique') is null or (select ActorEMailUnique from CKCore.tSystem ) = 1
begin
    alter table CK.tActorEMail add constraint UK_CK_tActorEMail_EMail unique nonclustered (EMail);
end
--[endscript]
