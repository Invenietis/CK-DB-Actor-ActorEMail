--[beginscript]

-- This is an enormous breaking change... Except that every known use of this package
-- de facto consider the EMail to be unique (ie. an EMail cannot be shared among users).
-- This version sets this to be the default.

if  COL_LENGTH('CKCore.tSystem', 'ActorEMailUnique') is null or (select ActorEMailUnique from CKCore.tSystem ) = 1
begin
    alter table CK.tActorEMail add constraint UK_CK_tActorEMail_EMail unique nonclustered (EMail);
end
--[endscript]
